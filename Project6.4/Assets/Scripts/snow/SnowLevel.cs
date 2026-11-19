using Expedition;
namespace Snow
{
    public enum SnowOutcome { Exploring, Stayed, CarriedFragment }
    public sealed class SnowLevel : ThermalLevel
    {
        public SnowOutcome Outcome{get;private set;}
        bool choosing;
        public override bool Choosing=>choosing;
        public override string Title=>"雪山 / SNOW MOUNTAIN";
        public override string GoalName=>"残缺的策划案";
        public override string Completion=>Outcome==SnowOutcome.Stayed?"你选择留下，属于这个即将消失的世界。":"你带上了残章，雪山探索完成。前往灯塔的故事仍将继续。";
        public override string CompletionTitle=>Outcome==SnowOutcome.Stayed?"留下 / END":"带上残章 / CONTINUE";
        public override bool BeginInteraction()=>Outcome==SnowOutcome.Exploring&&OpenDialogue(new[]{"一份不全的游戏 demo 策划案。","未焚烧干净的残章中还有着对你的设定描述。"});
        public override void AdvanceDialogue()
        {
            if(pages==null||session.InputBlocked||choosing)return;
            if(page+1<pages.Length){page++;return;}
            if(Outcome==SnowOutcome.CarriedFragment){CloseDialogue();session.Complete();return;}
            pages=new[]{"你决定如何面对这个即将消失的世界？"};page=0;choosing=true;
        }
        public override void Choose(int choice)
        {
            if(!choosing||session.InputBlocked||choice<0||choice>1)return;
            choosing=false;
            if(choice==0){Outcome=SnowOutcome.Stayed;CloseDialogue();session.Complete();}
            else{Outcome=SnowOutcome.CarriedFragment;pages=new[]{"这个东西也许还有用。"};page=0;}
        }
        protected override void ResetProgress(){Outcome=SnowOutcome.Exploring;choosing=false;}
    }
}
