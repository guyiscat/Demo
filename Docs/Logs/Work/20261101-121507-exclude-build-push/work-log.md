# 2026-11-01：排除构建产物并修复 GitHub 推送

用户明确授权解决 GH001 大文件拒绝并提交、推送到现有 GitHub 仓库；本次仅处理 Git 跟踪和文档，不清理磁盘上的游戏、备份或缓存。

## 原因与备份

开始时工作区和暂存区干净。main 为 5231c66912ccf18ace6a48bec2da97431aede392，仅比 origin/main 多一个提交；通过 git ls-remote 确认实际远端 main 为 61f94a2d70cfcd307c6fe7738d805a3e43de172c，与该本地提交的父提交一致。

最后一次本地提交新增 build 下 294 个 Git 文件，其中两个普通 Git blob 分别为 301.03 MiB、301.53 MiB，超过 GitHub 的单文件 100 MiB 限制。图片的 LFS 上传成功与这两个构建资源无关。原 .gitignore 的 **/[Bb]uilds/ 仅匹配复数 Builds，漏掉根目录 build。

修改前在 Docs/Backups/20261101-121507-exclude-build-push/ 保存 .gitignore、CHANGELOG.md、原 HEAD 和状态。repository-before.bundle 包含完整 Git 历史及原 main、origin/main、HEAD 引用，已通过 git bundle verify。build-files-before.json 记录全部 295 个本地构建文件的路径、大小，并为超过 100 MiB 的两个资源记录 SHA256；其中 ZIP 已被忽略，原本只有 294 个构建文件被跟踪。

## 操作

- .gitignore 增加 /build/，不修改美术 LFS 规则。
- git rm -r --cached -- build 只从索引移除构建产物，磁盘上的 FirstScene_Seaside、SeasideReview、构建 ZIP 和日志全部保留；statgame.cmd 仍可使用原路径。
- 保留用户本地提交中的源码、美术、场景、配置和历史日志，仅修正未推送成功的最后一个提交；使用 git commit --amend --no-edit 保留原提交说明和作者。
- 比较修正前后提交树，确认 build、.gitignore 和本次日志/CHANGELOG 之外的文件没有变化；扫描待推送 Git 对象，确认没有超过 100 MiB 的 blob。
- 使用正常 git push origin main，不使用 force、不删除远端内容、不推送备份引用。发布试玩仍应上传完整 ZIP 到 Releases；本次没有创建 Release。

GitHub 连接工具不提供本地 Git/LFS 推送，因此使用本机 Git 和仓库已有认证配置。本次不改身份或凭据，不提交、推送用户授权范围之外的仓库。

## 验证与交付记录

提交前核对暂存变更范围、build 忽略规则及 295 个本地构建文件保留情况，确认旧的大对象只保存在本地备份/历史对象中，不再由待推送 main 引用。具体检查结果及第一次成功推送的远端核对记录在验证后补入本工作目录。

本次没有运行 Unity、改写关卡、重建游戏或重新打包工程/试玩。旧 ZIP 仍为旧交付快照。不会通过删除必要的 resS 文件缩小成品。

## 回滚

可从 repository-before.bundle 检出原提交到单独目录核对。原被拒绝的提交不应再次推送到 main，否则会重新触发大文件限制。若只需重新纳入某个构建，应优先改用 Release 附件，不恢复 /build 的 Git 跟踪。

## 已完成的推送核对

修正提交 cf3dc66067ba1f99aee0afb7fbc141ad7254a30b 已通过普通 git push origin main 成功推送；git ls-remote 确认实际远端 main 与该提交一致。LFS 检查 25 个对象完成，没有 GH001 或超限拒绝，也没有使用强制推送。

修正前后提交树对比确认：除 build 的取消跟踪、.gitignore、CHANGELOG 和本次工作日志外，其余源码、美术、场景及配置完全相同。build 中 295 个本地文件的存在性、大小均保持一致；两个超过 100 MiB 的 resS 的 SHA256 也保持一致。Git 跟踪的 build 文件数为 0。待推送对象中的最大 blob 为 1.81 MiB，超过 100 MiB 的对象数为 0。

结构化验证结果见 push-verification.json；终端推送原始输出留存在本地备份目录的 push-repaired-commit.log。此成功验证记录作为后续文档提交同步，不改变已经上传的游戏源码或本地构建。
