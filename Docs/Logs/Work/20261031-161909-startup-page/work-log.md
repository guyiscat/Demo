# 2026-10-31：配置独立启动场景

## 需求与检查

用户要求检查 Assets/Art/startgame，为游戏配置启动页。本次仅处理启动场景、菜单交互及相应构建与交付入口。

读取 Demo 与 Project6.4 的 AGENTS.md，按要求先备份。发现 14 张 PNG 及 startgame.json.ps 文本布局；JSON 设计画布 960×540，PNG 约为设计尺寸 1.5 倍，不能直接用原始像素大小摆放。现有 PNG 还是普通 Texture，需转换为独立 Sprite。

Windows Profile 不覆盖全局 Scene List。检查时运行中的主编辑器以 TheFirstScene_Seaside.unity 启动，原全局 Scene List 只有 SeasideArtPreview_Recovered_20261031_1058。因此选择 TheFirstScene_Seaside 作为“开始游戏”的明确目标，并保留原启用场景。

## 文件与行为

- 新增 Assets/Scenes/StartGame.unity 及 meta。按 JSON 的坐标、尺寸及图层顺序实例化所有 14 张 PNG，组织为 Backgrounds、Emblem、Title、MenuButtons；未重绘、修改或改名 PNG。
- 14 张 PNG 的 meta 保留原 GUID，导入改为 Sprite / Single / FullRect / PPU 100 / Clamp / 无 MipMaps / 无压缩 / AlphaIsTransparency。对象 Scale 依据 JSON 宽高与实际 sprite.rect 计算。
- 新增 StartGameMenu、StartGameButton、StartGameMenuSmoke 及 meta。普通与高亮图分别显隐；鼠标悬停/点击，W/S 或上下方向键导航，Enter/空格确认。每个点击区域为 1.27×0.42 世界单位，避免高亮图片的透明外边缘与相邻按钮重叠。
- 开始使用场景完整路径异步加载 TheFirstScene_Seaside；设置页提供主音量及全屏，保存至本机 PlayerPrefs；退出在编辑器中停止 Play，在成品中退出程序。未添加 BGM，因为未提供音乐素材。
- 摄像机按完整 960×540 设计画布等比取景，宽高比变化留黑边。未改变游戏关卡摄像机、碰撞、复活点或玩法参数。
- EditorBuildSettings 改为 StartGame 第一、TheFirstScene_Seaside 第二，其后保留原启用的恢复场景。主编辑器中的当前场景没有被自动切换或保存。
- 新增批处理专用 StartGameSetup。首次生成拒绝覆盖已存在的 StartGame；普通 ValidateAndBuild 只读取保存的场景。检查图层、按钮、目标、缺失脚本及场景顺序，并输出三种比例预览、构建报告。
- BuildAndTest.ps1 增加 StartupPage 模式，保留原白盒流程。启动页模式不要求艺术关卡与旧白盒几何相同，也不重新生成地图。
- PackageRelease.ps1 增加 StartupPage 模式，生成名称带 Startup 的工程包及 Windows 包；尊重用户已删除的旧 .cmd 入口，不把这些入口重新创建为打包依赖。
- 新增 Docs/StartupPage.md，更新 README 与 CHANGELOG。

## 验证及失败记录

所有 Unity 生成、编译、构建及运行检查都在备份目录下的独立工程副本进行。没有向正在运行的主工程追加 Editor executeMethod 任务，没有关闭编辑器，没有调用其场景保存。

执行隔离副本 Tools/BuildAndTest.ps1 -StartupPage -GenerateStartupScene，首轮使用不存在的 TextureImporter.spriteMeshType 属性导致 CS1061；已修正为 ReadTextureSettings / TextureImporterSettings / SetTextureSettings，保留失败日志。新增脚本使用 FindAnyObjectByType 以及无排序参数的 FindObjectsByType，未新增这些 API 的弃用调用。

第二轮 Windows 构建成功、0 build errors，但 batchmode ScreenCapture 截图失败导致检查 FAIL。尝试隐藏 D3D11 非 batchmode 后交互和加载的 23 项断言通过，但截图是黑图，因此不将黑图当作界面验证证据。

最终改用显式 Camera.Render 到 RenderTexture 验证隐藏玩家的美术渲染，设置窗口通过状态与音量断言验证。执行隔离副本 Tools/BuildAndTest.ps1 -StartupPage：

- isolated-build-runs/20261031-162359-419/status.json：PASS；editorExit=0、startupExit=0。
- validation/build-result.json：Succeeded，0 errors；构建含 StartGame、TheFirstScene_Seaside 及原恢复场景。
- validation/runtime-result.txt：PASS assertions=24。覆盖初始状态、按钮鼠标命中/高亮互斥、键盘选择环绕、设置开关、模态拦截、音量应用与恢复、实际美术渲染、目标关卡加载、玩家与 Session 存在、菜单卸载与 timeScale。
- 已查看 start-menu-1280x720.png、start-menu-1024x768.png 及最终 runtime-artwork.png；1680×720 同样由编辑器离屏渲染输出。源 PNG 的透明断口通过原图像素 Alpha 核对，是素材本身内容。
- 未进行人工全关通关、实体鼠标键盘逐项试玩、全屏切换自动测试或设置面板屏幕截图验收。没有把这些写为已验证。
- 现有代码与旧 Input Manager 的弃用提示仍存在；本次没有迁移输入系统或修改现有玩法脚本。

最终通过后，仅同步新场景、新脚本、14 个图片 meta、构建列表及工具改动回主工程；将旧 Play64 整体移入已检查绝对路径的 before/Play64 后，复制新构建。复制隔离验证日志，保留首轮与次轮失败。

## 数据保护与回滚

备份根：Docs/Backups/20261031-161909-startup-page/。before 下保存原受影响文件、14 个图片及 meta 和旧 Play64。隔离副本位于 validation/Demo/Project6.4。

existing-scenes-before.json 记录任务开始时所有原有关卡及 meta 的 SHA256；同步前再次核对一致。主场景文件从未复制回、重存或生成，不据此断言用户内存中的未保存内容是否完整。切换到启动页前，由用户自行保存当前关卡。

若回滚，先退出 Play 并自行保存用户编辑，再恢复 before 中原图片 meta、EditorBuildSettings、工具及文档，移走本次新增启动场景与脚本；如要退回旧试玩，恢复 before/Play64。不要把隔离副本或旧场景备份覆盖到当前手工地图。

最终安装文件列表见 installed-files.txt；后续同步与打包结果见 completion.json。未提交或推送 GitHub。
