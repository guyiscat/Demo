namespace Snow
{
    public sealed class SnowHUD : Expedition.ThermalHUD
    {
        public SnowLevel level;
        protected override Expedition.ThermalLevel Level=>level;
    }
}
