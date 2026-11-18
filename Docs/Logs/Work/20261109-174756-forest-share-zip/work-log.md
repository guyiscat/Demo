# 森林独立试玩分享压缩包

用户要求：单独将森林场景打包为压缩包。按当前分享试玩语境提供Windows独立运行包，解压全部内容后运行forest.cmd，也可直接运行Play64/Forest/ForestWhitebox.exe，无需安装Unity。

本次不修改场景、代码或动作参数，不关闭/保存用户Unity编辑器。沿用上轮20261109-173608-596构建，含传送滞空。本次仅重新压缩，未重新构建、未新增运行或人工通关验证。

命令：powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\PackageRelease.ps1 -ForestWhitebox。

标准包：Forest-Unity64-Whitebox-Windows.zip；额外分享副本：森林独立试玩.zip。工程包同时按项目约定更新。校验记录见verification.json，包含CRC与二进制逐文件SHA256比较。

原双ZIP及CHANGELOG备份：Docs/Backups/20261109-174756-forest-share-zip/。若需回滚只恢复该备份中的文档和压缩包，删除本次新增分享ZIP即可；不覆盖其他工作。