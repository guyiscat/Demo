# Seaside 背景图与当前地图如何对齐

> 2026-10-30 交接更新：本文以下是原日期的历史方案。当前工程为 Project6.4，PNG 与布局统一在 Project6.4/Assets/Art；旧 Figures PNG 和 Project/ 已归档。当前分层场景没有旧整图 F6/F7 开关。当前入口见 [协作指南](../CONTRIBUTING.md) 和 [场景规范](SceneHierarchy.md)。


2026-10-28更新：已收到Figures/part1的分层素材及布局JSON，新的接入方案见[Seaside分层素材方案](Seaside分层素材方案.md)。下面保留2026-10-27针对单张合成图的实施说明；其中“只有一张扁平PNG”的素材条件已被新资料更新。

## 当前结论

图片包含背景、平台、墙体和装饰，已经是一张完整关卡美术合成图。它不只是一张远景。**图片没有物理碰撞，不能通过“拖入图片”自动获得可靠的可玩地图。** 当前建议沿用现有碰撞和触发器，把美术外观单独叠加。

对照源 JSON 和现有场景可见，部分美术边缘对应已有地形，部分结构已经变化。整体平移、统一缩放只能初步配准，无法让所有平台都重合。为此本次新增独立预览，不覆盖原来的正常关卡。

## 已经完成的接入

1. 将原图保存在 `Figures/Seaside.png`，并原样导入 `Project/Assets/Art/Backgrounds/Seaside.png`，两份图像文件的内容一致。
2. 在 Unity 中按 Sprite (2D and UI)、Single、Full Rect 导入；PPU=50、Max Size=8192、无 Mip Maps、Bilinear、Clamp。关闭自动生成物理形状。PPU 表示多少图片像素对应一个世界单位，参考 [Unity 官方 Sprite 导入说明](https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/textures-reference/texture-type-sprite)。本关的“一格”是一个世界单位，不能与显示器像素混为一谈。
3. 复制已保存的 `Seaside01.unity` 得到 `SeasideArtPreview.unity`，保留已有地形、尖刺、检查点、蓝线、角色和镜头设置。新增一个带 SpriteRenderer 的美术对象，其 Sorting Order=-100，排在所有玩法对象后面，不添加 Collider。
4. 通过 `SeasideArtAlignment` 单独设置图片左下角位置、世界宽度、透明度、图片与白盒显示开关。原正常场景不放入此组件。
5. 将预览加入构建，正常启动仍进白盒关卡，ArtPreview.cmd 使用 `-artPreview` 进入预览。

## 你在 Unity 中怎么调

打开预览场景，在 Hierarchy 选中 **SEASIDE ART - select to align, visual only**。Inspector 中的 Seaside Art Alignment 有以下参数：

|字段|当前初始值|作用|
|---|---|---|
|World Origin|(0, -5)|整张图左下角在地图坐标中的位置|
|World Width|155|整张图的世界宽度，按原图比例计算高度|
|Opacity|1|图片透明度，可临时调到 0.4～0.6 看重叠|
|Show Artwork|开启|图片显示开关|
|Show Whitebox|开启|只切换 Ground 的 SpriteRenderer，不改碰撞|

**这是初始配准参数，不代表全图已经对齐。** 图像 7783×4214，地图逻辑范围 155×73。按宽 155 等比显示，图片高约 **83.923**，左下角 (0,-5)，中心约 **(77.5,36.962)**，顶边约 78.923。强行压到 155×73 会使画面变形，并且仍不能修复结构差异。

初始配准以左侧起步地面为参照，其图中顶边约对应地图 y=1。先找两个距离较远的已知边缘确定缩放与偏移，再用第三个边缘检查。如果第三处仍错位，说明图片与地图的局部结构不同，应局部修图或明确改动碰撞，而不是继续靠整体拖动补偿。

图片坐标 (u,v) 从左上角量起时，转换为世界坐标：

```text
x = WorldOrigin.x + u / 7783 × WorldWidth
y = WorldOrigin.y + (4214 - v) / 7783 × WorldWidth
```

运行预览后，F6 切换地形外观，F7 切换图片，Tab 看全图；尖刺、检查点、蓝色路线仍保留，B 可以隐藏蓝线。F6 隐藏白盒只是查看美术，并不代表图片边缘已有碰撞。在编辑器中选地形对象并启用碰撞 Gizmos，可以对照真实可站立边缘。停止 Play 后修改并保存场景，才会永久保存参数。

## 接下来如何做成最终可玩美术关卡

建议以现有已经调过手感的碰撞图为基准，将图片拆为远景、地形外观、装饰几个图层。远景放后面；每个平台的外观贴合现有碰撞；角色、尖刺、检查点继续由游戏对象负责。白色桥梁、树叶和远处建筑在明确其玩法用途前，只作为装饰处理。

目前原图只提供一张扁平 PNG，无法无损拆出被遮挡的背景。若有 PSD、Krita 或其他分层原稿，用分层导出的透明 PNG 会更合适。也可以先按关卡分段导出地形，再逐段贴合白盒。

具体对照例子：现有白盒在 x≈67～74 有向上连接到 y≈38 的墙体，并从 x≈74 向右连接 y≈31～39 的大片实体地形；原图该区域主要画成低处平台、白色桥梁和开放背景。因此隐藏白盒后这里会出现明显的“隐形地面”。此类差异无法靠单一 PPU 或 Transform 修正。

若最终决定完全以图片中的地形为准，则需要在另一个关卡副本中按图片重建碰撞轮廓，并重新布置尖刺、检查点与复活点，再按 2×2 人物、短跳4/长跳8/冲刺8 的能力逐段验证。这属于关卡布局调整，本次保留原碰撞，没有擅自替换。

## 回滚与记录

直接打开 `Seaside01.unity` 或 StartGame.cmd 即为正常白盒流程。修改前完整副本在 `Docs/Backups/20261027-demo-migration-seaside-art/`。工作、兼容性处理、实际验证及限制见 `Docs/Logs/Work/20261027-demo-migration-seaside-art/work-log.md`。
