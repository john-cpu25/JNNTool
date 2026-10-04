using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Foundation.Models
{
    /// <summary>
    /// Mô hình hình học cấu kiện móng/đài cọc (POCO thuần C#, đơn vị mm).
    /// </summary>
    public sealed class FoundationModel
    {
        public long ElementId { get; set; }
        public string FoundationName { get; set; } = "F1";
        public string LevelName { get; set; } = "";

        public double LengthXMm { get; set; } = 2000.0;
        public double WidthYMm { get; set; } = 2000.0;
        public double HeightZMm { get; set; } = 800.0;

        public double BottomElevationMm { get; set; } = -1500.0;
        public double TopElevationMm => BottomElevationMm + HeightZMm;

        public Point3D CenterPoint { get; set; } = new Point3D(0, 0, -1100.0);

        // Kích thước cổ cột chờ (nếu có)
        public double ColumnWidthBMm { get; set; } = 400.0;
        public double ColumnHeightHMm { get; set; } = 400.0;
    }
}
