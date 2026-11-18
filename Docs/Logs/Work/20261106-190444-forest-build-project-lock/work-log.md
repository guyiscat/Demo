# 森林构建失败：同工程编辑器占用

2026-11-06，用户运行 BuildAndTest.ps1 -ForestWhitebox，原失败日志为 Docs/Logs/Build/20261106-190317-099/。

## 只读诊断与证据

status.json：editorExit=1，result=FAIL，1.66秒内退出，尚未进入执行方法、编译或检查。editor-build.log 在设置 projectPath 后即退出，无 C# 编译错误和测试失败报告。

Win32_Process 确认原 Unity 编辑器进程 42876 正在打开 Project6.4/Assets/Scenes/ForestWhitebox.unity。只读尝试以 FileShare.None 打开 Project6.4/Temp/UnityLockfile，收到被其他进程使用的 IOException，确认同一工程仍被原编辑器锁定。未删除锁文件，未终止/关闭编辑器，未保存用户场景。

## 处理

用户需先退出 Play，在编辑模式确认水车配置并 Ctrl+S 保存场景，然后关闭此工程的 Unity 编辑器。Unity Hub 可保留。待编辑器退出后重新执行原 BuildAndTest.ps1 -ForestWhitebox；成功后再运行 PackageRelease.ps1 -ForestWhitebox。

未修改游戏、场景或构建脚本，未代替用户关闭编辑器，未重新执行构建或测试、未重新打包，现有成品未更新。不将以前通过的结果计入本次。只新增诊断日志并更新 CHANGELOG；CHANGELOG 修改前备份见同时间戳 Backups 目录。未提交/推送/发布。
