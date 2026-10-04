using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Column.Models
{
    public enum ColumnRebarRole
    {
        VerticalMain,
        VerticalCranked,
        StirrupBaseDense,
        StirrupMidSparse,
        StirrupTopDense,
        StirrupBeamJoint,
        InternalStirrupOrTie
    }

    /// <summary>
    /// Thông số rải một nhóm thép cột (RebarSet hoặc thanh đơn) thuần C# (mm).
    /// </summary>
    public sealed class ColumnRebarSetLayoutInfo
    {
        public ColumnRebarRole Role { get; set; }
        public string Diameter { get; set; } = "D20";
        public int BarCount { get; set; } = 1;
        public double SpacingMm { get; set; } = 0.0;
        public double RangeHeightMm { get; set; } = 0.0;

        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();
        public Point3D DistributionVector { get; set; }

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ cốt thép của chuỗi cột theo tầng.
    /// </summary>
    public sealed class ColumnLayoutResult
    {
        public ColumnStackModel ColumnStack { get; }
        public ColumnRebarSettings Settings { get; }
        public List<ColumnRebarSetLayoutInfo> RebarSets { get; } = new List<ColumnRebarSetLayoutInfo>();

        public ColumnLayoutResult(ColumnStackModel columnStack, ColumnRebarSettings settings)
        {
            ColumnStack = columnStack;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;
    }
}
