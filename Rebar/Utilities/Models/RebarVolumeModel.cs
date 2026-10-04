using System;

namespace JNNTool.RebarSuite.Utilities.Models
{
    /// <summary>
    /// Bản ghi thống kê khối lượng của một nhóm cốt thép (POCO thuần C#).
    /// </summary>
    public sealed class RebarVolumeItem
    {
        public string Diameter { get; set; } = "D10";
        public int NominalDiameter { get; set; } = 10;
        public double UnitWeightKgPerM { get; set; } = 0.617;
        public int TotalBars { get; set; } = 0;
        public double TotalLengthM { get; set; } = 0.0;
        public double TotalWeightKg => TotalLengthM * UnitWeightKgPerM;
        public double TotalWeightTon => TotalWeightKg / 1000.0;
        public string HostCategory { get; set; } = "All";
        public string LevelName { get; set; } = "All";
    }
}
