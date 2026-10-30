# 2026-10-20：切换方向后偶发墙跳失效

## 需求

用户反馈贴墙按 A 后，再按 D 和空格有时没有向上跳。要求在当前 v3 文件夹内优化，并将本项目所有现存日志汇总，后续工作和优化继续生成记录。

## 复现与原因

旧墙跳条件必须当前物理帧 WallSide != 0。切换方向后角色加速离墙，很快超出 0.10 格探测范围；这时空格虽被缓存，但既没有地面资格、也没有墙跳资格，因此不会向上跳。
三种物理步长 0.005/0.01/0.02 秒、左右两侧，方向切换后延迟 0/20/40/60/80 毫秒按空格。旧版记录 wall-jump-before.txt：34 PASS，16 FAIL，失败集中在延迟 40～80 毫秒的情形。
另一个输入问题是原 A/D 轴使用 D-A，两个键重叠会抵消为 0；这不一定单独造成墙跳丢失，但会让切换方向手感不一致。

## 改动

- SeasideMotor.cs / SeasideSettings.cs：新增 wallCoyoteTime=0.10 秒，从最后一次真实贴墙探测开始计时，短暂离墙仍按最近墙面方向蹬跳。跳后立即消耗；落地、重置和冲刺清除旧墙面资格；保留 0.12 秒墙跳推离锁定，避免上升立刻被重新攀附吞掉。
- SeasideInputLatch.cs：A/D 重叠时后按方向优先；渲染帧采样到的空格按下边沿保留至下一物理帧，消费一次。修复后仍支持短按/长按高度差。
- WallJumpRegression.cs / WallJumpEditorChecks.cs：添加同帧、不同延迟、左右墙、连续输入、资格过期和短按输入缓存的可重复测试。完整回归调用同一测试集。
- SeasideLog.cs / SeasideSession.cs / SeasideMotor.cs：新增每局日志，保存跳跃请求与执行、检查点、死亡和 Unity 信息/异常；不逐帧写文件。
- Play/StartWithLogs.ps1 / StartGame.cmd：原启动入口不变，增加按时间保存原始引擎日志。
- Project/Tools：构建测试、历史归档和打包脚本；Project/AGENTS.md 及工作区 AGENTS.md 固化后续日志要求。

## 备份与数据保护

替换前完整备份：Docs/Backups/20261020-wall-jump，包含工程可编辑文件、Play、根目录原包和说明、原测试/构建记录。没有改动旧 D 盘项目。
不重建地图，保留已有场景和镜头设置。短跳 6 格、长跳 10 格、冲刺 10 格；平台土狼 0.14 秒、落地缓存 0.12 秒不变。

## 验证

定向修复后 wall-jump-after.txt：72 PASS / 0 FAIL；包括 30 个切换延迟组合、30 个单次消耗与后续上升检查、6 个过期拒绝检查、6 个输入边沿/重叠检查。
Build/20261020-192517-945：基础测试 40、完整编辑器回归 165、Windows 回归 121、Windows 校准 7，全部通过，所有进程退出码为 0。
同一批切换时序在真实 Windows 构建中再次验证。尚未完成真人键盘全图通关；不承诺解决键盘硬件本身的组合键冲突。
启动入口额外冒烟验证：StartWithLogs.ps1 以 batchmode/nographics/smokeTest 启动并正常退出；Runtime/20261020-192914-276-engine.log 和配套会话日志均含 SEASIDE_SMOKE_PASS，会话含 SESSION_START / SESSION_END，无 Unity Error/Exception。
与备份逐字节比较 Seaside01.unity 和 SeasideMap.json，均无差异；场景布局没有被重建或移动。
打包脚本已生成两类压缩包，并检查不含 Library/Temp/UserSettings/备份目录或递归打包目录。交付包内包含工作、历史和构建测试日志；整套替换前备份保留在本机，不塞入压缩包。

## 工作中遇到的问题

目录授权刷新曾导致 helper_unknown_error 和写入失败；改为授权实际子目录和交付文件后恢复，期间没有修改游戏工程。
新增测试补丁最初超过 Windows 命令行长度限制，拆分补丁后解决；测试文件缺失时曾出现编译失败，随后重跑的 before/after 记录为有效复现结果。
首次构建包装脚本没有等待 GUI 进程，status.json 记为 FAIL（退出码 null），但该目录 editor-build.log 显示 Unity 实际成功。已改为等待进程结束并完整重跑；旧失败记录原样保留，以最终 PASS 目录为验收依据。
