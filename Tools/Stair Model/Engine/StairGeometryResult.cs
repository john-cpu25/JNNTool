#nullable enable
using System;
using System.Collections.Generic;

namespace JNNTool.Tools.StairModel.Engine
{
    /// <summary>
    /// Điểm 3D độc lập với Revit API, đơn vị milimet (mm).
    /// </summary>
    public struct StairPoint3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public StairPoint3D(double x, double y, double z = 0.0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }

    /// <summary>
    /// Hình học bản thang nghiêng (Flight Waist Slab).
    /// </summary>
    public class FlightGeometry
    {
        public int FlightIndex { get; set; } = 1;
        public string Name { get; set; } = "Vế thang 1";

        /// <summary>
        /// Danh sách các đỉnh tạo thành vòng biên dạng kín phẳng (Profile loop) trên mặt bằng.
        /// Thường là hình chữ nhật 4 điểm theo chiều kim đồng hồ hoặc ngược chiều.
        /// </summary>
        public List<StairPoint3D> ProfilePoints { get; set; } = new List<StairPoint3D>();

        /// <summary>
        /// Điểm gốc của Slope Arrow (chân dốc, cao độ thấp).
        /// </summary>
        public StairPoint3D SlopeArrowStart { get; set; }

        /// <summary>
        /// Điểm ngọn của Slope Arrow (đỉnh dốc, cao độ cao).
        /// </summary>
        public StairPoint3D SlopeArrowEnd { get; set; }

        /// <summary>
        /// Độ dốc Slope = tan(alpha) = DeltaH / RunLength.
        /// </summary>
        public double Slope { get; set; }

        /// <summary>
        /// Cao độ đáy vế thang so với Base Level (mm).
        /// </summary>
        public double BaseOffsetMm { get; set; }

        /// <summary>
        /// Bề dày bản thang kết cấu (mm).
        /// </summary>
        public double ThicknessMm { get; set; }

        public int RiserCount { get; set; }
        public double RiserHeightMm { get; set; }
        public double TreadDepthMm { get; set; }
        public double FlightWidthMm { get; set; }
        public double RunLengthMm { get; set; }
    }

    /// <summary>
    /// Hình học bản chiếu nghỉ phẳng (Landing Slab).
    /// </summary>
    public class LandingGeometry
    {
        public int LandingIndex { get; set; } = 1;
        public string Name { get; set; } = "Chiếu nghỉ";

        /// <summary>
        /// Danh sách các đỉnh biên dạng vòng kín (Profile loop) trên mặt bằng.
        /// </summary>
        public List<StairPoint3D> ProfilePoints { get; set; } = new List<StairPoint3D>();

        /// <summary>
        /// Cao độ mặt trên bản chiếu nghỉ so với Base Level (mm).
        /// </summary>
        public double ElevationOffsetMm { get; set; }

        /// <summary>
        /// Bề dày bản chiếu nghỉ (mm).
        /// </summary>
        public double ThicknessMm { get; set; }

        public double WidthMm { get; set; }
        public double LengthMm { get; set; }
    }

    /// <summary>
    /// Hình học tim dầm đỡ kết cấu (Support Beam).
    /// </summary>
    public class BeamGeometry
    {
        public string Description { get; set; } = "Dầm đỡ";
        public StairPoint3D StartPoint { get; set; }
        public StairPoint3D EndPoint { get; set; }
        public double ElevationMm { get; set; }
        public double LengthMm => Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
    }

    /// <summary>
    /// Kết quả tính toán toàn bộ hình học của cầu thang.
    /// Dùng cho cả Preview 2D/3D trên WPF và Revit Builder.
    /// </summary>
    public class StairGeometryResult
    {
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }

        public List<FlightGeometry> Flights { get; set; } = new List<FlightGeometry>();
        public List<LandingGeometry> Landings { get; set; } = new List<LandingGeometry>();
        public List<BeamGeometry> Beams { get; set; } = new List<BeamGeometry>();

        // Các thông số kỹ thuật tính toán tổng hợp
        public double TotalRiseMm { get; set; }
        public double RiserHeightMm { get; set; }
        public double BlondelNumberMm { get; set; } // 2R + T
        public double InclineAngleDeg { get; set; } // Góc nghiêng của vế thang
    }
}
