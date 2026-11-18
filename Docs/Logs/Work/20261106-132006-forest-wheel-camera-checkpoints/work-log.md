# 森林水车、镜头与检查点修订

日期：2026-11-06（Asia/Shanghai）。

## 用户需求与边界

- 在原 ForestWhitebox.unity 修改，不创建新场景，不重生成地图。
- 水车只顺时针；玩家可自由走跳，重新落到平台仍能被带动；水平摩擦有限，需要主动跟上。
- 瀑布改为限制近景相机视野，Tab 远景显示全图；保留整个角色完全穿过后才切换近景区域的判断。
- 自由放置若干复活点，沿用海边机制。保留海边已有奔跑、跳跃、冲刺与原动作参数。

## 备份与用户工作区

工作前完整备份修改涉及的森林脚本、Editor检查/生成器、原森林场景及.meta、Forest.asset、Docs说明/CHANGELOG、原Play64/Forest成品和两个旧ZIP，位置为同时间戳 Docs/Backups/20261106-132006-forest-wheel-camera-checkpoints/。本次新报告替换前的根报告另放reports-before。

检测到用户正在用Unity打开主工程ForestWhitebox。未关闭编辑器、未切换其场景、未自动保存其未保存编辑；询问过是否存在未保存改动，未收到回复。继续使用已保存的原场景在独立副本工作，回写前逐字确认磁盘原场景自备份以来未变化。未保存的编辑器状态不作为本次输入；需要用户在退出Play后重新打开同一场景加载磁盘改动，避免从旧内存版本覆盖。

用户将ForestWaterwheel中的Active临时改成true，原样保存到备份；本次替换为可序列化的forceRotation（Inspector“调试：强制转动”），同场景保留启用，维持直接查看转动的选择。该开关不伪造两侧解锁，终点仍需UnlockCount==2。正常玩法取消此勾选，恢复双点解锁后才启动。

## 实现

- ForestWaterwheel：移除Board导致的反向，Direction固定-1，角速度取绝对值后负向推进，任何登车侧和负速度输入均无法逆转。每个ForestWheelPlatform计算下一位置及实际计划速度。
- ForestPlayer：移除RidingWheel锁定分支、Warp/MovePosition式自动乘坐、自动吊篮交接和临时忽略踏板碰撞。普通输入始终执行原SeasideMotor.Tick。每个物理步从脚下实际碰撞寻找支撑；平台竖直法向带动，水平以有限附加速度/加速度传递；空格、左右、冲刺均走原动作。起跳后不吸回，按相对平台竖直速度识别再次落地，包含仍在上升的平台。没有将角色设为平台子对象。
- ForestSettings：平台水平带动速度上限默认3单位/秒，摩擦加速度默认20单位/秒²；保留转速12度/秒，两个参数可Inspector调整，原Movement.asset未写入。
- ForestWaterfall：禁用旧FarSideOcclusion，不改地图对象可见性；在SeasideCamera正常跟随后，按实际投影边缘平移相机，不允许近景越过瀑布的另一侧边缘。完全穿过2格宽角色碰撞盒才换区域；进入瀑布期间保留来时侧。overview=true时不约束，远景完整显示左右地图。比例改变及远景切回近景的缩放过渡也检查实际视锥。
- 原ForestWhitebox.unity添加Checkpoints分组与5个SeasideCell.Checkpoint黄色旗帜、各自RespawnAnchor：(5.15,50.05)、(64,68.05)、(202,5.05)、(247.15,37.05)、(202,66.05)。沿左侧挑战到右侧挑战编号1..5；共享SeasideSession只接受更大编号，旧点不回退，死亡或R回当前点，Ctrl+R清空；保留水车解锁到完全重开。
- ForestGoal：真实双解锁后才能获胜，调试强制转动不再直接打开终点。
- ForestHUD：更新复活点编号、预览转动状态、完整全图和有限摩擦走跳说明。
- ForestLevel截图入口增加近景瀑布左右边界验证图；ForestEditorChecks和ForestRuntimeChecks覆盖新行为。
- ForestRevisionSetup是明确执行的一次性保存场景补丁，仅批处理读取现有ForestWhitebox，不调用NewScene或CreateScene；本次只在隔离副本执行，再同步同一文件。旧吊篮/锚点和遮罩对象保留作历史兼容，但旧自动交接已无运行代码，遮罩和篮子显示已禁用。

## 场景保护

scene-preservation.json确认：原ForestWhitebox.unity.meta/GUID一致，原已有对象块没有缺失，原Transform位置/旋转/缩放/父级及实体Collider/Rigidbody块未变化（Map分组仅新增Checkpoints子引用）。仅更新水车forceRotation、瀑布cameraRig、禁用旧遮罩，并增加37个检查点相关序列化块。角色在编辑器里为试水车调整的预览位置也保留，没有重置为出生点。原session.startPoint仍为(3,5.2)。

protected-before/after.json对26项原有海边场景、共享设置/地图、SeasideMotor/Session/Camera和EditorBuildSettings核对哈希一致。未接入海边进度，未改主启动菜单或原海边成品。

## 实际验证与失败记录

原编辑器仍运行，所有构建使用Backups/.../Validation/Project6.4隔离副本，入口仍为Tools/BuildAndTest.ps1 -ForestWhitebox。未使用旧报告冒充本次结果。

1. scene-patch-attempt-1/2：检查点(5,50.05)在左墙边界仅留0.01格，Box2D形状边缘仍将相邻墙纳入重叠；补充SyncTransforms诊断后确认是接触边距。将左中段x改为5.15，右中段x改为247.15，保留足够边距。第3次场景补丁通过，未重新生成任何地形。
2. build-attempt-1：两条旧测试假设失败。一条假设玩家编辑器预览位置等于出生点，但用户已将角色挪动用于预览；改为调用真实Respawn验证session的出生配置，不改用户场景角色位置。另一条左右反转测试只等待0.25秒，包含从正10到负10的正常加速阶段，净距离未超过断言阈值；等待增至0.35秒后通过，无改奔跑加速度。
3. build-attempt-2：新增转动上升平台实际起跳/落回测试并修正新接触判断为相对平台竖直速度（不能只看世界vy>0）；全部通过，且水车空闲146步(1.46s)滑离，对照主动输入944步保持并抬升37.42单位。
4. build-attempt-3：进一步验证从顺时针轮顶以普通跳跃/方向输入进入真实终点，不使用旧吊篮或强制位置交接。最终全通过。

最终原始报告：Docs/Logs/Build/20261106-133427-951/。隔离源目录路径保留在原始日志中，已复制同名日志到正常Build日志树，不修改历史路径。editorExit=0，forestExit=0，status PASS；Windows BuildSucceeded，0 errors；未发现C# error CS/warning CS。

本次实际执行：ForestEditorChecks 1874 PASS；SeasideTests 40 PASS；PlaytestRegression 203 PASS；ForestRuntimeChecks 93 PASS，均0 FAIL。汇总见test-summary.json。包括：原信标/滑索、顺时针/负速度保护、首次平台左右自由移动、静止和旋转平台起跳重落、有限摩擦无输入滑离和有输入延长跟随、普通跳跃到终点、右侧固定下降、预览不伪造解锁、瀑布近景双向整身跨越/实际视锥/4:3与16:9与21:9/切换缩放/完整远景、5个真实检查点逐个触发/死亡复活/安全落地/旧点不回退/完整重开。

画面：使用已通过build-attempt-2的同版本机制/场景D3D11成品，1440×900 Camera.Render+RenderTexture抓取，退出码0。已查看visual-qa中的完整全图和左右瀑布近景，远景无遮挡、近景止于瀑布正确；不包含IMGUI HUD，未将其作为HUD视觉验收。第3次仅追加测试断言，机制和场景未再改变。未进行真人从出生点完整走完两侧全部路线的验收。

## 交付与回滚

将隔离副本验证后的同一ForestWhitebox.unity同步回原路径，保留其.meta/GUID。同步更新Play64/Forest及forest.cmd对应成品；森林ZIP由Tools/PackageRelease.ps1 -ForestWhitebox更新，CRC/关键文件哈希对照详见packaging-verification.json，两个旧ZIP保留在工作前备份。说明更新在Docs/ForestWhitebox.md和CHANGELOG顶部。

未提交、推送或发布。已有11月2日/4日/5日未提交工作和日志保留。若需回滚，先保存后续自己的更改，从同时间戳Backups恢复本次涉及的森林脚本、Editor检查、ForestWhitebox原场景、Docs说明、Play64/Forest旧成品及两ZIP；仅移除本次新增ForestRevisionSetup.cs/.meta，不要重置整个仓库或删除先前森林工作。原海边场景、共享运动和地图不需要回滚。
