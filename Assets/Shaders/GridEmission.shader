Shader "Custom/GridEmission"
{
    Properties
    {
        _GridColor ("Grid Line Color", Color) = (0.18, 0.18, 0.18, 1.0)
        _FloorColor ("Floor Color", Color) = (0.76, 0.76, 0.76, 1.0)
        _GridSize ("Grid Density (lines per unit)", Float) = 1.0
        _LineWidth ("Line Width (fraction of cell)", Float) = 0.04
        _EmissionIntensity ("Emission Intensity", Float) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "GridEmissionPass"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 positionOS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _FloorColor;
                float  _GridSize;
                float  _LineWidth;
                float  _EmissionIntensity;
                float  _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionOS  = IN.positionOS.xyz;
                return OUT;
            }

            float gridLine(float2 pos, float size, float width)
            {
                float2 cell = frac(pos * size);
                float2 dist = min(cell, 1.0 - cell);
                return saturate(step(dist.x, width * 0.5) + step(dist.y, width * 0.5));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Object-space normal for triplanar blend — correct for rotated meshes
                float3 normalOS = TransformWorldToObjectNormal(IN.normalWS);
                float3 absN     = abs(normalize(normalOS));
                float3 blend    = pow(absN, 4.0);
                blend /= (blend.x + blend.y + blend.z + 1e-5);

                // Scale local position to world-unit equivalents for consistent grid density
                float4x4 o2w = GetObjectToWorldMatrix();
                float3 worldScale = float3(
                    length(float3(o2w._m00, o2w._m10, o2w._m20)),
                    length(float3(o2w._m01, o2w._m11, o2w._m21)),
                    length(float3(o2w._m02, o2w._m12, o2w._m22)));
                float3 posLS = IN.positionOS * worldScale;

                float gXZ = gridLine(posLS.xz, _GridSize, _LineWidth); // floor / ceiling
                float gXY = gridLine(posLS.xy, _GridSize, _LineWidth); // front / back wall
                float gYZ = gridLine(posLS.yz, _GridSize, _LineWidth); // side wall

                float onLine = saturate(gXZ * blend.y + gXY * blend.z + gYZ * blend.x);
                half3 baseRGB = lerp(_FloorColor.rgb, _GridColor.rgb, onLine);

                // PBR-style lighting (world-space position for view/light)
                float3 posWS     = IN.positionWS;
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDir   = normalize(GetWorldSpaceViewDir(posWS));
                Light  mainLight = GetMainLight();

                float  NdotL  = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = SampleSH(normalWS);
                float3 diffuse = mainLight.color * NdotL;

                float3 halfDir = normalize(mainLight.direction + viewDir);
                float  NdotH   = saturate(dot(normalWS, halfDir));
                float  specPow = exp2(_Smoothness * 10.0 + 1.0);
                float3 spec    = mainLight.color * pow(NdotH, specPow) * _Smoothness * 0.5;

                half3 lit = baseRGB * (ambient + diffuse) + spec;
                return half4(lit, 1.0);
            }
            ENDHLSL
        }
    }
}
