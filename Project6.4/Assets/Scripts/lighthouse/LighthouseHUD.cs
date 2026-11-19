namespace Lighthouse
{
    public sealed class LighthouseHUD : Expedition.ThermalHUD
    {
        public LighthouseLevel level;
        protected override Expedition.ThermalLevel Level=>level;
    }
}
