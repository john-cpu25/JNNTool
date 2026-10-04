using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Beam.Models
{
    public enum BeamRebarRole
    {
        MainTop,
        MainBottom,
        TopAddSupport,
        BottomAddMidSpan,
        StirrupDense,
        StirrupSparse,
        SideRebar
    }

    /// <summary>
    /// Thông số rải một nhóm thép dầm (RebarSet hoặc thanh đơn) thuần C# (mm).
    /// </summary>
    public sealed class BeamRebarSetLayoutInfo
    {
        public BeamRebarRole Role { get; set; }
        public string Diameter { get; set; } = "D20";
        public int BarCount { get; set; } = 1;
        public double SpacingMm { get; set; } = 0.0;
        public double RangeLengthMm { get; set; } = 0.0;

        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();
        public Point3D DistributionVector { get; set; }

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ cốt thép của chuỗi dầm.
    /// </summary>
    public sealed class BeamLayoutResult
    {
        public ContinuousBeamModel BeamModel { get; }
        public BeamRebarSettings Settings { get; }
        public List<BeamRebarSetLayoutInfo> RebarSets { get; } = new List<BeamRebarSetLayoutInfo>();

        public BeamLayoutResult(ContinuousBeamModel beamModel, BeamRebarSettings settings)
        {
            BeamModel = beamModel;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;
    }
}
