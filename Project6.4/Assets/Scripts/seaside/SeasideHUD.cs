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
            controls=CommonControls+(Story!=null&&Story.Read?"   右键 发射信标 / 传送":""),
            statistics=Statistics(Session),
            status=DashStatus(Session.player)+"    "+MotorState(Session.player),
            help="朝墙按住方向可贴墙悬停，按空格蹬墙跳。\n\n靠近灯塔时按 E 查看。\n\n在入口的传送标志按 E，可前往已探索的区域。",
            prompt=Story!=null&&Story.CanTravel?"按 E 前往灯塔":Story!=null&&Story.CanInteract?"按 E 交互":null,
            dialogueTitle="海边灯塔",dialogue=Story?.Dialogue,
            completion="你获得了传送能力，海边探索完成。"
        };
    }
}
