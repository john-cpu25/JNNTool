using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Wall.Models
{
    /// <summary>
    /// Cấu hình bố trí cốt thép vách (lưu trữ JSON tại %AppData%\JNNTool\Settings\WallRebarSettings.json).
    /// </summary>
    public sealed class WallRebarSettings
    {
        // === Thép Đứng 2 Lớp (Vertical) ===
        public string VerticalDiameter { get; set; } = "D12";
        public double VerticalSpacing { get; set; } = 150.0;
        public double LapMultiplier { get; set; } = 40.0;

        // === Thép Ngang 2 Lớp (Horizontal) ===
        public string HorizontalDiameter { get; set; } = "D10";
        public double HorizontalSpacing { get; set; } = 150.0;
        public double HorizontalAnchorLength { get; set; } = 250.0; // Đoạn uốn neo vào đầu vách

        // === Thép C / Đai Biên Vách (Boundary C-Ties) ===
        public bool CreateBoundaryTies { get; set; } = true;
        public string BoundaryTieDiameter { get; set; } = "D8";
        public double BoundaryTieSpacing { get; set; } = 150.0;

        // === Lớp Bảo Vệ & Chống Thấm ===
        public bool IsWaterproofOrBasement { get; set; } = false; // Vách tầng hầm / bể nước
        public double CoverNormalMm { get; set; } = 20.0;
        public double CoverWaterproofMm { get; set; } = 35.0;

        public double EffectiveCoverMm => IsWaterproofOrBasement ? CoverWaterproofMm : CoverNormalMm;

        // === Bản Vẽ Mặt Cắt ===
        public bool CreateWallSection { get; set; } = true;
        public string ViewNamePrefix { get; set; } = "MCD_Wall_";
    }
}
