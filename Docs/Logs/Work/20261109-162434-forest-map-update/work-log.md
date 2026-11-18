# 森林地图布局更新

日期：2026-11-09（Asia/Shanghai）

## 需求与范围

用户更新Demo/maps/森林地图.xlsx，要求同步当前游戏地图。沿用此前“原scene中修改，不创建新scene”的要求。用户本次明确授权工作簿布局更新，因此只增量更新来源变化的对象，不调用ForestBuilder.CreateScene、不清空重建整张地图。使用spreadsheets技能只读解析OOXML填色和绘图锚点，原Excel不写入。

## 来源与差异

旧来源SHA256 e684aa5c696d7299de07e1a30ad7b329e82058d162c5220f1fdb7ba725e56c0e
新版SHA256 aba7648201d440e96b2bdc52fab1499900ffe7e4f5e4829b0cb86e7bbb59d3c1

地图区AB24:MB129，313×106，1格=1Unity单位，传送标尺10格均未变。水车、瀑布3个drawing锚点及两组解锁块均未变化。985个地图来源格子改变（不含图例）。黑地形4365→4400，灰地形70不变，荆棘697→620，滑索316→361格/15→13条，路线52→40格/13→10组。逐地址差异见map-diff.json，来源统计和原稿诊断图见map-import/。

## 实现

- 新增Assets/Editor/ForestMapRevision.cs及.meta：显式batch-only差异补丁。读取备份旧ForestMap.json与新版Resource，只在原ForestWhitebox.unity内增删受影响的地形行块、荆棘、路线和滑索；未变化来源对象保留原序列化ID和人工调整。荆棘朝向随新地形邻接重算。新滑索复用原组件/材质及抓取机制，更新端点、线和把手，并同步ForestPlayer引用。补丁校验坐标框/绘图锚点/旧来源哈希，不支持误覆盖其他来源，验证成功才保存。
- 原Resources/ForestMap.json及ForestLevel.sourceSha256同步新版来源。Importer只在隔离副本运行，不修改原Excel。
- 5个原复活点全部通过静态安全检查，位置及其组件/锚点保持。原出生点(3,5.2)、角色预览位置、终点、水车/瀑布/解锁位置及参数保持。当前forceRotation=false、followSize=18保持。
- ForestEditorChecks从旧计数断言改为当前来源逐格实际碰撞/荆棘触发器/路线渲染覆盖/滑索端点与线的检查，防止仅改统计但遗漏对象、错误碰撞或残留旧滑索。
- ForestRuntimeChecks增加真实保存的13条滑索逐条抓取/滑行，共增加26项；保留原水车/终点/瀑布/复活点回归。增加UNITY_EDITOR条件下batch Play Mode退出，正常游戏行为不变。仅命令行-forestChecks激活验证。
- ForestMapRevision包含显式RunPlayModeChecks和Capture入口，供batch编辑器验证与只读渲染，不在日常Play或导入时自动执行。
- Docs/ForestWhitebox.md与CHANGELOG同步当前统计、更新方式和验证限制。

## 执行与保护

修改前Assets、Tools、ProjectSettings、Excel、文档、试玩和旧双ZIP备份至 `D:\UnityProjects\Demo\Docs\Backups\20261109-162434-forest-map-update`。隔离验证工程在该目录下Validation/Project6.4。开始及收尾均无打开原工程的Unity编辑器进程。原工程场景同步前与工作前备份哈希一致，未保存任何用户编辑器内存状态。更新完成复制同一个ForestWhitebox.unity回原路径，不改.meta/GUID。

scene-preservation.json：4727个现有序列化块不变；13个原块改变（来源哈希、滑索引用、地形/荆棘/路线/滑索父级子引用及受新地形影响的荆棘朝向）。1717个过期布局对象相关块删除、1191个新布局相关块加入。保护的11个水车/瀑布/终点/相机/运动/会话/检查点组件原块完全不变，角色原Transform不变。其余海边场景/资源/动作与参数、地图原文件和BuildSettings共242项哈希不变，见protected-before/after.json。16个森林运行/编辑器源码与实际验证副本一致，见source-validation.json。

## 实际验证、失败与限制

1. scene-update-attempt-1.log：删除多个旧路线对象时，临时SpriteRenderer数组包含先前已删除对象，查找name时MissingReferenceException。发生在隔离副本，未保存失败场景；查找加Unity对象有效性判断。第2次补丁成功，同一原场景保存前ForestEditorChecks全通过。日志均保留。
2. Tools/BuildAndTest.ps1 -ForestWhitebox：第一次构建日志20261109-163025-279。Unity编辑器检查/海边测试/Windows构建均成功（FOREST_BUILD_PASS errors=0，editorExit=0），独立EXE启动被本机Windows Application Control拒绝。CodeIntegrity事件3077/3033明确记录未满足企业签名要求/策略。未修改系统策略或绕过拦截。脚本整体status=FAIL源于独立进程未启动，不是地图检查或编译失败。
3. 同一隔离保存场景在Unity编辑器Play Mode运行实际ForestRuntimeChecks，132 PASS/0 FAIL，进程退出0。包含13条真实新版滑索逐条抓取/前行及原信标、顺时针/有限摩擦/跳后重落、普通跳向终点、0/1/2解锁终点、近景镜头/全景黑布整身换侧、5复活点的实际物理验证。报告editor-playmode-checks.txt与editor-playmode-attempt-1.log。此结果不声称为独立EXE运行。
4. 增加仅编辑器生效的退出分支后，用同一Tools/BuildAndTest.ps1重新构建最终源码，日志20261109-163426-678，editorExit=0且Windows BuildSucceeded/errors=0；独立启动仍被相同系统策略拒绝，status=FAIL保留。最终实际场景检查1747 PASS、海边基础40 PASS/回归203 PASS。未发现C# error CS/warning CS。两次构建日志均同步至正常Docs/Logs/Build，不覆盖失败报告。
5. Unity编辑器D3D11直接Camera.Render/RenderTexture读取原保存场景，visual-qa/输出1565×610两侧全景及近景图，进程退出0。已逐图查看，对照原稿确认新布局、全景黑布与近景瀑布边界正确。此为编辑器保存场景渲染，不是独立成品或IMGUI HUD截屏。不更改或保存截图期间临时相机/角色状态。

本轮未完成独立程序启动验收（系统策略阻止），未进行真人完整路线通关，不能把编辑器机制检查等同全路线难度/可达性验收。明确记录实际执行宿主和结果，见test-summary.json。

## 交付

同步更新同一原森林场景、ForestMap.json及Play64/Forest，原forest.cmd继续启动该成品。森林双ZIP使用Tools/PackageRelease.ps1 -ForestWhitebox更新，旧ZIP在工作前备份与Packaging历史保留。最终CRC及场景/源码/程序集/Excel/文档哈希核对见packaging-verification.json。新构建在本机启动仍可能受上述系统策略限制。

## 回滚

从本次Backups恢复ForestWhitebox.unity、ForestMap.json、ForestEditorChecks.cs、ForestRuntimeChecks.cs、两份文档、Play64/Forest和旧双ZIP；仅移除本次新增ForestMapRevision.cs及.meta。新Excel是用户提供的输入，不应恢复成旧地图；其他场景/动作参数无需回滚。保留此前未提交工作，不重置全仓库，未提交/推送/发布。
