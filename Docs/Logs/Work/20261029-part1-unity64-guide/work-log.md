# 2026-10-29：在已打开的6.4场景中使用part1素材的操作说明

用户询问如何在打开.unity场景后使用Figures中的partr1文件。本次核对实际目录为Demo/Figures/part1，20张PNG与part1.json.ps布局数据仍存在，布局复核另存layout-reviewed.json。

读取Project6.4的manifest，当前已为原6.4六项模块；ProjectVersion为6000.4.12f1。Editor.log显示Seaside01场景已加载。未通过屏幕工具检查实时UI，不声称看到了当前界面。

提供手动操作顺序：先将Seaside01另存为独立预览，将PNG复制到当前工程Project6.4/Assets/Art/SeasidePart1；以Single Sprite导入，设置PPU50、Full Rect、大图Max Size8192、Bilinear、无Mip Maps；在无变换的独立美术父对象中先加入bg3、Default层Order=-100，保留碰撞和玩法对象。

背景初始摆放示例基于已核对的bg3原始6438×3163像素和地图155×73：PPU50、无尺寸缩减时，统一缩放155*50/6438=约1.204，中心(77.5,36.5,0)，可覆盖地图逻辑范围。这只是背景覆盖示例，不是证明其他图层或地形已经对齐。物体父级应保持位置0/旋转0/缩放1，避免局部坐标混淆。

强调Figures是工程Assets外的源素材，不会自动出现在Unity Project面板；导入文件夹与把Sprite放入场景是两步。part1.json.ps不会自动运行或还原层次，需专门导入工具或按布局矩形换算位置。所有PNG已分文件，应使用Single而非Multiple来避免误切；bg0内部地形仍需后续处理。保留现有Collider与触发器，不把bg0或water直接变成碰撞。

参考Unity官方Sprite导入与Sorting Order说明：
- https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/sprite/import-images-sprites
- https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/textures-reference/texture-type-sprite
- https://docs.unity3d.com/ja/current/ScriptReference/Renderer-sortingOrder.html

本次只读检查并给出操作说明，没有导入/复制PNG到Assets、创建或修改场景、编写导入插件、构建或游戏测试。只新增日志资料并追加CHANGELOG，原总表备份在Docs/Backups/20261029-part1-unity64-guide/。用户手动操作后需保存场景，再观察Game与Play效果。
