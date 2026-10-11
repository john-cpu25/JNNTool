#nullable enable
using System;

namespace JNNTool.Tools.StairModel.Engine
{
    /// <summary>
    /// DTO chứa toàn bộ thiết lập cấu hình tham số hình học và kết cấu của thang.
    /// Đơn vị lưu trữ: mm và độ (deg), độc lập với Revit API.
    /// </summary>
    public class StairConfig
    {
        // 1. Phân loại thang
        public StairType StairType { get; set; } = StairType.UShaped;
        public TurnDirection TurnDirection { get; set; } = TurnDirection.Left;

        // 2. Cao độ và Tầng
        public string BaseLevelId { get; set; } = string.Empty;
        public string TopLevelId { get; set; } = string.Empty;
        public double BaseElevationMm { get; set; } = 0.0;
        public double TopElevationMm { get; set; } = 3600.0;
        public double BaseOffsetMm { get; set; } = 0.0;
        public double TopOffsetMm { get; set; } = 0.0;

        // 3. Kích thước hình học bậc thang
        public double StairWidthMm { get; set; } = 1100.0;          // Bề rộng vế thang
        public int TotalRisers { get; set; } = 21;                  // Tổng số cổ bậc
        public int Flight1Risers { get; set; } = 11;                // Số cổ bậc vế 1
        public int Flight2Risers { get; set; } = 10;                // Số cổ bậc vế 2 (cho thang 2 vế)
        public double TreadDepthMm { get; set; } = 280.0;           // Bề rộng mặt bậc (chiều sâu bậc)
        public double LandingLengthMm { get; set; } = 1200.0;       // Chiều dài chiếu nghỉ (theo phương di chuyển)
        public double GapBetweenFlightsMm { get; set; } = 100.0;    // Khoảng hở giữa 2 vế thang (well-hole)

        // 4. Chiều dày bản kết cấu
        public double WaistSlabThicknessMm { get; set; } = 120.0;   // Bề dày bản thang nghiêng
        public double LandingSlabThicknessMm { get; set; } = 120.0; // Bề dày bản chiếu nghỉ

        // 5. Cấu kiện Revit đã chọn
        public string SelectedFloorTypeId { get; set; } = string.Empty;
        public string SelectedBeamSymbolId { get; set; } = string.Empty;
        public SupportBeamOption SupportBeamOption { get; set; } = SupportBeamOption.LandingBeamOnly;

        // 6. Điểm chèn và góc xoay
        public double InsertionPointX { get; set; } = 0.0;
        public double InsertionPointY { get; set; } = 0.0;
        public double RotationAngleDeg { get; set; } = 0.0;

        /// <summary>
        /// Tính tổng chiều cao thang thực tế (Total Rise) theo mm.
        /// </summary>
        public double TotalRiseMm => (TopElevationMm + TopOffsetMm) - (BaseElevationMm + BaseOffsetMm);
    }
}
