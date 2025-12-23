namespace Hans.NET.Models
{
    public class FunctionSwitcherConfig
    {
        public bool EnablePowerOffset { get; set; }
        public bool EnablePowerCorrection { get; set; }
        public bool EnableZCorrection { get; set; }
        public bool EnableDiameterChange { get; set; }
        public bool EnableDynamicChangeVariables { get; set; }
        public bool LimitVariablesMinPoint { get; set; }
        public bool LimitVariablesMaxPoint { get; set; }
        public bool EnableVariableJumpDelay { get; set; }
    }
}
