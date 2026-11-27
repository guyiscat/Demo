# 接续开发交接

## 工程与依赖

唯一工程 `Project6.4`，Unity **6000.4.12f1 (3ca267ce8005)**。Git LFS 管理图片、PSD和音频，`.meta` 为普通文本；克隆后先 `git lfs pull`。包依赖由 `Packages/manifest.json` 与 `packages-lock.json` 锁定，均为 Unity 官方 2D/内置模块，没有指向原作者硬盘的本地包。

用 Hub 打开工程，等待导入，再打开 `Assets/Scenes/StartGame.unity`。只有启动页和六个当前区域参与游戏构建；`Assets/Editor/Tests/Fixtures` 是动作回归夹具，须保留。Library、Temp、Logs、UserSettings 会在本机自动生成，不提交，不是继续开发的依赖。

Windows 构建见仓库 README；`BuildAndTest.ps1 -Unity` 支持不同机器的安装路径。不在已打开的同一个工程上运行批处理；可以复制 Assets、Packages、ProjectSettings、Tools 到另一目录运行。不要运行 Generate/Revision 等历史地图生成入口覆盖已经保存的场景。

可选地图导入工具使用 Python 3 与 Pillow，只用于读取 Excel 原稿和更新中间 JSON/参考图，不是 Unity 导入或游戏构建的前置依赖。可用 `python -m pip install -r Project6.4/Tools/requirements.txt` 安装。

## 继续修改的位置

- 所有关卡在 `Assets/Scenes/`，场景碰撞、出口、触发器与复活锚点以手工版本为准。
- 通用逻辑在 `Assets/Scripts/`；关卡专属目录为 seaside、forest、village、mine、snow、lighthouse。保留类名、命名空间和 .meta GUID 后可以安全移动脚本。
- 统一动作/镜头/字体等在 `Assets/Resources/GameSystems.asset`，动作资源 `Assets/Settings/Movement.asset`；各关专属参数在 Settings。
- 音乐/角色帧在 `Assets/Resources/GameMedia.asset`，系统字体 `Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.otf`。
- 森林、村庄运行时装饰由 ForestArtwork/VillageArtwork 按现有地图挂载，不改碰撞；导出图位于 `Assets/Art/Resources/ForestArt`、`VillageArt`。
- H 与 Esc → 操作帮助共享 `GameHUD.DrawHelp`，只有基本操作。暂停美术在 SuspendArtwork；区域传送美术在 TransferArtwork。
- 保存/读取：CampaignFlow、CampaignSave、ManualSaveController、SaveSlotScreen；区域连接：WorldRegion、RegionExit、WorldTransition、RegionArrival、TravelPoint/Menu。

## 清理与交付政策

2026-11-27 按用户要求清除旧备份、重复解压试玩、重复ZIP、旧验证输出和无用日志。根目录保留一份当前 Play64 完整试玩供本机使用，它不进入 Git；四套重复独立试玩按需重建。唯一原始 PSD、策划PDF、地图、运行资源、资源导入配置、测试和构建脚本保留。完全相同的原始导出副本仅保留工程中的正式资源，清单见 Assets/README.md。

Git历史不重写，远端已有版本可按提交回溯。以后每次保留精简工作记录和最终验证摘要即可；备份、原始日志、打包件按需生成，不重复纳入源码。当前打开的 Unity 缓存不在运行中删除。

## 验证范围与限制

最终结果由 `Docs/Logs/Work/20261127-village-handoff/validation-summary.json` 和工作记录说明。验证包括实际 Unity 编译/构建、六区域流程、存档跨进程读写、菜单输入隔离、新村庄画面与碰撞不变、转场计时、Git/LFS及从干净源码重新导入构建。

既有森林手工尖刺与旧工作簿的差异、矿洞用户停用尖刺不因清理重新生成或启用。原画中的远景拱桥只作背景；村庄白盒坑口处保持实际可跳入矿洞的开口。机器人目前仍为既有白盒外观，未提供独立NPC原画。自动回归与截图检查不等同于人工全路线通关。

实际交付验证：从 GitHub 提交 `97b037e` 全新克隆，LFS对象/指针检查、空Library首次导入、完整Windows构建和六区域/多槽存档/菜单/村庄美术回归全部通过；导入没有改动任何受版本管理文件。最新摘要与工作记录随本仓库提供。
