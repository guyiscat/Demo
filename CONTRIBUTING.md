# 开发与美术协作

## 第一次获取项目

安装 Git、Git LFS 后配置并克隆：

~~~powershell
git lfs install
git clone https://github.com/guyiscat/Demo.git
cd Demo
git lfs pull
~~~

已有克隆先同步当前分支，再执行 git lfs pull。LFS 指针是几行文本，不是真实 PNG；图片导入失败时先检查 LFS 下载。不要把网页 Download ZIP 作为默认开发交接方式。

安装 Unity 6000.4.12f1，通过 Hub 添加 Project6.4。首次导入会生成 Library。工程已启用文本序列化和 Visible Meta Files。

## 美术资源入口

| 资源 | 位置 |
|---|---|
| 游戏实际使用的 PNG | Project6.4/Assets/Art/ 下对应目录 |
| Seaside 拆分图与布局 | Project6.4/Assets/Art/Seasidepart1/ |
| 整图参考 | Project6.4/Assets/Art/Backgrounds/Seaside.png |
| PSD、PSB 等分层源稿 | Figures/Source/ 下按关卡分类 |
| 导出说明 | 随源稿提供 README，或补充 Docs 中的素材说明 |

不要在 Figures 和 Assets 同时维护同一批导出 PNG。源稿和 PNG 各有职责；不要为了填目录再复制一遍 PNG。

源稿说明包括用途、作者及可用授权、版本、画布尺寸、图层关系、导出倍率、轴心、预期世界尺寸，以及影响碰撞的轮廓变化。part1.json.ps 是 JSON 布局数据，不是可执行脚本；PNG 与逻辑布局存在约 1.5 倍导出比例。

## 有 Unity 的美术人员

1. 开始前同步分支，与开发约定负责的资源或场景。
2. 替换已有图片保留原文件名和 .meta，保留 GUID、导入设置及切片定义。
3. 新资源放入指定 Assets 目录，让 Unity 生成 .meta；资源和 .meta 一起提交。
4. 在 Unity Project 窗口中移动、重命名资源，避免丢失引用。
5. 使用 SeasideArtPreview 摆放美术。现有 Collider、触发器、检查点和复活锚点不随装饰移动。
6. 检查 Console、透明边缘、遮挡和人物脚下接触面，保存实际修改的场景。

图片改名需同步布局 JSON 的 texture.uri。修改切片图片的尺寸或内部排版会影响切片坐标，需要同步调整引用。bg0 与 Seaside_Terrain_Modules 目前像素相同，但 Single/Multiple 用途不同，应分别保留。

## 没有 Unity 的美术人员

可以提交源稿和约定的导出 PNG。新增 PNG 尚无 .meta 时，由开发者拉取后导入并补交 .meta；导入验收前不作为接入完成。

替换已有 PNG 时保留原 .meta，不要删除后重新创建整个文件夹。Multiple 素材优先保持画布尺寸和排版，避免切片失效。大图片使用 Git 客户端上传，PNG/PSD 等已通过 .gitattributes 配置 LFS。

## 分支和提交

- 按任务建立 feature/主题 或 art/关卡-主题 分支，经 Pull Request 合并。
- 尽量由一人修改同一个场景或二进制美术文件。当前只配置文本属性，未自动配置 UnityYAMLMerge 驱动。
- 提交前查看差异，只提交本次工作；不要提交 Library、Temp、UserSettings、Play64、恢复场景、ZIP 和完整备份。
- 不要全局忽略所有 .log：Docs/Logs 的历史记录和失败日志需要保留。
- 地图源数据不能覆盖场景手动修改。SeasideBuilder、历史升级和旧整图创建工具不是日常打开或构建入口。

选择性添加本次文件：

~~~powershell
git status --short
git add <本次修改的文件或目录>
git diff --cached --stat
git lfs status
~~~

## 日志与验证

每次建立 Docs/Logs/Work/日期时间-主题/work-log.md，记录需求、改动、参数、验证、限制和回滚位置，并更新 Docs/CHANGELOG.md。旧记录保留原日期，不把旧 PASS 当作本次结果。

运行时代码或场景变化按需执行 Project6.4/Tools/BuildAndTest.ps1 并人工试玩。纯文档和仓库整理检查路径、Git 规则、资源完整性。未执行的构建或人工通关必须明确注明。

本地单仓库启用 LFS 可执行 git lfs install --local。图片提交后用 git lfs ls-files 核对。远端发布由负责人执行，本次整理不自动提交或推送。

参考：[GitHub Git LFS 配置](https://docs.github.com/en/repositories/working-with-files/managing-large-files/configuring-git-large-file-storage)、[Unity 资源工作流](https://docs.unity3d.com/es/current/Manual/AssetWorkflow.html)。
