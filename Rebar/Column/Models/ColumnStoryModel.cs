using System;
using System.Collections.Generic;
using System.Linq;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Column.Models
{
    /// <summary>
    /// Mô hình một đốt cột tại một tầng (POCO, đơn vị mm).
    /// </summary>
    public sealed class ColumnStoryModel
    {
        public long ElementId { get; set; }
        public string LevelName { get; set; } = "";
        public int StoryIndex { get; set; } = 0;

        public double BottomElevationMm { get; set; } = 0.0;
        public double TopElevationMm { get; set; } = 3600.0;
        public double TotalHeightMm => Math.Max(TopElevationMm - BottomElevationMm, 100.0);

        public double WidthBMm { get; set; } = 400.0;
        public double HeightHMm { get; set; } = 500.0;
        public double IntersectingBeamDepthMm { get; set; } = 500.0;

        public Point3D CenterPoint { get; set; }

        public double ClearHeightMm => Math.Max(TotalHeightMm - IntersectingBeamDepthMm, 100.0);
    }

    /// <summary>
    /// Mô hình chuỗi cột liên tục theo phương đứng qua nhiều tầng.
    /// </summary>
    public sealed class ColumnStackModel
    {
        public string GroupName { get; set; } = "C1";
        public List<ColumnStoryModel> Stories { get; set; } = new List<ColumnStoryModel>();

        public bool HasSectionChange(int storyIdx, out double deltaBMm, out double deltaHMm)
        {
            deltaBMm = 0;
            deltaHMm = 0;
            if (storyIdx >= Stories.Count - 1) return false;

            var curr = Stories[storyIdx];
            var next = Stories[storyIdx + 1];

            deltaBMm = curr.WidthBMm - next.WidthBMm;
            deltaHMm = curr.HeightHMm - next.HeightHMm;

            return Math.Abs(deltaBMm) > 1.0 || Math.Abs(deltaHMm) > 1.0;
        }
    }
}
