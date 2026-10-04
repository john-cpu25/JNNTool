using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Foundation.Models
{
    public enum FoundationRebarRole
    {
        BottomLayerX,
        BottomLayerY,
        TopLayerX,
        TopLayerY,
        ColumnDowel,
        ColumnDowelTie
    }

    /// <summary>
    /// Thông số rải một nhóm thép móng (RebarSet hoặc thanh đơn) thuần C# (mm).
    /// </summary>
    public sealed class FoundationRebarSetLayoutInfo
    {
        public FoundationRebarRole Role { get; set; }
        public string Diameter { get; set; } = "D14";
        public int BarCount { get; set; } = 1;
        public double SpacingMm { get; set; } = 0.0;
        public double RangeLengthMm { get; set; } = 0.0;

        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();
        public Point3D DistributionVector { get; set; }

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ cốt thép của móng / đài cọc.
    /// </summary>
    public sealed class FoundationLayoutResult
    {
        public FoundationModel Foundation { get; }
        public FoundationRebarSettings Settings { get; }
        public List<FoundationRebarSetLayoutInfo> RebarSets { get; } = new List<FoundationRebarSetLayoutInfo>();

        public FoundationLayoutResult(FoundationModel foundation, FoundationRebarSettings settings)
        {
            Foundation = foundation;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;
    }
}
