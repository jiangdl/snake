using UnityEngine;

/// Dragon head: two-cube (main head + snout), eyes, pupils, red mouth.
/// No whiskers — keep the design clean.
[DisallowMultipleComponent]
public class DragonHead : MonoBehaviour
{
    void Awake()
    {
        Box("Snout",
            new Vector3(0f, -0.18f, 0.55f),
            new Vector3(0.52f, 0.30f, 0.42f),
            new Color(1f, 0.52f, 0.12f));

        Box("EyeL",   new Vector3(-0.27f,  0.20f, 0.48f), new Vector3(0.19f, 0.19f, 0.07f), Color.white);
        Box("EyeR",   new Vector3( 0.27f,  0.20f, 0.48f), new Vector3(0.19f, 0.19f, 0.07f), Color.white);
        Box("PupilL", new Vector3(-0.27f,  0.20f, 0.54f), new Vector3(0.09f, 0.11f, 0.04f), Color.black);
        Box("PupilR", new Vector3( 0.27f,  0.20f, 0.54f), new Vector3(0.09f, 0.11f, 0.04f), Color.black);
        Box("Mouth",  new Vector3(0f, -0.22f, 0.82f),      new Vector3(0.45f, 0.07f, 0.07f), new Color(0.90f, 0.10f, 0.08f));
    }

    void Box(string id, Vector3 pos, Vector3 sz, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = id;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = sz;
        // Use the instance material (safe for all render pipelines)
        var mat = go.GetComponent<Renderer>().material;
        mat.SetColor("_BaseColor", col);
        mat.SetColor("_Color",     col);
        mat.color = col;
        Destroy(go.GetComponent<BoxCollider>());
    }
}
