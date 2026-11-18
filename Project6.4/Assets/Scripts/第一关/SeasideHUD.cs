namespace Seaside
{
    // Scene adapter; rendering and common wording live in GameHUD.
    public sealed class SeasideHUD : GameHUD
    {
        SeasideSession session;
        protected override SeasideSession Session=>session!=null?session:session=GetComponent<SeasideSession>();
        public override GameHUDContent Content=>new GameHUDContent
        {
            title="第一关 · 海边 SEASIDE",
            controls=CommonControls,
            statistics=Statistics(Session),
            status=DashStatus(Session.player)+"    "+MotorState(Session.player),
            help=CommonHelp+"\nTab切换近景/全景；B显示或隐藏路线。\n到达终点完成本关。",
            completion="已到达海边终点。"
        };
    }
}
