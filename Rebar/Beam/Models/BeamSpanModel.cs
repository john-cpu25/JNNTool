using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Beam.Models
{
    /// <summary>
    /// Mô hình một nhịp dầm đơn lẻ (POCO, đơn vị mm).
    /// </summary>
    public sealed class BeamSpanModel
    {
        public long ElementId { get; set; }
        public string Mark { get; set; } = "";
        public int SpanIndex { get; set; } = 0;

        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 500.0;
        public double LengthMm { get; set; } = 4000.0;

        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public Point3D DirectionVector { get; set; }

        public double LeftSupportWidthMm { get; set; } = 300.0;
        public double RightSupportWidthMm { get; set; } = 300.0;

        public bool IsFirstSpan { get; set; } = false;
        public bool IsLastSpan { get; set; } = false;

        public double ClearSpanLengthMm => Math.Max(LengthMm - LeftSupportWidthMm / 2.0 - RightSupportWidthMm / 2.0, 100.0);
    }

    /// <summary>
    /// Mô hình chuỗi dầm liên tục (nhiều nhịp dầm thẳng hàng).
    /// </summary>
    public sealed class ContinuousBeamModel
    {
        public string GroupName { get; set; } = "ContinuousBeam";
        public List<BeamSpanModel> Spans { get; set; } = new List<BeamSpanModel>();

        public double TotalLengthMm
        {
            get
            {
                double total = 0;
                foreach (var span in Spans)
                {
                    total += span.LengthMm;
                }
                return total;
            }
        }
    }
}
