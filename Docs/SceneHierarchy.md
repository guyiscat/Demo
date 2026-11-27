# SeasideArtPreview 场景层级与命名

2026-11-19当前入口：第一关为Assets/Scenes/TheFirstScene_Seaside.unity，沿用下述层级规范。旧SeasideArtPreview及恢复副本已移出工程；原Seaside01仅以Editor/Tests/Fixtures/SeasideMovementBaseline.unity保留为动作回归夹具。旧创建/迁移菜单已移除，编辑当前场景使用Seaside > Map editing palette。本文件下文是历史实施记录，旧打开和打包路径不作为当前操作入口，最新参见[README](../README.md)与[当前交接与验证](Handoff.md)。

2026-10-29开始，2026-10-30完成验证。适用工程：Demo/Project6.4，Unity6000.4.12f1。本次原地整理SeasideArtPreview.unity，保留场景GUID和用户保存的Part1Art/bg3，没有另建平行关卡。

```text
Seaside
├── Gameplay
│   ├── Map                         [SeasideMapRoot]
│   │   ├── Terrain
│   │   │   └── Terrain_EP48_GC48   等地形模块
│   │   ├── Hazards
│   │   │   └── Spike_格子地址
│   │   ├── Checkpoints
│   │   │   └── Checkpoint_01_格子地址
│   │   │       └── RespawnAnchor
│   │   ├── RouteGuides            [RouteDisplay]
│   │   │   └── Route_格子地址
│   │   ├── FinishTrigger
│   │   └── Markers
│   │       ├── Marker_StartText
│   │       └── Marker_FinishText
│   └── Player
├── Systems
│   ├── GameSession
│   └── MainCamera                  Tag仍为MainCamera
└── Art
    └── Part1                       [SeasideArtRoot]
        └── Backgrounds
            ├── Background_Far      原bg3，图片/材质/位置/尺寸不变
            ├── Background_Distant 原bg2
            └── Background_Midground 原bg1
```

2026-10-30追加bg0～bg2后，Part1下另有Foreground/Terrain_Artwork（bg0）。这里的Foreground表示美术中的主要地形层，Sorting Order仍低于白盒玩法对象，不表示遮住角色或新增碰撞。

|源图片|场景对象|分组|Order in Layer|
|---|---|---|---|
|bg3.png|Background_Far|Backgrounds|-100|
|bg2.png|Background_Distant|Backgrounds|-90|
|bg1.png|Background_Midground|Backgrounds|-80|
|bg0.png|Terrain_Artwork|Foreground|-70|

新增图层按Project6.4/Assets/Art/Seasidepart1/part1.json.ps排版，并以已摆好的bg3为坐标与缩放参照，保留现有bg3的调整。原PNG保留名称，避免破坏布局JSON引用；仅规范场景对象名。bg0仍是合并的地形美术，当前没有改变白盒碰撞或声称全图已经贴合。可通过Seaside > Art > Add remaining Part1 backgrounds执行一次性接入；已有相同素材时会停止，避免覆盖用户后续编辑或重复添加。

## 使用规则

- 空分组和主要对象采用PascalCase；角色、镜头、游戏会话归入各自职责组。
- 地图对象采用类别加标识，例如Terrain_Excel起止地址、Spike_Excel地址、Checkpoint_编号_Excel地址。原Excel地址仍在SeasideCell.excelAddress中，不因改名丢失。
- 检查点的RespawnAnchor保留在自己的检查点下；RouteDisplay与它控制的路线子对象留在同一分支。
- Start/Finish文本是标记，放在Map/Markers；实际终点触发器单独保留。标记名称改变，TextMesh显示文字不变。
- Player不放在Map下，因为镜头/残像测试需要暂时停用整张地图，不能连角色一起停用。
- 美术不放在碰撞地图下，后续图片、光效和装饰可继续在Art/Part1下按用途分组。新增中景等素材时可建立Midground或Decorations，不提前堆放无用空分组。
- Seaside/Gameplay/Systems/Art等新分组为位置0、旋转0、缩放1；原对象重新挂载时保留世界变换。不要通过缩放Gameplay或Map整体来调整美术。

## 脚本不再依赖固定名称

CameraPreview和TrailVisualPreview通过SeasideMapRoot.FindInScene定位地图。重构后的场景使用Map上的SeasideMapRoot标识；未重构的Seaside01根据SeasideCell的共同祖先定位，避免为修一个场景而修改另一个场景。

SeasideArtRoot标识分层美术根；原SeasideArtAlignment方式仍可被预览校验识别。本次用户已删除旧合成图对象，因此没有重新创建它，也没有修改bg3的导入参数或外观。

检查几何一致性时比较单元身份、实际世界位置/碰撞轮廓、层、活动状态、攀附属性及复活点，忽略显示名称与父级路径。名称变化不会再被当作地图变形；坐标和碰撞变化仍会失败。


## 打开、打包与回滚

打开Project6.4/Assets/Scenes/SeasideArtPreview.unity继续编辑。Build Profiles中Seaside01仍为正常启动场景，预览可通过ArtPreview64.cmd进入。

Project6.4/Tools/BuildAndTest.ps1使用6.4并输出Play64，修复了复制工程后脚本误指向Project/和6.6的问题。旧版Play仅在本地归档，不作为构建入口。构建不再强制设置动作预设或保存未编辑的原关卡。


## bg3 镜头边界（2026-10-30）

SeasideCamera 默认在运行时自动定位 SeasideArtRoot 下的 bg3，以其世界矩形限制完整视野；Tab 全图补黑边。没有 bg3 的旧关卡沿用原逻辑。本次不保存或改动现有场景，详细设置及验证见 [CameraBounds.md](CameraBounds.md)。
