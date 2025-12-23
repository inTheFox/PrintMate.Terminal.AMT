

namespace Hans.NET.Models
{
    public partial class ScanatorConfiguration
    {
        public CardInfo CardInfo { get; set; }
        public ProcessVariablesMap ProcessVariablesMap { get; set; }
        public ScannerConfig ScannerConfig { get; set; }
        public BeamConfig BeamConfig { get; set; }
        public LaserPowerConfig LaserPowerConfig { get; set; }
        public FunctionSwitcherConfig FunctionSwitcherConfig { get; set; }
        public ThirdAxisConfig ThirdAxisConfig { get; set; }
    }
}
