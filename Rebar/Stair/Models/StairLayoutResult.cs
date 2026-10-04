using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Stair.Models
{
    public enum StairRebarRole
    {
        BottomMainFlight,
        TopSupportBottom,
        TopSupportTop,
        DistributionFlight,
        StepTie
    }

    /// <summary>
    /// Thông số rải một nhóm thép bản thang (RebarSet hoặc thanh đơn) thuần C# (mm).
    /// </summary>
    public sealed class StairRebarSetLayoutInfo
    {
        public StairRebarRole Role { get; set; }
        public string Diameter { get; set; } = "D10";
        public int BarCount { get; set; } = 1;
        public double SpacingMm { get; set; } = 0.0;
        public double RangeLengthMm { get; set; } = 0.0;

        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();
        public Point3D DistributionVector { get; set; }

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ cốt thép của cầu thang.
    /// </summary>
    public sealed class StairLayoutResult
    {
        public StairModel Stair { get; }
        public StairRebarSettings Settings { get; }
        public List<StairRebarSetLayoutInfo> RebarSets { get; } = new List<StairRebarSetLayoutInfo>();

        public StairLayoutResult(StairModel stair, StairRebarSettings settings)
        {
            Stair = stair;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;
    }
}
