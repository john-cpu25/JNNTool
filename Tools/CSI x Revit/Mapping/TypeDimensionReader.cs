using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CSIxRevit.Mapping
{
    /// <summary>Kích thước thực của một type Revit (mm). 0 = không đọc được.</summary>
    public struct TypeSize
    {
        public double ThicknessMm;
        public double WidthMm;
        public double HeightMm;
        public double DiameterMm;
    }

    /// <summary>
    /// Đọc kích thước thực từ type Revit (không dựa vào tên) để xác định "đã có tiết diện".
    /// Có cache theo ElementId vì mỗi dòng đều quét toàn bộ type.
    /// </summary>
    public class TypeDimensionReader
    {
        private static readonly string[] WidthNames = { "b", "B", "Width", "width", "Chiều rộng", "b (Width)" };
        private static readonly string[] HeightNames = { "h", "H", "Height", "height", "Chiều cao", "h (Height)", "d", "Depth" };
        private static readonly string[] DiameterNames = { "D", "Diameter", "Đường kính" };

        private readonly Dictionary<ElementId, TypeSize> _cache = new Dictionary<ElementId, TypeSize>();

        public void Clear() => _cache.Clear();

        public TypeSize Get(ElementType type)
        {
            if (type == null || !type.IsValidObject) return default;
            if (_cache.TryGetValue(type.Id, out var cached)) return cached;

            var size = new TypeSize();
            try
            {
                if (type is FloorType ft)
                {
                    var cs = ft.GetCompoundStructure();
                    double feet = cs != null ? cs.GetWidth() : 0;
                    if (feet <= 0) feet = ft.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() ?? 0;
                    size.ThicknessMm = ToMm(feet);
                }
                else if (type is WallType wt)
                {
                    size.ThicknessMm = ToMm(wt.Width);
                }
                else if (type is FamilySymbol fs)
                {
                    size.WidthMm = ToMm(ReadDouble(fs, WidthNames));
                    size.HeightMm = ToMm(ReadDouble(fs, HeightNames));
                    size.DiameterMm = ToMm(ReadDouble(fs, DiameterNames));
                }
            }
            catch
            {
                // Type lỗi / family hỏng: coi như không đọc được kích thước, không làm sập tool.
            }

            _cache[type.Id] = size;
            return size;
        }

        private static double ReadDouble(FamilySymbol symbol, string[] names)
        {
            // Duyệt theo thứ tự ưu tiên tên, chỉ lấy tham số type kiểu Length.
            foreach (var name in names)
            {
                foreach (Parameter p in symbol.Parameters)
                {
                    if (p.StorageType == StorageType.Double && p.Definition != null &&
                        string.Equals(p.Definition.Name, name, StringComparison.Ordinal))
                    {
                        double v = p.AsDouble();
                        if (v > 0) return v;
                    }
                }
            }
            return 0;
        }

        private static double ToMm(double feet) =>
            feet > 0 ? UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters) : 0;
    }
}
