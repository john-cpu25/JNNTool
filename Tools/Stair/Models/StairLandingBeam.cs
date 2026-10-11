namespace JNN.Stair.Models
{
    /// <summary>
    /// Thông số dầm chiếu nghỉ (Landing Beam) tích hợp liền khối với bản thang
    /// </summary>
    public class StairLandingBeam
    {
        public double WidthMm { get; set; } = 200.0;
        public double HeightMm { get; set; } = 350.0;
        public double StructureTopMm { get; set; } = 0.0;
        public double LengthMm { get; set; } = 1200.0;

        public bool IsEnabled { get; set; } = true;

        // Vị trí gối đỡ
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
    }
}
