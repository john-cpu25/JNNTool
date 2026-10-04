using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Stair.Models
{
    /// <summary>
    /// Mô hình hình học một vế thang (Flight / Run) (POCO thuần C#, đơn vị mm).
    /// </summary>
    public sealed class StairFlightModel
    {
        public int FlightIndex { get; set; } = 1;
        public double WidthMm { get; set; } = 1200.0;
        public double SlabThicknessMm { get; set; } = 120.0;

        public int RiserCount { get; set; } = 10;
        public double RiserHeightMm { get; set; } = 160.0;
        public double TreadDepthMm { get; set; } = 280.0;

        public Point3D StartPoint { get; set; } = new Point3D(0, 0, 0);
        public Point3D EndPoint { get; set; } = new Point3D(2520, 0, 1600);

        public double HorizontalLengthMm => TreadDepthMm * (RiserCount - 1);
        public double TotalRiseMm => RiserHeightMm * RiserCount;
        public double InclinedLengthMm => Math.Sqrt(HorizontalLengthMm * HorizontalLengthMm + TotalRiseMm * TotalRiseMm);

        public Point3D Direction { get; set; } = new Point3D(1, 0, 0);
        public Point3D Normal { get; set; } = new Point3D(0, 1, 0);
    }

    /// <summary>
    /// Mô hình cấu kiện cầu thang tổng thể.
    /// </summary>
    public sealed class StairModel
    {
        public long ElementId { get; set; }
        public string StairName { get; set; } = "Stair 1";
        public string LevelName { get; set; } = "";

        public List<StairFlightModel> Flights { get; set; } = new List<StairFlightModel>();
    }
}
