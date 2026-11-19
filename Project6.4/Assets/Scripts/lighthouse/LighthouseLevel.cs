using Expedition;
namespace Lighthouse
{
    public sealed class LighthouseLevel : ThermalLevel
    {
        public bool Lit{get;private set;}
        public UnityEngine.SpriteRenderer lamp;
        public LighthouseCollapse collapse;
        public override string Title=>"灯塔 / LIGHTHOUSE";
        public override string GoalName=>"塔顶灯台";
        public override string Completion=>"你在道路坍塌前登上塔顶并点亮灯台。\n《DEMO》的最后一段旅程在此结束。";
        public override string CompletionTitle=>"灯塔终点 / END";
        public override bool BeginInteraction()=>!Lit&&OpenDialogue(new[]{"塔顶的灯台仍然完好。","点亮灯台，让光照向远方。"});
        public override void AdvanceDialogue()
        {
            if(pages==null||session.InputBlocked)return;
            if(++page<pages.Length)return;
            Lit=true;if(lamp!=null)lamp.color=new UnityEngine.Color(1,.87f,.35f);CloseDialogue();session.Complete();
        }
        protected override void ResetProgress(){Lit=false;if(lamp!=null)lamp.color=new UnityEngine.Color(.52f,.62f,.57f);if(collapse!=null)collapse.ResetSegment();}
    }
}
