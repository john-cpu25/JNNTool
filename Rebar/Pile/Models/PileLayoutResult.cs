using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Pile.Models
{
    public enum PileRebarRole
    {
        MainLongitudinal,
        StirrupDenseHead,
        StirrupSparseBody,
        StirrupDenseToe,
        StiffenerRing
    }

    /// <summary>
    /// Thông số rải một nhóm thép cọc (RebarSet hoặc thanh đơn) thuần C# (mm).
    /// </summary>
    public sealed class PileRebarSetLayoutInfo
    {
        public PileRebarRole Role { get; set; }
        public string Diameter { get; set; } = "D20";
        public int BarCount { get; set; } = 1;
        public double SpacingMm { get; set; } = 0.0;
        public double RangeLengthMm { get; set; } = 0.0;

        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();
        public Point3D DistributionVector { get; set; }

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ cốt thép của cấu kiện cọc.
    /// </summary>
    public sealed class PileLayoutResult
    {
        public PileModel Pile { get; }
        public PileRebarSettings Settings { get; }
        public List<PileRebarSetLayoutInfo> RebarSets { get; } = new List<PileRebarSetLayoutInfo>();

        public PileLayoutResult(PileModel pile, PileRebarSettings settings)
        {
            Pile = pile;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;
    }
}
