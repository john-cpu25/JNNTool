using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Foundation.Models
{
    public enum FoundationType
    {
        Isolated,   // Móng đơn
        Strip,      // Móng băng
        PileCap     // Đài cọc
    }

    /// <summary>
    /// Cấu hình thông số bố trí cốt thép móng và đài cọc theo TCVN 5574:2018 (lưu trữ JSON tại %AppData%\JNNTool\Settings\FoundationRebarSettings.json).
    /// </summary>
    public sealed class FoundationRebarSettings
    {
        public FoundationType Type { get; set; } = FoundationType.Isolated;

        // === Thép Lưới Đáy (Bottom Grid Rebar) ===
        public string BottomDiameterX { get; set; } = "D14";
        public double BottomSpacingX { get; set; } = 150.0;
        public string BottomDiameterY { get; set; } = "D14";
        public double BottomSpacingY { get; set; } = 150.0;
        public bool HookBottomBarsUp { get; set; } = true;
        public double BottomHookHeightMm { get; set; } = 200.0;

        // === Thép Lưới Mặt Trên (Top Grid Rebar - đài cọc / móng dày) ===
        public bool CreateTopGrid { get; set; } = false;
        public string TopDiameterX { get; set; } = "D12";
        public double TopSpacingX { get; set; } = 200.0;
        public string TopDiameterY { get; set; } = "D12";
        public double TopSpacingY { get; set; } = 200.0;
        public double TopHookHeightMm { get; set; } = 150.0;

        // === Thép Chờ Cột / Chân Vịt (Starter Dowels) ===
        public bool CreateColumnDowels { get; set; } = true;
        public int DowelsCountB { get; set; } = 3;
        public int DowelsCountH { get; set; } = 3;
        public string DowelDiameter { get; set; } = "D20";
        public double DowelFootLengthMm { get; set; } = 300.0; // Đoạn bẻ chân vịt uốn 90 độ tại đáy
        public double DowelExtendHeightMm { get; set; } = 800.0; // Đoạn chờ vươn lên trên mặt móng
        public string DowelTieDiameter { get; set; } = "D8";
        public double DowelTieSpacing { get; set; } = 150.0;
        public int DowelTieCount { get; set; } = 4;

        // === Lớp Bê Tông Bảo Vệ ===
        public double CoverBottomMm { get; set; } = 50.0; // TCVN 5574:2018 móng có bê tông lót: >= 35-50 mm
        public double CoverSideMm { get; set; } = 50.0;
        public double CoverTopMm { get; set; } = 50.0;
    }
}
