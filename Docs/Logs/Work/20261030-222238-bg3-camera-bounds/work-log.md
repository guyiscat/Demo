# bg3 镜头可见边界

## 需求及选择

用户希望看不到主画面外的资源。经明确选择，以 Background_Far（bg3.png，最底层远景）的外框为边界。普通跟随限制完整视野，Tab 全图按背景比例补黑边，保留当前手工地图。

## 备份

Docs/Backups/20261030-222238-bg3-camera-bounds/before/ 保存修改前 Assets、Packages、ProjectSettings、Tools、Play64 及受影响文档。before-hashes.json 记录修改前工程文件 SHA256。工作基于磁盘已保存场景，不关闭用户 Unity，不调用主工程场景保存或生成。

## 实现

- SeasideCamera：增加可关闭的背景边界和可选 Sprite Renderer 引用；自动查找同场景 SeasideArtRoot 下的 bg3。按背景世界坐标与大小跟随，并约束完整相机视野。没有 bg3 时沿用原相机行为。
- BackgroundCameraFrame：计算边界内可见矩形、中心和黑边视口。覆盖普通跟随、全图和过渡状态；超大视野按背景尺寸限制。
- 对齐视口到整数像素，并校正实际投影，避免部分屏幕比例下边缘舍入露出界外内容。黑边使用临时清屏相机；HUD 不随世界相机视口缩放。临时相机不保存入场景。
- 新增批处理专项检查 CameraBackgroundBoundsChecks 和 Docs/CameraBounds.md；不改变 CameraMotion 原平滑参数、玩家动作、碰撞、检查点或终点。

## 实际验证

- 标准 Tools/BuildAndTest 在隔离副本运行，失败于既有的 Map world geometry differs：预览地图已包含用户新增地形/危险物，与 Seaside01 不同。失败日志在 standard-pipeline/，没有改图或修改此检查。
- 首轮专项发现缩放过渡中实际投影的小幅边界误差，记录 first-projection-failure.*；第二轮发现投影修正影响全图尺寸，记录 second-projection-failure.*。以像素对齐视口和实际投影校正修复，不覆盖失败记录。
- 最新镜头专项通过41143项断言，覆盖4:3、16:9、21:9、竖屏、正方形；背景平移/缩放/极小尺寸；目标在角点及图外；双向全图过渡、暂停；关闭限制及原白盒场景回退。原有镜头舒适度检查通过。
- camera-bounds-checks.txt、camera-comfort.csv 为详细结果；follow-edge.png、overview-letterbox.png 已实际渲染并人工查看，黑边清屏通过像素检查。
- 专项已保存场景构建成功，focused-build-status.json 退出码0；新构建的运行回归、校准、美术预览启动检查均退出0，详见 runtime-checks.json 和 runtime-*.log。没有执行新人工全关通关。
- validation.json 确认：仅原有 SeasideCamera.cs 有代码变化；当前两份场景、所有保留美术和切片、预制件碰撞、Packages、ProjectSettings 的 SHA256 与开始快照一致。隔离构建使用的脚本和场景与当前工程对应文件一致。
- Play64 已替换为本次新构建，148个文件逐项哈希验证通过，详见 play-deployment.json。替换前目录额外归档在备份的 replaced-Play64/；原始 before/Play64 也保留。
- 两个交付 ZIP 由现有 PackageRelease.ps1 重打包；最终 ZIP 内容与 CRC 检查结果在打包后生成的 package-verification.json（不包含在同次 ZIP 快照内）。
- 首次打包因当前 Figures 目录已不存在而失败，记录 packaging-first-failure.txt。打包脚本改为跳过缺失的可选 Figures 源稿目录，Assets 和其它必需内容仍严格检查；没有重建或恢复用户已移除的素材目录。该工具修改不影响已验证的运行代码。

## 限制与交付

只限制轴对齐 bg3 的矩形范围，不裁切图片内部透明轮廓，不添加物理边界。Scene 窗口为编辑器工作区，不受 Game 镜头限制。无新人工全关通关；试玩只包含磁盘已保存场景。本次不提交或推送 Git。

## 回滚

先备份后续个人编辑。从 before/ 同路径恢复 SeasideCamera.cs；新增 BackgroundCameraFrame.cs、CameraBackgroundBoundsChecks.cs 及各自 .meta 可按需移出。需要旧试玩时恢复 before/Play64 的完整目录，不只替换 EXE。文档按差异恢复，避免覆盖后续工作日志。场景和资源本次不作保存修改，原则上无需回滚场景；以 validation.json 的哈希结果确认。
