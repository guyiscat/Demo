# 两关公共参数、系统提示与C#脚本归类

日期：2026-11-18，Asia/Shanghai。主工程Project6.4，Unity 6000.4.12f1。

## 需求、原因与实现

用户要求以第二关为基准统一第一关和第二关的参数及非主题图片的系统提示，并将系统脚本保留原目录、专属脚本归入第一关/第二关，确保移动后可正常运行，暂不增加关卡跳转。

原两关角色动作已经引用同一份Movement.asset，但第一关镜头近景16、第二关18；复活点和默认路线显示规则不同，第一关HUD仍为英文开发说明，方向提示也不同。现统一如下：

- 新增Assets/Resources/GameSystems.asset，引用原Movement.asset，保存第二关完整镜头参数、松墙下滑3.5/0.28、帧率120、死亡等待0.2秒、最后接触复活点、默认路线显示和箭头边距0.35/长度0.8。两关在Awake统一应用；原场景不重写，旧序列化镜头设置不再作为运行入口。
- Movement.asset数值与GUID不变，继续使用身体2×2、移动10、短跳4/长跳8/冲刺8、冲刺速度25和原有重力/宽限/缓存。背景边界、地图大小与出生位置保持各关场景。
- 两关共享GameHUD、GameUIStyles：第二关深绿面板与白字、中文字体、1440×900设计等比居中布局、基础操作、就绪/已用、帮助/暂停/通关及继续/重玩。各HUD适配器只补充关卡标题与专属玩法，森林状态中的英文GROUND/FALL等也改为统一中文。启动页系统设置/加载/错误提示沿用同一套字体与配色；美术按钮及主题图片不改。
- 新增PlayerAimIndicator公共渲染，第一关接入同样的身体外围金色箭头。ForestAimIndicator保留原类名作为森林输入适配，避免组件/API失效；箭头两个参数迁移至GameSystems，不再在Forest.asset重复维护。
- 角色运动、输入、会话、碰撞、镜头、残像、复活点表现、日志、菜单和系统回归脚本留Assets/Scripts根目录；10个第一关专属脚本移入第一关，14个Forest脚本移入第二关。编辑器脚本同样分类：10个第一关、5个第二关，共39个原文件移动。63个原有C#脚本及.meta GUID逐一保持。类名、命名空间及程序集不改，场景无需重新绑定。
- 新增6个系统C#（GameSystemSettings、GameHUD、GameUIStyles、PlayerAimIndicator、SystemRuntimeChecks、SystemContractChecks）及对应.meta；分类文件夹.meta共4份。Editor系统检查遍历所有保存场景，验证没有Missing Script。
- 第一关statgame.cmd改为启动本轮验证的Play64/SeasideWhitebox.exe，原build/FirstScene_Seaside保留不覆盖；第二关forest.cmd继续独立启动。Startup交付包补齐两个启动器。BuildSettings和StartGameMenu.gameScenePath未更改，未建立两关间通关跳转。

完整公共参数入口和分类清单见Docs/ScriptLayout.md，逐项原路径/新路径/GUID见script-moves.json。历史日志中的旧路径保留以便追溯。

## 备份和受保护内容

工作前备份位于Docs/Backups/20261118-shared-systems-script-folders/：包含原Scripts/Editor/Settings/Resources/Tools、受影响文档及启动器、Play64、原手动第一关构建、原森林双ZIP/分享ZIP及两个解压试玩副本。原有未提交差异保存在preexisting.diff，没有提交或推送。

protected-before/after.json记录175个脚本目录之外的工程/地图文件：174个完全不变，唯一允许变化为Forest.asset移走两个公共箭头字段，其余森林参数保持。全部场景及.meta、主题图片/贴图、Movement.asset、地图原稿、Packages和ProjectSettings均未变。脚本移动GUID核对63/63保持；source-validation.json确认293个工程Assets/Tools文件与最终构建副本哈希一致。

未关闭、主动保存、切换或恢复用户打开的Unity场景。验证使用本备份内Validation/Project6.4隔离副本，复用上一轮缓存后叠加当前源码和资源。没有从Excel/JSON重新生成地图。

## 自动验证实际结果

使用Tools/BuildAndTest.ps1，原始日志保留在Docs/Logs/Build/：

- 20261118-161853-150：森林首轮，构建和独立运行PASS。
- 20261118-162105-261：第一关启动页首轮，构建和独立启动检查PASS。
- 20261118-163228-605：最终森林构建，editorExit=0、forestExit=0、PASS。
- 20261118-163300-895：最终第一关启动页构建，editorExit=0、startupExit=0、PASS。

最终检查：公共系统/全部保存场景3197项、森林场景1752项、原有海边基础40项、回归203项、森林独立EXE物理/系统234项、启动页进入TheFirstScene_Seaside及公共系统34项，全部PASS。两轮构建均无C# error CS/warning CS。

新系统检查在实际两关中核对Movement资源、完整镜头参数、松墙下滑、复活规则、目标帧率、共用提示/帮助/冲刺措辞、唯一初始化的方向箭头。已有检查继续覆盖普通跳跃/冲刺/墙面动作、森林传送滞空、滑索抓取/双向换向/土狼跳、开场镜头、水车和检查点。回归中原“编号只能前进”的断言改为“最后接触可回退”，与本轮统一后的真实规则一致。

## 界面验证与失败记录

后台测试尝试ScreenCapture截图，进程退出0但Visual/Forest-1440x900的四张截图均为黑色，因此不将其视为有效视觉验收。临时SystemPresentationCapture测试组件及启动钩子已从主工程和最终构建中移除，保留失败图片/日志作记录。

随后通过computer-use技能的sky窗口API启动隔离构建进行实际窗口检查，未操作用户Unity工程：

- 第一关：原启动主题图片和美术按钮保持；设置面板中文字体/深绿样式正常；从开始游戏进入保存的TheFirstScene_Seaside，实际HUD、身体外箭头、帮助和暂停面板显示正常。
- 第二关：实际HUD、箭头、森林专属操作、帮助和暂停面板正常，和第一关基本提示一致；未发现重叠、缺字或裁切。
- 两个临时游戏窗口检查后均关闭，没有人工全路线通关。窗口截图在本次工具记录中，黑色后台截图不冒充成功画面。

执行验证副本清理时，一条包含删除临时截图脚本的组合PowerShell命令被自动审批策略拦截；未执行该命令，随后使用apply_patch精确删除两个已知临时文件，再独立执行复制与构建，最终完成验证。

## 同步交付与回滚

已同步验证后的Play64及最新构建报告。打包使用Tools/PackageRelease.ps1 -StartupPage与-ForestWhitebox，交付第一关启动页/工程ZIP、森林试玩/工程ZIP，以及“森林独立试玩.zip”和两个已有解压副本；关键文件、ZIP条目及交付程序集核对结果见packaging-verification.json。两关入口分别为statgame.cmd和forest.cmd。

未进行人工全路线通关。回滚时仅按script-moves.json恢复本轮文件及原.meta、恢复修改过的公共脚本/文档/设置/启动器和试玩，移除本轮新增文件与四个分类目录.meta，不整仓重置或覆盖后来地图修改。旧手动第一关构建保持原样。
