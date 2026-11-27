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
                    help="触摸黄色开关后，在电梯旁按 E 乘坐或呼叫。电梯不会自行往返。\n\n触摸紫色开关可打开大门。\n\n移动鼠标可以照亮附近，靠近可调查的物品时按 E。\n\n从入口竖井向上跳，或在返回标志按 E，可回到村庄。",
                    completion="已找到矿洞深处的比特币。",
                    prompt=level.Nearby==null?null:level.Nearby.Prompt,
                    dialogueTitle=level.DialogueTitle,dialogue=level.Dialogue
                };
            }
        }
    }
}
