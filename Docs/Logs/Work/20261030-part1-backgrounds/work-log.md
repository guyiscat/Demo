# 2026-10-30：接入并规范其余part1背景图片

## 需求与范围

用户要求对part文件夹其余背景图做与bg3相同的规范命名/层级处理。按bg系列处理bg0、bg1、bg2；桥柱、植物、检查点和光效不属于此次背景系列，没有额外加入。源PNG不改名，JSON文件引用保持有效。

## 保护与实施

通过computer-use确认当前正式SeasideArtPreview场景，已退出Play；Ctrl+S保存当前状态。逐个备份Assets、Packages、ProjectSettings、Tools及Play64，未使用上次会包含缓存的整根递归复制。备份位于Docs/Backups/20261030-part1-backgrounds，另含被改文档CHANGELOG、SceneHierarchy。随后正常Alt+F4关闭编辑器，待工程主进程退出后用Unity6000.4.12f1批处理操作，结束后重新打开原场景。

新增Editor/SeasidePart1Backgrounds.cs：读取part1.json.ps，使用既有bg3的Sprite完整矩形、局部位置、旋转、缩放作为参照，换算布局逻辑矩形至场景。PNG约1.5倍的实际像素差异由Sprite实际尺寸校正，没有直接套用旧Seaside整图坐标，也没有移动bg3重新配准。

- bg2 → Art/Part1/Backgrounds/Background_Distant，Sorting Order=-90。
- bg1 → Art/Part1/Backgrounds/Background_Midground，Sorting Order=-80。
- bg0 → Art/Part1/Foreground/Terrain_Artwork，Sorting Order=-70。它是主要地形美术，独立归组；仍排在玩法白盒后方，不添加Collider。
- 原bg3 → Background_Far，保持原位置、尺寸、Renderer与Sprite引用以及导入设置。其Sorting Order=-100。

新增背景按Single/Full Rect、与bg3相同PPU、Max8192、Bilinear/Clamp/无MipMaps导入，不自动生成物理形状。原PNG内容未编辑，没有创建/生成新图片。菜单采用一次性接入：已有对应PNG对象时拒绝重复添加，不覆盖后续手调。

没有修改运行时脚本、碰撞、检查点、复活点或角色参数；现有代码按组件识别地图/美术，不依赖新增显示名称，不需要再次修改查找逻辑。

## 验证

- apply-backgrounds.log记录SEASIDE_PART1_BACKGROUNDS_PASS与编辑器退出0。
- background-checks.txt记录5项PASS：三个图片各放置一次、既有bg3 Renderer/Transform未变、玩法几何及检查点数据未变、美术无碰撞、保存成功。
- background-layout.tsv保留最终坐标、尺寸、排序。
- 实际Camera.Render生成backgrounds-preview.png并人工查看，确认三个新图层与旧bg3、白盒、尖刺和检查点正常绘制。此静态场景截图不包含OnGUI HUD，不代表完整关卡通关或图形全部精确对齐。
- 按Project6.4/Tools/BuildAndTest.ps1执行构建与原有回归，具体轮次与结果见verification-summary.json。额外启动新Windows美术预览-smokeTest，日志见runtime-preview-smoke.log。
- protected-files.json核对Seaside01、Movement.asset、bg3 PNG及meta与本次备份一致。初次哈希汇总PowerShell将foreach直接接管道产生语法错误，改为先赋值结果再输出后完成，不涉及工程执行失败。

## 交付与限制

继续编辑原SeasideArtPreview.unity。Play64/试玩及Unity64-Hierarchy工程/试玩包同步更新，入口ArtPreview64.cmd，旧6.6工程和旧Play保留。原版工作日志/失败历史不覆盖，未提交或推送Git。

本次只还原bg系列美术相对排版，图中地形和当前白盒的局部结构差异仍然存在。没有为匹配图片而挪碰撞，也没有替换或隐藏原白盒。未真人全关通关。恢复场景/新脚本/三个导入meta可从本次备份回滚，回滚前先保留后续个人修改；其他part1图层仍可在后续单独接入。
