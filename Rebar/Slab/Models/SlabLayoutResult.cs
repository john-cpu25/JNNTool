using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Slab.Models
{
    public enum RebarLayerType
    {
        BottomX,
        BottomY,
        TopX,
        TopY,
        TopAdd,
        TopAddDistribution,
        Spacer
    }

    /// <summary>
    /// Thông số rải một bộ thép (RebarSet) độc lập với Revit API (đơn vị mm).
    /// </summary>
    public sealed class RebarSetLayoutInfo
    {
        public RebarLayerType LayerType { get; set; }
        public string Diameter { get; set; } = "D10";
        public double SpacingMm { get; set; } = 150.0;
        public int BarCount { get; set; } = 1;

        /// <summary>
        /// Danh sách các điểm tạo thành đường tâm của thanh thép đại diện (Point3D trong hệ tọa độ mm).
        /// </summary>
        public List<Point3D> CurvePoints { get; set; } = new List<Point3D>();

        /// <summary>
        /// Hướng vector rải thép (ví dụ: (0, 1, 0) rải dọc trục Y).
        /// </summary>
        public Point3D DistributionVector { get; set; }

        /// <summary>
        /// Tổng chiều dài phạm vi rải (mm).
        /// </summary>
        public double DistributionLengthMm { get; set; }

        public HookAngle StartHook { get; set; } = HookAngle.None;
        public HookAngle EndHook { get; set; } = HookAngle.None;
        public double HookLengthMm { get; set; } = 0.0;

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Kết quả tổng hợp sau khi Layout Engine tính toán toàn bộ cốt thép cho một sàn.
    /// </summary>
    public sealed class SlabLayoutResult
    {
        public SlabModel Slab { get; }
        public SlabRebarSettings Settings { get; }
        public List<RebarSetLayoutInfo> RebarSets { get; } = new List<RebarSetLayoutInfo>();

        public SlabLayoutResult(SlabModel slab, SlabRebarSettings settings)
        {
            Slab = slab;
            Settings = settings;
        }

        public int TotalRebarSets => RebarSets.Count;

        public int TotalBars
        {
            get
            {
                int total = 0;
                foreach (var set in RebarSets)
                {
                    total += set.BarCount;
                }
                return total;
            }
        }
    }
}
