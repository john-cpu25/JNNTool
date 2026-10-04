using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Stair.Models
{
    public enum StairType
    {
        OneFlight,          // Thang 1 vế thẳng
        TwoFlightsDogLeg,   // Thang 2 vế chữ U (đảo chiều)
        TwoFlightsStraight, // Thang 2 vế thẳng liên tục
        ThreeFlights        // Thang 3 vế
    }

    /// <summary>
    /// Cấu hình thông số bố trí cốt thép bản thang theo TCVN 5574:2018 (lưu trữ JSON tại %AppData%\JNNTool\Settings\StairRebarSettings.json).
    /// </summary>
    public sealed class StairRebarSettings
    {
        public StairType Type { get; set; } = StairType.TwoFlightsDogLeg;

        // === Thép Bản Thang Lớp Dưới (Chịu Mô-men Dương Giữa Vế) ===
        public string BottomDiameter { get; set; } = "D10";
        public double BottomSpacing { get; set; } = 150.0;
        public double BottomAnchorLengthMm { get; set; } = 350.0; // Neo vào dầm chân thang và dầm chiếu nghỉ (35d)

        // === Thép Bản Thang Lớp Trên / Mũ Gối (Chịu Mô-men Âm Tại Gối) ===
        public bool CreateTopBars { get; set; } = true;
        public string TopDiameter { get; set; } = "D10";
        public double TopSpacing { get; set; } = 150.0;
        public double TopSpanFactor { get; set; } = 0.25; // L/4 từ mép dầm ra nhịp thang

        // === Thép Phân Bố (Distribution Bars) ===
        public string DistributionDiameter { get; set; } = "D6";
        public double DistributionSpacing { get; set; } = 200.0;

        // === Thép Cấu Tạo Bậc Thang (Tùy chọn) ===
        public bool CreateStepTies { get; set; } = false;
        public string StepTieDiameter { get; set; } = "D6";

        // === Lớp Bê Tông Bảo Vệ Bản Thang (mm) ===
        public double CoverMm { get; set; } = 15.0; // Bản sàn/thang theo TCVN 5574:2018
    }
}
