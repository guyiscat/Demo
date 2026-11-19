using Seaside;

namespace Mine
{
    public sealed class MineHUD : GameHUD
    {
        public MineLevel level;
        protected override SeasideSession Session=>level==null?null:level.session;
        protected override void Replay(){level.Restart();}
        protected override void AdvanceDialogue(){level.AdvanceDialogue();}
        public override GameHUDContent Content
        {
            get
            {
                var p=level.player;
                string state=p.BeaconActive?"信标滞空":p.PostTeleportHoverRemaining>0?"传送后滞空":p.RidingRail!=null?"滑索滑行":p.RidingSurface!=null?"电梯乘坐":MotorState(p.motor);
                return new GameHUDContent
                {
                    title="第三关 · 矿洞 MINE",
                    controls=CommonControls+"   右键 发射信标/传送   W 抓住滑索   E 交互",
                    statistics="电梯 "+(level.lift.Activated?"已启动":"待启动")+"   "+Statistics(Session),
                    status=DashStatus(p.motor)+"    传送 "+(p.BeaconActive?"信标飞行中":p.TeleportCharged?"就绪":"已用")+"    "+state,
                    help=CommonHelp+"\n右键可打断冲刺并发射信标，再次右键或信标停下时传送。\n传送后短暂滞空；靠近滑索按W，A/D换向，空格跳离。\n灰色地形可站立与蹬墙，无法贴墙悬停。\n靠近工作台、橙色控制台和浅蓝矿物，按E交互。\n启动电梯后可乘坐往返；可自由走动、跳跃或传送离开。\n探索矿洞最深处，调查遗物后完成本关。",
                    completion="已找到矿洞深处的比特币。",
                    prompt=level.Nearby==null?null:level.Nearby.Prompt,
                    dialogueTitle=level.DialogueTitle,dialogue=level.Dialogue
                };
            }
        }
    }
}
