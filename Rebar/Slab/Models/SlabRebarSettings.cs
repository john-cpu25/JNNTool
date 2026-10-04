using System;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Slab.Models
{
    /// <summary>
    /// Cấu hình thông số bố trí thép sàn (lưu trữ JSON tại %AppData%\JNNTool\Settings\SlabRebarSettings.json).
    /// </summary>
    public sealed class SlabRebarSettings
    {
        // === Lớp dưới (Bottom Bars) ===
        public string BottomXDiameter { get; set; } = "D10";
        public double BottomXSpacing { get; set; } = 150.0;
        public HookAngle BottomXHook { get; set; } = HookAngle.None;
        public double BottomXHookLength { get; set; } = 100.0;

        public string BottomYDiameter { get; set; } = "D10";
        public double BottomYSpacing { get; set; } = 150.0;
        public HookAngle BottomYHook { get; set; } = HookAngle.None;
        public double BottomYHookLength { get; set; } = 100.0;

        // === Lớp trên toàn bộ (Top Full Layer - tùy chọn) ===
        public bool IsCreateTopFullLayer { get; set; } = false;
        public string TopXDiameter { get; set; } = "D10";
        public double TopXSpacing { get; set; } = 150.0;
        public HookAngle TopXHook { get; set; } = HookAngle.Deg90;
        public double TopXHookLength { get; set; } = 100.0;

        public string TopYDiameter { get; set; } = "D10";
        public double TopYSpacing { get; set; } = 150.0;
        public HookAngle TopYHook { get; set; } = HookAngle.Deg90;
        public double TopYHookLength { get; set; } = 100.0;

        // === Thép mũ tăng cường gối dầm (Top Add Rebar) ===
        public bool IsCreateTopAdd { get; set; } = true;
        public string TopAddDiameter { get; set; } = "D10";
        public double TopAddSpacing { get; set; } = 150.0;
        public double TopAddSpanRatio { get; set; } = 0.25; // L/4
        public double MinSpanToCreateTopAdd { get; set; } = 800.0; // Bỏ qua nhịp < 800mm
        public double TopAddHookDownA { get; set; } = 150.0; // Đoạn bẻ xuống mép gối
        public double TopAddHookDownB { get; set; } = 100.0; // Mỏ móc mép ngoài
        public bool ExtendTopAddToCantileverBoundary { get; set; } = false;

        // Thép phân bố cho mũ
        public bool IsCreateTopAddDistribution { get; set; } = true;
        public string TopAddDistributionDiameter { get; set; } = "D6";
        public double TopAddDistributionSpacing { get; set; } = 300.0;

        // === Dò dầm đỡ sàn ===
        public double BeamTolerance { get; set; } = 500.0;
        public double EdgeBeamWidth { get; set; } = 300.0;
        public double InternalBeamWidth { get; set; } = 300.0;

        // === Lỗ mở sàn (Openings) ===
        public bool IgnoreSmallHoles { get; set; } = false;
        public double SmallHoleThreshold { get; set; } = 1000.0;

        // === Con kê / Chân chó (Spacer) ===
        public bool IsCreateSpacer { get; set; } = false;
        public string SpacerDiameter { get; set; } = "D10";
        public double SpacerSpacingX { get; set; } = 1000.0;
        public double SpacerSpacingY { get; set; } = 1000.0;
        public double SpacerHookLength { get; set; } = 200.0;

        // === Lớp bảo vệ (mm) ===
        public double CoverTop { get; set; } = 15.0;
        public double CoverBottom { get; set; } = 15.0;
        public double CoverSide { get; set; } = 15.0;

        // === Quản lý & Thể hiện ===
        public bool AssignPartitionFromHost { get; set; } = true;
        public string DefaultPartition { get; set; } = "JNN_Slab";
        public bool SelectIn3DView { get; set; } = true;
    }
}
