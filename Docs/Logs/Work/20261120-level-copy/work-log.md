# 文案逐页核对与补齐

需求：重新考察根Assets/文案.pdf，排除中转站村庄，其余关卡对应配置完整；海边当前有美术仍须按参考位置显示文案。本轮无提交/推送授权，不提交或推送。

修改前备份Docs/Backups/20261120-level-copy/，包含Scripts、Editor、Scenes、Resources、Tools、Play64及修改文档。此前五关存档/继续游戏未提交工作与用户保存的海边改动均保留。未操纵、保存、切换或关闭用户Unity场景。渲染核对PDF第1—3页及原第6—7页，村庄第4—5页明确不实施。

## 审计与实现

完整逐项核对、参数与触发范围见Docs/LevelCopy.md。原矿洞工作台3页/开关1页/遗物2页、雪山残章2页和两个选择均存在；缺海边两页与按E交互，缺六处按位置出现的顶部原文。森林已有W说明、雪山已有通用冷热提示但并不等于PDF指定区域提示。

新增公共TutorialZone（Collider2D区域、文字、优先级）与LevelTutorials（按角色当前位置查询，不依赖一次性碰撞回调），GameHUD统一顶部区域显示，与主控栏、底部交互和模态对话互斥。ThermalHUD去除一直占底部的合并冷热文案，改为指定顶部区域及可用攀岩点上下文提示。所有文本沿用GameUIStyles/GameTypography。

海边专用SeasideStory置于Scripts/seaside，显式引用当前美术lighthouse的Collider2D。靠近按E、冻结、原文两页、E或按钮继续；旧FinishTrigger只在没有SeasideStory的老夹具中直接通关，当前海边读完两页才调用统一Complete。传送能力文本后衔接已支持传送的森林，不引入一套不同的海边传送实现。死亡/R/继续不会恢复冻结的中间对话。第五关无PDF单列文案，保留已有结局。

LevelCopySetup.Install仅在隔离batch工程中增量配置三个现有scene，禁止重复安装覆盖已有提示；未调用任何地图生成。LevelCopyEditorChecks接入各构建公共校验。同步场景前逐个比较全部原对象，除Session新增组件/子列表与ForestPlayer首次保存自动补出的environment:{fileID:0}默认空引用，原有对象序列化数据不变，见geometry-verification.json；主工程场景当时仍与开工备份一致，才同步已验证增量结果。

## 本次实际检查

隔离工程仍为Docs/Backups/20261118-shared-systems-script-folders/Validation/Project6.4。全部通过规定Tools/BuildAndTest.ps1，最终源码与已测输出同步主工程/Play64。

- 20261120-224917-525：StartupPage，editor+startup+campaign+两次saveProbe全部exit0。
- 20261120-225113-585：ForestWhitebox，editor+森林机制exit0。
- 20261120-225130-420：MineWhitebox，editor+矿洞机制exit0。
- 20261120-225149-182：SnowWhitebox，editor+雪山机制exit0。
- 20261120-225205-081：LighthouseWhitebox，editor+灯塔机制exit0。

精确统计final-test-summary.json：公共系统7306，文案29，原动作40+203；流程及文案225，saveProbe3/4；森林301、矿洞127、雪山189、灯塔205项运行检查通过。过程真实加载五场景，检查区域进入/离开、海边不能远读/旧终点不提前完成、帮助遮挡/复活取消/继续恢复、两页顺序后过关；矿洞逐页内容与真实机关，雪山原文/选择/继续页后过关和留下结束。自动机制检查仍非人工完整路线通关。

使用PDF技能做逐页图文核对；使用computer-use技能检查真实Windows试玩窗口，海边灯塔按E打开第一页再按E第二页，放大窗口中文字清晰。LevelCopyVisualChecks仅-copyVisual启用，隔离真实玩家存档；1440×900和1024×768各13张画面在visuals-visible/与visuals-1024x768/。全部顶部提示、海边对话、矿洞最长工作台页、雪山残章/选择无裁切重叠。

## 失败和限制（保留原始记录）

第一遍隐藏窗口ScreenCapture得到黑图，visuals/和hidden-visual-attempt/不能用于视觉验收，之后用可见窗口重拍。可见窗口完成13张截图后在Application.Quit时返回-1073741819；这只证明截图内容生成，不记作退出测试通过。

进一步以本次程序普通开始页点击“退出游戏”，normal-exit.log原生崩溃，exit=-1073740771。再启动开工前备份Play64中未修改程序，点击同一按钮，baseline-exit.log同样exit=-1073740771；原生栈同样UnityPlayer/USER32窗口/DPI回调。可确认退出异常并非本次文案新增导致；根因未定位，未改Unity/驱动/系统设置，不能声明普通窗口退出验收通过。自动batch和nographics机制测试退出均正常。最终答复应明确这一已有问题。

git diff --check发现Unity新增序列化空m_Name字段尾空格，其中部分来自前次存档工作；不为清尾空格重写已验证scene。C#和本次Markdown无空白错误。没有重新运行场景生成器，没有改动根PDF/Excel/字体/美术。

## 交付和回滚

完整入口statgame.cmd，五套Play64和Windows/Project包按PackageRelease.ps1更新；校验结果见verification.json。保留根目录ZIP替换前版本于各次Packaging日志。回滚使用本次备份逐文件恢复本轮修改、删除本次新增组件/脚本及meta，保留此前工作和后续用户改动，不整仓reset或整目录覆盖。
