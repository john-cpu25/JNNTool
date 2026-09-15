using System.Collections.Generic;

namespace JNNTool.Tools.CSIxRevit.Models
{
    public class WallData
    {
        public string Name { get; set; }
        public List<string> PointNames { get; set; } = new List<string>();
        public string Section { get; set; }
        public double TopElevation { get; set; }
        public double BottomElevation { get; set; }
    }
}
