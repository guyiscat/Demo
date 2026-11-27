using Seaside;

namespace Forest
{
    public sealed class ForestHUD : GameHUD
    {
        public ForestLevel level;
        protected override SeasideSession Session=>level==null?null:level.session;
        protected override void Replay(){level.Restart();}
        public override GameHUDContent Content
        {
            get
            {
                var actor=level.player;
                string state=actor.BeaconActive?"信标滞空":actor.PostTeleportHoverRemaining>0?"传送后滞空 "+actor.PostTeleportHoverRemaining.ToString("F1")+"s":actor.RidingRail!=null?"滑索滑行（A/D换向）":actor.RailCoyoteRemaining>0?"离索可补跳":actor.RidingWheel!=null?"水车运送":MotorState(actor.motor);
                return new GameHUDContent
                {
                    title="第二关 · 森林 FOREST",
                    controls=CommonControls+"   右键 发射信标/传送   W 抓住滑索   空格 跳离滑索",
                    statistics="水车解锁 "+level.wheel.UnlockCount+"/2"+(level.wheel.forceRotation?"（预览转动）":"")+"   "+Statistics(Session),
                    status=DashStatus(actor.motor)+"    传送 "+(actor.BeaconActive?"信标飞行中":actor.TeleportCharged?"就绪":"已用")+"    "+state+"    瀑布可见侧 "+(actor.waterfall.VisibleSide<0?"左":"右"),
                    help="右键发射信标，再按一次传送；传送可以打断冲刺。\n\n传送后会短暂停在空中，不能左右移动，但可以冲刺。\n\n靠近滑索按W抓住，A/D换向，空格跳离。离开滑索后仍有短暂的起跳机会。\n\n水车平台会转动，注意跟上平台。",
                    completion="已到达水车上方终点平台。"
                };
            }
        }
    }
}
