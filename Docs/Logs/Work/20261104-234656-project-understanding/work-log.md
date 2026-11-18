# 项目理解与静态核对

- 日期：2026-11-04（Asia/Shanghai）。
- 用户需求：理解现有 2D 横版闯关游戏，玩家操作角色到达终点。
- 范围：阅读工程文档、核心脚本、动作配置和保存的场景；不修改玩法或地图。
- 工程定位：根据 Demo/AGENTS.md 和 README.md，当前唯一主工程是 Demo/Project6.4，Unity 6000.4.12f1。game1 为迁移来源，不修改；同级 leihuoGame 不作为此次主工程。

## 流程与玩法

1. EditorBuildSettings.asset 当前启用 StartGame、TheFirstScene_Seaside 两个场景；StartGameMenu.gameScenePath 指向后者。启动页提供开始、音量/全屏设置、退出；选项保存在 PlayerPrefs。
2. SeasideMotor 使用 Rigidbody2D + BoxCollider2D；Update 采样旧 Input Manager 输入，由 SeasideInputLatch 缓存边沿，FixedUpdate/Tick 计算速度与地面/墙面探测。碰撞地形查询使用 Layer 6。
3. Player 引用 Movement.asset（GUID 5032a02bcdef9934dbac1a99073e9f81）。保存参数：角色 2×2 格，移动速度 10，短跳 4 格、长跳 8 格、冲刺 8 格/速度 25；平台宽限 0.14 秒、离墙宽限 0.10 秒、跳跃缓存 0.12 秒。没有自由二段跳。
4. A/D 移动，空格可变高度跳跃或蹬墙跳，鼠标左键朝指针冲刺。落地或贴墙悬停恢复冲刺；朝可攀附墙按方向键悬停，松开后缓慢下滑，可再次按向墙悬停。冲刺碰撞地形立即终止。
5. SeasideCell 的 Spike/Checkpoint/Finish 触发器调用 SeasideSession。检查点只接受更大的编号，优先使用 RespawnAnchor 的世界位置。尖刺或低于 y=-6 触发死亡；死亡计数后约 0.2 秒复活并重置角色与残像、对齐镜头。终点调用 Complete，设置 Won 并冻结角色；代码没有集齐检查点的胜利前置条件。
6. R 复活、Ctrl+R 重开、胜利后 R 重开、Esc 暂停、Tab 全图、B 路线、H 帮助。HUD 显示计时、死亡数、检查点与动作状态。

## 场景与职责

- 当前关卡 YAML 直接序列化 514 个 GameObject；SeasideCell 389 个，其中 Ground 184、Spike 192、Checkpoint 12、Finish 1。它们是场景组件数，不是原 Excel 格子数。
- 保存场景含 1 个角色控制器、1 个会话、1 个镜头、12 个 CheckpointVisual，终点对象为 FinishTrigger；出生配置为 (5, 3.2)，镜头地图配置为 (155, 73)。
- SeasideSession 管计时、暂停、死亡、复活、进度与胜利；SeasideCell 连接地图触发器；CheckpointVisual 切换当前检查点的显示。
- SeasideCamera/CameraMotion 负责死区与平滑跟随、全图缩放；镜头启用背景边界约束，运行时查找 SeasideArtRoot 下的 bg3。DashAfterimage 采用对象池显示残像。
- 玩法碰撞与美术分开，当前保存场景保留后续手工修改，应以实际 Collider、触发器和复活锚点为准。Excel/JSON 是来源资料，不重新生成覆盖场景。
- 已实现海边；矿洞、森林按现有 README 仍只有地图原稿，未确认有对应可玩场景。

## 验证与限制

- 实际执行：git status --short；读取 AGENTS/README/工程导览/场景层级/启动页说明；读取版本、Build Scene List、Movement.asset 与 GUID、输入/运动/会话/触发/HUD/镜头/残像/菜单脚本；使用 Python 标准库解析两个场景的 YAML 块、脚本 GUID、关键引用与组件数量。
- 已核对启动场景目标与构建列表一致，Player 的动作设置 GUID 与 Movement.asset.meta 一致，保存关卡有 12 个检查点与 1 个终点。
- 仓库已有动作、墙跳、镜头、残像与启动页检查，统一入口是 Project6.4/Tools/BuildAndTest.ps1；本次未执行这些测试，未启动 Unity/成品、未构建或打包、未做人工全路线通关，未将历史通过报告当作本次结果。
- 美术与碰撞贴合、全关可达性及操作体验仍需实际运行验证。文档中旧场景入口以当前 README 与 EditorBuildSettings 为准。

## 改动与回滚

- 仅新增本记录并在 Docs/CHANGELOG.md 顶部追加此次条目；工程脚本、场景、参数和构建均未修改。
- 开始时 Docs/CHANGELOG.md 已有用户工作区改动，且已有未跟踪的 20261102-143449-storage-cleanup 日志目录，均保留。
- 本次修改前的 CHANGELOG 完整副本在同时间戳 Docs/Backups/<本次目录名>/CHANGELOG.md。如需撤销本次工作，优先只删除此次新增条目与工作日志，保留后续改动。
