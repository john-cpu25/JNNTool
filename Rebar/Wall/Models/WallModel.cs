using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Wall.Models
{
    /// <summary>
    /// Mô hình hình học và thuộc tính cấu kiện vách bê tông cốt thép (POCO thuần C#, đơn vị mm).
    /// </summary>
    public sealed class WallModel
    {
        public long ElementId { get; set; }
        public string WallName { get; set; } = "Wall 1";
        public string LevelName { get; set; } = "";

        /// <summary>Chiều dài vách (mm)</summary>
        public double LengthMm { get; set; } = 3000.0;

        /// <summary>Chiều dày vách (mm)</summary>
        public double ThicknessMm { get; set; } = 250.0;

        /// <summary>Chiều cao thông thủy/tầng của vách (mm)</summary>
        public double HeightMm { get; set; } = 3600.0;

        public double BottomElevationMm { get; set; } = 0.0;
        public double TopElevationMm { get; set; } = 3600.0;

        /// <summary>Điểm bắt đầu đường tâm chân vách</summary>
        public Point3D StartPoint { get; set; }

        /// <summary>Điểm kết thúc đường tâm chân vách</summary>
        public Point3D EndPoint { get; set; }

        /// <summary>Vector hướng dọc theo chiều dài vách</summary>
        public Point3D Direction { get; set; } = new Point3D(1, 0, 0);

        /// <summary>Vector pháp tuyến ngang vuông góc với vách (hướng theo chiều dày)</summary>
        public Point3D Normal { get; set; } = new Point3D(0, 1, 0);

        /// <summary>Vách tầng trên cùng (không có tầng tiếp theo nối chồng)</summary>
        public bool IsTopStory { get; set; } = false;
    }
}
