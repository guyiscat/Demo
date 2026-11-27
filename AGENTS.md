# Demo 开发约定

- 唯一主工程为 Project6.4，Unity 6000.4.12f1；开发入口 README.md、CONTRIBUTING.md、Docs/Handoff.md。
- 场景为当前手工地图依据，禁止以旧 Excel/JSON 或生成工具覆盖。不要保存、切换或关闭用户打开的 Unity，不要重置用户参数。
- 修改资源保留 .meta/GUID；通用脚本在 Scripts 根目录，专属脚本在 seaside/forest/village/mine/snow/lighthouse。
- 每次在 Docs/Logs/Work/日期-主题 留精简工作记录和最终结果，更新 CHANGELOG；如需临时备份应在验证后清理，避免重新堆积历史日志、重复工程包和试玩包。
- 构建验证用 Project6.4/Tools/BuildAndTest.ps1，编辑器打开时使用隔离副本；不要把旧 PASS 当新验证。打包仅在实际需要时用 PackageRelease.ps1。
- 代码、场景、原稿、地图、包锁和 .meta 纳入 Git；Library/Temp/UserSettings、Play64、ZIP、完整备份和原始批处理日志不提交。保留唯一原始素材，重复导出只留正式资源。
- Git LFS 规则见 .gitattributes，不改写已发布历史；提交、推送、发布需要用户授权。
