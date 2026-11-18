# 2026-11-02：清理临时副本与旧交付产物

用户明确授权删除上一轮统计的无用内容，并要求不能改变最新游戏制作进度。此前中断的请求未产生清理记录或删除；本次重新检查当前目录、Git 状态和运行进程，再制定白名单。

## 删除范围

本次使用原生 PowerShell，在同一 shell 内验证路径并删除，没有跨 shell 拼接删除命令，没有操作 Unity 编辑器或启动游戏。

- 5 份已完成测试的 validation 临时工程，约 6.08 GiB。
- Docs/Logs/Packaging 下的 34 个历史 ZIP，约 3.34 GiB；该目录中的清单、日志保留。
- 备份中的 9 份旧 Play/Play64/replaced-Play64 成品，约 2.50 GiB；同级源码、场景、设置和其他原始备份保留。
- 两份旧备份工程的 Library 缓存，约 1.15 GiB，分别位于 20261029-hierarchy-refactor/Project6.4 和 20261030-170137-github-handoff/archived/Project。
- 根目录 Play64 和 build/SeasideReview 两份旧输出，合计约 0.77 GiB。
- 根目录两个旧 Startup 交付 ZIP，合计约 220 MiB。

完整绝对路径、文件数和字节数见 deletion-plan.json；逐文件清单见 deletion-file-inventory.csv。删除了 54 个白名单目标内的 12,193 个文件，共 15,085,642,200 字节，约 14.05 GiB。

## 保留内容与验证

以下内容没有删除或改写：

- 当前 Project6.4 主工程，包括 Assets、脚本、素材及 meta、所有场景、Packages、ProjectSettings、Tools；主工程 Library 缓存也保留，避免重新导入。
- build/FirstScene_Seaside 最新游戏的全部文件、build/FirstScene_Seaside.zip 及 statgame.cmd。
- maps、.git、现有工作/构建/历史日志。
- 备份中的原始源码和场景、before 中非成品文件、20261031-111631-recover-unsaved-scene 恢复资料及 20261101 Git bundle 备份。

删除前后核对了 2,416 个保留文件的大小和 SHA256，覆盖主工程数据、最新成品与 ZIP、既有日志和保留备份。结果全部一致，缺失数、哈希不一致数均为 0。保护清单见 protected-files-before.json，最终结果见 cleanup-result.json。当前主工程可变缓存、运行日志和 Git 内部文件未参与哈希，但均不在删除白名单中。

开始时 Git 工作区干净，HEAD 为 435f952a65ef20a08b007944bcf3845763cfa145。所有删除目标均未被 Git 跟踪。执行前检查目标没有被运行中的 Unity/游戏使用；逐个解析绝对路径，拒绝根目录以外路径、受保护目录重叠、链接或重解析点，确认文件数和大小未在计划后改变，再用 Remove-Item -LiteralPath 删除。每个目标的开始与完成时间写入 deletion-journal.jsonl。

## 空间与限制

清理前总文件大小为 18,253,383,874 字节，约 17.00 GiB；清理后约 3,171,482,959 字节，约 2.95 GiB。添加本记录和 CHANGELOG 后仅增加少量文本空间，实际磁盘释放量还受文件系统分配等因素影响。

本次只新增清理记录并更新 CHANGELOG。原 CHANGELOG 备份在 Docs/Backups/20261102-143449-storage-cleanup/CHANGELOG-before.md。删除的是明确授权的临时副本、缓存和旧成品，没有再复制一份这些大文件造成相同空间占用；已保留原始制作资料和必要恢复/Git 备份。

没有编译、运行、重新打包或新做游戏通关验证；旧产物删除后不会自动恢复。没有修改游戏参数、关卡或 Git 历史，也没有提交或推送本次记录。现在可继续通过 Demo/statgame.cmd 启动保留的最新手动构建。下次需要 Play64 或交付 ZIP 时，应按既有工具重新构建/打包；本次不运行这些工具，以免重新生成刚清理的大量文件。
