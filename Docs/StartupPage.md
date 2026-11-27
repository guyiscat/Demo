# 2026-11-27 设置与继续界面更新

游戏选项现采用新增蓝色参考图的“游戏操作 / 显示与声音”双页界面；支持按键修改、亮度、声音与重置。继续游戏打开五槽存档选择，开始新游戏保留已有槽位。说明见[设置与多存档](SettingsAndSaves.md)。以下内容为历史开发记录；原设置弹框及单存档说明已替代。

---

# 2026-11-24完整入口更新

statgame.cmd仍为完整试玩入口，包含海边/森林/村庄/矿洞/雪山/灯塔六区域，菜单四项保持。开始新游戏进海边，继续游戏恢复最新区域/安全点与机关状态；玩法不再按终点自动顺序跳关。详细连接和传送菜单见[区域玩法](WorldRegions.md)。

---

# 2026-11-20 当前四项菜单与流程
当前开始页已扩展为开始游戏、继续游戏、游戏选项、退出游戏。原8层主题背景/徽标/标题保留，按钮使用阿里巴巴普惠体动态文字并支持鼠标/键盘。无存档时继续置灰，新游戏替换已有进度前确认；全流程自动连接五关。当前实现、存档位置和验证见[流程与存档说明](CampaignAndSaves.md)。以下旧三按钮描述保留为历史设计记录，不代表当前版本。

# 启动页使用与编辑

启动场景为 `Project6.4/Assets/Scenes/StartGame.unity`。先在 Unity 中手动保存你正在编辑的关卡，再双击打开此场景，切到 Game 窗口并点击 Play。

## 已配置的行为

- 开始游戏：加载 `Assets/Scenes/TheFirstScene_Seaside.unity`，即本次检查时编辑器正在打开的正式命名关卡。
- 游戏选项：打开主音量、全屏设置；修改保存在本机 PlayerPrefs。素材目录没有提供背景音乐，本次未新增音乐。
- 退出游戏：成品退出程序；编辑器中停止 Play。
- 鼠标移入按钮显示高亮，单击执行；方向键或 W/S 选择，Enter 或空格确认；选项窗口按 Esc 返回。

Build Profiles 的全局 Scene List 顺序为 StartGame、TheFirstScene_Seaside，然后保留原来启用的恢复场景。Windows Profile 沿用全局列表。默认启动页是打包程序的入口；在 Unity 中直接对某个关卡按 Play，仍从当前打开的关卡运行。

## 场景结构

```text
StartGame                  StartGameMenu 控制器
├─ MenuCamera              正交摄像机及 AudioListener
├─ Art
│  ├─ Backgrounds          Background、BackdropMask
│  ├─ Emblem               EmblemRing、HeroShadow、HeroEmblem、LightRays
│  └─ Title                Divider、TitleLogo
└─ MenuButtons
   ├─ StartButton          StartGameButton
   │  ├─ Normal
   │  └─ Highlighted
   ├─ SettingsButton       同上
   └─ ExitButton           同上
```

14 张 PNG 均以独立 SpriteRenderer 挂入场景，可以直接选择、移动和替换。普通、高亮图片是互斥显示，不应把两个子对象都删除或取消引用。按钮点击范围在各自 StartGameButton 的 Hit Size 中配置，选中时显示青色辅助框；它不是碰撞体。

## 调整布局与目标关卡

原稿 `Assets/Art/startgame/startgame.json.ps` 是文本布局说明，设计画布为 960×540，PNG 实际约为设计尺寸的 1.5 倍。场景依据 JSON 的坐标、宽高和图层顺序还原，每 100 个设计像素对应 1 个 Unity 单位。Sprite 设置为 Single、Full Rect、PPU 100，关闭 Mip Maps，保留透明度，使用 Clamp 和无压缩。每个对象再按设计尺寸设置 Scale，因此不要把图片的实际像素尺寸直接当成设计尺寸。

- 改按钮位置：移动 StartButton / SettingsButton / ExitButton 根对象，两个状态及点击区域会一起移动。
- 改图层遮挡：调整各 SpriteRenderer 的 Order in Layer；不要用 Z 位置随意改变遮挡关系。
- 改开始按钮进入的关卡：选择 StartGame 根对象，在 Start Game Menu 的 Game Scene Path 中填写场景的完整 Assets 路径，并把该场景加入 Build Profiles 的 Scene List。
- 摄像机自动完整显示 960×540 的设计画布；窗口比例不同会留黑边，图片不会被拉伸或裁切。运行时不要通过 Camera Size 改整体缩放，它由 FitCamera 控制。
- 素材中的镂空、光效边缘与分隔线断口沿用原 PNG，未重绘图片。

本次创建启动场景，没有重存或重建已有地图。关卡中尚未保存的修改，需要你自行保存后才会进入后续构建。

## 构建与交付

Unity 手动构建：先保存已确认的关卡内容，再从 Build Profiles 构建；确保 StartGame 是第一个启用场景，TheFirstScene_Seaside 也已启用。

命令行构建：关闭同一工程的编辑器，或在独立副本中执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\BuildAndTest.ps1 -StartupPage
```

此模式读取保存的场景，验证启动页引用，构建 Play64，并运行启动页交互、设置与加载关卡的检查。它不要求美术关卡的几何与旧白盒相同，也不重新生成地图；它不替代关卡通关测试。默认不带 StartupPage 的旧白盒检查流程仍保留。

`-GenerateStartupScene` 仅供初次在隔离副本中生成启动页，发现 StartGame.unity 已存在会拒绝覆盖。后续编辑与构建不需要该参数。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Project6.4\Tools\PackageRelease.ps1 -StartupPage
```

生成 `Seaside-Unity64-Startup-Windows.zip` 和 `Seaside-Unity64-Startup-Project.zip`。试玩直接运行 `Play64/SeasideWhitebox.exe`。本次没有迁移输入系统，启动页与现有游戏均使用当前已启用的 Input Manager。
