# 开发与美术协作

先读 [README](README.md) 和 [开发交接](Docs/Handoff.md)。使用 Unity 6000.4.12f1、Git、Git LFS，克隆后 `git lfs pull`，再在 Hub 添加 Project6.4。

## 修改原则

- 编辑保存的场景，不运行旧生成器覆盖手工地图。避免多人同时修改同一场景或二进制原画。
- 替换图片保留文件名与 .meta。新增素材与 .meta 一起提交；在 Unity 移动资源，保留 GUID、切片和导入配置。
- 正式运行图片放 Project6.4/Assets/Art，音频在 Assets/Audio，字体在 Assets/Resources/Fonts。根 Assets 保留唯一原稿、PSD、策划和参考图，不重复维护已经导入的同一批导出PNG。
- 修改分层图名称同步其布局引用；保留比例和轴心，检查白盒碰撞、脚下接触、透明边缘、前后遮挡和场景出口。
- 脚本分类与职责见 Docs/ScriptLayout.md；层级约定见 Docs/SceneHierarchy.md。

## 验证与提交

运行时逻辑、美术或场景变动按需运行 Tools/BuildAndTest.ps1，并检查实际画面。已打开的同一工程不能同时批处理构建，使用隔离副本。测试存档必须指向独立路径，不上传玩家进度。

每次只保留必要的精简工作记录和最终验证摘要；缓存、原始构建日志、重复ZIP与完整备份不进仓库。当前源码和原画已经足够继续开发，无需历史备份。

建议使用 feature/主题、art/关卡 分支，经评审合并。提交前检查 `git status`、`git diff --cached --stat`、`git lfs status`；不要提交密钥、Library、Temp、UserSettings、Play64 或压缩包。GitHub 的源码依靠 LFS 下载真实图像，不能只交几行 LFS 指针文本。全新克隆的导入与构建应可复现。

