# 2026-10-29：说明6.4工程打包Windows游戏的流程

用户询问如何打包游戏。本次提供操作教程，未请求立即构建。

本地核对Project6.4版本6000.4.12f1；EditorBuildSettings现有两个启用场景，顺序为Seaside01、SeasideArtPreview。只打开场景不会决定打包启动场景，应检查实际构建配置的Scene List；如果有profile覆盖，以其列表为准。

核对Project6.4/Tools/BuildAndTest.ps1仍默认6.6编辑器，且project目录计算固定为Demo/Project，因此本次不指导用户调用该脚本，以免构建错误工程。脚本没有被修改。

建议用户先停止Play并保存场景/资源，通过File > Build Profiles选择Windows、Intel64/x86_64并激活目标，在Scene List指定并排序所需场景。新建自定义profile若未显示Scene List，可通过Add Settings添加。选择Build或Build and Run，输出到独立Demo/Play64_Test目录并指定exe名；此目录与旧Play分开，且符合现有运行日志按二级相对路径定位Demo根目录的逻辑。该建议目录本次未创建，现有.gitignore未专门列出此新目录。

说明Windows成品需要整个输出文件夹（exe、同名_Data、UnityPlayer.dll及其他生成文件），不能只复制exe。Figures位于Assets之外，仅放入Figures的PNG不会自动成为已引用游戏资源。首次构建通常耗时较长，实际是否成功以构建结果及成品运行检查为准。

官方参考：
- https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/build-settings/build-profile-scene-list
- https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/build-settings/build-profiles-reference
- https://docs.unity3d.com/cn/6000.0/Manual/WindowsStandaloneBinaries.html

仅新增工作说明、追加CHANGELOG，原总表备份位于Docs/Backups/20261029-windows-build-guide/。未改工程、场景列表、构建脚本或试玩，没有执行6.4构建和测试，也未更新交付zip或推送Git。
