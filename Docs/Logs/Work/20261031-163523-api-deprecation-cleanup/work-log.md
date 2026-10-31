# 2026-10-31：统一处理 Unity API 弃用警告

用户明确要求处理上一条提供的全部 50 条 CS0618。范围是旧对象查找、排序参数和对象标识接口，不包含 Input Manager 或渲染管线迁移。

## 备份与保护

读取 Demo 与 Project6.4 的 AGENTS.md。受影响的 20 个现有脚本在修改前完整复制至 Docs/Backups/20261031-163523-api-deprecation-cleanup/before/，CHANGELOG.md 和旧 Play64 同样保留。独立验证工程位于该备份目录下的 validation/Demo/Project6.4。

本次没有保存、重建、重新导入地图或切换主编辑器的场景，也没有修改脚本的序列化字段、图片、碰撞或玩法参数。同步前逐个比较脚本备份哈希，避免覆盖并发代码修改。已有全部场景及 meta 的前后哈希见 existing-scenes-before.json 和 completion.json 对应检查记录。这里的磁盘一致性不代表对编辑器内未保存内容作出完整性判断。

## 修改及语义核对

涉及 20 个脚本、40 处调用，对应原 50 条编译警告（每个带排序参数的查找通常同时报告方法与枚举各一条）。完整列表和 SHA256 见 installed-files.json，原调用数量见 replaced-api-counts.json。

- 27 处 FindFirstObjectByType 改用 FindAnyObjectByType。用途为查找唯一的 SeasideSession、SeasideMotor、SeasideCamera 或美术根组件。在隔离副本中逐一打开现有 5 个场景，核对每个游戏场景的活动 Session、玩家和跟随相机均唯一；启动场景没有这些玩法组件，故不依赖旧的 InstanceID 顺序。
- 10 处带 FindObjectsSortMode.None 的 FindObjectsByType 改成无排序参数重载；原本就是不排序。包含非活动对象的相机美术根查找仍保留 FindObjectsInactive.Include。检查点进度测试已有 OrderBy(checkpointId)，继续保留；其他批量遍历不依赖顺序。基础测试中的 cps[0] 仅验证任意一个检查点的复活锚点，不将数组位置当检查点编号。
- SeasideHierarchyRefactor 的 3 处 GetInstanceID 改为 GetEntityId。oldPaths 是通过 ToDictionary 推导键类型，现自动成为 EntityId；前后访问同时更新。快照键仍用返回标识的字符串，日志格式和前后匹配机制保持一致。EntityId 仅用于当前会话的身份匹配，不作为跨运行的资源 GUID。增加说明注释。
- 没有使用 pragma、关闭警告或删除功能来隐藏提示。新增启动页脚本本来已使用新接口，本次不改动它们。

## 验证

临时验证入口 ApiMigrationValidation 仅存在于隔离工程，留存源代码为 ApiMigrationValidation.cs.txt，不安装到主工程。它不运行会重命名、重存原地图的 Apply/ValidateScenes，而是调用已有玩法与镜头检查，并用临时对象验证新标识的稳定性。

隔离 Unity 6000.4.12f1 命令行调用 ApiMigrationValidation.Run，退出码 0；原始输出为 editor-api-checks.log。

- C# 编译 warning/error 数量均为 0。
- api-migration-checks.txt：22 项通过，包括场景唯一性、新查找结果、改名与重设父级后的 EntityId 字典查找和世界状态/组件快照一致性。
- physics-tests.txt：40 PASS、0 FAIL。
- playtest-regression.txt：203 PASS、0 FAIL，涵盖跳跃、冲刺、墙跳、检查点推进及复活、相机和拖影。
- camera-background-checks/camera-bounds-checks.txt：41,143 次断言通过，涵盖多种窗口比例、镜头边界和全图/跟随转换。

随后执行隔离工程 Tools/BuildAndTest.ps1 -StartupPage：

- isolated-build-runs/20261031-163650-998/status.json：PASS，editorExit=0、startupExit=0。
- Windows 构建 Succeeded，0 build errors；编译阶段没有 CS0618 或其他 C# warning。
- startup-validation/runtime-result.txt：24 项通过，包含菜单交互、音量设置、实际美术渲染和加载 TheFirstScene_Seaside 后玩家与 Session 存在。
- 场景列表沿用已配置的 StartGame、TheFirstScene_Seaside、原恢复场景，未重新配置默认入口。

确认通过后，只同步 20 个脚本及重建的 Play64 回主目录。脚本 meta 均沿用，不改场景引用。未运行旧的“艺术预览必须与白盒几何相同”断言，因为已知手工美术场景与白盒有意不同；该历史检查没有被删除或削弱。本次未执行人工全关通关或单独的 Windows 159 项旧回归/7 项校准，不将编辑器测试冒充为这些结果。

## 交付与限制

通过 Tools/PackageRelease.ps1 -StartupPage 更新 Startup Windows 与工程包；旧包由打包脚本保留在本次 Packaging 运行目录。最终包路径、大小、校验结果和主场景前后对比写入 completion.json。

入口仍为 Play64/SeasideWhitebox.exe；Unity 中从 StartGame.unity 运行即可。Input Manager 与 Built-in 渲染管线的弃用提示属于独立迁移任务，本次不改这些设置。控制台可清除先前保留的旧编译记录后查看新编译结果。

回滚时先自行保存当前编辑，再从 before 还原这 20 个脚本；若需要旧试玩，恢复 before/Play64。不以备份场景覆盖手工地图。未提交、推送或发布 GitHub。

补充：主编辑器当前的 Assembly-CSharp.dll 时间为 16:30，早于本次迁移源码；尚未在前台触发自动刷新。主 Editor.log 中的 100 次 warning CS0618 文本命中属于此前编译记录（同一警告重复输出），不能作为迁移后结果。main-editor-compiler-diagnostics.json 已明确标记 AWAITING_MAIN_EDITOR_REFRESH。验证后的 0 warning/0 error 来自隔离编译及新 Windows 构建日志。用户回到 Unity 等待编译，未触发时可使用 Assets > Refresh；无需保存或重开场景。初次使用 ReadAllText 读取运行中的 Editor.log 被文件共享锁拒绝，随后用只读 FileShare.ReadWrite 成功读取，没有修改编辑器日志。
