# GitHub 协作目录整理

## 需求与授权

用户在只读审计后要求“开始按照你的思路工作”。按已说明方案整理当前主工程、重复素材、交接文档和 Git 规则；不执行 commit、push 或远端发布。

## 修改前状态与备份

- 当前工程：Project6.4，Unity 6000.4.12f1；用户编辑器正在打开该工程，本次不关闭、不触发场景保存。
- 当前素材和场景从磁盘快照读取，不包含 Unity 内尚未保存的编辑。
- Git 本地只跟踪 README.md，其他源码尚未纳入跟踪；现有忽略规则下候选文件约 218.56 MiB。
- 备份：Docs/Backups/20261030-170137-github-handoff/before/。包含当前 Assets、Packages、ProjectSettings、Tools、Play64、受影响文档、Git config 和原有 pre-push（如存在）。
- before-hashes.json 为当前工程与已有试玩的修改前 SHA256；context.json 记录原本不存在的新文件。

## 已完成改动

1. 将旧 Project、Play、三个旧入口、四个根 ZIP、Figures 的重复图/布局/ZIP、旧 6.6 锁文件备份和无用导出警告日志移入本次备份的 archived/。15 组、1611 个文件，移动前后逐文件 SHA256 一致。未永久删除；完整清单见备份根 archive-manifest.json，分组见 archived-items.json。
2. 移动前核对 Figures/part1 的21个文件及 Seaside.png 均与 Assets 中的对应文件一致。
3. 保留所有当前 PNG、资源 .meta、Seaside_Terrain_Modules 的 Multiple 设置及两个正式场景。Assets/_Recovery 原位保留并排除 Git 和交付包，未决定恢复或覆盖正式场景。
4. 统一 README，保留 README-Unity64 兼容入口；新增 CONTRIBUTING 和 Figures/Source 说明；更新工程导览、日志入口和当前布局引用。旧美术方案增加历史状态提示，历史工作/构建日志不改。
5. .gitattributes 配置 PNG、PSD/PSB 等二进制美术为 LFS，Unity YAML、.meta 和代码保持文本。git lfs install --local 仅配置当前仓库；无全局配置变更，无真实索引暂存。
6. SeasidePart1Backgrounds 从 bg3 实际素材目录读取布局，摆脱已归档的 Figures 副本。SeasideArtSetup 使用已有 Assets 参考图，且在任何导入前拒绝覆盖已存在预览场景。均未执行创建/摆放菜单。
7. PackageRelease 纳入统一 README、CONTRIBUTING 和 .gitattributes，并排除恢复场景、实时日志和嵌套 ZIP。已有试玩二进制沿用，交付包重新打包；不声称新游戏构建或通关。

## 验证

已完成：归档 1611 个文件的内容校验；Git LFS 的 PNG/大写PSD匹配及 .meta/场景文本属性；PackageRelease PowerShell 语法解析。

validation.json 的17项检查全部 PASS：只有预期两个编辑器脚本变化、三项无用文件被归档；两个正式场景、全部保留美术/切片设置、恢复场景、ProjectSettings 和 Play64 哈希不变；布局图片齐全、当前文档链接可达、缓存/归档排除且历史日志保留。

隔离副本完成 Unity 6000.4.12f1 首次导入与脚本编译，unity-compile.log 明确报告 return code 0，没有 C# 编译错误。GUI 启动器先返回，未捕获子进程的操作系统退出码，以 Unity 自身日志为依据。没有操作或编译构建主工程的场景。

临时 Git 索引进行完整添加检查：git-audit.json 记录768个候选文件、约108.88 MiB原始体积（该次检查时），41个美术文件均为合法 LFS 指针，Multiple 的 .meta 保留。实际 .git/index 前后哈希一致；没有暂存到用户索引，没有 commit/push。最终日志增加后文件数和体积会略增。

交付包的最终检查结果见 package-verification.json，打包完成后生成，因此该最终报告不包含在同次 ZIP 快照内。重打包使用既有 Play64，没有新游戏构建或人工通关。

## 限制

- 不运行主工程构建或生成场景，不打断当前 Unity 编辑，不保存用户未保存内容。
- Play64 是已有构建，不能据本次目录整理认定已包含用户之后未重新构建的美术修改。
- 本次没有人工通关，也没有重新验证角色动作或调整地图碰撞。
- 配置 LFS 不等于图片已经提交；当前未暂存、提交或推送。实际推送时需远端 LFS 权限和可用配额。

## 回滚

先保存并额外备份整理之后的个人修改，再按差异恢复，勿覆盖后续编辑。

- 文档、编辑器脚本和工具：从 before/ 的同相对路径恢复需要回滚的文件。
- 归档内容：对照 archive-manifest.json，将 archived/ 内的对应项目移回 Demo 下原相对位置；目标存在时先比较，禁止直接合并覆盖。
- 恢复场景和 Multiple 素材一直在原处，不需要从归档搬回。
- .gitattributes、CONTRIBUTING、Figures 两份 README 原先不存在，回滚时先检查后续修改再移走。
- 本地 LFS 配置及 pre-push 的原状态保存在 before/.git；仅恢复相关项，不覆盖后续 Git 配置改动。无 Git 历史改写需要撤销。
