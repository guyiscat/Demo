# 推送当前五关流程、存档与文案配置

用户于2026-11-21明确要求推送GitHub。当前main，远端origin=https://github.com/guyiscat/Demo.git；开工HEAD与origin/main均为7df614a5db969864ea77cc5b7129c84f57acfba1，fetch后左右差异0/0。追加提交并普通push，不force、不改写历史。

范围为前两轮已完成未提交的五关流程、安全存档与第四项继续游戏、检查点、文案提示/海边灯塔交互、构建工具及说明/工作日志/成功失败检查记录，并保留磁盘上的用户场景调整。沿用20261120-level-copy/verification.json及final-test-summary.json通过结果，不重新构建，不声明新人工全路线通关。已知Unity原生窗口退出异常在此前版本同样复现，未修复，保留文档和对照日志。

本轮修改前备份在Docs/Backups/20261121-push/，含.gitignore、CHANGELOG及起始已跟踪改动patch；本轮日志保存起始HEAD。仅新增精确规则忽略根目录Seaside-Unity64-Startup-Windows解压试玩副本，目录原物保留。Play64、ZIP、Library/Temp及完整本地备份继续排除；不上传运行时玩家存档，不触碰用户未保存Unity场景。图片依现有Git LFS规则提交。历史失败日志保留，不将旧“未提交/未推送”表述改成虚构历史。

提交前检查源码/场景与最后已验证隔离工程一致性、暂存文件范围、LFS对象及非LFS大文件。C#与文档空白校验通过；Unity YAML自动序列化空m_Name有尾空格，不为Git交付重写已验证场景。此轮仅Git/日志/.gitignore操作，二进制与试玩包沿用上一轮校验版本，无须重建；不为新增推送说明重新制作试玩。最终commit、push及远端核对结果见本轮工具输出和用户最终回复。

回滚本轮说明/忽略规则可按备份逐文件恢复；游戏改动属于用户授权推送范围。远端提交若需回退应新增revert，不强制改写公共main。
