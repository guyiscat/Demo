# 2026-10-27：迁移 Demo 与 Seaside 美术图接入

## 用户需求与范围

阅读 game1 工程、当前海边关卡、三个地图文件与既有开发日志；迁入 Demo 作为后续主目录；沿用每次留日志的习惯；研究并尽可能操作 Seaside 背景图与当前地图的接入，解释操作方法。

本次复制保留原 game1 与 leihuoGame，没有删除来源、覆盖原工程或推送 GitHub。Demo 原来只有 README.md 和已有 .git，继续使用原远程 `https://github.com/guyiscat/Demo.git`。未提交 Git，便于用户审核全部迁入内容。

## 阅读与理解

阅读了当前交付 README、Project/AGENTS.md、CHANGELOG、Logs入口、四个 Work 子目录的工作与诊断说明、既有构建状态及历史归档来源清单；核对了场景、JSON、角色设置、输入、流程/触发器、镜头、日志、建图/构建/回归脚本。

当前可玩内容是 Seaside；森林与矿洞为地图原稿，没有对应可玩场景。4/8/8、2×2角色、地面与墙面宽限、可重复贴墙悬停、慢滑和稳定镜头/残像均属于迁入前已有实现，未将这些记作本次新增功能。原蓝线在4/8/8数值下尚未人工全关验证。

三份 xlsx 只读检查，海边 JSON 中1975个来源格与海边工作簿对应地址的填色类型一致；源表在导出区域之外还有填色，因此不能用全表总数直接判定导入错误。脚本及结果：inspect_sources.py、source-audit.json。没有改写或重新保存任何来源地图表。

## 迁移与备份

来源：

- game1/Seaside-PlaytestFix-v3-Project/Project → Demo/Project。
- 同交付的 Docs → Demo/Docs，包括旧工作日志、History、Build和根报告，保持旧日期。
- game1/maps 与 Figures → Demo 同名目录。
- game1/Seaside-PlaytestFix-v3-Windows(2)/Play、StartGame.cmd、CameraPreview.cmd → Demo。
- 原交付 README → Docs/OriginalDeliveryREADME.md，Demo根README改写为当前使用入口。

迁入后修改前，完整复制 Project、Play、原Demo README、CHANGELOG及日志入口到 `Docs/Backups/20261027-demo-migration-seaside-art/`。来源 game1 也保留原件。

migration-manifest.csv 记录567个来源文件、目的位置、大小与SHA256，复制时全部一致。初次对正在被Excel使用的海边表计算哈希被文件共享限制拒绝，改为以 FileShare.ReadWrite 只读计算，结果一致。复制时夹带了一份 `~$海边地图.xlsx` 临时锁文件，已从 Demo 删除；第一遍删除因其隐藏/只读属性失败，随后使用明确路径加 Force 成功，来源锁文件未操作。分析脚本也排除这类临时文件。

## 原因与美术判断

原图7783×4214，包含背景、平台、墙体、白桥、植物与装饰，属于合成关卡美术。源地图155×73单位。若按宽155等比显示，图片高约83.923，无法同时对齐地图上下边缘。

初始配准保持横向对应，以左侧起步地面约y=1为参考：WorldOrigin=(0,-5)、WorldWidth=155、Opacity=1。图片中心约(77.5,36.962)。这只是初始估计，未将其描述为全图精确配准。

实际渲染显示：左侧起步与部分墙体近似对应，中段x≈67～74的向上墙体和x≈74向右的大片实体平台，与原图的低处地形、白桥/开放背景明显不同。无法用整体平移、统一缩放消除。若直接隐藏原地形，玩家会在空中站立或撞上看不见的墙。因此本次不擅自调整碰撞布局，改为独立对齐预览；后续可沿用碰撞分层补画地形，或另开关卡副本按图重建并重新验收。

## 本次修改

- 新增根AGENTS.md与.gitignore，覆盖工程、素材、文档和地图；排除Library/Temp/本地Play/完整备份/zip/临时锁文件。历史工作和构建原始日志保留；新运行会话本地记录。
- SeasidePaths.cs计算当前交付根；SeasideBuilder、PlaytestFixBuild和PlaytestRegression使用它，避免继续写入旧电脑的D:/SeasideWhitebox-PlaytestFix-v3。
- BuildAndTest.ps1默认本机Unity路径，改用Start-Process Hidden/Wait/PassThru读取真实退出码，保留逐轮独立日志和快照。新增Tools/StartWithLogs.ps1作为受Git管理的启动日志脚本；构建后复制到Play，避免新克隆缺失此文件。
- CollectProjectLogs.ps1默认只收集当前交付，不再写死前作者的个人工作目录；需要额外来源可显式传AdditionalProjectRoots。
- SeasideLog.cs更新build为demo-migration-art-preview-20261027，保留gameplay=v3-stablecam-4-8-8-trail-20261020，增加当前场景名，方便区分旧包和预览。
- 原图原样导入Assets/Art/Backgrounds/Seaside.png；Sprite/Single、Full Rect、PPU50、默认最大8192、Bilinear、Clamp、无MipMaps、不生成物理形状。图片文件内容与Figures原图一致。最大尺寸设置记录在meta的DefaultTexturePlatform中；非覆盖平台的2048字段不代表实际启用了2048覆盖。
- 新增SeasideArtPreview.unity，由已保存原场景复制，增加一个无碰撞的SpriteRenderer，Sorting Order=-100。未调用SeasideBuilder.CreateScene或任何重建地图菜单。
- SeasideArtAlignment.cs在独立预览中管理位置、等比宽度、透明度及Ground渲染器开关，F6白盒、F7图片，默认白盒可见。尖刺、检查点、蓝线和全部碰撞继续保留。新增显式离屏渲染参数用于实际视觉核对，不参与正常玩法。
- SeasideArtSetup.cs提供打开预览菜单、一次性创建与几何检查。新预览加入构建；-artPreview选择预览，StartGame正常入口仍进Seaside01。
- 新增ArtPreview.cmd并给启动脚本增加ArtPreview开关，避免在cmd/PowerShell间把-gameArgs里的负号误判为命名参数。
- PackageRelease.ps1把新入口、原图、地图和根约定加入交付包，继续排除缓存、完整备份和递归压缩包。
- 更新README、CHANGELOG和日志入口，新增工程导览、背景图接入说明。本目录保留分析/验证脚本、清单和实际截图。

## 环境、验证与失败记录

源Unity6000.4.12f1在本机未安装，本机有6000.6.7f1及Windows构建模块。本次在Demo副本内用6000.6.7f1导入，ProjectVersion与必要引擎序列化字段随导入更新；原来源仍为6000.4.12f1，没有覆盖原件。

1. art-setup.log：受限环境中首次Unity启动未连接包管理器，退出1；保留原日志，未算作成功。使用具有本机授权/包管理器访问权限的启动方式重试，art-setup-retry.log确认创建预览并退出0。
2. 初次只读工作簿脚本误读Excel锁文件，产生BadZipFile，增加临时文件排除后成功；verify_geometry脚本初试缺少PyYAML，改用标准库只提取Unity文本中需要比较的标量与内联向量，没有下载依赖。
3. 四轮构建记录均独立存于Docs/Logs/Build/20261027-*/，结果均为PASS。最终交付轮次为20261027-175109-430，编辑器构建、Windows回归、校准退出码均为0；本目录verification-summary.json汇总实际结果。
4. 每轮完成构建包含：基础40 PASS、编辑器回归203 PASS、Windows回归159 PASS、Windows校准7 PASS，无FAIL；新增4项预览检查PASS，检查几何一致、图片存在/无碰撞、隐藏白盒不动碰撞、恢复白盒可见。原始报告与构建快照均保留。
5. verify_geometry.py从源game1场景、Demo正常场景和预览场景提取381个SeasideCell的名称、层、活动、Transform/祖先Transform、BoxCollider大小/偏移/触发状态/材质、类型、攀附标志及检查点/复活锚点字段；两份Demo场景与源场景完全一致。JSON逐字节一致，图片原文件与Unity导入副本逐字节一致，详见geometry-verification.json。
6. 隐藏窗口配合原ScreenCapture得到全黑图，保留art-overlay.png与art-overlay-capture.log作为失败证据，没有当作视觉验收。改为Camera.Render到RenderTexture，实际构建生成art-overlay-render.png，人工查看确认图片、白盒、尖刺、检查点、蓝线均渲染。
7. 通过正式Play/StartWithLogs.ps1 -ArtPreview启动并生成art-only-render.png，退出0；日志确认scene=SeasideArtPreview、whitebox=False。该图仅隐藏Ground外观，其他玩法标记保留。离屏截图不含OnGUI HUD，不能声称核对了完整窗口UI或真人操作。
8. Git检查：保留origin；cache、Play、完整备份均被忽略，PNG单文件约30.47MiB；git diff --check通过，仅有Git本机LF/CRLF提示。没有git add/commit/push。
9. 最终通过正式启动脚本分别运行正常关卡-smokeTest和美术预览-artRenderCapture，均退出0。会话日志确认新build标识，正常场景为Seaside01，预览为SeasideArtPreview。引擎日志复制到本目录normal-smoke-engine.log、art-final-engine.log；最终交付截图art-overlay-final.png。所有Tools PowerShell脚本语法解析通过。再次计算567个原来源文件哈希，无变化。

## 未完成与限制

- 尚未把原关卡整体替换成与图片边缘完全一致的最终地图。当前独立预览用于对照；隐藏白盒不改变任何碰撞。
- 没有分层原稿，不能无损拆出图片中被前景遮挡的背景。推荐分层补画或导出后逐段贴合。
- 没有人工全路线通关，没有实际键盘逐键验收；自动化动作测试不等同完整关卡可达性或美术精确对齐。
- 未在未安装的6000.4.12f1重新构建，也没有再次验证其他机器的编辑器/显卡环境。
- 未推送GitHub，代码和素材在Demo工作区待审核；两个本地交付包另行生成，最终清单见verification-summary.json及Docs/Logs/Packaging。

## 回滚

正常关卡从StartGame.cmd或Seaside01.unity进入，不加载美术预览。

若需还原迁入前副本：用Docs/Backups/20261027-demo-migration-seaside-art/下的Project和Play覆盖对应Demo目录，并恢复Demo-README.md、CHANGELOG.md与日志入口；先保存后续个人改动。也可直接参照未修改的game1原交付。备份不纳入Git和zip，以免递归和大量重复文件。
