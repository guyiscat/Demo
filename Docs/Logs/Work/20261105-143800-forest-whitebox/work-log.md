# 森林白盒开发记录

用户需求：依据森林地图制作独立第二关白盒，保留海边机制与场景，新增信标传送、滑索、水车双解锁与瀑布遮挡。

已完成独立森林白盒、Windows 构建与机制自动验证。尚未进行真人从出生点走完两侧完整路线的通关验收，未开始森林贴图。
备份：D:\UnityProjects\Demo\Docs\Backups\20261105-143800-forest-whitebox

## 来源、范围与参数

- 实际源文件是 `maps/森林地图.xlsx`，用户消息的 `.slxs` 为扩展名笔误。只读解析 XLSX 的 XML 单元格填充与绘图锚点；未写回或保存 Excel。Sheet1 的地图裁剪范围 AB24:MB129，313×106 格。文件 SHA256：`e684aa5c696d7299de07e1a30ad7b329e82058d162c5220f1fdb7ba725e56c0e`。
- 上方蓝色方块 L6:M7 与 V6:W7 中心相距 10 格，因此森林信标极限距离 10 Unity 单位；原冲刺距离仍 8。源地图诊断图 `forest-source-map.png` 与 `map-import.json` 保留来源格数、分组范围及绘图坐标。
- 导入 4365 黑色实体格、70 灰色不可攀附格、697 红色危险格、316 绿色滑索格（15 条）、52 蓝色路线格（13 组）、24 解锁格（左右各一组）。黑/灰按行合并碰撞，保留 Excel 地址；荆棘直接复用海边图片和死亡触发。
- 信标速度 45、半径 0.12、射程 10；滑索速度 16、接触距离 1.6；水车 12 度/秒、16 块水平踏板。共享 Movement.asset 仍为角色 2×2、移动 10、短跳4/长跳8/冲刺8，以及原有地面/离墙宽限和输入缓存。
- 原稿未标出生与终点触发器，采用左下蓝方块上方 `(3,5.2)` 作为出生点，右上既有黑色平台上放置终点旗帜。没有新增普通检查点。普通死亡保留解锁，Ctrl+R 完全重置。
- 新建 ForestWhitebox.unity，独立输出 Play64/Forest/ForestWhitebox.exe；没有修改海边启动页目标、全局场景列表、现有海边场景或成品。森林没有跨关卡跳转或存档。

## 实现与涉及文件

- `Project6.4/Tools/ImportForestMap.py`：只读原稿，写 ForestMap.json、来源分析和诊断图，不编辑场景。使用 Spreadsheets 技能的读取/分析流程及 bundled Python，未创建或改写工作簿。
- `Assets/Scripts/ForestMapData.cs`、`ForestSettings.cs`：来源及森林独立参数。
- `ForestPlayer.cs`：仅森林启用的输入驱动；普通动作仍调用原 SeasideMotor.Tick；右键信标、安全落点回退、接触恢复、滑索双向/斜索、跳离、踏板乘坐与顶部交接、复活清理。
- `ForestZipline.cs`、`ForestWaterwheel.cs`、`ForestWheelPlatform.cs`、`ForestUnlock.cs`、`ForestGoal.cs`、`ForestWaterfall.cs`：索段、双点锁定、水车和踏板、双解锁后有效的真实终点、按整个角色碰撞盒切换的视野遮罩。滑索绿线运行时跟随 Start/End。
- `ForestLevel.cs`、`ForestHUD.cs`：森林专用流程、说明、状态和胜利界面；特定启动参数支持离屏画面验证。Art 分组为空，白盒与后续美术分开。
- `SeasideMotor.cs`：仅追加可选外部机制方法与 ResetPerformed 事件。原 Update/FixedUpdate/Tick 计算和参数不改；海边没有 ForestPlayer，普通行为不调用新增接口。
- `Assets/Editor/ForestBuilder.cs`、`ForestEditorChecks.cs`：只首次创建、拒绝覆盖已有森林场景；构建只读取保存场景。使用显式单场景构建列表，不修改 EditorBuildSettings。
- `Assets/Scripts/ForestRuntimeChecks.cs`：仅 `-forestChecks` 运行，使用真实 Physics2D 和场景组件测试新增机制，包含真实荆棘、解锁触发器、踏板运送及终点。
- `Tools/BuildAndTest.ps1 -ForestWhitebox`、`Tools/PackageRelease.ps1 -ForestWhitebox`：新增独立分支，旧海边分支保留；森林独立试玩入口 `forest.cmd`。
- 新增 `Assets/Art/ForestWhitebox/Block.png` 与线材质、`Assets/Settings/Forest.asset`、ForestMap.json、ForestWhitebox.unity 及其全部 .meta；更新 README、工程导览和新增 Docs/ForestWhitebox.md；本次动作测试正常刷新 Docs 下报告，原报告已备份。

## 问题、失败记录与解决

1. 初始保护清单的 PowerShell Get-FileHash 无法共享读取正由其他程序占用的森林 XLSX；没有关闭该程序，改用 Python 只读文件流记录原稿 SHA256，最后复核一致。该问题未导致文件写入或替换。
2. `build-attempt-1.txt` / Build/20261105-145255-826：Unity 批处理自带未命名空场景，拒绝 additive NewScene。生成器改为 batchmode 下 NewSceneMode.Single，交互编辑器仍 additive，不自动保存用户未保存场景。
3. `build-attempt-2.txt` / Build/20261105-145342-238：首次生成后地形检查失败，BoxCollider2D 没自动跟随 sliced SpriteRenderer 尺寸，仍为1×1。为所有黑/灰条块、解锁点、瀑布和终点显式配置碰撞尺寸，并针对新森林场景修复483个碰撞盒。修复前森林场景副本已备份，未重新生成或修改海边。
4. 第3次：森林暂停断言失败，原因是测试器在 Time.timeScale=0 时仍手动 Physics2D.Simulate，实际正常游戏不会这样推进。测试器暂停时停止模拟；保留暂停期间信标与第二次右键检查，通过后继续测试。
5. 第4、5次：斜索端点检查的模拟步数过少，增加等待步数；右侧水车上升撞到原稿终点平台底部，是真实地图几何问题。
6. 第6次：增加可见顶部交接吊篮和安全锚点，先从平台左侧绕行再向上、向右落到平台。原踏板仍随轮转动，继续向上推挤交接中的乘客导致失败。
7. 第7次：交接期间仅暂时忽略乘客与其原水车踏板的一对碰撞，保留全部地形碰撞；交接每步用完整身体 sweep 和 overlap 检查；完成/跳离/冲刺/传送/死亡均恢复该碰撞。两侧真实运送均通过。
8. 第8、9次：补充相机离屏截图和运行时滑索绿线同步，重新构建/验证通过。隐藏 Windows 窗口的普通 ScreenCapture 图片全黑（visual-qa、visual-qa-window），保留失败图片；使用实际 Camera.Render + RenderTexture 读回得到有效图（visual-qa-render）。
9. 第10次：补充“停留同一地面不无限恢复传送”及实际进入左右解锁碰撞盒验证，全部通过。所有失败构建的原始 editor/runtime 日志及 status.json 按原时间保留，未覆盖成成功结果。

## 本次实际验证结果

- 最终命令：`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\BuildAndTest.ps1 -ForestWhitebox`。
- 最终构建：`Docs/Logs/Build/20261105-151329-650/`，status PASS；editorExit=0、forestExit=0；Windows 构建 0 errors，日志无 error CS / warning CS。Unity 许可日志中的临时 access token 提示没有阻止实际构建成功。
- 本次执行：ForestEditorChecks 1840 PASS / 0 FAIL；SeasideTests 40 PASS / 0 FAIL；PlaytestRegression 203 PASS / 0 FAIL；ForestRuntimeChecks 56 PASS / 0 FAIL。范围和计数见 test-summary.json。
- 测试涵盖：4/8跳跃与8格冲刺、独立能力次数、信标手动/碰墙/射程触发、过窄通道安全落点、同地面不反复恢复、暂停/恢复、黑墙恢复/灰墙不可攀附、W抓索和双能力恢复、斜索上行及反向下行、跳离/重抓冷却/端点释放、复活信标清理、真实荆棘死亡、瀑布双向全身跨越、全图遮挡、两侧实际触发器解锁、重复解锁去重、未解锁终点无效、左右踏板真实运送/顶部落点/步入终点、完全重开复位。
- Build 脚本会快照 Docs 中其他已有 TXT/CSV；art-preview、hierarchy、standalone-calibration 等报告只是沿用的历史文件，本次没有执行这些旧报告对应的运行检查，不计入此次通过数。
- 原有21项场景/设置/地图及场景列表 SHA256 保持一致，见 protected-before.json、protected-after.json。SeasideMotor 原 Tick 方法文本与工作前备份逐字一致；新增可选接口单独列出。未修改运行时共享 Movement.asset、海边地图手工编辑、原启动入口和旧试玩。
- 画面验证：最终二进制以 `-forestCapture <目录>`、D3D11、1440×900 启动并退出码0；最终截图在 visual-qa-final，已检查对应的出生画面、左侧/右侧全图。来源诊断图与实际画面对照，地形/荆棘/索段、水车及瀑布遮挡正确。相机离屏图不含 IMGUI HUD，不能作为 HUD 视觉验收。
- `git diff --check` 无补丁空白错误；Git 提示的 CRLF/LF 规范化为既有仓库换行规则，不属于编译或玩法失败。

## 限制与回滚

- **没有进行真人从出生点完整走完左右两侧路线的通关测试**。自动机制与从踏板到真实终点的验证不等同于全地图路线可达性、难度和手感验收。
- 当前只制作白盒。未贴森林美术；不把原稿图片当作可靠碰撞；灰墙沿用海边 attachable=false（禁止贴墙悬停，不另改原墙跳规则）。原稿15条滑索均水平，斜索上行已用物理测试夹具验证。
- 普通复活保留水车进度，完全重开才清除，不持久化跨次启动进度。顶部交接采用可见绕行吊篮，保留原终点平台几何。
- 现有 Docs/CHANGELOG.md 的11月2日与11月4日未提交变更、对应已有工作日志保留；没有提交、推送、发布或关闭用户程序。
- 备份目录保存修改前 motor、BuildAndTest/PackageRelease、README、CHANGELOG、工程导览及旧根报告。撤销时先保留后续手工修改，再针对性恢复这些文件并移除本次新增 Forest 文件及独立试玩；海边场景和动作参数不需要回滚。
- 交付包验证结果在本工作目录 packaging-verification.json；Windows/工程 ZIP 的原始打包清单保存在 Docs/Logs/Packaging 对应时间目录。
