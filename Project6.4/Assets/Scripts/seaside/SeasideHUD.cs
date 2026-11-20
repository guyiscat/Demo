namespace Seaside
{
    // Scene adapter; rendering and common wording live in GameHUD.
    public sealed class SeasideHUD : GameHUD
    {
        SeasideSession session;
        protected override SeasideSession Session=>session!=null?session:session=GetComponent<SeasideSession>();
        SeasideStory Story=>Session.GetComponent<SeasideStory>();
        protected override void AdvanceDialogue(){Story?.AdvanceDialogue();}
        public override GameHUDContent Content=>new GameHUDContent
        {
            title="第一关 · 海边 SEASIDE",
            controls=CommonControls,
            statistics=Statistics(Session),
            status=DashStatus(Session.player)+"    "+MotorState(Session.player),
            help=CommonHelp+"\nTab切换近景/全景；B显示或隐藏路线。\n靠近终点灯塔按 E 交互；读完两页文案后获得传送并前往森林。",
            prompt=Story!=null&&Story.CanInteract?"按 E 交互":null,
            dialogueTitle="海边灯塔",dialogue=Story?.Dialogue,
            completion="你获得了传送能力，海边探索完成。"
        };
    }
}
