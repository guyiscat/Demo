# 海边白盒：跳跃手感改进版

启动：D:\SeasideWhitebox\StartGame.cmd。
对应工程：D:\SeasideWhitebox\Project，Unity 6000.4.12f1。

人物仍为 2×2 格，一个身位为 2 格。短跳 3 身位（6 格）、长跳 5 身位（10 格）、鼠标方向冲刺 5 身位（10 格）。

## 改进

|参数|原值|新值|
|---|---:|---:|
|上升重力 Ascent Gravity|36|60|
|下落初始重力 Fall Gravity Start|22|35|
|下落最终重力 Fall Gravity End|62|85|
|下落加速过渡 Fall Ramp Time|0.45 秒|0.25 秒|
|下落速度上限 Terminal Fall Speed|24|32|
|冲刺后滞空 Dash Hover Duration|0.07 秒|0.03 秒|

上升更快到达顶点，下落更干脆，减少冲刺后悬浮感。跳跃力度由控制器根据目标高度和重力自动计算。
地图、玩家尺寸、检查点、普通跑速和冲刺速度保持原有设置，未使用 Excel 重建覆盖场景修改。

## 验证

40 项编辑器检查通过，包括：
- 短跳实测 5.9995 格（2.9997 身位）。
- 长跳实测 9.9998 格（4.9999 身位）。
- 长跳到顶点约 0.59 秒，原约 0.75 秒。
- 各方向冲刺约 10 格（5 身位），落地/攀附恢复与空中次数限制通过。
- 冲刺后 0.03 秒滞空，再恢复渐进加速下落。
- 12 个检查点安全复活测试通过。

独立 Windows 程序校准通过：角色尺寸、长短跳、斜向冲刺、冲刺次数限制、实际检查点触发及尖刺死亡。
完整结果在 Docs/physics-tests.txt、Docs/standalone-calibration.txt，构建日志 build-snappy.log。
本次验证不包含整关路线可通关性；手感还可根据试玩反馈继续微调。

## 使用

A/D 左右；空格短按/长按；鼠标左键朝光标冲刺；按住朝墙的 A/D 攀附，空格蹬墙；R 回检查点；Ctrl+R 重开；Tab 全图。
传送动作尚未定义，因此仍未加入。Unity 工程中选 Assets/Settings/Movement.asset 可继续调整以上参数。

更新前的参数与工程保存在 D:\SeasideWhitebox\Backups\Before-SnappierJump。
下载包用带 Snappy 名称的新版；解压 Windows 包后运行 StartGame.cmd。Project 包供 Unity 编辑使用。
