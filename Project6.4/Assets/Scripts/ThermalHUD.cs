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
                string prompt=Level.CanInteract?"按 E 交互："+Level.GoalName:ready!=null?"攀岩点范围内：按空格二次跳跃":null;
                return new GameHUDContent{
                    title=Level.Title,controls=CommonControls+"   右键 信标/传送   S 防寒/隔热   W 滑索   E 交互",
                    statistics=Statistics(Level.session),status=DashStatus(p.motor)+"   传送 "+(p.TeleportCharged?"就绪":"已用")+"   核心："+t.ModeName+"   "+MotorState(p.motor)+race,
                    prompt=prompt,tutorial=ready!=null?"在攀岩点周围可以二次跳跃":null,dialogueTitle=Level.GoalName,dialogue=Level.Dialogue,
                    choice1=Level.Choosing?"1  留下吧，我属于这个即将消失的世界":null,
                    choice2=Level.Choosing?"2  不，还没有结束，一切都还来得及":null,
                    completion=Level.Completion,completionTitle=Level.CompletionTitle,
                    help=CommonHelp+"\n右键发射信标/再次右键传送，可打断冲刺；传送后短暂滞空。\nS 防寒/隔热；青色为冷、粉红为热，模式不符接触属性地形/滑索会死亡。\nW 抓滑索，A/D 换向；空格跳离，端点离索可短暂补跳。\n小方块为攀岩点：匹配圆环内按空格补跳，并恢复冲刺与传送。\n同一点每段腾空仅一次；E交互，残章选择可点击或按数字1/2。"+(Level is Lighthouse.LighthouseLevel?"\n灯塔离开休整点后限时坍塌；接触新休整点或死亡复活重置追赶。":"")};
            }
        }
    }
}
