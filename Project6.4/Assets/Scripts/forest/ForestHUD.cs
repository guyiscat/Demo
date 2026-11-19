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
                    help=CommonHelp+"\n右键可打断冲刺并发射信标，再次右键或信标停下时传送。\n传送后滞空"+actor.settings.teleportHoverDuration.ToString("0.#")+"秒，可A/D移动或左键冲出。\n靠近滑索按W，A/D换向，空格跳离，离索后短暂可补跳。\n前行几步展示全图，停留"+actor.settings.introHoldDuration.ToString("0.#")+"秒后恢复。\n水车平台可走跳，需要主动跟上；到达上方终点完成本关。\nTab全景遮挡瀑布另一侧；穿过瀑布后镜头平滑换侧。",
                    completion="已到达水车上方终点平台。"
                };
            }
        }
    }
}
