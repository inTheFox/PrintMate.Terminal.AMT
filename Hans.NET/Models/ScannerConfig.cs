namespace Hans.NET.Models
{
    public partial class ScannerConfig
    {
        public float FieldSizeX { get; set; }
        public float FieldSizeY { get; set; }
        public int ProtocolCode { get; set; }
        public int CoordinateTypeCode { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float OffsetZ { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float ScaleZ { get; set; }
        public float RotateAngle { get; set; }
    }
}
