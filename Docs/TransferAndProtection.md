# 区域传送界面与防护光效

传送点按 E 打开新增 transfer 美术界面。可传送区域依次为海边、森林、村庄、矿洞、雪山；未探索区域暗显且不可点击，当前区域发光且不可重复传送。鼠标点击区域名传送，也可用方向键选择、回车确认。“继续游戏”或 Esc 关闭界面。传送仍抵达目标地图的传送标志旁安全地面，并沿用黑屏转场。

灯塔不出现在区域传送菜单中；即使探索过也不能从这个菜单进入，灯塔内部也不能打开区域传送。原有雪山选择继续、解锁海边左边缘和 lighthouse 交互入口的剧情条件保留。

在村庄机器人处取得核心后，角色身后出现柔和光圈：防寒蓝色，隔热红色。雪山与灯塔按 S 切换时光圈同步换色，死亡复活保留选中状态；回访其他区域时显示已保存的模式颜色。没有核心时不显示，死亡等待和区域转场中暂时隐藏。光圈仅为状态提示，不影响碰撞、矿洞照明范围或能力数值。

运行时角色上的 `ProtectionAura` 组件可调 `防护光效直径`、`防护光效亮度`、`Cold Color`、`Heat Color`。默认直径 5.5、亮度 0.85。它由 `SeasideSession.Start` 自动安装；希望永久改默认值时修改 `Project6.4/Assets/Scripts/ProtectionAura.cs` 字段初始值。

传送布局：`Project6.4/Assets/Scripts/TransferArtwork.cs`。运行素材：`Project6.4/Assets/Art/Resources/Transfer/`，源稿中与正式资源相同的副本已清理，替代路径见 `Assets/README.md`。运行路线图仅截去雪山右侧通往灯塔的部分，五个节点居中；未裁剪原路线仍保留在根 `Assets/transfer/transfer/transfer_line.png`，原布局保留为正式资源中的 `transfer-source-layout.json.txt`。光圈：`Project6.4/Assets/Art/Resources/Effects/ProtectionAura.png`。

