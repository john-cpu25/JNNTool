using System;
using System.Collections.Generic;

namespace JNN.Stair.Models
{
    /// <summary>
    /// Mô hình dữ liệu toàn diện về hình học cầu thang (Stair Layout DTO)
    /// </summary>
    public class StairLayout
    {
        // 1. Phân loại & Chế độ tính toán
        public StairPreset Preset { get; set; } = StairPreset.UShape;
        public StairInputMode InputMode { get; set; } = StairInputMode.ByPreferredRiser;
        public int FlightCount { get; set; } = 2;

        // 2. Kích thước cơ bản (mm)
        public double FloorToFloorMm { get; set; } = 3600.0;
        public double StairWidthMm { get; set; } = 1000.0;
        public double PreferredRiserMm { get; set; } = 160.0;
        public double TreadMm { get; set; } = 280.0;
        public double LandingDepthMm { get; set; } = 1200.0;
        public double WaistThicknessMm { get; set; } = 120.0;
        public double FlightGapMm { get; set; } = 100.0;

        // 3. Hoàn thiện & Kiến trúc
        public double MortarThicknessMm { get; set; } = 30.0;
        public double NosingLengthMm { get; set; } = 20.0;
        public double TreadFinishMm { get; set; } = 20.0;
        public double RiserFinishMm { get; set; } = 15.0;

        // 4. Bậc rẻ quạt (Winder)
        public int WinderStepCount { get; set; } = 3;
        public int FirstWinderStepCount { get; set; } = 3;
        public int SecondWinderStepCount { get; set; } = 3;

        // 5. Cấu hình số bậc tùy chỉnh từng vế
        public int CustomRisersFlight1 { get; set; } = 11;
        public int CustomRisersFlight2 { get; set; } = 11;
        public int CustomRisersFlight3 { get; set; } = 0;

        // 6. Dầm chiếu nghỉ
        public double LandingBeamWidthMm { get; set; } = 200.0;
        public double LandingBeamHeightMm { get; set; } = 350.0;
        public List<StairLandingBeam> LandingBeams { get; set; } = new();

        // 7. Định danh Family xuất ra
        public string FamilyName { get; set; } = "JNN_Thang_BTCT_2Ve";
        public string TypeName { get; set; } = "1000x280_H3600";
        public string MaterialName { get; set; } = "Concrete - Cast In Situ";

        // Các thuộc tính tính toán tự động
        public int TotalRisers
        {
            get
            {
                if (InputMode == StairInputMode.ByStepCount)
                {
                    return Math.Max(1, CustomRisersFlight1 + CustomRisersFlight2 + CustomRisersFlight3);
                }

                if (PreferredRiserMm <= 0) return 21;
                int count = (int)Math.Round(FloorToFloorMm / PreferredRiserMm);
                return Math.Max(1, count);
            }
        }

        public double ActualRiserMm => TotalRisers > 0 ? FloorToFloorMm / TotalRisers : 160.0;

        public double BlondelComfortIndex => 2 * ActualRiserMm + TreadMm; // Lý tưởng: 600 - 640 mm

        public bool IsWinder => Preset == StairPreset.LShapeWinder || Preset == StairPreset.ThreeFlightUWinder;

        public bool IsThreeFlight => Preset == StairPreset.ThreeFlightU || Preset == StairPreset.ThreeFlightUWinder;

        /// <summary>
        /// Phân bổ số bậc cho từng vế thang
        /// </summary>
        public IReadOnlyList<int> GetRisersPerFlight()
        {
            if (FlightCount == 1 || Preset == StairPreset.Straight)
            {
                return new[] { TotalRisers };
            }

            if (FlightCount == 2 || Preset == StairPreset.LShape || Preset == StairPreset.UShape || Preset == StairPreset.LShapeWinder)
            {
                int f1 = TotalRisers / 2;
                int f2 = TotalRisers - f1;
                return new[] { f1, f2 };
            }

            if (FlightCount == 3 || IsThreeFlight)
            {
                int f = TotalRisers / 3;
                int rem = TotalRisers % 3;
                return new[] { f, f + rem, f };
            }

            return new[] { TotalRisers };
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của tham số thiết kế
        /// </summary>
        public string? Validate()
        {
            if (FloorToFloorMm < 1000 || FloorToFloorMm > 10000)
                return "Chiều cao tầng không hợp lệ (1000mm - 10000mm).";

            if (StairWidthMm < 600 || StairWidthMm > 3000)
                return "Chiều rộng vế thang không hợp lệ (600mm - 3000mm).";

            if (TreadMm < 200 || TreadMm > 450)
                return "Bề rộng mặt bậc không hợp lệ (200mm - 450mm).";

            if (WaistThicknessMm < 80 || WaistThicknessMm > 300)
                return "Chiều dày bản nghiêng không hợp lệ (80mm - 300mm).";

            return null;
        }
    }
}
