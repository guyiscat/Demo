# 森林传送滞空

日期：2026-11-09（Asia/Shanghai）。用户本轮明确要求增加角色传送滞空能力。

## 原因与实现

原ForestPlayer发射信标后继续执行普通运动，角色仍按原竖直速度上升或下落。SeasideMotor的重力是手动计算，单改Rigidbody2D.gravityScale不能实现滞空。

- ForestPlayer在发射时使用已有BeginTraversal清理跳跃、冲刺、贴墙和延迟动作状态，保留角色自身水平速度，去除已有水车水平带动；退出滑索/水车乘坐。
- 信标飞行未结束时调用森林专用TickBeaconHover：保留SeasideMotor原横向加速度与碰撞探测，仅传入横向/松墙输入，抵消每步竖直速度。角色保持动态刚体，实体碰撞仍有效，没有位置锁定或关闭模拟。
- 空格、左键、W在短暂信标飞行窗口内不执行、不缓存；第二次右键仍立即传送。发射结束当前冲刺但不额外消耗冲刺次数。信标速度45、距离10均未改，无遮挡窗口约0.22秒；不新增固定滞空时长。
- 手动、射程或撞地形落点仍走原WarpForTraversal清理状态；无安全落点、CancelSpecial、死亡、复活或组件禁用也清理滞空状态。暂停时保持信标与角色状态，恢复后继续剩余飞行。
- HUD显示“传送滞空”，H帮助与森林玩法文档同步。
- 不修改SeasideMotor/Movement.asset，不增加/修改场景或地图、参数资产，不重新生成白盒。

## 文件及备份

受影响源码：Project6.4/Assets/Scripts/ForestPlayer.cs、ForestHUD.cs、ForestRuntimeChecks.cs。文档：Docs/ForestWhitebox.md、CHANGELOG.md及本日志。

修改前备份：Docs/Backups/20261109-173259-forest-teleport-hover/，包含三个源码、两份文档、原森林试玩目录及双ZIP。隔离验证工程在该备份的Validation/Project6.4，复用上轮缓存后叠加当前主工程Assets/Packages/ProjectSettings/Tools，不关闭、保存或重载用户Unity编辑器。

protected-before.json记录所有未授权变更的Assets/Packages/ProjectSettings及maps原始哈希，交付前复核。仓库原有未提交修改保留，无提交或推送。

## 验证

使用隔离工程Tools/BuildAndTest.ps1 -ForestWhitebox，日志已同步到Docs/Logs/Build/20261109-173608-596。editorExit=0、forestExit=0、result=PASS，Windows构建成功且独立EXE实际运行通过，并非仅编辑器验证。

- ForestEditorChecks 1752 PASS；SeasideTests 40 PASS；PlaytestRegression 203 PASS；ForestRuntimeChecks 173 PASS，全部0 FAIL。无C# error CS/warning CS。
- 新增25项真实Physics2D检查：上升/下落立即滞空，保留水平初速与A/D，连续固定步高度不漂移，跳跃/冲刺输入不消耗冲刺且取消后不延迟起跳，结束正在执行的竖直冲刺，贴墙碰撞，滑索发射即脱离且W不抢回、不会刷传送次数，水车支持脱离，手动/范围/地形三种传送结束恢复重力，取消、死亡、复活、禁用再启用恢复，暂停/恢复高度保持。
- protected-after.json：292个受保护文件全部哈希不变，包含原ForestWhitebox.unity及.meta、SeasideMotor.cs、Movement.asset、Forest.asset、海边场景、全地图与ProjectSettings/Packages。source-validation.json确认三个修改源码与实际构建副本完全相同。
- 原Unity编辑器47724保持打开，没有关闭、主动保存或重载。真实场景仍使用当前已保存布局，本轮不写场景。

## 同步交付

已同步验证后的Play64/Forest、Docs最新报告和原始构建日志。报告覆盖前另行备份到本次Backups/Docs目录。forest.cmd继续启动新ForestWhitebox.exe。按Tools/PackageRelease.ps1 -ForestWhitebox更新Windows试玩与工程ZIP，旧ZIP在工作前备份及Packaging历史中保留；CRC、关键源码/场景/文档/程序集哈希核对记录见本目录packaging-verification.json。未提交、推送或发布。

未进行人工全路线通关。回滚时仅恢复备份中的三个森林源码、两份文档及森林试玩/双ZIP，不重置整个工程或地图。
