# 村庄美术

`Village.unity` 的地面和全部机制保持原位；以白盒为准接入新增 part3 三层美术。远景村屋、拱桥与山峦在后方，近景地面复用原图沙土纹理，矿洞坑口留出实际开口并增加暗色井道，机器人和传送点仍可见、可交互。三层画面按宽高比排布，不把参考整图当碰撞轮廓。

运行资源位于 `Project6.4/Assets/Art/Resources/VillageArt/`，布局 `part3-layout.json.txt`，原始导出与导入 .meta 保留。整体参考图在根 `Assets/village/village.png`（根 `Assets/村庄.png` 为相同重复图，已清理）。

`Assets/Scripts/village/VillageArtwork.cs` 在进入村庄时由 SeasideSession 自动挂载。它按保存的 BoxCollider2D 尺寸铺设地面并给实际矿洞出口加视觉井道，不新增碰撞或出口。改画面间距、远近景尺寸、排序时编辑该脚本；修改玩法形状在 Village 场景操作相应地面/触发器。

本轮同步精简 H／Esc 帮助，删除“当前区域”页，只显示基本操作；场景中的剧情对白与教程触发器保留。

![村庄当前全景](Images/village-overview.jpg)

![基本帮助](Images/help-basic.jpg)
