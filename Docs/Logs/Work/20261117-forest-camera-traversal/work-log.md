# 森林镜头、滑索、传送与方向箭头

日期：2026-11-17（Asia/Shanghai）。主工程：Demo/Project6.4，Unity 6000.4.12f1。

## 用户需求与实现

1. 实际向右步行累计3单位后平滑展示全图，包含完整水车与地图两侧；完全到位后停留3秒，再平滑恢复跟随。Forest.asset暴露触发距离、缩放速度和停留时间。默认速度0.5进度/秒，单程约2秒。展示期间临时禁用瀑布远侧黑布，结束恢复原规则。镜头在原跟随/瀑布更新后叠加；暂停重新应用当前展示画面但不推进计时，避免被原跟随相机覆盖。普通复活取消已开始的展示，触发前死亡不丢失首次展示；Ctrl+R/通关重开重新启用。
2. 滑索骑乘中A/D可实时改变沿索方向，松开保留当前方向，保留斜索上行。
3. ziplineCatchDistance由1.6扩大为2.1，ziplineContactPadding由0.25扩大为0.55；目标身体空间和移动碰撞检查继续生效。用户已有ziplineSpeed=12保留。
4. 滑到端点自动脱离后提供ziplineCoyoteTime=0.18秒的一次补跳，沿用原长跳/短跳动力；超时、起跳、冲刺、传送、重新抓索、触地、死亡或复活清理窗口。
5. 保留信标飞行滞空，成功传送后额外增加teleportHoverDuration=1秒；设为0可关闭。保留A/D和碰撞，冲刺、有效地面/墙面跳跃、抓索可提前退出；不赠送空中跳跃或额外传送次数。暂停保留时间，死亡/复活/取消/禁用清理。
6. 角色身体外围显示金色箭头及深色描边，按同一瞄准方向显示冲刺/信标方向；根据身体盒计算偏移，避免斜向箭头落入身体内，边距0.35、长度0.8可调。运行时创建LineRenderer并复用点数组，销毁时释放材质。

ForestLevel/ForestPlayer运行时创建镜头及箭头组件，无需改写场景。原场景及.meta、手工地图、SeasideMotor.cs、Movement.asset均保持修改前哈希。保留仓库已有未提交改动，没有提交或推送。

## 文件与参数

修改：Assets/Scripts/ForestPlayer.cs、ForestLevel.cs、ForestWaterfall.cs、ForestSettings.cs、ForestHUD.cs、ForestRuntimeChecks.cs、Assets/Settings/Forest.asset。

新增：Assets/Scripts/ForestIntroCamera.cs及.meta、ForestAimIndicator.cs及.meta。

所有新参数位于Project6.4/Assets/Settings/Forest.asset，中文Inspector名称与默认值见Docs/ForestWhitebox.md的“2026-11-17 参数入口”。HUD与帮助已同步操作说明。

## 备份、验证与实际结果

修改前备份：Docs/Backups/20261117-forest-camera-traversal/，包含受影响原源码、设置资源、文档、Play64/Forest、两份标准ZIP、“森林独立试玩.zip”以及两个已有解压目录。原有工作差异保存在preexisting.diff。

未关闭、保存或重载用户Unity编辑器；验证使用备份目录内Validation/Project6.4隔离副本，复用先前验证缓存并覆盖当前工程资源。构建入口始终使用Tools/BuildAndTest.ps1 -ForestWhitebox。

本次三轮原始构建日志均保留在Docs/Logs/Build：

- 20261117-230050-998：首版构建及220项森林运行检查PASS。
- 20261117-230512-295：补充镜头速度与重开检查，224项PASS。
- 20261117-230611-657：最终版，修正暂停时原跟随相机覆盖展示姿态，并复用箭头顶点数组；editorExit=0、forestExit=0、result=PASS。

最终验证：森林场景1752项、海边基础40项、原有回归203项、独立Windows EXE森林运行224项，合计2219项，全部PASS。构建0错误，无C# error CS/warning CS。运行检查驱动真实Physics2D及场景组件，包含双向滑索换向、两端土狼跳与超时/不可双跳、扩大范围真实抓索、传送后手动/射程/撞墙三路径一秒滞空、可调/零时长/暂停/死亡清理、完整开场行走触发/三秒停留/恢复/速度/重开、箭头与实际冲刺/信标一致性。

使用最终EXE以D3D11启动并执行-forestCapture，退出码0。Visual-final/记录最终相机直接渲染：方向箭头、拉远中间帧、无遮罩完整全图、瀑布两侧原有近景及Tab全景。已逐图检查中间帧、完整全图及方向箭头，完整地图和水车未被裁切。Camera.Render截图不包含OnGUI，镜头为HUD预留上下边距并有运行断言。最初Visual/中的中间截图被正常LateUpdate覆盖，已修正截图时机并重新验证；保留初次图与日志作为历史，最终以Visual-final/为准。

protected-after.json确认五个关键受保护文件哈希不变；source-validation.json确认11个变更/新增工程文件与最终构建副本一致。

## 交付与限制

新试玩同步至Play64/Forest，入口仍为Demo/forest.cmd。使用Tools/PackageRelease.ps1 -ForestWhitebox更新标准Windows试玩ZIP与工程ZIP；“森林独立试玩.zip”和已有两个解压试玩目录同步最终版本，具体ZIP条目与关键文件哈希见packaging-verification.json。此前成品已备份。没有进行人工全路线通关。

回滚时仅恢复本次备份内被修改文件、文档及森林试玩，并移除本轮新增ForestIntroCamera/ForestAimIndicator及其.meta；不要重置整个工程、地图或用户其他改动。隔离Validation工程保留以便复查。
