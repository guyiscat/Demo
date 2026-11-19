# 各关公共参数、提示与脚本分类

2026-11-19，主工程Project6.4，Unity 6000.4.12f1。

## 参数与提示入口

所有关卡运行时读取 `Assets/Resources/GameSystems.asset`，引用原来的 `Assets/Settings/Movement.asset`。公共跟随镜头完整采用森林配置：近景大小18，其余平滑、死区、前瞻等值一致。松墙下滑3.5/平滑0.28秒、目标帧率120、复活等待0.2秒、默认显示路线也集中在GameSystems。所有关卡都使用最后实际接触的复活点，回头接触小编号也可更新。

GameSystems.systemFont引用Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf，所有现有及后续关卡的GameHUD/GameUIStyles自动共用阿里巴巴普惠体。GameTypography在场景加载时统一TextMesh标签字体。原主题图片内文字保持图片原样。

角色动作仍是2×2身体、移动速度10、短跳4、长跳8、冲刺距离8/速度25，原Movement.asset数值不变。方向箭头也共用，边距0.35、长度0.8；这两个参数已从Forest.asset迁入GameSystems。森林的传送、滑索、水车和开场展示参数仍在 `Assets/Settings/Forest.asset`。

公共配置在开始Play时应用；原场景保留的旧序列化镜头数值不再是运行参数入口。两关地图大小、背景边界、出生位置与专属玩法保持各自场景。

`GameHUD.cs`统一1440×900设计布局、等比居中缩放、中文基本操作、冲刺就绪/已用、复活点/时间/死亡、底部快捷键和帮助/暂停/通关面板。`GameUIStyles.cs`统一第二关原有深绿面板、白字、中文字体和按钮样式，启动页系统设置/加载提示也使用它。海边/森林HUD只补充关卡名称和专属提示。主题图片与地图贴图未改变。

## 运行时脚本

|位置|脚本与职责|
|---|---|
|Assets/Scripts/ 根目录|两关共享的SeasideMotor、SeasideSettings、SeasideInputLatch、SeasideCamera、CameraMotion、BackgroundCameraFrame、SeasideSession、SeasideCell、CheckpointVisual、RouteDisplay、DashAfterimage、SeasideLog|
|同上|GameSystemSettings、GameHUD、GameUIStyles、GameTypography、PlayerAimIndicator、MovingSurface，以及共用残像/镜头/跳跃回归检查|
|同上|ForestPlayer、ForestSettings、ForestZipline、ForestAimIndicator、ForestMapData：森林/矿洞复用的传送、滑索、移动地面和地图合同，保留历史类型名、命名空间与GUID|
|同上|StartGameMenu、StartGameButton、StartGameMenuSmoke：整体游戏启动系统，仍保留在根目录|
|Assets/Scripts/seaside/|SeasideHUD、SeasideArtRoot、SeasideMapRoot、ExcelMapData|
|同上|CameraPreview、TrailVisualPreview、StandaloneCalibration、PlaytestRuntimeChecks：第一关场景专用预览和检查|
|Assets/Scripts/forest/|ForestLevel、ForestHUD、ForestIntroCamera、ForestGoal、ForestUnlock、ForestWaterfall、ForestWaterwheel、ForestWheelPlatform、ForestRuntimeChecks|
|Assets/Scripts/mine/|MineLevel、MineHUD、MineSettings、MineLift、MineInteraction、MineRuntimeChecks|
|Assets/Scripts/ 根目录|ThermalSettings、ThermalSurface、ThermalZipline、ClimbPoint、ThermalTraversal、ThermalLevel、ThermalHUD：雪山/灯塔共用的冷热防护、属性滑索与恢复能力的攀岩补跳；ThermalRuntimeChecks/ThermalVisualChecks为独立程序验证入口|
|Assets/Scripts/snow/|SnowLevel、SnowHUD：残章原文、两种结局选择|
|Assets/Scripts/lighthouse/|LighthouseLevel、LighthouseHUD、LighthouseCollapse：限时道路坍塌追赶、塔顶灯台交互与结局|

部分共用系统保留历史Seaside类名。分类按实际职责，保留类名和命名空间以兼容Unity序列化。森林的ForestAimIndicator只适配森林输入，实际箭头渲染由公共PlayerAimIndicator完成。

## 编辑器脚本

雪山/灯塔共用ThermalLevelBuilder、ThermalEditorChecks放在Editor根目录；首次创建与构建入口SnowBuilder、LighthouseBuilder分别位于Editor/snow、Editor/lighthouse。场景存在时构建只验证，不重新生成。两关参数与地图解释见[独立关卡说明](SnowLighthouse.md)。

|位置|脚本|
|---|---|
|Assets/Editor/ 根目录|SeasidePaths、SeasideTools、StartGameSetup、SeasideTestScenes、WallJumpEditorChecks、SystemContractChecks|
|Assets/Editor/seaside/|SeasideTests、PlaytestRegression、CameraBackgroundBoundsChecks：当前动作和镜头回归|
|Assets/Editor/Tests/Fixtures/|SeasideMovementBaseline.unity：原白盒的动作测试夹具，字节及GUID保持，不参与游戏构建|
|Assets/Editor/forest/|ForestBuilder、ForestEditorChecks、ForestMapRevision、ForestRevisionSetup、ForestTraversalRevision|
|Assets/Editor/mine/|MineBuilder、MineEditorChecks|

2026-11-19将四个第一关/第二关目录改名为seaside/forest，并将矿洞复用的五个原森林类型移回公共根目录。69个原C#的GUID保持，不改程序集、类名或命名空间；编辑器菜单和executeMethod调用名保持。目录原.meta保留，详细迁移记录见本轮日志目录script-guids.json。森林只按新旧工作簿差异更新原场景；矿洞为首次独立创建。

## 运行与回滚

五个独立入口为statgame.cmd（启动页进入海边）、forest.cmd、mine.cmd、snow.cmd、lighthouse.cmd。当前Assets/Scenes只包含StartGame、TheFirstScene_Seaside、ForestWhitebox、MineWhitebox、SnowWhitebox、LighthouseWhitebox；旧预览/恢复场景、旧第一关生成与迁移工具、旧手动输出build及重复解压试玩已移出工作目录。BuildAndTest和PackageRelease无参数时默认当前启动页版本，不再调用旧白盒构建入口。清理明细及回滚见[清理日志](Logs/Work/20261119-project-cleanup/work-log.md)。

本轮备份：Docs/Backups/20261119-forest-mine-font/，实施记录见[工作日志](Logs/Work/20261119-forest-mine-font/work-log.md)。下段为上轮历史回滚说明。

备份：`Docs/Backups/20261118-shared-systems-script-folders/`。回滚时按迁移表移回本次脚本及原.meta、恢复本次修改文件，移除本轮新增的六个系统C#及.meta、GameSystems.asset及.meta、四个分类文件夹.meta。不要整仓重置或覆盖用户地图。验证与限制见 [工作日志](Logs/Work/20261118-shared-systems-script-folders/work-log.md)。
