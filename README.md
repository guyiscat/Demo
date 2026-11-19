# 雷火游戏创作 Demo

当前唯一主工程是 **Project6.4/**，使用 **Unity 6000.4.12f1**。海边 Seaside 已有玩法与美术场景；森林 Forest、矿洞 Mine、雪山 Snow 和灯塔 Lighthouse 为独立白盒关卡，尚未连接关卡进度，尚未制作对应主题贴图。

## 开始开发

1. 安装 Git、Git LFS 和 Unity 6000.4.12f1。首次使用 LFS 执行 git lfs install。
2. 克隆仓库后执行 git lfs pull，确保图片是真实文件，而不是 LFS 指针文本。见 [协作指南](CONTRIBUTING.md)。
3. Unity Hub → Add project from disk → 选择仓库内的 **Project6.4**，不要选择 Demo 根目录。
4. 编辑当前第一关打开 **Assets/Scenes/TheFirstScene_Seaside.unity**；编辑启动页打开 **Assets/Scenes/StartGame.unity**。切换前先自行保存当前未保存的编辑。Scenes目录只保留启动页和五个当前关卡；旧预览/恢复场景已移出工程。动作测试夹具单独位于Assets/Editor/Tests/Fixtures，不作为游戏场景。
5. 单独编辑或试玩森林白盒打开 **Assets/Scenes/ForestWhitebox.unity** 直接 Play；Windows 入口为根目录 **forest.cmd**。森林的地图解释、新操作、构建与测试见 [森林白盒说明](Docs/ForestWhitebox.md)。
6. 独立矿洞打开 **Assets/Scenes/MineWhitebox.unity** 直接 Play；Windows 入口 **mine.cmd**。交互、电梯、参数及地图解释见 [矿洞说明](Docs/MineWhitebox.md)。
7. 独立雪山/灯塔打开 **Assets/Scenes/SnowWhitebox.unity** 或 **Assets/Scenes/LighthouseWhitebox.unity**；Windows入口 **snow.cmd**、**lighthouse.cmd**。冷热切换、攀岩补跳、雪山残章选择及塔顶灯台见 [雪山与灯塔说明](Docs/SnowLighthouse.md)。

场景保留后续手工修改，是当前关卡的依据。不要重新运行 Excel/JSON 场景生成工具覆盖已有场景。

## 目录与分工

| 目录 | 内容 |
|---|---|
| Project6.4/Assets | 场景、脚本、预制件、运行时美术及其 .meta |
| Project6.4/Assets/Scripts | 公共系统留根目录，seaside/、forest/、mine/、snow/、lighthouse/存放关卡脚本；Editor目录同样分类 |
| Project6.4/Packages | 包清单与依赖锁文件 |
| Project6.4/ProjectSettings | Unity 版本和工程配置 |
| Project6.4/Tools | 构建、验证、日志收集和打包脚本 |
| Assets（仓库根目录） | 用户提供的PSD、字体、文案等原始素材，保留用于后续开发 |
| maps | 海边、森林、矿洞、雪山、灯塔的 Excel 原稿 |
| Docs | 工程导览、场景规范、CHANGELOG 和历史工作/验证日志 |
| Play64 | 本地游戏构建输出，不提交 Git |
| Docs/Backups | 本地备份和旧版本归档，不提交 Git |

PNG 的统一入口是 **Project6.4/Assets/Art/**。整图参考在Backgrounds/Seaside.png，拆分素材及布局JSON在Seasidepart1/。原始素材、未实例化的后续美术和编辑器动态加载的预制件仍有用途，不因当前无场景引用而删除。

所有关卡系统字体统一使用 `Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf`，入口为GameSystems.systemFont；字体来自仓库根Assets中的阿里巴巴普惠体。根Assets的两个IMG PSD是角色动作帧源图，说明见 [素材识别](Docs/ImportedAssets.md)。

## 当前美术状态

当前第一关已接入分层美术和手工碰撞。Seaside_Terrain_Modules.png是继续切分bg0的Multiple副本，应保留它的.meta。其余拆分图作为资源导入，不代表都已实例化或绑定玩法。

美术稿与现有碰撞存在局部差异，仍需分区域对齐并实际测试。图片不自带可靠碰撞，也不会自动实现检查点状态切换。当前分层场景没有旧整图组件提供的 F6/F7 开关。

- [工程导览](Docs/工程导览.md)
- [公共参数、统一提示与脚本分类](Docs/ScriptLayout.md)
- [场景层级与命名](Docs/SceneHierarchy.md)
- [bg3 镜头边界与全图黑边](Docs/CameraBounds.md)
- [启动页布局、按钮及构建](Docs/StartupPage.md)
- [美术上传与开发协作](CONTRIBUTING.md)
- [变更记录](Docs/CHANGELOG.md)

## 构建与试玩

第一关启动入口为 **statgame.cmd**，从启动页进入海边；其余独立入口为 **forest.cmd**、**mine.cmd**、**snow.cmd**、**lighthouse.cmd**，均运行Play64中的当前成品。暂不设置关卡跳转。请保留完整构建目录中的_Data、DLL和运行库。旧build输出和两份重复解压森林试玩已移出工作目录，当前ZIP和启动入口保留。

在 Demo 根目录执行：

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\BuildAndTest.ps1 -StartupPage -Unity '你的Unity.exe完整路径'
~~~

脚本默认使用本机 D:/Development/Unity/6000.4.12f1/Editor/Unity.exe；其他电脑请传入自己的路径。构建和自动检查输出到 Play64/ 与 Docs/Logs/Build/时间戳/。不要在编辑器正在打开同一工程时同时运行批处理构建。

构建成功后运行 **Play64/SeasideWhitebox.exe**，从启动页点击开始游戏进入 TheFirstScene_Seaside。启动页支持鼠标与方向键/W/S、Enter/空格，选项页提供主音量和全屏。Git 克隆不附带 Play64，需自行构建或下载另行发布的完整试玩包。

StartupPage模式验证已保存的启动场景和第一关；省略关卡参数也默认构建这个当前入口。-ForestWhitebox、-MineWhitebox、-SnowWhitebox、-LighthouseWhitebox分别验证并构建其他独立关卡。所有入口仍执行公共动作回归，使用Editor/Tests/Fixtures中的测试场景，不依赖旧游戏场景，不重建手工地图。

A/D 移动，空格短跳/长跳，鼠标左键冲刺，Tab 全图，B 路线，R 复活，H 帮助。自动检查通过不等同于人工全关通关。

公共镜头、松墙下滑、会话和方向箭头在 `Assets/Resources/GameSystems.asset` 调整，公共动作使用其引用的 `Assets/Settings/Movement.asset`。各关专属参数位于Settings对应资源。详见[脚本分类与参数入口](Docs/ScriptLayout.md)。

启动页版本交付打包使用 Project6.4/Tools/PackageRelease.ps1 -StartupPage，生成名称带 Startup 的工程及 Windows 包。ZIP、游戏成品、缓存和完整备份不提交源码仓库；试玩包可另行上传 GitHub Releases。

## 记录与版本

每次工作保留日志并更新 CHANGELOG，遵循 [开发约定](AGENTS.md)。旧 6.6 工程及重复素材已作本地归档，清单和回滚方法见 [本次整理记录](Docs/Logs/Work/20261030-170137-github-handoff/work-log.md)。历史日志中的旧目录和测试结论保留原样，只用于追溯。
