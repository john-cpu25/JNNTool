using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Column.Models
{
    public enum ColumnStirrupPattern
    {
        CN,   // Đai chữ nhật đơn
        AB,   // Đai chữ nhật + đai lồng hình chữ nhật/hình thoi
        CTie  // Đai chữ nhật + đai móc C giữ các thanh giữa
    }

    /// <summary>
    /// Cấu hình thông số bố trí thép cột (lưu trữ JSON tại %AppData%\JNNTool\Settings\ColumnRebarSettings.json).
    /// </summary>
    public sealed class ColumnRebarSettings
    {
        public string ColumnGroupName { get; set; } = "C1";

        // === Thép Dọc (Main Vertical Rebar) ===
        public string MainDiameter { get; set; } = "D20";
        public int CountB { get; set; } = 3; // Số thanh dọc theo cạnh B
        public int CountH { get; set; } = 3; // Số thanh dọc theo cạnh H
        public double LapLengthMultiplier { get; set; } = 40.0; // Chiều dài đoạn nối chồng = 40d
        public bool EnableCrankedLap { get; set; } = true; // Bẻ cổ chai tỉ lệ 1:6 khi thu nhỏ tiết diện

        // === Cốt Đai (Stirrups) ===
        public ColumnStirrupPattern StirrupPattern { get; set; } = ColumnStirrupPattern.AB;
        public string StirrupDiameter { get; set; } = "D8";
        public double StirrupSpacingDense { get; set; } = 100.0;  // Vùng chân cột và đầu cột
        public double StirrupSpacingSparse { get; set; } = 200.0; // Vùng giữa thân cột
        public double DenseZoneHeightMm { get; set; } = 600.0;    // Chiều cao vùng đai dày (mặc định 600 hoặc H/6)

        // === Lớp Bảo Vệ & Mặt Cắt ===
        public double CoverMm { get; set; } = 25.0;
        public bool CreateColumnSection { get; set; } = true;
        public string ViewNamePrefix { get; set; } = "MCD_Col_";
    }
}
