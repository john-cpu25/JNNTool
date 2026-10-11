using System.Globalization;
using System.Text.RegularExpressions;

namespace JNNTool.Tools.ElementMark.Engine
{
    /// <summary>Giá trị thay vào các token của template Mark.</summary>
    public class MarkTokens
    {
        public string Prefix { get; set; } = "";
        public string Level { get; set; } = "";
        public string Axis { get; set; } = "";
        public int Number { get; set; }
        /// <summary>0 = không có nhịp, token {S} sẽ thành chuỗi rỗng.</summary>
        public int Span { get; set; }
    }

    /// <summary>
    /// Thay token trong template. Token không phân biệt hoa thường; token lạ giữ nguyên
    /// để người dùng thấy ngay mình gõ sai trong preview.
    /// </summary>
    public static class MarkTemplateEngine
    {
        private static readonly Regex TokenRegex =
            new Regex(@"\{(?<k>[A-Za-z]+)(?::(?<f>0+))?\}", RegexOptions.Compiled);

        public static string Format(string template, MarkTokens tokens)
        {
            if (string.IsNullOrEmpty(template)) return "";
            tokens ??= new MarkTokens();

            return TokenRegex.Replace(template, m =>
            {
                string key = m.Groups["k"].Value.ToUpperInvariant();
                string pad = m.Groups["f"].Value;
                switch (key)
                {
                    case "P": return tokens.Prefix ?? "";
                    case "L": return tokens.Level ?? "";
                    case "D": return tokens.Axis ?? "";
                    case "N": return Pad(tokens.Number, pad);
                    case "S": return tokens.Span > 0 ? Pad(tokens.Span, pad) : "";
                    default: return m.Value;
                }
            });
        }

        /// <summary>Template có chứa token số thứ tự {N} hay không.</summary>
        public static bool HasNumberToken(string template)
        {
            if (string.IsNullOrEmpty(template)) return false;
            foreach (Match m in TokenRegex.Matches(template))
            {
                if (string.Equals(m.Groups["k"].Value, "N", System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string Pad(int value, string pad)
        {
            return pad.Length > 0
                ? value.ToString(new string('0', pad.Length), CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
