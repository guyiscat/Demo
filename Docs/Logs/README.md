# 项目日志入口

当前主工程为 Project6.4，统一日志目录为 Demo/Docs/Logs。协作见 [CONTRIBUTING.md](../../CONTRIBUTING.md)，变更见 [CHANGELOG.md](../CHANGELOG.md)。

- Work/：需求、改动、验证、限制和回滚；此次整理见 20261030-170137-github-handoff/。
- Build/：逐次构建输出、报告和 status.json；失败记录保留，旧 PASS 不代表新版本验证。
- History/：历史交付资料和来源清单，保留原目录和日期。
- Runtime/：本地试玩实时日志，Git 忽略。
- Packaging/：本地交付包和清单，Git 忽略。
- ../Backups/：修改前快照和旧工程归档，Git 忽略。

构建验证使用 Project6.4/Tools/BuildAndTest.ps1；打包使用 Project6.4/Tools/PackageRelease.ps1；历史收集使用 Project6.4/Tools/CollectProjectLogs.ps1。运行入口为 Demo/StartGame64.cmd 等，需已有 Play64 构建。

以下保留原入口说明用于追溯，其中 Project/、Play/、6.6 及旧机器路径属于历史状态。

## 2026-10-27 原入口说明（历史保留）

当前统一日志位置：Demo/Docs/Logs（2026-10-27 迁入）。以下历史记录中的 D:\SeasideWhitebox-PlaytestFix-v3 为原交付路径，保留用于追溯。
总变更表：上一级 CHANGELOG.md。

- Work/20261027-demo-migration-seaside-art/：迁移校验、工程/地图核对、背景图接入、几何一致性检查、渲染截图及失败记录。
- Build/20261027-*/：在本机 Unity 6000.6.7f1 执行的新构建、回归和校准，结果以各目录 status.json 为准。
- 本次说明：../工程导览.md 和 ../Seaside背景图接入说明.md。game1 原件未修改；Demo 完整本地备份在 ../Backups/20261027-demo-migration-seaside-art/。

- Work/20261020-wall-jump/：本次需求、原因、改动、修复前后对照、实际测试与环境问题记录。
- History/20261020-192429-652/：已发现的历史项目日志和交付/验收材料，142 个文件，manifest.csv 提供原路径、原修改时间、大小及 SHA256 校验值；unavailable.txt 为空表示这次归档没有读取失败。
- Build/：每次构建有独立时间戳目录，包含编辑器原始输出、Windows 运行测试输出、报告快照与 status.json。FAIL 记录保留，不删掉。
- Runtime/：启动试玩时自动生成独立的运行事件日志，含跳跃请求、地面/墙跳是否执行、墙边容错、检查点、死亡和 Unity 信息/异常。双击 StartGame.cmd 还会保留启动阶段的原始 engine.log。直接运行 exe 仍有事件日志。

## 以后如何留记录

后续开发遵循 Project/AGENTS.md：每次工作创建独立 Work/日期时间-主题/，写明需求、原因、文件/参数改动、测试结果、未验证事项和备份位置，并更新 CHANGELOG.md。
Project/Tools/BuildAndTest.ps1 自动构建和测试、保存时间戳日志及报告快照。
Project/Tools/CollectProjectLogs.ps1 可再次收集历史资料到新的快照，不修改原始来源。
构建会产出新的 Play 内容；启动日志脚本是 Play/StartWithLogs.ps1，不要只拷走 exe。

## 历史范围和局限

收集范围是本项目的 UnityWallDashPrototype、SeasideWhitebox、PlaytestFix-v2、当前 v3，以及当前工作区 outputs 中的项目资料，并包含各工程 Project/Logs 内的 Unity 导入和着色器日志。
不把 Unity 第三方包说明、Library 缓存、无关项目日志或私人聊天复制进来。过去未保存或已被覆盖的原始日志无法凭空恢复；历史需求摘要是根据现存材料和本次对话补记，不冒充当时的逐条工作流水。
旧阶段地图或操作规则不同，历史 PASS 只证明对应旧版本的测试，不代表当前地图已经人工通关。
日志仅保存在本机，没有自动上传或联网收集。不会自动删除旧日志，可按需要自行归档。
