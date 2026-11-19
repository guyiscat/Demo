# 森林地图更新、矿洞独立关卡与公共字体

需求：按maps/森林地图.xlsx更新已有森林关卡；脚本专属目录第一关/第二关改为seaside/forest并保留引用；森林传送可打断冲刺；按矿洞地图.xlsx制作独立第三关；所有系统字体改为根Assets/AlibabaPuHuiTi-3-55-Regular.otf；识别IMG_2979.psd和IMG_2981.psd。

修改前备份：Docs/Backups/20261119-forest-mine-font/，含主工程Assets、Tools、ProjectSettings、Play64、已有ZIP、相关文档及原有差异。maps/森林地图.xlsx的用户改动保留，所有源工作簿只读。主工程Unity已打开，不关闭、保存或重载用户未保存场景。地图编辑及构建在隔离副本进行，最终仅同步本轮结果。

上一项墙面问题被本次新任务打断，尚未更改Seaside_Terrain_Modules_13的attachable；不将其诊断计划算作已完成修复。

## 实际实施

森林按旧ForestMap.json与当前工作簿差异更新原ForestWhitebox.unity，未调用场景重建：43格改变，黑地形4400→4431、尖刺620→601，新增/替换6条地形行块，保留水车、瀑布、6个检查点及未变对象。当前磁盘源SHA256为720838dc5a33b6ef79c9948c28d20cfe244e1a203703b31f50510a1044d064a6。绿色滑索仍13条/361格，所有滑索地址与旧来源相同，故未猜删；已向用户提示Excel可能还有未保存改动，多次重新读取磁盘哈希均相同。变更列表forest-map-changes.txt，来源诊断ForestImport/。

传送打断冲刺原本可结束Dashing但会继承水平冲刺速度。本轮在发射时清零残余冲刺速度，优先进入信标滞空，保留A/D、充能和安全落点规则。新增ForestSettings.teleportInterruptsDash默认true，Forest.asset显式开启，关闭时右键不会打断进行中的冲刺或消耗传送。15组方向/冲刺阶段组合验证取消和后续手动传送。保留用户当前Forest.asset的teleportHoverDuration=0.5与ziplineSpeed=12；矿洞沿用这两个值。原Movement.asset未改。

Scripts和Editor下第一关/第二关目录及文件夹.meta改名为seaside/forest，新增mine目录。矿洞复用的ForestPlayer、ForestSettings、ForestZipline、ForestAimIndicator和ForestMapData移回Scripts公共根目录，保留历史类型名及命名空间。69个原C#及其GUID全部保留，详细路径迁移见script-guids.json。脚本不新增按目录字符串加载；原菜单、executeMethod和场景组件引用继续有效。

新增GameTypography和GameSystems.systemFont，引用Resources/Fonts/AlibabaPuHuiTi-Regular.otf，字节与根Assets原字体一致。GameUIStyles不再创建/销毁操作系统字体，HUD/帮助/暂停/通关/交互/菜单系统面板和场景TextMesh共用此字体；现有预览提示同样更新。未来关卡使用公共GameHUD/GameUIStyles与GameTypography入口即可沿用，主题图片中的字不重绘。

矿洞按maps/矿洞地图.xlsx首次建立MineWhitebox.unity，170×272，5546黑地形、580灰地形、585尖刺、12条滑索/230绿格。新MineLevel/MineHUD/MineSettings/MineLift/MineInteraction及编辑器、实际EXE检查。沿用共用动作、镜头、箭头和深绿面板/浅绿白盒背景。工作台、重启开关、井底比特币文案按根Assets/文案.pdf第三页实现，E逐页交互。橙色横条为同一真实电梯的两站(86,254.5)/(86,161.5)，开关激活后往返，速度8、停留2秒可调，可走跳/传送脱离，不绑定角色父对象。MovingSurface提取公共移动地面支撑，原水车平台继承该类，矿洞电梯复用相同物理接触。

两处紫色按检查点、深蓝按路线标记、井口浅蓝按提示解释；只有井底浅蓝遗物读完才通关。用户后续确认按森林当前磁盘版本继续，并确认紫色复活点、橙色横条电梯；实现与确认一致，无需更改代码或重建。深蓝和井口浅蓝仍为无文字图例时的默认解释；未写回原工作簿。入口复活放在安全静态地面，两个检查点另设安全锚点。三个独立入口statgame.cmd、forest.cmd、mine.cmd，不修改EditorBuildSettings或增加跨关跳转。矿洞参数入口和默认解释详见Docs/MineWhitebox.md。

PSD用Pillow读取合成预览：IMG_2979为2578×1153透明图中的6个连续奔跑姿态；IMG_2981为1411×707透明图中的5个空中/落下/站立姿态。未替换角色或猜测动画帧率、切片。原PSD保留，说明Docs/ImportedAssets.md，预览在本日志目录。

## 实际验证

全部使用Tools/BuildAndTest.ps1，在Docs/Backups/20261118-shared-systems-script-folders/Validation/Project6.4隔离工程进行，主工程未关闭或自动保存。旧中文目录和五个共用文件从验证Assets移至验证根RetiredSources/RetiredSharedTraversal，避免重复编译，保留可恢复副本。

- 20261119-125121-071：最终森林editorExit=0、forestExit=0、PASS；森林场景1733项、实际EXE301项（含字体和60项打断检查）。
- 20261119-124805-490：最终矿洞editorExit=0、mineExit=0、PASS；矿洞场景3635项、实际EXE112项。
- 20261119-124902-334：海边启动页editorExit=0、startupExit=0、PASS；实际从菜单进入TheFirstScene_Seaside及公共系统38项。
- 公共保存场景检查5039项，海边基础物理40项与回归203项通过。以上数字按实际执行入口统计，其他构建目录Snapshot复制的旧报告不算该轮重新执行。
- 最终三次构建未出现C# error CS/warning CS。final-test-summary.json保留最终报告统计，原始日志在Docs/Logs/Build/。
- script-guids.json核对原69脚本；isolated-source-check.json确认主工程Assets与最终隔离构建来源逐文件一致。protected-content.json确认第一关保存场景、Movement和EditorBuildSettings与本次备份完全一致，源工作簿哈希保持，字体副本字节一致。

使用computer-use技能的sky窗口API检查本次实际EXE：矿洞入口HUD/帮助、森林HUD/帮助、海边启动主题图/设置面板及海边HUD/帮助，字体可读、无缺字和裁切。工具截图保存在会话工具记录，不把早期隐藏窗口当作有效画面。打开的试玩窗口均在检查后关闭，用户Unity场景保持未保存状态。未从出生点人工走完整条路线，自动局部物理和通关交互通过不代表路线难度验收。

失败记录保留：123714编译错误为测试直接写只读Paused，已改为与旧测试相同的反射设置；123824首次创建后内存态独立遍历引用检查失败，加载保存场景后通过，创建器改为校验重新加载的保存场景；124444森林回归固定按1秒断言，与用户0.5秒配置冲突且长等待落地后合法恢复传送，已改为按剩余配置验证，未重设用户参数。一次组合删除旧验证目录命令被自动策略拒绝，未执行，改为检查绝对路径后移动至RetiredSources，不删除源文件。PSD/PDF原文提取初次尝试fitz不可用，改用已有pdftoppm成功渲染第三页。

git diff --check仅在Unity原生保存场景新增的空序列化字段上报尾随空格，未为格式清理重写整个手工场景；源码/文档/工具检查另行通过。

## 交付与回滚

已同步三关Play64成品，使用Tools/PackageRelease.ps1分别-StartupPage/-ForestWhitebox/-MineWhitebox更新Windows与工程ZIP；工程ZIP包含三关源码、运行字体及原始Assets素材和只读地图。森林分享ZIP及两个既有解压副本沿用同一最终森林成品，新增矿洞独立试玩ZIP。最终条目/程序集/字体/来源校验以本目录delivery-verification.json的PASS为准。

备份含原试玩/ZIP、两个森林解压副本、原工程Assets/Tools/ProjectSettings/文档及本次读取的源工作簿。回滚只恢复本次修改文件、按GUID迁移表恢复目录及.meta，并移除本次新增矿洞/字体支持等文件；不整仓重置或覆盖用户之后的地图/未保存场景改动。没有提交、推送或发布。
