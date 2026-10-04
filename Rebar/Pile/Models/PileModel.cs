using System;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Pile.Models
{
    /// <summary>
    /// Mô hình hình học cấu kiện cọc (POCO thuần C#, đơn vị mm).
    /// </summary>
    public sealed class PileModel
    {
        public long ElementId { get; set; }
        public string PileName { get; set; } = "P1";
        public string LevelName { get; set; } = "";

        public PileType Type { get; set; } = PileType.BoredPile;

        /// <summary>Đường kính cọc tròn (mm)</summary>
        public double DiameterMm { get; set; } = 800.0;

        /// <summary>Cạnh cọc vuông B (mm)</summary>
        public double WidthBMm { get; set; } = 400.0;

        /// <summary>Cạnh cọc vuông H (mm)</summary>
        public double HeightHMm { get; set; } = 400.0;

        /// <summary>Chiều dài cọc (mm)</summary>
        public double LengthMm { get; set; } = 15000.0;

        public double TopElevationMm { get; set; } = -1500.0;
        public double BottomElevationMm => TopElevationMm - LengthMm;

        public Point3D CenterPoint { get; set; } = new Point3D(0, 0, -9000.0);
    }
}
