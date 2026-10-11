using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JNNTool.Tools.ElementMark.Engine
{
    /// <summary>
    /// Gợi ý mã tầng {L} từ tên Level. Chỉ là gợi ý: người dùng sửa lại trong bảng Mã tầng.
    /// </summary>
    public static class LevelCodeResolver
    {
        private static readonly Regex NumberRegex = new Regex(@"\d+", RegexOptions.Compiled);
        private static readonly Regex BasementWord = new Regex(@"\b(ham|basement)\b", RegexOptions.Compiled);
        private static readonly Regex BasementPrefix = new Regex(@"^\s*B\s*\d", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RoofWord = new Regex(@"\b(mai|roof|rf)\b", RegexOptions.Compiled);

        public static string Suggest(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName)) return "";

            string name = levelName.Trim();
            string plain = RemoveDiacritics(name).ToLowerInvariant();
            Match num = NumberRegex.Match(name);
            string number = num.Success ? TrimLeadingZeros(num.Value) : "";

            // Tầng hầm phải tách riêng, nếu không "Tầng hầm 1" sẽ trùng mã với "Tầng 1".
            if (BasementWord.IsMatch(plain) || BasementPrefix.IsMatch(name))
                return "B" + number;

            if (RoofWord.IsMatch(plain))
                return (plain.Contains("roof") || Regex.IsMatch(plain, @"\brf\b") ? "R" : "M") + number;

            if (number.Length > 0) return number;

            // Không có số: lấy chữ cái đầu mỗi từ, ví dụ "Tầng trệt" → "TT".
            string initials = new string(plain
                .Split(new[] { ' ', '-', '_', '.' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(w => char.ToUpperInvariant(w[0]))
                .ToArray());
            return initials;
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            // "đ" không tách dấu bằng FormD nên phải thay tay.
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
        }

        private static string TrimLeadingZeros(string digits)
        {
            string trimmed = digits.TrimStart('0');
            return trimmed.Length == 0 ? "0" : trimmed;
        }
    }
}
