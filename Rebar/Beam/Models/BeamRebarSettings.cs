using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Beam.Models
{
    /// <summary>
    /// Cấu hình bố trí cốt thép dầm liên tục (lưu trữ JSON tại %AppData%\JNNTool\Settings\BeamRebarSettings.json).
    /// </summary>
    public sealed class BeamRebarSettings
    {
        // === Thép Chủ Trên (Top Main) ===
        public string TopMainDiameter { get; set; } = "D20";
        public int TopMainCount { get; set; } = 2;

        // === Thép Chủ Dưới (Bottom Main) ===
        public string BottomMainDiameter { get; set; } = "D20";
        public int BottomMainCount { get; set; } = 2;

        // === Thép Gia Cường Gối (Top Add Rebar) ===
        public bool EnableTopAdd { get; set; } = true;
        public string TopAddDiameter { get; set; } = "D20";
        public int TopAddCount { get; set; } = 2;
        public double TopAddSpanRatio { get; set; } = 0.25; // L/4

        // === Thép Gia Cường Nhịp (Bottom Add Rebar) ===
        public bool EnableBottomAdd { get; set; } = true;
        public string BottomAddDiameter { get; set; } = "D20";
        public int BottomAddCount { get; set; } = 2;
        public double BottomAddOffsetRatio { get; set; } = 0.15; // Cách mép gối 0.15L

        // === Cốt Đai (Stirrups) ===
        public string StirrupDiameter { get; set; } = "D8";
        public double StirrupSpacingEnd { get; set; } = 100.0; // Vùng dày gần gối
        public double StirrupSpacingMid { get; set; } = 200.0; // Vùng thưa giữa nhịp
        public double StirrupDenseRangeRatio { get; set; } = 0.25; // L/4
        public HookAngle StirrupHook { get; set; } = HookAngle.Deg135;

        // === Thép Giá / Thép Cấu Tạo Thành Dầm (Skin Rebar) ===
        public bool AutoSideRebar { get; set; } = true;
        public double MinBeamHeightForSideRebar { get; set; } = 600.0; // Dầm cao >= 600mm
        public string SideRebarDiameter { get; set; } = "D12";
        public double MaxSideRebarSpacing { get; set; } = 300.0;

        // === Neo & Nối Cốt Thép ===
        public double AnchorLengthMultiplier { get; set; } = 35.0; // 35d
        public double CoverMm { get; set; } = 25.0; // Lớp bảo vệ dầm

        // === Bản Vẽ & Chi Tiết ===
        public bool CreateLongitudinalSection { get; set; } = true; // Mặt cắt dọc (MCD)
        public bool CreateCrossSections { get; set; } = true;       // Mặt cắt ngang (MCN)
        public string ViewNamePrefix { get; set; } = "MCD_";
    }
}
