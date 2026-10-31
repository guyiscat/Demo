# 项目工作与优化总日志

## 2026-11-01：修复构建产物导致的 GitHub 大文件推送拒绝

根目录 build 被误纳入最后一个未推送提交，两个 resS 超过 GitHub 普通 Git 单文件限制。补充 /build/ 忽略规则，仅取消构建产物的 Git 跟踪，保留全部本地游戏与 statgame.cmd。备份完整 Git 历史后修正该本地提交，再以正常快进方式推送；源码、美术、地图及现有场景不改写，不清理缓存或旧备份，不重建或重新打包游戏。

完整记录：[工作日志](Logs/Work/20261101-121507-exclude-build-push/work-log.md)。回滚备份位于 Backups/20261101-121507-exclude-build-push/。


## 2026-10-31：新增手动构建的快捷启动入口

新增 Demo 根目录 statgame.cmd，使用相对脚本路径启动 build/FirstScene_Seaside/Seaside Whitebox.exe，支持含空格路径和缺失文件提示。README 增加入口说明；只读核对两个 Startup ZIP，分别为 Play64 Windows 成品快照和 Unity 源工程快照。本次未改场景、重建或新运行游戏，也未更新 ZIP。

记录：[工作日志](Logs/Work/20261031-170614-statgame-launcher/work-log.md)。文档备份在 Backups/20261031-170614-statgame-launcher/。


## 2026-10-31：统一迁移弃用的 Unity 对象查找和标识接口

修改 20 个脚本的 40 处调用，消除用户列出的 50 条 CS0618：FindAnyObjectByType 替代旧单对象查找，批量查找移除弃用排序参数，层级日志标识改为 GetEntityId。保留非活动对象选项和明确的检查点排序；未屏蔽警告或改变序列化字段，原有关卡不重存。

独立副本 C# 编译 0 warning/0 error；22 项查找与标识检查、40 项基础、203 项玩法回归、41,143 次镜头断言和 24 项启动页运行检查通过。Windows 构建成功，更新 Play64 及 Startup 两个交付包。Input Manager/渲染管线弃用提示不在本次迁移范围，未执行人工全关通关。

详细记录：[工作日志](Logs/Work/20261031-163523-api-deprecation-cleanup/work-log.md)。原脚本、文档与试玩备份在 Backups/20261031-163523-api-deprecation-cleanup/。


## 2026-10-31：配置独立游戏启动页

新增 StartGame.unity，按 startgame.json.ps 还原 14 张素材，配置开始/选项/退出的鼠标与键盘操作；开始进入 TheFirstScene_Seaside。支持主音量、全屏及窗口等比取景，Build Scene List 以启动页为首并保留原启用场景。

独立副本 Windows 构建成功（0 errors），启动页 24 项运行检查通过，生成 16:9/4:3/超宽预览并检查实际美术渲染。未重存现有地图，未保存或切换主编辑器场景；切换启动页前需用户自行保存当前关卡。Play64 更新为启动页构建，交付包名称带 Startup，工具增加 -StartupPage 模式。未执行全关通关或迁移输入系统。

使用说明见 [StartupPage.md](StartupPage.md)，完整记录及失败日志见 [本次日志](Logs/Work/20261031-161909-startup-page/work-log.md)，备份位于 Backups/20261031-161909-startup-page/。


## 2026-10-31：将跟随镜头适当拉远

按用户要求将SeasideArtPreview及Seaside01的Follow Size从10调至12，普通可见宽高各增加约20%，保留bg3边界和全图适配。当前Unity原本有未保存修改，通过Inspector只调整此字段、不整体保存或重载；磁盘两场景各只修改一个数值。手动入口为MainCamera → Seaside Camera → Tuning → Follow Size，说明更新在CameraBounds.md。

备份：Backups/20261031-110419-camera-follow-size/；日志、隔离构建及实际验证结果：Logs/Work/20261031-110419-camera-follow-size/。构建不包含用户尚未保存的其他编辑，未执行新人工全关通关。

## 2026-10-30：按 bg3 背景矩形限制镜头可见范围

用户明确选择 Background_Far/bg3 作为边界。SeasideCamera 自动读取背景实际位置和缩放，限制完整视野；Tab 全图及过渡按比例补黑边，加入像素对齐与实际投影校正。原 CameraMotion 跟随平滑参数不改，无 bg3 的场景保持原逻辑；没有保存用户场景或调整碰撞/玩法。

镜头专项41143项断言与渲染检查通过。标准构建仍被用户当前预览地图与原白盒不一致的历史检查阻止，保留失败结果；专项已保存场景构建与运行检查结果以 Logs/Work/20261030-222238-bg3-camera-bounds/ 为准。说明见 CameraBounds.md，回滚快照在 Backups/20261030-222238-bg3-camera-bounds/。没有新人工全关通关或 Git 推送。

## 2026-10-30：整理 GitHub 协作目录与美术资源入口

按用户确认开始整理：主目录只保留 Project6.4，旧 Project/、Play/、旧入口和重复 Figures 导出资源移至本地备份归档；恢复场景保留原位并排除提交，保留地形 Multiple 副本。统一 README、新增 CONTRIBUTING 和 Figures/Source 说明，配置 Git LFS 和文本属性。两个编辑器美术工具改读 Assets 中的统一资源；打包包含协作文档并排除恢复/重复压缩文件。历史日志和失败报告保留。

工作记录与核验结果见 Logs/Work/20261030-170137-github-handoff/work-log.md；备份和归档清单在 Backups/20261030-170137-github-handoff/。不保存或覆盖用户场景，不关闭当前 Unity，不自动提交或推送。游戏二进制沿用已有 Play64；本次重新打包，不把历史构建/通关当作新验证。

## 2026-10-30：解释背景选中轮廓（未改工程）

确认Background_Distant/bg2、Background_Midground/bg1为透明背景图层；实际UI中Midground显示橙色选中描边，非碰撞轮廓。核对Full Rect导入设置，说明其与Scene选中高亮的区别。仅只读检查，未操作用户当前Play流程。记录及备份见Logs/Work/20261030-background-outline-explanation/。

## 2026-10-30：整理并接入part1其余bg图层

在原SeasideArtPreview中以现有bg3为基准，按part1布局JSON接入bg2/Background_Distant、bg1/Background_Midground，以及独立Foreground下的bg0/Terrain_Artwork。规范场景对象名，保留PNG文件名和原bg3参数；全部是无碰撞的美术层，未移动白盒、角色或检查点。新增一次性编辑器接入菜单，避免重复/覆盖手调。场景不另存，继续用6.4；本次备份与验证见Logs/Work/20261030-part1-backgrounds/，结构说明见SceneHierarchy.md。

## 2026-10-30：核对Recovering Scene Backups提示（未改工程）

确认Temp/__Backupscenes/0.backup及日志中的恢复场景加载记录；该提示说明Unity检测到会话备份，不等于脚本或场景报错。已额外保留恢复文件和正式场景副本，当前正式预览SHA256与上一交付包一致。建议Yes保留Assets/_Recovery，再比较恢复内容；没有覆盖场景或操作弹窗。详细证据、限制和备份见Logs/Work/20261030-scene-recovery-prompt/。

## 2026-10-29～30：整理6.4美术预览层级并移除名称依赖

保存并保留用户Part1Art/bg3，原地规范SeasideArtPreview为Gameplay/Systems/Art；地图按类型和Excel地址命名，复活锚点与路线保持正确归属。426个原对象的世界状态/组件快照一致，未重新生成地图。预览脚本改按地图组件定位并兼容原关卡；几何校验忽略名字，检查真实世界碰撞与复活数据。原Seaside01最终字节不变。

修复Project6.4构建脚本误指向6.6工程、强制保存原关卡和等待后台子进程的问题；新输出Play64及独立入口，保留旧Play。另修正回归用例对预览全图模式的错误前提，保留用户相机设置；残像截图改为独立时间戳目录。Unity6000.4.12f1最终构建20261030-104306-496共418项自动检查PASS，并核对实际渲染；额外预览运行验证见本次汇总。未人工全关通关或重新适配美术地图。

使用说明：[SceneHierarchy.md](SceneHierarchy.md)。备份Backups/20261029-hierarchy-refactor/；原始记录、路径映射、截图与验证汇总见Logs/Work/20261029-hierarchy-refactor/。新6.4工程/试玩包文件名带Unity64-Hierarchy，未推送Git。

## 2026-10-29：检查场景空对象的名称依赖（未改工程）

搜索Project6.4脚本，发现CameraPreview和TrailVisualPreview按固定名称查找地图根对象EXCEL MAP - Sheet1 - 1 CELL = 1 UNIT；此对象改名需同步两处字符串。一般空父对象的引用/组件检索不依赖名称；另提醒Name与Tag/Layer区别，以及地图对象名称参与几何校验的情况。未重命名对象或修改脚本。详情及备份见Logs/Work/20261029-object-renaming-review/。

## 2026-10-29：说明Unity6.4的Windows打包步骤（未执行构建）

核对Project6.4当前场景列表为Seaside01、SeasideArtPreview，说明Build Profiles平台、场景顺序、独立输出和完整文件夹交付。发现副本Tools/BuildAndTest.ps1仍固定使用Demo/Project及默认6.6，本次指导使用GUI以免构建错误工程；未改脚本或启动打包。详情及备份见Logs/Work/20261029-windows-build-guide/。

## 2026-10-29：说明如何在Unity6.4场景中使用part1（未改工程）

确认Project6.4已使用6000.4.12f1和原六项依赖，日志显示Seaside01加载。核对Figures/part1素材和JSON，提供从Assets导入、Single Sprite设置、另存预览、先摆bg3背景及保留碰撞的手动步骤；说明布局JSON不会自动摆图。仅新增工作说明和记录，未改场景、导入素材或重新测试。详见Logs/Work/20261029-part1-unity64-guide/。

## 2026-10-29：定位6.4副本的包解析失败（仅诊断与候选清单）

确认用户打开Demo/Project6.4，但清单及锁文件仍含6.6内置的physicscore2d、tetgen、timelinefoundation依赖，这三项在本机6.4不存在。对照原6.4工程准备六项模块的候选manifest，并逐项确认本机可用。未覆盖当前工程或操作Unity弹窗；建议退出后替换副本清单、备份重建锁文件再验证。证据、候选清单和备份位置见Logs/Work/20261029-unity64-package-diagnosis/。

## 2026-10-28：确认手动安装的Unity6.4，更正此前环境判断

根据用户提供路径，确认D:/Development/Unity/6000.4.12f1/Editor/Unity.exe存在，产品版本6000.4.12f1_3ca267ce8005，并有Windows Standalone支持目录。此前只检查Hub默认安装目录，漏掉手动安装位置，“本机仅有6.6”的结论不准确。未启动6.4、切换Demo版本或重新构建。证据及备份见Logs/Work/20261028-unity64-install-discovery/work-log.md。

## 2026-10-28：比较URP迁移与回退Unity6.4（仅建议）

说明编辑器版本与渲染管线是两个独立选择，建议保留已验证的6000.6.7f1并迁移URP 2D，以解决Built-In使用状态。回到6.4通常可避开6.5新增弃用提示，但会引入对当前已保存工程的降级兼容性问题；若必须用6.4，建议从保留的game1原版6000.4.12f1副本起步。未安装编辑器、转换管线、修改工程或运行新测试。依据与备份见Logs/Work/20261028-urp-versus-64/work-log.md。

## 2026-10-28：核对Built-In渲染管线弃用提示（未改工程）

读取工程版本、Graphics/Quality配置与依赖，确认Demo在Unity6000.6.7f1仍沿用Built-In管线。核对Unity官方资料：从6.5标记弃用，但仍支持至6.7 LTS生命周期；此提示本身不是工程加载或运行失败。说明当前可以继续编辑，后续在正式美术制作前评估URP 2D迁移。未转换管线、修改场景、构建或重新游戏测试。证据、来源与备份位置见Logs/Work/20261028-render-pipeline-warning/work-log.md。

## 2026-10-28：评估part1分层美术素材（仅方案，未改工程）

检查Figures/part1的20张透明PNG及part1.json.ps布局JSON，确认可分别接入背景、桥柱、光效与检查点，并按逻辑画布4288×2107还原图层位置。PNG约为布局尺寸1.5倍，不能沿用旧整图像素坐标。地形仍主要合在bg0.png中，建议保留现有碰撞、分段适配外观，不自动按整图透明轮廓重建碰撞。

新增[Seaside分层素材方案.md](Seaside分层素材方案.md)并给旧说明增加更新入口。只读素材审计和工作记录见Logs/Work/20261028-part1-layer-review/，被修改文档备份在Backups/20261028-part1-layer-review/。未修改Unity场景、代码、玩法或试玩文件，未构建、重新游戏测试或推送Git。

## 2026-10-27：迁入 Demo，建立 Seaside 美术对齐预览

- 从 game1 复制有效 v3 Unity 工程、试玩包、三张 Excel 地图、Seaside.png 及工程的完整历史日志到 Demo，保留来源原件和 Demo 既有 Git 远程。567 个来源文件迁移前后一致，清单见本次 Work 目录。
- 继续沿用工作日志、修改前备份、构建/测试时间戳记录；新增根 AGENTS.md、Git 忽略规则、工程导览与背景图接入说明。完整本地备份位于 Backups/20261027-demo-migration-seaside-art/。
- 构建与测试输出根目录由旧机器固定路径改为相对 Unity 工程位置计算；构建脚本改为可靠等待 GUI 进程退出，并保存正确退出码。启动日志脚本保留为 Tools 源文件，构建时复制到 Play，便于 Git 克隆后使用。
- 新增独立 SeasideArtPreview.unity 和美术对齐组件，等比显示原图、支持透明度/位置/尺寸及外观开关。原正常关卡仍为 Seaside01；未重新生成地图。现有381个地图对象的几何、碰撞与检查点字段和源工程一致，JSON 逐字节一致。
- 原图与现有中后段地形布局不同，预览只是初步配准，尚未把整个关卡替换为图中碰撞。原图边缘不能直接作为可玩边缘；保留白盒以避免隐形碰撞误导。
- 使用本机安装的 Unity 6000.6.7f1（来源6000.4.12f1）完成新构建。已有40基础、203编辑器、159 Windows回归、7校准及新增4预览检查全部通过；逐次结果见 Logs/Build/20261027-*/status.json。实际相机离屏渲染核对了图片与白盒叠加。未人工全路线通关。
- 首轮受限环境启动包管理器失败、隐藏窗口截图全黑等过程如实保留在工作日志，未计为通过。没有提交或推送 GitHub；Play、zip、完整备份和缓存不纳入仓库。

完整记录：[Logs/Work/20261027-demo-migration-seaside-art/work-log.md](Logs/Work/20261027-demo-migration-seaside-art/work-log.md)。

## 2026-10-20：稳定镜头、4/8/8 像素与冲刺拖影

用户反馈镜头晃动明显：缩小方向前视、关闭即时速度预测、增加方向确认时间、扩大死区并统一普通/贴墙死区；高速下落动态收缩窗口提前跟上，减少硬边界追赶。短跳改 4 格，长跳和冲刺改 8 格，人物仍 2×2 格；增加按距离生成、短时淡出的池化残像，无震屏或碰撞。
最终构建 20261020-201730-222：40 基础、203 编辑器回归、159 Windows 回归和 7 校准全部通过；实际测量 4.0000/7.9996/7.9999 格。完成拖影实际渲染截图检查。
完整方案、对照指标、失败迭代记录及限制见 Logs/Work/20261020-camera-motion-v2/work-log.md。地图布局未改，未验证缩短动作后全蓝线通关。替换前备份 Backups/20261020-camera-motion-v2/，HUD 标记 STABLE CAMERA + TRAIL / BUILD 4-8-8。

## 2026-10-20：镜头优化方案核对与总结（未改实现）

核对 CameraMotion/SeasideCamera 源码、已保存场景参数、Rigidbody 插值和现存镜头回归记录，整理详细方案至 Logs/Work/20261020-camera-summary/camera-plan.md。说明死区、横纵阻尼、状态响应、前视/预测、边界取景、全图及复活策略；明确 4 项逻辑镜头检查的范围和快速下落阈值测试缺口，微调值只作建议。本次未修改镜头或玩法，未重新测试或打包；旧总表备份于 Backups/20261020-camera-summary/。

## 2026-10-20：墙跳后续诊断（未改规则）

读取用户最新实际试玩日志，确认已运行本次新版。部分无响应按键发生在 wall=0、groundGrace=0、wallGrace=0 的空中状态；现规则不提供自由二段跳，因此不会起跳。也有离墙后成功执行墙跳的实际记录，说明修复已加载。详细证据与局限见 Logs/Work/20261020-wall-jump-followup/diagnosis.md。等待确认是扩大离墙窗口还是改变跳跃资格保留规则，本次不重新出包。

## 2026-10-20：v3 原文件夹追加墙跳修复

- 需求：贴墙后反向键 + 空格偶发不向上跳；本项目统一留日志。
- 原因：反向离墙超过探测范围后，空格没有可用墙跳资格。旧版可重复复现 16 个失败案例。
- 修改：新增 0.10 秒离墙墙跳宽限；A/D 重叠时后按优先；输入边沿跨帧保留，跳跃资格单次消耗。
- 保留：同墙松开慢滑/再按悬停、0.14 秒平台土狼、0.12 秒跳跃缓存、短跳/长跳/冲刺距离、地图、检查点与镜头。
- 验证：40 基础 + 165 编辑器回归 + 121 Windows 回归 + 7 校准，全部通过。定向对照：旧版 34 PASS/16 FAIL，新版 72 PASS/0 FAIL。
- 日志：Logs/Work/20261020-wall-jump/work-log.md；最终构建记录 Logs/Build/20261020-192517-945/。
- 备份：Backups/20261020-wall-jump/。
- 归档：Logs/History/20261020-192429-652/，142 个现存项目日志和验收资料，含来源及校验清单。
- 后续：每次优化写 Work 日志、追加本总表，构建测试及运行日志按时间戳留存；规则已写入 Project/AGENTS.md。

## 历史阶段补记（不是原始逐条流水）

以下根据现有项目说明、验收文件及本次对话补记。原始资料按文件原貌保留在 Logs/History，具体日期以 manifest.csv 的原文件时间和文件正文为准，无法恢复过去未保存的日志。

- UnityWallDashPrototype：最初白盒、路线/蓝框测试和地图坐标矫正阶段；资料含 route/physics-tests 与蓝框轨迹 CSV。
- SeasideWhitebox：Excel 海边地图导入；地面、尖刺、检查点、蓝色路线可视标记；后续人物改为 2×2 格，短跳 3 身位、长跳和冲刺 5 身位，调整下落与跳跃手感。
- PlaytestFix-v2：检查点只向后推进、镜头平滑及帧率回归；斜冲碰地形结束整个冲刺，松键缓慢下滑。当时限制同墙重新悬停，属于旧阶段要求，不再适用新版。
- v3 初版：按用户新要求恢复同墙再按悬停；地面土狼 0.14 秒，落地跳跃缓存 0.12 秒，防额外空中跳跃。原始报告 40/93/49/7 项均通过。

旧测试只代表对应版本；不能把旧地图路线验收当作当前地图通关证明。后续在此文件顶部追加最新工作，不覆盖已有记录。
