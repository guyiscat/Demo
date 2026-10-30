# 2026-10-28：确认手动安装的Unity6000.4.12f1

用户提供手动安装目录D:/Development/Unity/6000.4.12f1，要求确认是否可见。

只读检查确认编辑器存在：D:/Development/Unity/6000.4.12f1/Editor/Unity.exe。文件ProductVersion为6000.4.12f1_3ca267ce8005，FileVersion为6000.4.12.3973735。Editor/Data/PlaybackEngines包含windowsstandalonesupport目录。

更正此前环境结论：之前只枚举C:/Program Files/Unity/Hub/Editor，发现6000.6.7f1，没有检查本次提供的手动安装路径。因此此前“本机仅安装6.6/未安装6.4”的说法不准确；当时实际构建使用6.6的日志仍有效，但不能据此推断6.4不存在。6.4编辑器版本号与原game1工程匹配。

本次未启动编辑器、验证授权或运行构建，不能仅凭程序存在宣称其运行/构建已经通过。未切换Demo版本、修改工程或构建脚本。新增本记录并追加CHANGELOG；原总表备份于Docs/Backups/20261028-unity64-install-discovery/。
