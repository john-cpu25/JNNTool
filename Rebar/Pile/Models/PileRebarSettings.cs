using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Pile.Models
{
    public enum PileType
    {
        BoredPile,          // Cọc khoan nhồi (tròn)
        PrecastSquarePile   // Cọc bê tông cốt thép vuông đúc sẵn
    }

    public enum PileStirrupType
    {
        Spiral,     // Đai xoắn liên tục
        Circular,   // Đai tròn rời
        Square      // Đai vuông khép kín
    }

    /// <summary>
    /// Cấu hình thông số bố trí cốt thép cọc khoan nhồi và cọc vuông theo TCVN 5574:2018 (lưu trữ JSON tại %AppData%\JNNTool\Settings\PileRebarSettings.json).
    /// </summary>
    public sealed class PileRebarSettings
    {
        public PileType Type { get; set; } = PileType.BoredPile;

        // === Thép Dọc Cọc (Main Bars) ===
        public int MainBarCount { get; set; } = 8;
        public string MainDiameter { get; set; } = "D20";
        public double DowelExtendLengthMm { get; set; } = 800.0; // Đoạn thép dọc đập đầu cọc vươn vào ngàm đài móng

        // === Cốt Đai Cọc (Stirrups) ===
        public PileStirrupType StirrupType { get; set; } = PileStirrupType.Spiral;
        public string StirrupDiameter { get; set; } = "D10";
        public double StirrupSpacingDense { get; set; } = 100.0; // Vùng dày đầu cọc và mũi cọc
        public double StirrupSpacingSparse { get; set; } = 200.0; // Vùng thân cọc
        public double DenseZoneLengthMm { get; set; } = 1500.0; // Chiều dài vùng đai dày đầu cọc

        // === Đai Gia Cường / Vành Định Vị (Cọc khoan nhồi) ===
        public bool CreateStiffenerRings { get; set; } = true;
        public string StiffenerDiameter { get; set; } = "D14";
        public double StiffenerSpacingMm { get; set; } = 2000.0; // Khoảng cách giữa các vành gia cường

        // === Lớp Bảo Vệ Bê Tông (mm) ===
        public double CoverMm { get; set; } = 50.0; // Cọc ngập trong đất >= 50mm
    }
}
