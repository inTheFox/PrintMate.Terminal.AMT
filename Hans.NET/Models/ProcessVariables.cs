namespace Hans.NET.Models
{
    public partial class ProcessVariables
    {
        public int MarkSpeed { get; set; }
        public int JumpSpeed { get; set; }
        public int PolygonDelay { get; set; }
        public int JumpDelay { get; set; }
        public int MarkDelay { get; set; }
        public double LaserOnDelay { get; set; }
        public double LaserOffDelay { get; set; }
        public double LaserOnDelayForSkyWriting { get; set; }
        public double LaserOffDelayForSkyWriting { get; set; }
        public double CurBeamDiameterMicron { get; set; }
        public double CurPower { get; set; }
        public double JumpMaxLengthLimitMm { get; set; }
        public int MinJumpDelay { get; set; }
        public bool Swenable { get; set; }
        public double Umax { get; set; }
    }
}
