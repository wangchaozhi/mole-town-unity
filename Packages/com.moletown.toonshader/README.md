# MoleTown Toon Shader

URP 17（Unity 6）三渲二着色器 `MoleTown/Toon`，附平滑法线烘焙工具。

## 安装

把整个 `com.moletown.toonshader` 目录拷到目标项目的 `Packages/` 下（作为嵌入式包），或在 Package Manager 里选 *Add package from disk* 指向 `package.json`。

## 使用

- 材质选择着色器 **MoleTown/Toon**；代码里可以用 `MoleTown.Toon.ToonMaterial.Create(color, outlineWidth, smoothNormals)` 创建。
- **深度边缘光**（默认开启）需要在不透明物体绘制时读到深度图：
  - URP Asset 勾选 *Depth Texture*
  - Universal Renderer 的 *Depth Texture Mode* 设为 **Force Prepass**
  - 不满足时关掉材质上的 *Screen Space Depth Rim*，会退回菲涅尔边缘光
- **硬边模型描边断开**：选中模型，菜单 **Tools → Toon → Bake Smooth Normals**，再在材质上勾选 *Use Smooth Normal*；代码里可用 `SmoothNormalUtility.CreateSmoothedCopy(mesh)`。烘焙会覆盖切线，使用法线贴图的材质不要共用这份网格。

## 主要参数

| 分组 | 参数 | 说明 |
| --- | --- | --- |
| Cel Shading | Shadow Tint / Threshold / Smooth | 暗部颜色（乘在固有色上）、明暗分界位置与软硬 |
| | Cast Shadow Strength | 投影的强度，投影也会被压成硬边 |
| | Use Ramp Map | 用横向渐变贴图代替两色分界 |
| Specular | Specular Color / Size | 硬边高光，颜色支持 HDR |
| Rim Light | Depth Rim Width / Threshold | 深度边缘光宽度（1080p 下的像素）和深度差阈值 |
| | Rim In Shadow | 暗部保留多少边缘光 |
| Outline | Width / Full Width Distance | 描边宽度；超过该距离后描边随透视变细 |
| | Use Smooth Normal | 用 tangent 里的平滑法线外扩 |

## Pass

`ToonForward`、`Outline`（SRPDefaultUnlit）、`ShadowCaster`、`DepthOnly`、`DepthNormals`，全部共用同一个 `UnityPerMaterial` CBUFFER，兼容 SRP Batcher；附加光源同时支持 Forward 与 Forward+。
