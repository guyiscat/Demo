using Seaside;
namespace Expedition
{
    public abstract class ThermalHUD : GameHUD
    {
        protected abstract ThermalLevel Level{get;}
        protected override SeasideSession Session=>Level==null?null:Level.session;
        protected override void Replay(){Level.Restart();}
        protected override void AdvanceDialogue(){Level.AdvanceDialogue();}
        protected override void ChooseDialogue(int choice){Level.Choose(choice);}
        public override GameHUDContent Content
        {
            get
            {
                var p=Level.player;var t=Level.thermal;var ready=t.NearestReadyPoint();
                string race=Level is Lighthouse.LighthouseLevel tower&&tower.collapse!=null?"   坍塌限时 "+tower.collapse.Remaining.ToString("F0")+"秒":"";
                string prompt=Level.CanInteract?"按 E 交互："+Level.GoalName:ready!=null?"攀岩点范围内：可反复按空格补跳":null;
                return new GameHUDContent{
                    title=Level.Title,controls=CommonControls+"   右键 信标/传送   S 防寒/隔热   W 滑索   E 交互",
                    statistics=Statistics(Level.session),status=DashStatus(p.motor)+"   传送 "+(p.TeleportCharged?"就绪":"已用")+"   核心："+t.ModeName+"   "+MotorState(p.motor)+race,
                    prompt=prompt,tutorial=ready!=null?"靠近匹配属性攀岩点，可反复按空格补跳":null,dialogueTitle=Level.GoalName,dialogue=Level.Dialogue,
                    choice1=Level.Choosing?"1  留下吧，我属于这个即将消失的世界":null,
                    choice2=Level.Choosing?"2  不，还没有结束，一切都还来得及":null,
                    completion=Level.Completion,completionTitle=Level.CompletionTitle,
                    help="从村庄取得核心后，按 S 切换防寒 / 隔热。\n\n青色区域需要防寒，粉红区域需要隔热；墙面和滑索也要匹配。\n\n靠近属性匹配的攀岩点可反复按空格起跳，并恢复冲刺和传送。\n\n"+(Level is Lighthouse.LighthouseLevel?"离开休整点后，坍塌开始追赶。到达下一处休整点可以重新计时。":"灰色墙面不能贴墙或蹬墙跳。")};
            }
        }
    }
}
