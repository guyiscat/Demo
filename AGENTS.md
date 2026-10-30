# Demo 开发约定

- 当前唯一主工程为 Project6.4/，编辑器 Unity 6000.4.12f1。旧 Project/、Play/ 和旧入口归档至 Docs/Backups/ 的 github-handoff 目录；game1 为迁移来源，不修改原件。
- 当前说明入口为 README.md，协作及美术交付见 CONTRIBUTING.md，层级规范见 Docs/SceneHierarchy.md。
- 每次工作在 Docs/Logs/Work/日期时间-主题/ 留需求、原因、改动、参数、验证结果、限制和回滚位置，并在 Docs/CHANGELOG.md 顶部追加；失败和历史记录保留。
- 修改前备份受影响的工程、文档或试玩到 Docs/Backups/日期时间-主题/。保留手工地图修改，禁止以 Excel/JSON 重新生成场景覆盖。
- 构建和自动测试使用 Project6.4/Tools/BuildAndTest.ps1，输出 Play64/。只读、文档和仓库整理如未重建游戏应如实标明，不能把旧报告当本次验证。
- 交付包通过 Project6.4/Tools/PackageRelease.ps1 更新。仅仓库整理而游戏内容未改时，可重新打包已有试玩，并注明二进制沿用、未重建或新通关验证。
- 源码、素材及 .meta、地图、文档纳入 Git；Library、临时文件、Play64、压缩包、本地完整备份和恢复场景排除。不得一概忽略历史 .log。
- PNG 的统一运行时入口是 Project6.4/Assets/Art/；Figures/Source/ 保留真实可编辑源稿，不再复制同批导出 PNG。图片改名同步布局引用。
- 场景对象采用有意义的 PascalCase，地图单元保留 Excel 地址。脚本通过组件或显式引用定位，不依赖显示名和父路径。改层级保留世界变换，验证碰撞、复活点、Tag/Layer 和组件引用。
- 不自动保存用户未保存的场景、不自动恢复备份、不触发无关场景保存或动作参数重置。不为仓库整理关闭用户正在使用的 Unity 工程。
- Git LFS 规则见 .gitattributes；不未经请求提交、推送或发布。
