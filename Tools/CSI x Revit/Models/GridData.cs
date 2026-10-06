namespace JNNTool.Tools.CSIxRevit.Models
{
    public class GridData
    {
        public string Name { get; set; } = "";
        public string SystemName { get; set; } = "";
        public double Coordinate { get; set; } // feet (for Cartesian)
        public string Direction { get; set; } = ""; // "X", "Y", or "GEN"
        public bool IsGeneral { get; set; }
        public double X1 { get; set; } // feet
        public double Y1 { get; set; } // feet
        public double X2 { get; set; } // feet
        public double Y2 { get; set; } // feet
    }
}
