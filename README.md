# 雷火游戏创作 Demo

Unity 2D 横版游戏；唯一主工程是 **Project6.4/**，版本 **Unity 6000.4.12f1**。当前保存场景是地图依据，不能用旧 Excel 或生成工具覆盖手工修改。

## 获取并运行

```powershell
git lfs install
git clone https://github.com/guyiscat/Demo.git
cd Demo
git lfs pull
```

Unity Hub 添加 **Project6.4**（不是仓库根目录），首次导入完成后打开 `Assets/Scenes/StartGame.unity`，按 Play 体验完整流程。无需 Python、Excel、Photoshop、旧备份或原作者电脑上的文件。图片打不开时执行 `git lfs pull`；GitHub 网页下载 ZIP 不作为默认开发交接方式。

| 场景 | 内容 |
|---|---|
| StartGame.unity | 开始、继续、设置、退出 |
| TheFirstScene_Seaside.unity | 海边，灯塔交互获得信标传送 |
| ForestWhitebox.unity | 森林，已接入分层美术、水车与滑索 |
| Village.unity | 村庄，已接入分层美术；机器人、矿洞坑口 |
| MineWhitebox.unity | 矿洞，电梯、开关、大门、比特币、照明 |
| SnowWhitebox.unity | 雪山，冷热切换、攀岩点与残章选择 |
| LighthouseWhitebox.unity | 灯塔，最终追逐与灯台 |

表中场景均在 `Project6.4/Assets/Scenes/`，可单独打开编辑或 Play。海边↔森林↔村庄↔雪山通过地图边缘往返；村庄坑口进矿洞，矿洞上方跳回村庄。雪山选择继续后，从海边左侧或与 lighthouse 按 E 交互进入灯塔。区域传送面板只包含已探索的海边、森林、村庄、矿洞、雪山，灯塔不参与传送。

## 操作与存档

A/D 移动，空格跳跃，左键冲刺，右键信标／传送，W 滑索，E 交互，S 防寒／隔热，Esc 暂停，H 基本帮助。可改键动作在游戏设置调整；Tab 镜头、B 路线、R 复活、Ctrl+R 重开为固定快捷键。

安全地面按 F5 或 Esc → 保存游戏选择 5 个槽位。开始页“继续游戏”自主选档；地图存档点只负责本次游玩的自动复活。真实玩家存档位于 `Application.persistentDataPath`，不纳入仓库。

矿洞比特币交给村庄机器人取得核心后，才能使用冷热防护。角色身后蓝色代表防寒、红色代表隔热；S 切换。具体参数和场景组件见下列文档。

## 接续开发入口

- [开发交接与验证](Docs/Handoff.md)：工程依赖、场景与脚本职责、构建、已知限制。
- [脚本分类与参数](Docs/ScriptLayout.md)、[场景层级](Docs/SceneHierarchy.md)。
- [区域连接与矿洞机制](Docs/WorldRegions.md)、[机制修订](Docs/RegionMechanics.md)。
- [设置和存档](Docs/SettingsAndSaves.md)、[文案](Docs/LevelCopy.md)。
- [村庄美术](Docs/VillageArtwork.md)、[森林美术](Docs/ForestArtAndTravel.md)、[区域传送与核心光效](Docs/TransferAndProtection.md)。
- [协作指南](CONTRIBUTING.md)、[变更记录](Docs/CHANGELOG.md)。

`Project6.4/Assets` 保留源码、美术、音频、字体、场景、预制件与 .meta；`Packages` 和 `ProjectSettings` 保留包锁文件与工程配置；`maps` 保留地图原稿。根 `Assets` 仅作为原始素材/策划资料入口，已导入的完全相同副本不重复保留，替代路径见 [原稿目录](Assets/README.md)。

## Windows 构建

关闭**将要构建的那份工程**的编辑器，或复制 Assets、Packages、ProjectSettings、Tools 到隔离工程。仓库根执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\BuildAndTest.ps1 -StartupPage -Unity 'C:\你的安装目录\Editor\Unity.exe'
```

它验证当前保存场景，构建包含六区域的 `Play64/SeasideWhitebox.exe`，执行动作、流程、存档、菜单与美术回归。`statgame.cmd` 启动本地完整试玩。GitHub 不存游戏二进制、ZIP、缓存或历史备份，克隆后需要自行构建；无需独立关卡包也能体验完整游戏。

独立调试仍可用 `-ForestWhitebox`、`-MineWhitebox`、`-SnowWhitebox`、`-LighthouseWhitebox` 构建；对应 `.cmd` 会检查是否已有构建。打包工具保留供需要时使用，不常驻维护重复工程ZIP。
