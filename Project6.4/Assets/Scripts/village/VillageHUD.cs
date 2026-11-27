using Seaside;
namespace Village
{
    public sealed class VillageHUD : GameHUD
    {
        public SeasideSession session;
        public VillageRobot robot;
        protected override SeasideSession Session=>session;
        protected override void AdvanceDialogue(){robot.Advance();}
        public override GameHUDContent Content=>new GameHUDContent{
            title="村庄 VILLAGE",controls=CommonControls+"   右键 传送   E 交互",statistics=Statistics(session),
            status=DashStatus(session.player)+"    "+MotorState(session.player),
            help="靠近机器人按 E 交谈。\n\n村庄中央的坑洞通往矿洞。\n\n进入雪山前，先向机器人了解核心的事。\n\n在传送标志按 E，可返回已探索的区域。",
            prompt=robot!=null&&robot.Nearby?"按 E 与机器人交谈":null,dialogueTitle="机器人",dialogue=robot?.Dialogue};
    }
}
