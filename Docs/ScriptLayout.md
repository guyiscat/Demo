# 两关公共参数、提示与脚本分类

2026-11-18，主工程Project6.4，Unity 6000.4.12f1。

## 参数与提示入口

两关运行时读取 `Assets/Resources/GameSystems.asset`，引用原来的 `Assets/Settings/Movement.asset`。公共跟随镜头完整采用第二关配置：近景大小18，其余平滑、死区、前瞻等值一致。松墙下滑3.5/平滑0.28秒、目标帧率120、复活等待0.2秒、默认显示路线也集中在GameSystems。两关都使用最后实际接触的复活点，回头接触小编号也可更新。

角色动作仍是2×2身体、移动速度10、短跳4、长跳8、冲刺距离8/速度25，原Movement.asset数值不变。方向箭头也共用，边距0.35、长度0.8；这两个参数已从Forest.asset迁入GameSystems。森林的传送、滑索、水车和开场展示参数仍在 `Assets/Settings/Forest.asset`。

公共配置在开始Play时应用；原场景保留的旧序列化镜头数值不再是运行参数入口。两关地图大小、背景边界、出生位置与专属玩法保持各自场景。

`GameHUD.cs`统一1440×900设计布局、等比居中缩放、中文基本操作、冲刺就绪/已用、复活点/时间/死亡、底部快捷键和帮助/暂停/通关面板。`GameUIStyles.cs`统一第二关原有深绿面板、白字、中文字体和按钮样式，启动页系统设置/加载提示也使用它。海边/森林HUD只补充关卡名称和专属提示。主题图片与地图贴图未改变。

## 运行时脚本

|位置|脚本与职责|
|---|---|
|Assets/Scripts/ 根目录|两关共享的SeasideMotor、SeasideSettings、SeasideInputLatch、SeasideCamera、CameraMotion、BackgroundCameraFrame、SeasideSession、SeasideCell、CheckpointVisual、RouteDisplay、DashAfterimage、SeasideLog|
|同上|GameSystemSettings、GameHUD、GameUIStyles、PlayerAimIndicator，以及共用残像/镜头/跳跃回归检查|
|同上|StartGameMenu、StartGameButton、StartGameMenuSmoke：整体游戏启动系统，仍保留在根目录|
|Assets/Scripts/第一关/|SeasideHUD、SeasideArtAlignment、SeasideArtRoot、SeasideArtPreviewLaunch、SeasideMapRoot、ExcelMapData|
|同上|CameraPreview、TrailVisualPreview、StandaloneCalibration、PlaytestRuntimeChecks：第一关场景专用预览和检查|
|Assets/Scripts/第二关/|全部Forest脚本，包括玩家驱动、HUD、地图、参数、箭头适配、开场镜头、终点、解锁点、滑索、瀑布、水车/平台及运行检查|

部分共用系统保留历史Seaside类名。分类按实际职责，保留类名和命名空间以兼容Unity序列化。森林的ForestAimIndicator只适配森林输入，实际箭头渲染由公共PlayerAimIndicator完成。

## 编辑器脚本

|位置|脚本|
|---|---|
|Assets/Editor/ 根目录|SeasidePaths、SeasideTools、StartGameSetup、WallJumpEditorChecks、SystemContractChecks|
|Assets/Editor/第一关/|SeasideBuilder、SeasideTests、PlaytestRegression、PlaytestFixBuild、SeasideArtSetup、SeasideHierarchyRefactor、SeasidePart1Backgrounds、SeasideSceneGeometry、SeasideFeelUpgrade、CameraBackgroundBoundsChecks|
|Assets/Editor/第二关/|ForestBuilder、ForestEditorChecks、ForestMapRevision、ForestRevisionSetup、ForestTraversalRevision|

39个文件移动均保留原.meta；原有63个C#脚本GUID逐一核对不变。没有更改程序集或重新生成/保存场景。编辑器菜单和executeMethod依赖类型名，原调用名仍有效。逐项迁移表见本轮日志目录的 `script-moves.json`。

## 运行与回滚

第一关运行根目录 `statgame.cmd`（Play64/SeasideWhitebox.exe，启动页进入海边），第二关运行 `forest.cmd`。没有增加两关间的跳转。旧手动输出build/FirstScene_Seaside保留原样。

备份：`Docs/Backups/20261118-shared-systems-script-folders/`。回滚时按迁移表移回本次脚本及原.meta、恢复本次修改文件，移除本轮新增的六个系统C#及.meta、GameSystems.asset及.meta、四个分类文件夹.meta。不要整仓重置或覆盖用户地图。验证与限制见 [工作日志](Logs/Work/20261118-shared-systems-script-folders/work-log.md)。
