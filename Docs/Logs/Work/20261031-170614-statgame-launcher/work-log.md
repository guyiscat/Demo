# 2026-10-31：创建手动构建启动脚本并核对 ZIP

用户指定在 Demo 根目录创建 statgame.cmd，启动已存在的 build/FirstScene_Seaside，并解释根目录两个 ZIP。文件名按用户要求保留为 statgame.cmd。

检查发现实际入口为 build/FirstScene_Seaside/Seaside Whitebox.exe，配套 Seaside Whitebox_Data、UnityPlayer.dll、MonoBleedingEdge 等目录文件均存在。

新增 statgame.cmd：使用脚本自身目录 %~dp0 计算构建位置，对包含空格的路径加引号；检查 exe 与 globalgamemanagers 存在，失败时给出提示并暂停；通过 start 设置构建目录为工作目录、启动程序并结束命令窗口；保留 %* 以转发可选运行参数。使用 ASCII、CRLF，避免中文代码页导致脚本乱码。README 新增使用说明。

只读打开两个 ZIP 的文件目录进行核对：

- Seaside-Unity64-Startup-Windows.zip：Windows 成品包，含 Play64/SeasideWhitebox.exe、配套运行文件和说明/日志，供玩家解压后运行，无需 Unity 编辑器。
- Seaside-Unity64-Startup-Project.zip：开发工程包，含 Project6.4/Assets、Packages、ProjectSettings、Tools、文档等，确认包含 StartGame.unity 和 TheFirstScene_Seaside.unity；供开发与美术协作用 Unity 6000.4.12f1 打开。没有 Library/Temp 缓存。

两个 ZIP 是之前由 PackageRelease.ps1 生成的交付快照，Windows 包的成品来自 Play64，并非本次用户指定的 build/FirstScene_Seaside。以后手动 Build 或修改源码不会自动更新 ZIP。本次没有重建游戏、更新这两个 ZIP、替换已有构建或修改 Unity 场景，也未提交、推送 Git。

验证范围为入口路径、数据依赖及脚本静态核对，没有新启动游戏或新做通关测试。源构建未改变，不复用历史运行结果声称本次已运行。

修改前备份 README.md 和 CHANGELOG.md 至 Docs/Backups/20261031-170614-statgame-launcher/。statgame.cmd 原先不存在。回滚时移走新增启动脚本并还原备份文档即可，构建与地图不涉及回滚。
