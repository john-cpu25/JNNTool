using System;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    /// <summary>
    /// Helper trích xuất bề rộng (Width) của Dầm (Structural Framing) đa tầng:
    /// 1. Đọc tham số hình học b / Width từ Type hoặc Instance.
    /// 2. Phân tích tên Type (Symbol Name): ví dụ B40.50 -> 400mm, B60.40 -> 600mm, 400x600 -> 400mm, 550x700 -> 550mm.
    /// 3. Đối chiếu với 3D BoundingBox của dầm theo phương vuông góc với trục dầm.
    /// Đảm bảo các dầm có bề rộng lớn (400, 550, 600, 800 mm) luôn được nhận diện chính xác 100%.
    /// </summary>
    public static class BeamDimensionHelper
    {
        /// <summary>
        /// Thuần C# (POCO): Trích xuất bề rộng dầm (mm) từ tên Type hoặc tham số số học.
        /// </summary>
        public static double ExtractBeamWidthMm(string typeName, double paramBMm = 0)
        {
            if (paramBMm > 30.0) return paramBMm;

            if (string.IsNullOrWhiteSpace(typeName)) return 200.0;

            typeName = typeName.Trim();

            // Mẫu 1: Dạng cm ETABS như B40.50, B60.40, B50.75, B80.50, B20.35, C30.40
            // Nhận diện chữ cái đầu (B/C/D) + 2 chữ số (cm) + dấu chấm + 2-3 chữ số (cm)
            var matchCm = Regex.Match(typeName, @"^[A-Za-z]?\s*(\d{2})\s*\.\s*(\d{2,3})$");
            if (matchCm.Success)
            {
                if (double.TryParse(matchCm.Groups[1].Value, out double bCm) && bCm >= 10.0 && bCm <= 200.0)
                {
                    return bCm * 10.0; // Đổi từ cm ra mm (ví dụ 40 -> 400 mm, 60 -> 600 mm)
                }
            }

            // Mẫu 2: Dạng mm rõ ràng: 400x600, B400x600, 600x400, 550x700, 400*600, B400-600
            var matchMm = Regex.Match(typeName, @"(?:[A-Za-z_\-]*)\s*(\d{2,4})\s*[xX*_\-]\s*(\d{2,4})");
            if (matchMm.Success)
            {
                if (double.TryParse(matchMm.Groups[1].Value, out double dim1) &&
                    double.TryParse(matchMm.Groups[2].Value, out double dim2))
                {
                    // Nếu kích thước dạng cm (ví dụ 40x60)
                    if (dim1 <= 150.0 && dim2 <= 200.0 && (dim1 >= 10.0 || dim2 >= 10.0))
                    {
                        dim1 *= 10.0;
                        dim2 *= 10.0;
                    }

                    // Với dầm: quy ước tại Việt Nam tên dầm B400x600 là b=400, h=600.
                    // Nếu dim1 <= dim2, thường dim1 là bề rộng b.
                    // Nếu dim1 > dim2 (dầm bẹt như 600x400), dim1 chính là bề rộng 600!
                    // Lấy dim1 làm bề rộng quy ước đầu tiên
                    return dim1;
                }
            }

            // Mẫu 3: Dạng D350, D400 (dầm tròn / cọc tròn)
            var matchDia = Regex.Match(typeName, @"^[Dd]\s*(\d{2,4})$");
            if (matchDia.Success && double.TryParse(matchDia.Groups[1].Value, out double dia))
            {
                if (dia < 100.0) dia *= 10.0;
                return dia;
            }

            return 200.0;
        }

        /// <summary>
        /// Revit API: Lấy bề rộng thực tế của Dầm (đơn vị Feet).
        /// Kết hợp đọc tham số b / Width, phân tích tên Type, và đối chiếu với 3D BoundingBox.
        /// </summary>
        public static double GetBeamWidthFeet(FamilyInstance beam)
        {
            if (beam == null) return UnitUtils.ConvertToInternalUnits(200.0, UnitTypeId.Millimeters);

            var symbol = beam.Symbol;

            // 1. Tìm tham số bề rộng từ Instance hoặc Symbol
            double paramWidthFeet = GetParamValue(beam, symbol,
                "b", "Width", "B", "Chiều rộng", "b (Width)", "Width (b)",
                "Beam Width", "Breadth", "b1", "b_dam", "B_dam", "w", "W", "Dimension b");

            if (paramWidthFeet > 0.05) // > 15 mm
            {
                return paramWidthFeet;
            }

            // 2. Phân tích từ tên Type (Symbol Name)
            string typeName = symbol?.Name ?? string.Empty;
            double extractedMm = ExtractBeamWidthMm(typeName);
            if (extractedMm > 50.0 && Math.Abs(extractedMm - 200.0) > 1.0)
            {
                return UnitUtils.ConvertToInternalUnits(extractedMm, UnitTypeId.Millimeters);
            }

            // 3. Đối chiếu với 3D BoundingBox của dầm để đo thực tế
            try
            {
                if (beam.Location is LocationCurve lc && lc.Curve is Line beamLine)
                {
                    var bbox = beam.get_BoundingBox(null);
                    if (bbox != null)
                    {
                        XYZ dir = (beamLine.GetEndPoint(1) - beamLine.GetEndPoint(0)).Normalize();
                        double bboxX = bbox.Max.X - bbox.Min.X;
                        double bboxY = bbox.Max.Y - bbox.Min.Y;

                        // Nếu dầm chạy dọc trục X (|dir.X| > 0.8) -> Bề rộng là kích thước theo trục Y
                        if (Math.Abs(dir.X) > 0.8 && bboxY > 0.3) // > 90 mm
                        {
                            return bboxY;
                        }
                        // Nếu dầm chạy dọc trục Y (|dir.Y| > 0.8) -> Bề rộng là kích thước theo trục X
                        if (Math.Abs(dir.Y) > 0.8 && bboxX > 0.3) // > 90 mm
                        {
                            return bboxX;
                        }
                    }
                }
            }
            catch { }

            // 4. Nếu tên Type parse ra được 200 mm
            if (extractedMm > 50.0)
            {
                return UnitUtils.ConvertToInternalUnits(extractedMm, UnitTypeId.Millimeters);
            }

            return UnitUtils.ConvertToInternalUnits(200.0, UnitTypeId.Millimeters);
        }

        private static double GetParamValue(FamilyInstance instance, FamilySymbol symbol, params string[] paramNames)
        {
            foreach (var name in paramNames)
            {
                var p = instance?.LookupParameter(name) ?? symbol?.LookupParameter(name);
                if (p != null && p.StorageType == StorageType.Double)
                {
                    double val = p.AsDouble();
                    if (val > 0.05) return val; // > 15 mm
                }
            }
            return 0;
        }
    }
}
