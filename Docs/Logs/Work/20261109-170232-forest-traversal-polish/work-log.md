# 森林存档、抓索、水车及瀑布镜头手感修订

日期：2026-11-09（Asia/Shanghai）

## 用户需求与解释范围

1. 询问“传送技能过程中持续滞空”的含义：解释为信标发射至传送完成期间暂停竖直下落、完成后恢复重力；未将询问擅自改为启用新行为。当前传送过程普通运动不变。
2. 滑索速度参数：Assets/Settings/Forest.asset 的 ziplineSpeed，默认16世界单位/秒；Inspector提供中文“滑索滑动速度”。
3. 森林复活规则改为最后接触的检查点，编号不再决定优先级。
4. 截图位置添加检查点：旧布局右侧上层通道向下方两条长滑索跳落之前的安全凸台，映射当前新版布局为(242,66.05)，6号黄色旗帜。截图是位置参考，不把界面文字当新指令。
5. 身体碰到滑索后W可抓取，稍扩大接触容差。
6. 水车踏板16→12且均匀分布。
7. 瀑布换侧不再硬切，平滑移镜；移动速度对外暴露；移动中左右无遮罩，抵达后再恢复远侧遮挡。

## 实现与参数

- SeasideSession新增 latestCheckpointWins 默认false。森林场景true，海边保持原编号递增逻辑；森林按触碰事件切换当前检查点，即6→2→1后在1号复活。activeCheckpoint避免同一个触发器重复改写。Restart清理记录，死亡/R保留最后接触点和水车解锁。编号只作HUD和对象标识，没有修改原5点编号。
- 新增Checkpoint_06_RightUpperLip：克隆既有旗帜组件与RespawnAnchor，位置(242,66.05)，编号6。实际地形检查与落地复活通过。原5个点保持原位置/组件。
- ForestSettings：ziplineSpeed原值16保持，Inspector标注“滑索滑动速度”；新增 ziplineContactPadding 默认0.25，中文“滑索身体接触容差”。保留原ziplineCatchDistance=1.6的头部范围，避免扩大身体范围时反而缩小旧抓取距离；该旧字段继续生效。Forest.asset文件本身哈希不变，新字段使用初始化默认值。
- ForestZipline：以线段与完整身体AABB（加接触容差）的相交判定接受头/脚/左右/索端接触，支持原斜索。保留原头部中心距离判定作为兼容范围。ForestPlayer仍选最近有效索，检查挂索目标点为空、维持跳离冷却、冲刺禁止抓取和原滑行机制。
- 水车实际场景保留前12个实体平台的ID/材质/碰撞/刚体参数，移除后4个，按0..330度每30度均布并更新wheel.platforms引用。固定顺时针、转速、半径、水平有限摩擦均保持。首次创建代码默认同步12块，但本次从未调用CreateScene或重生成地图。
- ForestWaterfall：在原SeasideCamera跟随后计算目标侧的受限视野，通过缓存上一帧实际显示位置与MoveTowards逐帧平移；cameraTransitionSpeed=40（世界单位/秒）公开，Inspector“瀑布换侧镜头移动速度”，更大更快。仍整身完全穿过才换侧。过渡期间IsTransitioning=true禁止黑布，目标抵达后近景受边界约束、全景启用另一侧黑布。全景本来已全图居中，没有新侧镜头目标，保留与穿越距离/速度相应的短暂双侧显示期后换遮罩。中途反穿从当前已显示位置连续折返，Tab过渡中不提前遮挡；dt=0暂停不推进。ResetSide在复活/重开显式取消过渡，避免跨图慢慢飞回复活点。
- 原共用SeasideCamera/SeasideMotor/Movement.asset均未改；仅SeasideSession增加场景选择的规则开关。
- 新ForestTraversalRevision显式batch-only补丁及截图入口，只操作已保存原ForestWhitebox.unity，不自动运行、不调用NewScene/CreateScene。ForestEditorChecks及RuntimeChecks按新需求扩展，HUD及ForestWhitebox说明同步。

## 场景与文件保护

用户要求仍在原场景修改，已遵守。修改前备份受影响Assets/Tools/ProjectSettings/文档/试玩/双ZIP至 `D:\UnityProjects\Demo\Docs\Backups\20261109-170232-forest-traversal-polish`。所有补丁、构建和渲染在该目录Validation/Project6.4进行。原工程Unity进程47724继续运行，未关闭、保存或重载用户编辑器。同步前原磁盘场景与备份哈希一致，未覆盖工作中外部保存。

scene-preservation.json：GUID保持，不新建场景；原序列化块改变27（12块平台的Transform/ForestWheelPlatform角度、wheel平台列表、session规则字段、waterfall速度等），删除后4平台28个块，新增检查点7个块。原有保留Collider/触发器块未改变，地形/荆棘/滑索布局、水车半径和转速、终点、原5点组件、相机调参、运动参数保持。224个海边资源/场景/动作源码（排除本次授权改动的SeasideSession）与Excel/BuildSettings哈希前后相同。source-validation.json确认18个森林/会话源码与实际验证副本一致。Forest.asset亦保持原哈希。

## 失败与实际验证

- scene-patch-attempt-1：初选存档点(240,65.05)身体与新版凸台碰撞，编辑器检查失败，未保存场景。改为平台上方的安全点(242,66.05)，第2次补丁通过，1752项编辑器断言通过。
- editor-playmode-attempt-1：误在失败补丁后仍跑旧保存场景，导致旧5检查点/递增规则与新断言不符；水车测试选5号平台在旧16块布局中初始角度112.5度，也不满足新12块150度的上升弧条件。失败记录保留，没有将其视为修改后的玩法缺陷或更改原运动参数。
- editor-playmode-attempt-2：修正补丁后真实12平台/6检查点通过全部145项（随后增加3条镜头验证）。无输入112步滑离，有普通方向输入828步支持并上升30.31单位，仅是本轮特定试验，不承诺所有站位时长。
- 第一次Windows BuildAndTest日志20261109-170819-467，editorExit=0、forestExit=0、PASS，新独立程序本次允许启动，148项全部通过。未调整系统安全策略。
- 为使首次创建默认同样为12块且森林默认最近接触，同步ForestBuilder后再用Tools/BuildAndTest.ps1构建最终源码，最终日志 `20261109-171004-093`，editorExit=0、forestExit=0、PASS，Windows BuildSucceeded、0 errors。场景1752 PASS、海边基础40 PASS、海边回归203 PASS、森林独立运行148 PASS，全部0 FAIL。未发现C# error CS/warning CS。两次Build原始日志均保留在Docs/Logs/Build。
- 森林148项包含：新6点逐个触发/死亡落地/复活、6号后回低编号成为最新点、死后/R返回低编号、完全重开；头脚/身体侧面/索端抓取、远距不抓取、ziplineSpeed改8时实际0.1秒滑0.8格、13条原索真实滑行；12块水车的行走/跳后重落/有限摩擦/主动追赶/普通跳上真实终点；瀑布双向过渡每步位移不超过速度×dt、到位恢复边界/全景黑布、零时间不推进、速度改10立即减慢、中途逆向/Tab保持连续、复活取消过渡。海边203项确认默认编号优先行为继续通过。
- capture.log第一次编辑器抓图失败：编辑模式motor.Body尚未Awake初始化；改用已有Rigidbody2D组件定位，第二次成功。第三次调整抓图采样在实际过渡中间并显式断言IsTransitioning，人物挪开以显示新黄旗。visual-qa-final/含1440×900 D3D11实际Camera.Render图片，退出0；已查看近景过渡中画面两侧无遮挡、抵达边界、全景过渡中全图/抵达后远侧黑布及截图对应新旗帜。图为编辑器实际渲染，不含IMGUI HUD。截图入口不保存临时位置或画面状态。第二次图片作为历史保留，最终采用第三次。

未进行真人完整两侧路线通关。自动物理机制验证不能代表关卡难度/全路线通关已验收。

## 同步交付与回滚

将验证后的同一ForestWhitebox.unity同步回原路径，保留.meta，新增编辑器补丁.meta一并保留；更新Play64/Forest、实际执行的报告和Docs说明。forest.cmd仍启动更新后森林试玩。使用Tools/PackageRelease.ps1 -ForestWhitebox更新双ZIP，CRC/场景/源码/程序集/文档一致性见packaging-verification.json，旧ZIP留存于本次备份和Packaging历史。

若回滚，仅恢复本次备份的森林相关脚本、SeasideSession、ForestBuilder/EditorChecks、原森林场景、文档、森林试玩与双ZIP，移除本次新增ForestTraversalRevision.cs及.meta；不要重置整个仓库或地图。保留此前所有未提交工作。未提交/推送/发布。
