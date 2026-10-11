using System;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class ColumnDimensionHelper
    {
        /// <summary>
        /// Thuần C# (POCO): Tính kích thước phủ bì thực tế của tiết diện cột theo trục X và Y sau khi xoay góc rotationDeg.
        /// Công thức hình học tổng quát:
        /// ExtentX = b * |cos θ| + h * |sin θ|
        /// ExtentY = b * |sin θ| + h * |cos θ|
        /// </summary>
        public static void CalculateExtentsMm(double bMm, double hMm, double rotationDeg, out double extentXMm, out double extentYMm)
        {
            if (bMm <= 0) bMm = 200.0;
            if (hMm <= 0) hMm = bMm;

            double rad = rotationDeg * Math.PI / 180.0;
            double cos = Math.Abs(Math.Cos(rad));
            double sin = Math.Abs(Math.Sin(rad));

            // Tránh sai số số thực với góc trực giao 0, 90, 180, 270
            if (cos < 1e-6) cos = 0.0;
            if (sin < 1e-6) sin = 0.0;
            if (Math.Abs(cos - 1.0) < 1e-6) cos = 1.0;
            if (Math.Abs(sin - 1.0) < 1e-6) sin = 1.0;

            extentXMm = bMm * cos + hMm * sin;
            extentYMm = bMm * sin + hMm * cos;
        }

        /// <summary>
        /// Thuần C# (POCO): Tính khoảng cách dời (từ tim ra mép ngoài) của cột theo hướng vector (dirX, dirY).
        /// </summary>
        public static double CalculateShiftDistanceMm(double bMm, double hMm, double rotationDeg, double dirX, double dirY, double customOffsetMm = 0)
        {
            if (customOffsetMm > 0) return customOffsetMm;

            CalculateExtentsMm(bMm, hMm, rotationDeg, out double extX, out double extY);

            bool isAlongY = Math.Abs(dirY) >= Math.Abs(dirX);
            return isAlongY ? (extY / 2.0) : (extX / 2.0);
        }

        /// <summary>
        /// Revit API: Lấy kích thước phủ bì thực tế của cột theo trục World X và Y (đơn vị Feet).
        /// Kết hợp đọc tham số b, h và góc xoay với BoundingBox thực tế.
        /// </summary>
        public static void GetColumnExtentsFeet(FamilyInstance col, out double extentXFeet, out double extentYFeet)
        {
            extentXFeet = 0;
            extentYFeet = 0;

            if (col == null) return;

            // 1. Đọc kích thước b và h từ Type / Instance
            GetColumnDimensionsFeet(col, out double bFeet, out double hFeet);

            // 2. Lấy góc xoay của cột quanh trục Z
            double rotationDeg = 0;
            if (col.Location is LocationPoint locPt)
            {
                rotationDeg = locPt.Rotation * 180.0 / Math.PI;
            }

            double bMm = UnitUtils.ConvertFromInternalUnits(bFeet, UnitTypeId.Millimeters);
            double hMm = UnitUtils.ConvertFromInternalUnits(hFeet, UnitTypeId.Millimeters);

            CalculateExtentsMm(bMm, hMm, rotationDeg, out double extXMm, out double extYMm);

            extentXFeet = UnitUtils.ConvertToInternalUnits(extXMm, UnitTypeId.Millimeters);
            extentYFeet = UnitUtils.ConvertToInternalUnits(extYMm, UnitTypeId.Millimeters);

            // 3. Đối chiếu với BoundingBox để kiểm chứng hình học
            try
            {
                var bbox = col.get_BoundingBox(null);
                if (bbox != null)
                {
                    double bboxX = bbox.Max.X - bbox.Min.X;
                    double bboxY = bbox.Max.Y - bbox.Min.Y;

                    // Nếu kích thước BoundingBox hợp lệ (> 50mm) và sai lệch quá lớn so với tham số (do Family định nghĩa đặc biệt)
                    if (bboxX > 0.15 && bboxY > 0.15)
                    {
                        // Nếu tham số chưa đọc được chính xác (b=h=default 200), ưu tiên BoundingBox
                        if (Math.Abs(bMm - 200.0) < 1.0 && Math.Abs(hMm - 200.0) < 1.0 && (bboxX > 0.7 || bboxY > 0.7))
                        {
                            extentXFeet = bboxX;
                            extentYFeet = bboxY;
                        }
                    }
                }
            }
            catch
            {
                // Giữ nguyên kết quả tính từ tham số hình học
            }
        }

        /// <summary>
        /// Revit API: Đọc cả 2 tham số b (Width) và h (Height / Depth) của Cột từ FamilyInstance hoặc FamilySymbol.
        /// </summary>
        public static void GetColumnDimensionsFeet(FamilyInstance col, out double bFeet, out double hFeet)
        {
            var symbol = col.Symbol;

            bFeet = GetParamValue(col, symbol, "b", "Width", "B", "Chiều rộng", "b (Width)", "Width (b)");
            hFeet = GetParamValue(col, symbol, "h", "Depth", "Height", "H", "Chiều cao", "h (Height)", "Height (h)", "d", "D");

            // Xử lý cột vuông (chỉ có 1 tham số b hoặc Width)
            if (bFeet > 0 && hFeet <= 0) hFeet = bFeet;
            if (hFeet > 0 && bFeet <= 0) bFeet = hFeet;

            // Fallback nếu không tìm thấy tham số
            if (bFeet <= 0) bFeet = UnitUtils.ConvertToInternalUnits(200.0, UnitTypeId.Millimeters);
            if (hFeet <= 0) hFeet = bFeet;
        }

        private static double GetParamValue(FamilyInstance instance, FamilySymbol symbol, params string[] paramNames)
        {
            foreach (var name in paramNames)
            {
                var p = instance?.LookupParameter(name) ?? symbol?.LookupParameter(name);
                if (p != null && p.StorageType == StorageType.Double)
                {
                    double val = p.AsDouble();
                    if (val > 0.001) return val;
                }
            }
            return 0;
        }
    }
}
