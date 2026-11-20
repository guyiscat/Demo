# 五关流程、安全存档及继续游戏

用户需求：为新增关卡添加合适存档点；按海边→森林→矿洞→雪山→灯塔自动连接；探究第四个开始页选项；确认阿里巴巴普惠体使用。用户随后明确“同时添加继续游戏第四项”，因此实施跨进程持久化和四项菜单。

修改前备份Docs/Backups/20261120-campaign-saves/，包括Assets/Tools/ProjectSettings及相关文档。开工时第一关已有用户保存改动（角色位置、某墙可攀附及材质、会话字段），与Git差异记录user-starting-changes.diff。本轮保持其字节，不覆盖这些改动。Unity开工时打开StartGame，工作期间用户自行切换到海边；不操纵、保存或关闭其编辑器。地图增量在既有隔离工程中进行，绝不重建地图。

## 实施

CampaignSetup增量在原地图已有中立静态落脚面增加存档点，身体无刺/无实体重叠，离旧点至少9单位：矿洞新增3—7号5个，共7；雪山新增6号1个，共6；灯塔新增5号1个，共5。安全坐标/候选未采用原因见setup-report.txt与CampaignAndSaves.md。无合适中立面的候选不强放点，不创建新平台，不改变地形/尖刺。

新增CampaignFlow、CampaignSave、CampaignCheckpointPersistence公共系统，SeasideSession统一在激活点/关卡入口/关键机关变化/退出时存档；保存最近点、关卡、时间/死亡及矿洞电梯/工作台、森林左右水车锁，恢复时解析当前场景锚点，不记录不安全的瞬时位置。存档位于persistentDataPath/campaign-save.json；原子.tmp替换、上一份.bak、开始新游戏.previous；输入字段和白名单关卡校验，坏主档回退备份。测试参数仅在明确检查模式使用独立文件，不读写玩家实际进度。

原有终点调用统一Complete后0.8秒自动加载下一关，提前存下一关入口，重复完成不重复排队。雪山留下结束；带走残章继续灯塔；灯塔最终结束。六场景进入全局BuildSettings，各单关程序包含后续及菜单，保留原五个入口。nextLevelDelay在GameSystems暴露。暂停/结束面板增加返回开始页。

开始页三按钮区域改为四项开始/继续/选项/退出，按枚举动作绑定；原8层主题美术不改。四按钮为共享阿里巴巴普惠体TextMesh、适配现有主题面板，无档继续置灰，覆盖现存进度前提供新游戏确认。字体副本与用户根Assets原字体SHA256均为0d0b12d1ea1cbbc0bf152da7a39f2b02a385a6c3ee8b78475e50f1f31379a364；GameSystems引用有效。

## 实际验证

全部使用Tools/BuildAndTest.ps1在Docs/Backups/20261118-shared-systems-script-folders/Validation/Project6.4隔离工程进行，主工程不关闭或自动保存。最终运行：

- 20261120-221931-875：启动页/第一关、完整五关流程、磁盘存档检查、独立写/读两个游戏进程全部exit=0，完整流程190项PASS。
- 20261120-222339-305：森林构建及原机制实际EXE通过。
- 20261120-222356-969：矿洞构建及新增存档点/原机制实际EXE通过。
- 20261120-222415-207：雪山构建及新增存档点/原机制实际EXE通过。
- 20261120-222432-944：灯塔构建及新增存档点/坍塌回归实际EXE通过。

最终报告精确统计见final-test-summary.json，原始成功及早期报告同步Docs/Logs/Build。各次editor-reports还会复制未执行入口的旧文件，本轮统计只采用实际执行的报告。最终源码/文档/工具diff检查和C#编译诊断在verification.json核对，不把旧结果当新测试。

流程检查实际加载全部五scene，原通关/叙事条件触发后等待自动跳转，测试进入下一关之前已保存下一关入口；真实CP激活/落脚/死亡复活，森林双锁继续恢复，矿洞电梯/工作台继续恢复，坏主档备份恢复，未知scene拒绝，空档禁用继续，新游戏确认及两种雪山结局/灯塔终局。跨进程-saveProbe write/read使用各构建日志目录的test-save.json，未触碰玩家实际存档。

使用computer-use技能打开真实Windows成品，640小窗和1920放大窗口核对四项菜单：白字可读、继续在空档时置灰，游戏选项仍正确打开且中文无缺字/裁切；工具截图留在会话记录。第一次菜单TextMesh字符尺寸过大导致重叠，实际画面发现后改为0.032，并增加菜单文本边界校验，重新构建和运行完整流程/全部受影响成品。早期结果留存，未将其作为最终交付。

本轮为流程和存档的局部/端点自动检查，未执行人工从出生点完整跑完五关的路线难度验收。原始工作簿/素材不修改；主工程用户开工已保存的第一关改动保留，森林原scene不改；新三关只增存档标记，既有实体/尖刺/对象变换由verification.json校验。

## 交付与回滚

已备份之前Play64和根目录ZIP；最终场景、GameSystems和BuildSettings及成品按通过验证的隔离版本同步，使用规定PackageRelease更新五套Windows/工程包及中文别名。最终文件哈希/元数据/几何保留/压缩包CRC核对以verification.json的PASS为准。存档默认路径为用户AppData/LocalLow/Friends Studio/Seaside Whitebox/campaign-save.json，不打包玩家存档。当前statgame.cmd为完整游戏入口。

回滚仅恢复Docs/Backups/20261120-campaign-saves/内本轮修改的Assets/Tools/ProjectSettings与文档，去除本轮新增Campaign脚本及新存档点，保留用户后续编辑和本地存档，不整仓重置。没有提交或推送。
