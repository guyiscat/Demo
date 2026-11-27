using Expedition;
using Seaside;
namespace Snow
{
    public enum SnowOutcome { Exploring, Stayed, CarriedFragment }
    public sealed class SnowLevel : ThermalLevel
    {
        public SnowOutcome Outcome{get;private set;}
        public void RestoreOutcome(bool carried){if(carried)Outcome=SnowOutcome.CarriedFragment;}
        bool choosing;
        public override bool Choosing=>choosing;
        public override string Title=>"雪山 / SNOW MOUNTAIN";
        public override string GoalName=>"残缺的策划案";
        public override string Completion=>Outcome==SnowOutcome.Stayed?"你选择留下，属于这个即将消失的世界。":"你带上了残章。返回海边后向左走，可以前往灯塔。";
        public override string CompletionTitle=>Outcome==SnowOutcome.Stayed?"留下 / END":"带上残章 / CONTINUE";
        public override bool BeginInteraction()=>Outcome==SnowOutcome.Exploring&&OpenDialogue(new[]{"一份不全的游戏 demo 策划案。","未焚烧干净的残章中还有着对你的设定描述。"});
        public override void AdvanceDialogue()
        {
            if(pages==null||session.InputBlocked||choosing)return;
            if(page+1<pages.Length){page++;return;}
            if(Outcome==SnowOutcome.CarriedFragment){CloseDialogue();CampaignFlow.UnlockLighthouse(session);return;}
            pages=new[]{"你决定如何面对这个即将消失的世界？"};page=0;choosing=true;
        }
        public override void Choose(int choice)
        {
            if(!choosing||session.InputBlocked||choice<0||choice>1)return;
            choosing=false;
            if(choice==0){Outcome=SnowOutcome.Stayed;CloseDialogue();CampaignFlow.SnowEnding(session);}
            else{Outcome=SnowOutcome.CarriedFragment;pages=new[]{"这个东西也许还有用。"};page=0;}
        }
        protected override void ResetProgress(){Outcome=SnowOutcome.Exploring;choosing=false;}
    }
}
