# 3D Snake Game — Unity URP 设计文档（已实现版）

> 最后更新：2026-04-01，反映 Final Visual Verification 通过后的完整实现状态。

---

## 1. 概述

基于参考截图在 Unity URP 渲染管线下实现的 3D 贪吃蛇游戏，采用程序化网格地面、龙形蛇头、食物蠕动消化动画、按键加速等视觉特性。

---

## 2. 渲染管线

- **管线**: Universal Render Pipeline (URP) 14.0.9
- **资产**: `Assets/Settings/URP-Asset.asset`
- **渲染器**: `Assets/Settings/URP-Renderer.asset`
- **后处理**: URP Volume + Bloom（Threshold 0.9，Intensity 1.2，Scatter 0.5）

---

## 3. 场景设置

### 相机
- **组件**: `CameraFollow.cs`（挂载在 Main Camera）
- **模式**: 跟随蛇头，deadzone=3格，SmoothDamp（smoothTime=0.5s）
- **Offset**: `(0, 14, -14)`（正南方俯视，保证 WASD 方向与视觉一致）
- **目标**: 蛇头世界坐标（`snakeGame.HeadWorldPos`）
- **后处理**: `UniversalAdditionalCameraData` m_RenderPostProcessing=1

### 光照
- **Directional Light**: 强度 1.5，颜色 (1, 0.957, 0.839)，软阴影，Position (5, 10, -5)

### 地图朝向
- **地图整体旋转 Y=60°**（Quaternion 0, 0.5, 0, 0.866）实现角落视角视觉效果
- 相机保持正南方 offset 不变，WASD 控制逻辑完全不受影响

---

## 4. 场景物件

### GridFloor（网格地面）
- **几何体**: Unity Plane（10×10 局部单位），Scale `(4.5, 1, 4.5)` → 世界 45×45 单位
- **旋转**: Y=60°（与墙体一起构成角落视角）
- **材质**: `GridFloorMaterial.mat`（Custom/GridEmission shader）
  - `_FloorColor`: (0.76, 0.76, 0.76)（浅灰，略带光泽）
  - `_GridColor`: (0.18, 0.18, 0.18)
  - `_GridSize`: 1.0（1线/世界单位）
  - `_LineWidth`: 0.04
  - `_Smoothness`: 0.25

### 墙体（WallN / WallS / WallE / WallW）
- **几何体**: Cube，旋转 Y=60°，厚度 0.1，高度 1.5
- **长度**: 33 单位（覆盖 30 格宽度 + 余量）
- **材质**: `WallMaterial.mat`（Custom/GridEmission shader）
  - `_FloorColor`: (0.918, 0.918, 0.918)（#EAEAEA）
  - `_GridColor`: (0.72, 0.72, 0.72)
  - `_Smoothness`: 0.15

**墙体世界坐标（旋转后）**：

| 墙 | Position | Scale |
|----|----------|-------|
| WallN | (14.289, 0.75, 8.25) | (33, 1.5, 0.1) |
| WallS | (-14.289, 0.75, -8.25) | (33, 1.5, 0.1) |
| WallE | (8.25, 0.75, -14.289) | (0.1, 1.5, 33) |
| WallW | (-8.25, 0.75, 14.289) | (0.1, 1.5, 33) |

### GridEmission Shader（`Assets/Shaders/GridEmission.shader`）
- **关键特性**: Object-space triplanar 网格（非 world-space）
  - 使用 `normalOS`（TransformWorldToObjectNormal）确定混合权重
  - 使用 `positionOS × worldScale` 采样，网格密度与世界单位一致
  - 旋转物体后网格线自动跟随面的局部轴，消除"垂直边框"问题
- **光照**: Blinn-Phong（ambient + diffuse + specular）

---

## 5. 蛇

### SnakeHeadPrefab
- **几何体**: Cube，Scale `(0.9, 0.9, 0.9)`（= body 0.75 × 1.2）
- **材质**: `SnakeHeadMaterial.mat`（URP Lit，橙色）
- **组件**: `DragonHead.cs`（Awake 程序化生成子物件）
- **加速动画**: 运行时由 `SnakeGame.ApplyHeadAnimation()` 覆盖 localScale

### DragonHead.cs（程序化龙头组件）
生成以下子 Cube（均无 BoxCollider，instance material）：

| 部件 | 局部位置 | 尺寸 | 颜色 |
|------|----------|------|------|
| Snout | (0, -0.18, 0.55) | (0.62, 0.40, 0.52) | 橙色 (1, 0.52, 0.12) |
| EyeL | (-0.27, 0.20, 0.48) | (0.19, 0.19, 0.07) | 白色 |
| EyeR | (0.27, 0.20, 0.48) | (0.19, 0.19, 0.07) | 白色 |
| PupilL | (-0.27, 0.20, 0.54) | (0.09, 0.11, 0.04) | 黑色 |
| PupilR | (0.27, 0.20, 0.54) | (0.09, 0.11, 0.04) | 黑色 |
| Mouth | (0, -0.22, 0.82) | (0.45, 0.07, 0.07) | 红色 (0.9, 0.1, 0.08) |

### SnakeBodyPrefab
- **几何体**: Cube，Scale `(0.75, 0.75, 0.75)`
- **尾节**: Scale `(0.525, 0.525, 0.525)`（= 0.75 × 0.70，小 30%）
- **材质**: `SnakeBodyMaterial.mat`（URP Lit Transparent）
  - 颜色: 蓝紫色 (0.4, 0.2, 1.0)，**alpha=0.7**
  - `_Surface: 1`，`_SrcBlend: 5`，`_DstBlend: 10`，Queue: 3000

---

## 6. 食物

### FoodPrefab
- **几何体**: Cube，Scale `(0.7, 0.7, 0.7)`
- **材质**: `FoodMaterial.mat`（URP Lit Transparent + Emission）
  - 颜色: 红色 (1, 0.12, 0.05)，**alpha=0.7**
  - EmissionColor HDR: (3, 0.06, 0.02)（红色自发光）
  - `_Surface: 1`，Queue: 3000
- **组件**: `FoodFloat.cs`
  - 上下浮动：amplitude=0.15，speed=2.2
  - Awake 添加红色点光源：intensity=3，range=5，color=(1, 0.10, 0.04)

---

## 7. 游戏逻辑（SnakeGame.cs）

### 网格系统
- `gridHalfSize = 15`（30×30 网格，对应 45×45 世界单位地面）
- `ToWorld(g)` → `Vector3(g.x, 0.5f, g.y)`
- 蛇头朝向：`Quaternion.Euler(0, Atan2(dir.x, dir.y)*Rad2Deg, 0)`

### 控制
- **方向切换**: `Input.GetKeyDown`（WASD / 方向键，防止反向）
- **持续加速**: `Input.GetKey` 检测按住状态

### 加速系统
```
_holdTime 累积（max 1s），松开时 3× 速度衰减
effectiveInterval = Lerp(moveInterval, moveInterval×0.35, accelT²)
```
平方曲线 = 先缓后急，有明显的"加速感"。

### 头部加速动画（ApplyHeadAnimation）
```
stretch = 1 + accelT×0.40 + stepFlash×0.18   // Z轴拉伸
squash  = 1 - accelT×0.14 - stepFlash×0.09   // XY轴压缩
head.localScale = (0.9×squash, 0.9×squash, 0.9×stretch)
```
每步进触发 `_stepFlash=1`，以 10×/s 衰减，产生视觉脉冲。

### 食物蠕动消化（GrowthToken）
吃到食物时：
1. 食物立刻消失，新尾节以 `scale=0` 隐藏
2. 创建 `GrowthToken { seg=1, tailIdx=N }`
3. 每步进 token 向后移动一格：当前格放大 1.25× + 食物色，前一格恢复正常
4. 抵达尾部时显示新尾节（TailScale），token 销毁

### 初始状态
- 初始长度：5节（蛇头 + 4节身体，向 -X 延伸）
- 移动间隔：0.3s（每吃一食物 -0.005s，最低 0.1s）
- 计分：每食 +10

---

## 8. 材质一览

| 材质 | Shader | 透明 | 主色 | 用途 |
|------|--------|------|------|------|
| GridFloorMaterial | Custom/GridEmission | 否 | 浅灰 | 地面 |
| WallMaterial | Custom/GridEmission | 否 | #EAEAEA | 墙体 |
| SnakeHeadMaterial | URP Lit | 否 | 橙色 | 蛇头 |
| SnakeBodyMaterial | URP Lit | 是 α=0.7 | 蓝紫 | 蛇身 |
| FoodMaterial | URP Lit + Emission | 是 α=0.7 | 红色 HDR | 食物 |

---

## 9. 文件结构（实际）

```
Assets/
├── Prefabs/
│   ├── SnakeHeadPrefab.prefab   # Cube 0.9³ + DragonHead
│   ├── SnakeBodyPrefab.prefab   # Cube 0.75³
│   └── FoodPrefab.prefab        # Cube 0.7³ + FoodFloat
├── Materials/
│   ├── GridFloorMaterial.mat
│   ├── WallMaterial.mat
│   ├── SnakeHeadMaterial.mat
│   ├── SnakeBodyMaterial.mat
│   └── FoodMaterial.mat
├── Shaders/
│   └── GridEmission.shader      # Object-space triplanar
├── Scripts/
│   ├── SnakeGame.cs             # 主逻辑 + 加速 + 蠕动
│   ├── CameraFollow.cs          # Deadzone + SmoothDamp
│   ├── DragonHead.cs            # 程序化龙头
│   └── FoodFloat.cs             # 浮动 + 点光源
├── Settings/
│   ├── URP-Asset.asset
│   └── URP-Renderer.asset
└── Scenes/
    └── SnakeScene.unity
```

---

## 10. 验收标准（已通过）

- [x] URP 网格地面：浅灰底色 + 深灰网格线，object-space 对齐
- [x] 墙体：#EAEAEA 奶白色，网格纹理随墙面旋转正确显示
- [x] 地图旋转 60°，呈角落俯视视角，WASD 方向与视觉一致
- [x] 地图尺寸 30×30 格（gridHalfSize=15）
- [x] 龙头：双眼 + 瞳孔 + 口鼻 + 红色嘴巴，无龙须
- [x] 蛇头尺寸 = 蛇身 × 1.2（0.9 vs 0.75）
- [x] 蛇尾小 30%（scale 0.525）
- [x] 初始 5 节
- [x] 食物蠕动消化动画（GrowthToken 逐节传递）
- [x] 食物红色自发光 + 半透明 + 点光源
- [x] 蛇身半透明（alpha 0.7）
- [x] 相机 SmoothDamp 跟随 + deadzone 防抖
- [x] 按住方向键加速（effectiveInterval 最低 moveInterval×0.35）
- [x] 头部加速挤压拉伸动画
- [x] UI 分数显示右上角
- [x] 碰壁/自撞 GAME OVER
