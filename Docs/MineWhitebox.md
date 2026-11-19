# 矿洞独立白盒

场景：Project6.4/Assets/Scenes/MineWhitebox.unity。Unity 6000.4.12f1，直接Play；Windows独立入口为根目录mine.cmd，程序Play64/Mine/MineWhitebox.exe，不接入前两关进度。

来源：maps/矿洞地图.xlsx，Sheet1，170×272格，一格一单位，纵轴翻转到Unity向上坐标。黑色5546格为可攀附地形，灰色580格为不可悬停实体，红色585格为尖刺，绿色230格组成12条滑索。地形、尖刺、滑索均保留Excel地址，首次建立场景后不自动重新生成。

操作及提示沿用前两关：A/D移动，空格长短跳/蹬墙，左键沿身体外箭头冲刺，右键发射信标/再次右键传送，W抓索，A/D换向，空格跳离；R最近复活点、Ctrl+R重开，Esc暂停，H帮助，Tab全景，B路线。传送可以打断冲刺并清除冲刺速度；本次矿洞沿用当前森林的滞空0.5秒和滑索速度12，端点离索可短暂补跳。第三关共用Movement、镜头、方向箭头、深绿提示面板及阿里巴巴普惠体。

## 地图特殊标记与交互

- 浅绿色EK30附近为工作台，靠近按E，逐页展示根Assets/文案.pdf第三页关于资金、人员离职和项目停工的三段原文。
- 橙色DD54附近为电梯重启控制台，E交互并读完提示后开启电梯。
- DL33和DL126橙色横条作为同一电梯的上/下停靠站，中心(86,254.5)/(86,161.5)。只创建一个真实运动平台，初始停在上站；启用后往返、到站停留。可以自由走跳和传送，不锁定角色或改变角色父对象。
- 两处紫色区域按复活点解释，实际复活锚点放在附近安全静态地面，遵循最后接触优先。深蓝标记保留为路线提示。
- 两处浅蓝标记分别为深井入口提示和井底遗物，最深处E交互展示“获得：比特币”“当初的人们没有发现它的价值，直到它求而不得”，读完完成本关。井口提示不会提前触发通关。

工作台、重启开关和井底遗物文案由PDF第三页确认；用户已确认紫色块为复活点、橙色横条为电梯平台。深蓝路线标记和井口浅蓝提示是原表无文字图例时采用的默认解释，可在场景的MineInteraction/SeasideCell中调整。不会将这些解释写回工作簿。

## 参数和结构

- Assets/Resources/GameSystems.asset：所有关卡系统字体、公共移动资源、镜头、下滑、复活及箭头参数。
- Assets/Settings/Movement.asset：所有关卡移动/跳跃/冲刺参数，原数值保持。
- Assets/Settings/MineTraversal.asset：矿洞传送射程、滞空秒数、滑索速度/抓取/土狼时间和传送打断冲刺开关。
- Assets/Settings/Mine.asset：电梯速度8单位/秒、到站停留2秒、交互距离3.5单位。
- Mine/Gameplay/Map：Terrain、Hazards、Ziplines、Checkpoints、Interactions、MineLift及RouteGuides；Mine/Systems：GameSession、MainCamera、MineLevel、HUD和信标。

矿洞复用现有ForestPlayer/ForestSettings/ForestZipline等公共遍历能力，保留历史类型名和命名空间以保持前两关序列化引用。矿洞不创建森林水车、瀑布或森林关卡流程。可移动地面支持抽取到根目录MovingSurface，森林水车踏板与矿洞电梯共用真实接触处理。

## 构建与验证

在关闭的主工程或隔离副本运行Tools/BuildAndTest.ps1 -MineWhitebox，构建现有保存场景并执行独立EXE物理检查；Tools/PackageRelease.ps1 -MineWhitebox更新矿洞试玩和工程ZIP。

自动检查覆盖逐格黑/灰碰撞及尖刺、12条真实滑索、公共参数与字体、冲刺打断/滞空、工作台原文、电梯开关/往返/乘坐/跳离/暂停、两个真实复活点、尖刺死亡、遗物通关与重开。尚未完成人工从入口走完整条路线的难度验收。日志与备份见Docs/Logs/Work/20261119-forest-mine-font/及Docs/Backups/20261119-forest-mine-font/。
