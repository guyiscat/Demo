# 2026-10-29：Unity6.4回退时的包清单错误

用户提供Package Manager报错截图，询问为什么找不到physicscore2d、tetgen、timelinefoundation。

## 证据与原因

- Editor.log的当前命令行指向Demo/Project6.4/Assets/Scenes/Seaside01.unity，进程路径为D:/Development/Unity/6000.4.12f1/Editor/Unity.exe。已核对Project6.4/AGENTS.md。
- Project6.4/Packages/manifest.json保留了6.6版本的9个模块依赖，其中physicscore2d、tetgen、timelinefoundation三个模块在本机6.4的BuiltInPackages目录中不存在，在6.6中存在。
- 当前packages-lock.json也保留6.6依赖关系，包括physics2d对physicscore2d的依赖；本机6.4的physics2d/package.json的dependencies为空。
- game1原始6.4工程清单只有audio、physics2d、imgui、imageconversion、jsonserialize、screencapture六项；这六项已逐个核对，均存在于6.4安装。候选清单并非凭报错随意删除，而是恢复原工程的依赖集合。
- Assets/*.cs搜索未发现对Unity.U2D.Physics或三个模块名称的直接引用。此项仅说明当前搜索范围，没有宣称完整构建兼容。

这是复制6.6保存过的工程后再由6.4打开时的依赖不兼容，原来源为6.4并不能消除升级过程中加入的依赖。此错误不直接表示地图损坏或渲染管线错误；本机安装对比已给出原因，没有依据把它归结为普通联网下载失败。

## 建议处理

先在错误弹窗选择Quit并退出编辑器。备份Project6.4/Packages，在该副本中将manifest.json恢复为六项原始模块，保留physics2d；将packages-lock.json改名备份，使6.4重新解析生成锁文件。重开后再检查场景、编译和运行结果；包解析恢复不等于所有跨版本问题已解决。

本目录manifest-unity64.json是准备好的候选清单，builtin-module-verification.json记录静态检查。未覆盖项目清单、改名锁文件、操作当前Unity进程或重新构建。用户仍控制当前错误弹窗。

## 改动与回滚

仅新增本目录资料并追加CHANGELOG。原CHANGELOG和Project6.4的两份包文件已备份到Docs/Backups/20261029-unity64-package-diagnosis/。工程文件实际未改，无需回滚工程；文档可恢复备份总表。未替换试玩/工程压缩包，也未提交或推送Git。

参考：Unity故障排除文档说明包管理器依照manifest依赖解析：https://docs.unity3d.com/ja/2023.2/Manual/upm-errors.html 。本问题的具体模块差异以本机两版编辑器安装目录与原工程清单为直接证据。
