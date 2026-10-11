using System;
using System.Text.RegularExpressions;

namespace JNNTool.Tools.CSIxRevit.Models
{
    /// <summary>
    /// Nhãn loại đối tượng dùng chung giữa Parser, UI và Command.
    /// Gom về một chỗ để tránh lệch chuỗi (bug cũ: so sánh chuỗi rải rác).
    /// </summary>
    public static class ElementKinds
    {
        public const string Column = "Column";
        public const string Beam = "Beam";
        public const string Wall = "Wall";
        public const string Slab = "Slab (S)";
        public const string FoundationSlab = "Foundation Slab (F)";
        public const string VirtualSlab = "Virtual Slab (skip)";

        public static bool IsFloorLike(string kind) =>
            kind == Slab || kind == FoundationSlab || kind == VirtualSlab;
    }

    public enum SlabKind
    {
        /// <summary>Sàn thường (S) → Floor.</summary>
        Slab,
        /// <summary>Sàn móng / bè (F) → Structural Foundation: Slab.</summary>
        Foundation,
        /// <summary>Sàn ảo (membrane rất mỏng, chỉ để truyền tải) → không dựng.</summary>
        Virtual
    }

    /// <summary>
    /// Phân loại tiết diện sàn ETABS. Thuần C# để unit test.
    /// </summary>
    public static class SlabClassifier
    {
        /// <summary>Sàn có chiều dày ≤ ngưỡng này (mm) được coi là sàn ảo.</summary>
        public const double VirtualThicknessMm = 1.0;

        private static readonly Regex FoundationNameRegex =
            new Regex(@"^\s*F\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static SlabKind Classify(string name, string modelingType, double thicknessMm)
        {
            // Sàn ảo ưu tiên trước: dù tên/kiểu gì, dày ≤ 1 mm thì không có ý nghĩa hình học.
            if (thicknessMm > 0 && thicknessMm <= VirtualThicknessMm + 1e-6)
                return SlabKind.Virtual;

            if (!string.IsNullOrWhiteSpace(name) && FoundationNameRegex.IsMatch(name))
                return SlabKind.Foundation;

            if (!string.IsNullOrWhiteSpace(modelingType) &&
                modelingType.Trim().Equals("ShellThick", StringComparison.OrdinalIgnoreCase))
                return SlabKind.Foundation;

            return SlabKind.Slab;
        }

        public static SlabKind Classify(SectionDefinition def, string fallbackName)
        {
            if (def == null) return Classify(fallbackName, null, 0);
            return Classify(string.IsNullOrEmpty(def.Name) ? fallbackName : def.Name, def.ModelingType, def.DepthMm);
        }

        public static string ToElementKind(SlabKind kind)
        {
            switch (kind)
            {
                case SlabKind.Foundation: return ElementKinds.FoundationSlab;
                case SlabKind.Virtual: return ElementKinds.VirtualSlab;
                default: return ElementKinds.Slab;
            }
        }
    }
}
