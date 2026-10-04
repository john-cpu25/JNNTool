using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Wall.Models
{
    public enum WallRebarRole
    {
        VerticalLayerOuter,
        VerticalLayerInner,
        HorizontalLayerOuter,
        HorizontalLayerInner,
        BoundaryTieStart,
        BoundaryTieEnd,
        InternalSpacerTie
    }

    /// <summary>
    /// Thông số rải một nhóm thép vách (RebarSet hoặc thanh đơn) thuần C# (mm).
    /// </summary>
    public sealed class WallRebarSetLayoutInfo
    {
        public WallRebarRole Role { get; set; }
        public string Diameter { get; set; } = "D12";
        public int BarCount { get; set; } = 1;
        public double SpacingMm { get; set; } = 0.0;
        public double RangeLengthMm { get; set; } = 0.0;

        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();
        public Point3D DistributionVector { get; set; }

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ cốt thép của vách bê tông cốt thép.
    /// </summary>
    public sealed class WallLayoutResult
    {
        public WallModel Wall { get; }
        public WallRebarSettings Settings { get; }
        public List<WallRebarSetLayoutInfo> RebarSets { get; } = new List<WallRebarSetLayoutInfo>();

        public WallLayoutResult(WallModel wall, WallRebarSettings settings)
        {
            Wall = wall;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;
    }
}
