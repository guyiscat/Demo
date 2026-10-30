# 2026-10-30：说明背景图层选中时的不规则轮廓

用户询问Background_Distant和Background_Midground的用途与点击后出现的不规则轮廓。

核对实际布局记录：Distant引用bg2，排序-90；Midground引用bg1，排序-80。两份Sprite导入meta均为Single/Full Rect、PPU50、不生成fallback物理形状，默认平台Max8192。源图此前已直接查看，为含透明区的远景建筑/岩体合并图层，不是独立可玩平台。

通过computer-use只读查看当前Unity窗口：用户选中Background_Midground，Inspector的Sprite为bg1，Scene中出现沿背景形状描绘的橙色选中轮廓。此时窗口正在进入Play/Reloading Domain，没有进行点击、停止Play、改对象、保存或退出等操作。

判断本次看到的是编辑器选中高亮，随Sprite的不透明区域形状显示；不是新增碰撞，也不表示脚本报错。Full Rect是渲染网格设置，不能据此认为编辑器选中描边必定只能是矩形。取消选中或查看Game窗口可区分辅助描边和图片实际内容；如果黑色块在未选中/Game中仍存在，则属于图片像素或层间遮挡，需要另行查看，不与选中描边混为一谈。

仅新增本记录与CHANGELOG条目，原总表备份于Docs/Backups/20261030-background-outline-explanation/。没有修改工程、运行构建或替换交付包。
