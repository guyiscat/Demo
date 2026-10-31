# bg3 背景边界与镜头

2026-10-30：按用户指定，以 Background_Far 使用的 bg3.png 为可见范围。用于 Game 窗口及构建后的游戏；Scene 编辑窗口仍可看到界外资源。

## 使用

在 Seaside/Systems/MainCamera 的 Seaside Camera 组件中：

- Constrain To Background：启用背景边界限制，默认开启。
- Background Boundary：可拖入 Background_Far 的 Sprite Renderer；留空时运行时自动查找同场景 SeasideArtRoot 下使用 bg3 的对象，不要求固定的对象显示名称。
- bg3 保持轴对齐；边界来自 Sprite Renderer 的实际世界矩形，会随图片的位置及缩放更新，不按图片透明轮廓裁切。
- Follow Size 继续控制普通跟随视野大小。镜头接近背景边缘时停止越界，不要求玩家始终居中。
- 2026-10-31 已将两个已保存场景的 Follow Size 从10调整为12；普通视野高度由20变24，16:9时宽度约由35.56变42.67。背景边界处仍按实际可容纳范围限制。
- Tab 全图显示完整背景；长宽比不一致时使用黑边，缩放过渡、暂停和窗口比例变化时也限制实际视野。
- 若目标视野比背景还大，自动缩小有效显示区域并补黑边；不会为了铺满屏幕显示界外资源。
- 没有 bg3 的 Seaside01 场景继续使用原有相机逻辑。取消 Constrain To Background 可关闭此功能。

源码在 SeasideCamera.cs、BackgroundCameraFrame.cs；CameraMotion.cs 的原跟随平滑算法未修改。镜头边界不是角色碰撞墙，不会阻止角色移动到图外；背景范围内的装饰仍会正常显示。

## 验证与构建

手动调节：停止Play，在Hierarchy选中Seaside/Systems/MainCamera，展开Seaside Camera → Tuning → Follow Size。数值越大，镜头越远、角色看起来越小；12为本次设置，14会更远。修改后保存场景；在Play中试调的值退出后通常会还原，需要在停止后再填写并保存。Seaside01中的相机对象名是Main Camera。

不要只改上方原生Camera组件的Size：运行时会被SeasideCamera按Follow Size重新设置。该参数作用于普通跟随，不改变Tab全图的背景适配；全图中按Tab返回跟随观察效果。

CameraBackgroundBoundsChecks 仅允许批处理执行，会打开场景，应在隔离副本运行。覆盖五种屏幕比例、边角及界外目标、平移/缩放/极小背景、Tab 过渡、暂停、关闭功能、无 bg3 场景，以及实际相机投影与黑边渲染。

现有 BuildAndTest 的历史几何检查仍要求 SeasideArtPreview 和 Seaside01 完全一致。当前用户已独立编辑预览地图，该检查会失败；本次保留失败记录，没有改图或放宽该检查。新增专项入口 CameraBackgroundBoundsChecks.RunAndBuildSavedScenes 在隔离副本验证镜头后，直接构建两份已保存场景；它不代表旧地图一致性检查通过。

完整结果、首轮失败、渲染截图、备份及回滚方法见 [本次日志](Logs/Work/20261030-222238-bg3-camera-bounds/work-log.md)。构建不包含编辑器内尚未保存的场景修改。
