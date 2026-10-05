namespace JNNTool.Tools.CSIxRevit.Models
{
    public class ColumnData
    {
        public string Name { get; set; }
        public string PointI { get; set; }
        public string PointJ { get; set; }
        public string Section { get; set; }
        public double TopElevation { get; set; }
        public double BottomElevation { get; set; }
        public int CardinalPoint { get; set; } = 5;
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
    }
}
