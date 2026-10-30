# 2026-10-29～30：SeasideArtPreview命名与层级重构

## 用户需求与决定

用户已在当前SeasideArtPreview创建Part1Art并放入bg3，授权规范命名、重构不合理挂载关系，并修改受影响脚本，可自行决定是否另存场景。

本次原地整理Project6.4/Assets/Scenes/SeasideArtPreview.unity，保留文件名/GUID，避免产生多个不清楚用途的平行预览场景。只改6.4副本；Project/的6.6源文件和旧Play交付保留。Unity内置组件类型、C#脚本类及素材文件不作无意义改名。

## 开始前保护

使用computer-use技能检查实际窗口，确认Project6.4的SeasideArtPreview有未保存标记，Play未运行，Hierarchy内存在用户新增Part1Art/bg3，旧的整图美术对象已被用户移除。通过Ctrl+S保存用户工作，确认未保存标记消失后备份。

备份位于Docs/Backups/20261029-hierarchy-refactor/，包含重构前Project6.4、根AGENTS、CHANGELOG等。初次Copy-Item的Exclude未正确排除深层缓存，因而额外复制Library等；若干被占用Temp文件复制失败，相关缓存不作为备份必需内容。Assets、Packages、ProjectSettings、Tools以及用户刚保存的场景副本均已保存；回滚不需要缓存。备份Scene中包含Part1Art/bg3，并非最初整图预览版本。

之后窗口枚举未再发现该Unity编辑器窗口，在没有该工程编辑器锁的状态下使用安装的Unity6000.4.12f1批处理执行迁移，没有强行终止用户编辑器。CIM初次在受限环境读取进程信息被拒绝，实际窗口以computer-use返回结果为准。

## 命名与层级

创建Seaside根，按Gameplay、Systems、Art分类。Map保留Terrain、Hazards、Checkpoints、RouteGuides，终点逻辑命名FinishTrigger，起终点文字归入Markers。Player与Map同属Gameplay但不挂到Map之下，保证测试停用地图时角色仍可运行。GameSession、MainCamera归入Systems，Tag和所有参数保持。

Part1Art改为Art/Part1，加入SeasideArtRoot标识；bg3改为Backgrounds/Background_Far，Sprite引用、材质、颜色、位置、缩放和导入设置保留。没有重新导入整套part1、恢复用户已删除的合成图、调整图片配准或变更地图布局。

地图模块按类型与原Excel地址命名，检查点含编号，复活锚点保留在对应检查点下并统一名RespawnAnchor。路线保留在RouteDisplay控制的分支。全路径清单hierarchy.txt、426个原有对象路径映射rename-map.tsv，使用说明Docs/SceneHierarchy.md。

## 脚本与工具修正

- 新增SeasideMapRoot：重构场景按组件标识查找Map；原Seaside01通过SeasideCell共同祖先回退定位，不依赖显示名称。没有为兼容新名字而改写原关卡对象。
- CameraPreview、TrailVisualPreview移除固定GameObject.Find字符串，使用场景内地图定位，并记录SEASIDE_PREVIEW_MAP_HIDDEN。只有显式启动这些测试模式才运行。
- 新增SeasideArtRoot标记当前分层美术，SeasideArtSetup检查同时支持旧合成图组件和新的分层根，不再要求用户删掉的整图必须存在。
- 新增SeasideSceneGeometry，对比Excel身份、类型、世界位置/缩放/旋转、世界碰撞角点、材质物理参数、层、活动状态、攀附属性及检查点/复活点；忽略名称、父路径，四位小数用于浮点容差。坐标或玩法属性变化仍会失败。
- 新增SeasideHierarchyRefactor一次性迁移菜单及检查/实际渲染入口。迁移使用Undo并保留世界变换；重复运行会拒绝覆盖已经整理的场景。
- 发现Project6.4/Tools复制自6.6，原脚本仍固定Demo/Project及6.6编辑器。改为当前Tools的工程根及安装的6.4，输出Play64，新增StartGame64/ArtPreview64/CameraPreview64入口，避免覆盖旧Play。
- 构建旧代码会强制设置动作参数、保存原关卡，首轮观察到原Seaside01仅因6.4序列化重写发生文本变化。恢复其备份，移除构建时重设玩法/保存场景的副作用，最终再次构建后原Seaside01仍逐字节一致；Movement.asset也保持备份哈希一致。
- 第二轮编辑器已成功退出，但Start-Process -Wait等待后台后代进程而卡住；中断该次等待，保留日志/状态，不算完整成功。修正为保持进程句柄并只调用该进程WaitForExit，重新执行完整流程通过。
- 6.4打包脚本按当前工程名打包，产物名带Unity64-Hierarchy，包含新入口和README-Unity64，不混入旧Project6.6。根AGENTS与.gitignore记录当前工程和Play64。
- 会话标识更新为unity64-hierarchy-refactor-20261030，保留原玩法版本标识。
- 重新打开编辑器后首次打包无法以默认共享方式读取正在写入的editor-reopen.log，保留该轮不完整压缩包，未复制为交付产物。打包改为FileShare.ReadWrite只读流并限制为打开时的文件长度，取得该时刻快照；不修改原日志。

## 已验证结果

1. editor-refactor.log退出0，refactor-result.txt记录PASS。426个原有对象的世界变换（四位小数）、Tag/Layer、有效活动状态、原组件序列化数据/引用前后完全相同；before-world-state.txt与after-world-state.txt SHA256相同。没有重新生成地图。
2. 新旧场景几何对比通过；地图组件定位在任意改名后仍返回同一对象，旧场景的无标记回退也通过；已有bg3仍存在。hierarchy-checks.txt有5项PASS，art-preview-checks.txt有4项PASS。
3. 首轮完整构建20261030-100351-771通过。中间20261030-101648-240因等待后台后代进程被中断，编辑器原始日志成功不等于该完整流程完成；保留该轮记录。
4. 构建20261030-102227-342通过；额外预览运行暴露原镜头用例默认跟随模式的前提，在保存为全图模式的预览中该断言失败。保留preview-regression-first-failure.log与preview-runtime-regression-before-camera-fixture-fix.txt；只修改测试以明确进入跟随模式再恢复，不改场景相机设置。后续重建通过。
5. 残像预览本身在约一秒完成并主动退出，因此不能要求它必须输出三秒的SEASIDE_SMOKE_PASS；首轮用例误将这种正常退出判成证据缺失，保留original-trail-first-harness-mismatch.log。增加明确的SEASIDE_TRAIL_PREVIEW_FINISHED并按测试模式分别检查。还发现旧残像截图硬编码写进20261020历史Work，现改成VisualPreview独立时间戳路径；将此次生成的未验收旧路径截图移到本工作目录，若game1有原历史图则从未改原件恢复。最终重建20261030-104306-496使用6000.4.12f1，editorExit、regressionExit、calibrationExit均为0，status.json为PASS。40基础、203编辑器、159 Windows回归、7校准、5层级与4美术检查，共418项PASS，无FAIL。以该轮快照为最终构建验收。
6. editor-render.log中SEASIDE_REFACTOR_CAPTURE_PASS；已直接查看refactored-scene.png，确认bg3、地形、尖刺、检查点与路线正常渲染。该图为编辑器Camera.Render输出，不包含OnGUI HUD，不冒充键盘全图通关。
7. 额外运行新预览场景的Windows回归/校准，以及新旧场景镜头/残像预览冒烟；实际退出码与证据以preview-runtime-status.json为准，报告快照在本目录，最终汇总见verification-summary.json。无图形设备的残像截图不作为视觉验收图片。
8. 额外六个运行用例最终均退出0；预览场景159回归与7校准通过，两个场景均成功通过组件定位隐藏地图并完成镜头/残像预览。已重新用6.4打开原SeasideArtPreview，窗口标题无未保存星号，直接核对Scene外观及Seaside/Gameplay/Systems/Art层级。现有Input Manager弃用提示未在本次改动范围内处理。

## 交付、限制与回滚

继续打开原Project6.4/Assets/Scenes/SeasideArtPreview.unity。试玩入口ArtPreview64.cmd，原关卡入口StartGame64.cmd。新工程包/试玩包由Project6.4/Tools/PackageRelease.ps1生成，条目清单在Docs/Logs/Packaging/时间戳目录。

仍未人工全关通关，未补齐所有part1图层或调整美术/碰撞配准；Unity关于旧对象查找API等已有弃用警告未作为本次层级任务全部改写。没有修改Tag/Layer、动作距离、地图JSON、检查点逻辑或用户美术位置。未提交/推送Git。

回滚前保存之后的新编辑，再从本次Backups/Project6.4恢复对应场景、脚本、Tools，并恢复根AGENTS/CHANGELOG；不要恢复Library/Temp等缓存。建议场景与识别组件/校验脚本一起恢复，以免缺脚本。新Play64与64后缀入口独立于旧Play，原交付仍可运行。
