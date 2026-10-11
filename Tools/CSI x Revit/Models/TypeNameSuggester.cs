using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace JNNTool.Tools.CSIxRevit.Models
{
    /// <summary>
    /// Gợi ý tên type Revit khi nhân bản: tiền tố chữ của tên ETABS + kích thước thực (mm).
    /// Tên ETABS thường ghi theo cm và đảo b/h (vd. C50X20 = 200×500 mm), nên không dùng lại nguyên tên.
    /// </summary>
    public static class TypeNameSuggester
    {
        public static string Suggest(string etabsName, string kind, double widthMm, double depthMm, bool isCircular)
        {
            string name = (etabsName ?? string.Empty).Trim();
            if (kind == ElementKinds.VirtualSlab) return name;

            string prefix = GetPrefix(name, kind);

            if (kind == ElementKinds.Column || kind == ElementKinds.Beam)
            {
                if (isCircular && depthMm > 0)
                {
                    // C-D50 → tiền tố "C-D": bỏ ký hiệu đường kính để không thành "C-DD500".
                    string p = prefix.TrimEnd('D', 'd', 'Ø', 'ø').TrimEnd('-', '_', ' ', '.');
                    if (string.IsNullOrEmpty(p)) p = GetPrefix(string.Empty, kind);
                    return p + "D" + Fmt(depthMm);
                }
                if (widthMm > 0 && depthMm > 0)
                    return prefix + Fmt(widthMm) + "x" + Fmt(depthMm);
                return name;
            }

            if (kind == ElementKinds.Slab || kind == ElementKinds.FoundationSlab || kind == ElementKinds.Wall)
            {
                return depthMm > 0 ? prefix + Fmt(depthMm) : name;
            }

            return name;
        }

        /// <summary>Phần chữ đứng trước chữ số đầu tiên (C50X20 → C, DC-30x60 → DC); rỗng thì lấy theo loại cấu kiện.</summary>
        public static string GetPrefix(string etabsName, string kind)
        {
            var m = Regex.Match(etabsName ?? string.Empty, @"^[^\d]*");
            string prefix = m.Value.Trim().TrimEnd('-', '_', ' ', '.');
            if (!string.IsNullOrEmpty(prefix)) return prefix;

            if (kind == ElementKinds.Column) return "C";
            if (kind == ElementKinds.Beam) return "B";
            if (kind == ElementKinds.Wall) return "W";
            if (kind == ElementKinds.FoundationSlab) return "F";
            return "S";
        }

        private static string Fmt(double mm) =>
            Math.Round(mm, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
    }
}
