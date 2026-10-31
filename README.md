# 雷火游戏创作 Demo

当前唯一主工程是 **Project6.4/**，使用 **Unity 6000.4.12f1**。当前已实现海边 Seaside 的 2D 平台跳跃原型；矿洞、森林保留地图原稿，尚无对应可玩场景。

## 开始开发

1. 安装 Git、Git LFS 和 Unity 6000.4.12f1。首次使用 LFS 执行 git lfs install。
2. 克隆仓库后执行 git lfs pull，确保图片是真实文件，而不是 LFS 指针文本。见 [协作指南](CONTRIBUTING.md)。
3. Unity Hub → Add project from disk → 选择仓库内的 **Project6.4**，不要选择 Demo 根目录。
4. 编辑当前关卡打开 **Assets/Scenes/TheFirstScene_Seaside.unity**；编辑启动页打开 **Assets/Scenes/StartGame.unity**。切换前先自行保存当前未保存的编辑。旧 Seaside01、SeasideArtPreview 和恢复场景仍保留。

场景保留后续手工修改，是当前关卡的依据。不要重新运行 Excel/JSON 场景生成工具覆盖已有场景。

## 目录与分工

| 目录 | 内容 |
|---|---|
| Project6.4/Assets | 场景、脚本、预制件、运行时美术及其 .meta |
| Project6.4/Packages | 包清单与依赖锁文件 |
| Project6.4/ProjectSettings | Unity 版本和工程配置 |
| Project6.4/Tools | 构建、验证、日志收集和打包脚本 |
| Figures/Source | PSD、PSB 等可编辑美术源稿及导出说明；当前尚未提供分层源稿 |
| maps | 海边、矿洞、森林的 Excel 原稿 |
| Docs | 工程导览、场景规范、CHANGELOG 和历史工作/验证日志 |
| Play64 | 本地游戏构建输出，不提交 Git |
| Docs/Backups | 本地备份和旧版本归档，不提交 Git |

PNG 的统一入口是 **Project6.4/Assets/Art/**。整图参考在 Backgrounds/Seaside.png，拆分素材及布局 JSON 在 Seasidepart1/。不再在 Figures 中重复维护同一批导出 PNG。

## 当前美术状态

保存的预览场景已接入 bg0～bg3，保留白盒碰撞。Seaside_Terrain_Modules.png 是继续切分 bg0 的 Multiple 副本，应保留它的 .meta。其余拆分图作为资源导入，不代表都已实例化或绑定玩法。

美术稿与现有碰撞存在局部差异，仍需分区域对齐并实际测试。图片不自带可靠碰撞，也不会自动实现检查点状态切换。当前分层场景没有旧整图组件提供的 F6/F7 开关。

- [工程导览](Docs/工程导览.md)
- [场景层级与命名](Docs/SceneHierarchy.md)
- [bg3 镜头边界与全图黑边](Docs/CameraBounds.md)
- [启动页布局、按钮及构建](Docs/StartupPage.md)
- [美术上传与开发协作](CONTRIBUTING.md)
- [变更记录](Docs/CHANGELOG.md)

## 构建与试玩

本机手动构建的启动入口为 **statgame.cmd**，双击后运行 `build/FirstScene_Seaside/Seaside Whitebox.exe`。脚本按自身位置寻找程序，移动整个 Demo 目录后仍可使用。请保留该构建目录中的 `_Data`、DLL 和运行库；再次手动 Build 到这个目录即可更新此入口启动的游戏。

在 Demo 根目录执行：

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\BuildAndTest.ps1 -StartupPage -Unity '你的Unity.exe完整路径'
~~~

脚本默认使用本机 D:/Development/Unity/6000.4.12f1/Editor/Unity.exe；其他电脑请传入自己的路径。构建和自动检查输出到 Play64/ 与 Docs/Logs/Build/时间戳/。不要在编辑器正在打开同一工程时同时运行批处理构建。

构建成功后运行 **Play64/SeasideWhitebox.exe**，从启动页点击开始游戏进入 TheFirstScene_Seaside。启动页支持鼠标与方向键/W/S、Enter/空格，选项页提供主音量和全屏。Git 克隆不附带 Play64，需自行构建或下载另行发布的完整试玩包。

StartupPage 模式验证已保存的启动场景和关卡跳转，不重新生成地图。省略该参数仍执行原白盒回归流程；该旧流程的场景几何一致性要求不适用于已手动改动的美术地图。

A/D 移动，空格短跳/长跳，鼠标左键冲刺，Tab 全图，B 路线，R 复活，H 帮助。自动检查通过不等同于人工全关通关。

启动页版本交付打包使用 Project6.4/Tools/PackageRelease.ps1 -StartupPage，生成名称带 Startup 的工程及 Windows 包。ZIP、游戏成品、缓存和完整备份不提交源码仓库；试玩包可另行上传 GitHub Releases。

## 记录与版本

每次工作保留日志并更新 CHANGELOG，遵循 [开发约定](AGENTS.md)。旧 6.6 工程及重复素材已作本地归档，清单和回滚方法见 [本次整理记录](Docs/Logs/Work/20261030-170137-github-handoff/work-log.md)。历史日志中的旧目录和测试结论保留原样，只用于追溯。
