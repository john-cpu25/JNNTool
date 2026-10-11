#nullable enable
using System;
using System.Text;

namespace JNNTool.Tools.StairModel.Engine
{
    /// <summary>
    /// Kiểm tra tính hợp lệ về mặt kỹ thuật, kiến trúc và kết cấu của cấu hình thang.
    /// </summary>
    public static class StairValidator
    {
        public static (bool IsValid, string? Error, string? Warning) Validate(StairConfig config)
        {
            if (config == null)
                return (false, "Cấu hình thang không hợp lệ (null).", null);

            double totalRise = config.TotalRiseMm;
            if (totalRise <= 100.0)
                return (false, $"Chiều cao thang không hợp lệ: Total Rise = {totalRise:F1} mm (phải > 100 mm). Vui lòng kiểm tra cao độ Base và Top Level.", null);

            if (config.TotalRisers <= 1)
                return (false, $"Tổng số cổ bậc không hợp lệ: {config.TotalRisers} bậc (phải >= 2 bậc).", null);

            if (config.StairWidthMm < 500.0)
                return (false, $"Bề rộng vế thang {config.StairWidthMm:F0} mm quá nhỏ (tối thiểu 500 mm).", null);

            if (config.TreadDepthMm < 180.0)
                return (false, $"Bề rộng mặt bậc {config.TreadDepthMm:F0} mm quá nhỏ (tối thiểu 180 mm).", null);

            if (config.WaistSlabThicknessMm < 60.0)
                return (false, $"Chiều dày bản thang {config.WaistSlabThicknessMm:F0} mm quá mỏng (tối thiểu 60 mm).", null);

            if (config.LandingSlabThicknessMm < 60.0)
                return (false, $"Chiều dày bản chiếu nghỉ {config.LandingSlabThicknessMm:F0} mm quá mỏng (tối thiểu 60 mm).", null);

            // Kiểm tra phân bổ bậc cho thang 2 vế (U-shaped, L-shaped)
            if (config.StairType == StairType.UShaped || config.StairType == StairType.LShaped)
            {
                if (config.Flight1Risers < 1 || config.Flight2Risers < 1)
                    return (false, "Mỗi vế thang phải có ít nhất 1 cổ bậc.", null);

                if (config.Flight1Risers + config.Flight2Risers != config.TotalRisers)
                {
                    // Tự động điều chỉnh hoặc báo lỗi nếu không khớp
                    return (false, $"Tổng số bậc vế 1 ({config.Flight1Risers}) + vế 2 ({config.Flight2Risers}) = {config.Flight1Risers + config.Flight2Risers} không khớp tổng số bậc ({config.TotalRisers}).", null);
                }

                if (config.LandingLengthMm < 500.0)
                    return (false, $"Chiều dài chiếu nghỉ {config.LandingLengthMm:F0} mm quá nhỏ (tối thiểu 500 mm).", null);
            }

            // Kiểm tra chỉ số ecgônômi theo công thức Blondel (2R + T)
            double riserHeight = totalRise / config.TotalRisers;
            double blondel = 2.0 * riserHeight + config.TreadDepthMm;

            var warnings = new StringBuilder();
            if (riserHeight < 130.0 || riserHeight > 200.0)
            {
                warnings.AppendLine($"• Chiều cao cổ bậc R = {riserHeight:F1} mm nằm ngoài dải tiêu chuẩn khuyến nghị (140 - 190 mm).");
            }

            if (blondel < 580.0 || blondel > 660.0)
            {
                warnings.AppendLine($"• Chỉ số Blondel 2R + T = {blondel:F1} mm lệch so với bước chân người Việt Nam (600 - 640 mm).");
            }

            string? warnMsg = warnings.Length > 0 ? warnings.ToString().TrimEnd() : null;
            return (true, null, warnMsg);
        }
    }
}
