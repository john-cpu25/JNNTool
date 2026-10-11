using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNN.Stair.Models;

namespace JNN.Stair.Services
{
    /// <summary>
    /// Dịch vụ đẩy Transaction tạo RebarSet 3D trong Revit từ dữ liệu đã tính toán
    /// </summary>
    public class StairRebarService
    {
        private readonly Document _doc;

        public StairRebarService(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        /// <summary>
        /// Tạo một bộ thép RebarSet trong đối tượng bê tông (Host)
        /// </summary>
        public Rebar? CreateRebarSet(
            Element hostElement,
            RebarBarType barType,
            StairRebarData rebarData,
            XYZ normalVector,
            View activeView)
        {
            if (rebarData == null || rebarData.CenterlinePoints.Count < 2) return null;

            double mmToFt = 1.0 / 304.8;
            var curves = new List<Curve>();

            for (int i = 0; i < rebarData.CenterlinePoints.Count - 1; i++)
            {
                XYZ p1 = ToXyz(rebarData.CenterlinePoints[i], mmToFt);
                XYZ p2 = ToXyz(rebarData.CenterlinePoints[i + 1], mmToFt);
                curves.Add(Line.CreateBound(p1, p2));
            }

            try
            {
#if REVIT2027
                var terminations = new BarTerminationsData(_doc);
                Rebar rebar = Rebar.CreateFromCurves(
                    _doc,
                    RebarStyle.Standard,
                    barType,
                    hostElement,
                    normalVector,
                    curves,
                    terminations,
                    true,
                    true);
#else
                Rebar rebar = Rebar.CreateFromCurves(
                    _doc,
                    RebarStyle.Standard,
                    barType,
                    null,
                    null,
                    hostElement,
                    normalVector,
                    curves,
                    RebarHookOrientation.Right,
                    RebarHookOrientation.Right,
                    true,
                    true);
#endif

                if (rebar != null)
                {
                    // Thiết lập rải đều theo khoảng cách tối đa
                    double spacingFt = rebarData.SpacingMm * mmToFt;
                    double arrayLenFt = rebarData.ArrayLengthMm * mmToFt;

                    if (arrayLenFt > spacingFt)
                    {
                        rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(
                            spacingFt,
                            arrayLenFt,
                            true,
                            true,
                            true);
                    }

                    // Hiển thị xuyên bê tông trong view
                    rebar.SetUnobscuredInView(activeView, true);
                }

                return rebar;
            }
            catch
            {
                return null;
            }
        }

        private static XYZ ToXyz(StairPoint3D pt, double scale) => new XYZ(pt.X * scale, pt.Y * scale, pt.Z * scale);
    }
}
