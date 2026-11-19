# 当前项目与第一关旧场景清理

需求：删除第一关无用旧scene及当前项目中的无用文件。开始前备份Assets、Tools、ProjectSettings与会修改的文档到Docs/Backups/20261119-project-cleanup/；被移出工作目录的文件保存在其中Removed/，保留可恢复副本。用户Unity仍打开有未保存改动的TheFirstScene_Seaside，不保存、关闭或切换用户场景。

实施清单见cleanup-manifest.json、actions.json，引用审计见asset-audit.json。仅无序列化引用不作为删除依据，同时检查Resources动态加载、编辑器路径、构建入口、回归测试和美术来源。

已移出Assets：旧SeasideArtPreview、与当前保存第一关字节相同的Recovered副本、Assets/_Recovery/0.unity及.meta；7个已完成任务的第一关生成/升级/旧预览/迁移编辑器脚本、2个旧整图预览运行脚本及.meta；未使用的旧Player.png及.meta。当前Scenes只保留启动页和5个实际关卡，当前六scene字节不修改。

Seaside01仍是40项动作/地图源数据检查和203项回归的必要基准，将其连同.meta移至Assets/Editor/Tests/Fixtures/SeasideMovementBaseline.unity，保持原场景字节和GUID。新增SeasideTestScenes统一入口，所有关卡构建、墙跳/镜头检查改引用此夹具，场景不会加入玩家构建。已有CameraBackgroundBoundsChecks改检查当前第一关及此无背景夹具，保留有效镜头检查。

旧PlaytestFixBuild及生成器移除后，BuildAndTest与PackageRelease无参数默认构建/打包当前StartGame+TheFirstScene_Seaside；第一关构建现在也执行共用动作回归。Windows Build Profile取消仅启动页的独立覆盖，使用现有全局场景表，避免编辑器直接构建漏掉第一关。全局场景表不改。

移出根目录：旧build（含旧手动第一关成品/ZIP）、两份重复解压森林试玩、列举废弃入口的README-Unity64.md。保留唯一Play64当前成品、当前有效ZIP与五个启动器；原始地图/PDF/PSD/字体、角色参考图、尚未实例化的美术图、布局文件和SeasideTools动态加载预制件继续保留。Library/Temp/UserSettings在用户编辑器运行中不动；历史日志及旧备份不清空。

README、CONTRIBUTING、脚本目录说明、工程导览和层级文档更新为当前入口，历史日志保持原文。

## 实际验证

隔离工程：Docs/Backups/20261118-shared-systems-script-folders/Validation/Project6.4。同步清理后的Assets、Tools、ProjectSettings，隔离副本废弃内容也移到本轮备份RetiredValidation，避免残留掩盖引用问题。规定的BuildAndTest.ps1实际执行五次，全部editorExit=0且实际EXE exit=0：

- 20261119-232524-143：无参数默认StartupPage，实际从启动页进入当前第一关并通过公共系统38项。
- 20261119-232654-870：森林运行301项、森林场景1733项。
- 20261119-232711-773：矿洞运行112项、矿洞场景3635项。
- 20261119-232725-788：雪山运行186项、雪山场景3742项。
- 20261119-232739-496：灯塔运行202项、灯塔场景3401项。

五次均执行迁移后的公共动作测试夹具，基础40项、回归203项；各次实际报告计数在final-test-summary.json，未将editor-reports中的其他历史快照当作本轮重跑。最终构建无C# error/warning。CameraBackgroundBoundsChecks.Run额外在隔离工程执行，exit=0，对当前第一关bg3与迁移后的无背景夹具检查通过，原始日志camera-checks.log及camera-background-checks/保留。源码/工具/文档git diff --check通过，仅有既有行尾格式提示。

source-verification.json核对六个当前scene和.meta、Movement、GameSystems及全局EditorBuildSettings与清理前完全相同；测试夹具场景字节/GUID相同；主工程Assets与本次构建来源一致；成品与已验证输出一致；全部资源.meta和序列化GUID引用有效，废弃路径/脚本没有悬空引用。用户主Unity始终保留未保存海边场景，未自动保存、关闭或切换。没有进行新的人工全路线通关；本次游戏玩法和关卡几何未修改。

## 交付与回滚

工作目录移出477个文件共714788018字节（约682MiB），其中大部分为旧build及两份重复解压试玩；原件保留在Removed备份，因此不声称磁盘物理空间已释放。当前Play64同步五关本轮重新构建的成品，使用规定PackageRelease分别更新五关Windows/工程共10个ZIP及4个中文试玩别名。压缩包CRC、所需程序集/运行库、源码字节和无废弃场景/工具的校验以verification.json的PASS为准。

回滚：按actions.json从Docs/Backups/20261119-project-cleanup/Removed恢复被移出的内容；按relocate记录将测试夹具及.meta移回；恢复本轮修改前的Assets/Tools/ProjectSettings/文档。不要整仓重置或覆盖用户之后的改动。既有历史日志保留原文。没有提交、推送或对外发布。
