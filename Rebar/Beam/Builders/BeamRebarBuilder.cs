using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Beam.Models;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Beam.Builders
{
    /// <summary>
    /// Trích xuất chuỗi dầm liên tục từ Revit và tạo cốt thép dầm trong Document.
    /// </summary>
    public static class BeamRebarBuilder
    {
        /// <summary>
        /// Trích xuất danh sách dầm được chọn và gom thành chuỗi dầm liên tục theo trục.
        /// </summary>
        public static ContinuousBeamModel ExtractContinuousBeam(Document doc, IList<FamilyInstance> beamInstances)
        {
            var model = new ContinuousBeamModel();
            if (beamInstances == null || beamInstances.Count == 0) return model;

            var rawSpans = new List<BeamSpanModel>();

            foreach (var beam in beamInstances)
            {
                var curve = (beam.Location as LocationCurve)?.Curve;
                if (curve == null) continue;

                var p0 = curve.GetEndPoint(0);
                var p1 = curve.GetEndPoint(1);

                double widthMm = 300.0;
                double heightMm = 500.0;

                // Lấy kích thước b x h từ Beam Type
                var symbol = beam.Symbol;
                if (symbol != null)
                {
                    double bFeet = symbol.LookupParameter("b")?.AsDouble() ?? symbol.LookupParameter("Width")?.AsDouble() ?? 1.0;
                    double hFeet = symbol.LookupParameter("h")?.AsDouble() ?? symbol.LookupParameter("Height")?.AsDouble() ?? 1.64;
                    widthMm = GeometryHelper.FeetToMm(bFeet);
                    heightMm = GeometryHelper.FeetToMm(hFeet);
                }

                var dir = (p1 - p0).Normalize();

                rawSpans.Add(new BeamSpanModel
                {
                    ElementId = beam.Id.GetIdValue(),
                    Mark = beam.LookupParameter("Mark")?.AsString() ?? "D",
                    WidthMm = widthMm,
                    HeightMm = heightMm,
                    LengthMm = GeometryHelper.FeetToMm(curve.Length),
                    StartPoint = new Point3D(GeometryHelper.FeetToMm(p0.X), GeometryHelper.FeetToMm(p0.Y), GeometryHelper.FeetToMm(p0.Z)),
                    EndPoint = new Point3D(GeometryHelper.FeetToMm(p1.X), GeometryHelper.FeetToMm(p1.Y), GeometryHelper.FeetToMm(p1.Z)),
                    DirectionVector = new Point3D(dir.X, dir.Y, dir.Z)
                });
            }

            // Sắp xếp các nhịp nối tiếp nhau theo tọa độ StartPoint
            var sortedSpans = rawSpans.OrderBy(s => s.StartPoint.X).ThenBy(s => s.StartPoint.Y).ToList();
            for (int i = 0; i < sortedSpans.Count; i++)
            {
                sortedSpans[i].SpanIndex = i;
                sortedSpans[i].IsFirstSpan = (i == 0);
                sortedSpans[i].IsLastSpan = (i == sortedSpans.Count - 1);
            }

            model.Spans = sortedSpans;
            model.GroupName = sortedSpans.Count > 0 ? sortedSpans[0].Mark : "ContinuousBeam";

            return model;
        }

        /// <summary>
        /// Tạo các RebarSet từ BeamLayoutResult vào chuỗi dầm Revit.
        /// </summary>
        public static int BuildRebar(Document doc, IList<FamilyInstance> beams, BeamLayoutResult layoutResult)
        {
            if (beams.Count == 0 || layoutResult.RebarSets.Count == 0) return 0;

            int createdCount = 0;
            var primaryHost = beams[0];

            foreach (var setInfo in layoutResult.RebarSets)
            {
                if (setInfo.CurvePoints.Count < 2) continue;

                try
                {
                    var curves = new List<Curve>();
                    for (int i = 0; i < setInfo.CurvePoints.Count - 1; i++)
                    {
                        var p1 = new XYZ(
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i].X),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i].Y),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i].Z));

                        var p2 = new XYZ(
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i + 1].X),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i + 1].Y),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i + 1].Z));

                        if (p1.DistanceTo(p2) > 0.001)
                        {
                            curves.Add(Line.CreateBound(p1, p2));
                        }
                    }

                    if (curves.Count == 0) continue;

                    var barInfo = BarCatalog.Parse(setInfo.Diameter);
                    int dia = barInfo?.NominalDiameter ?? 20;
                    var barType = RebarTypeResolver.ResolveBarType(doc, dia);
                    if (barType == null) continue;

                    // Móc đai hoặc móc cốt chủ
                    var hookType = (setInfo.Role == BeamRebarRole.StirrupDense || setInfo.Role == BeamRebarRole.StirrupSparse)
                        ? RebarTypeResolver.ResolveHookType(doc, HookAngle.Deg135)
                        : null;

                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        hookType,
                        hookType,
                        primaryHost,
                        new XYZ(0, 0, 1),
                        curves,
                        JNNTool.Core.Compat.JnnHookOrientation.Right,
                        JNNTool.Core.Compat.JnnHookOrientation.Right,
                        true,
                        true);

                    if (rebar != null)
                    {
                        if (setInfo.BarCount > 1 && setInfo.SpacingMm > 0)
                        {
                            var accessor = rebar.GetShapeDrivenAccessor();
                            if (accessor != null)
                            {
                                double spacingFeet = GeometryHelper.MmToFeet(setInfo.SpacingMm);
                                accessor.SetLayoutAsNumberWithSpacing(setInfo.BarCount, spacingFeet, true, true, true);
                            }
                        }

                        try
                        {
                            if (doc.ActiveView != null)
                            {
                                rebar.SetUnobscuredInView(doc.ActiveView, true);
                            }
                        }
                        catch { }

                        rebar.LookupParameter("Partition")?.Set("JNN_Beam");
                        rebar.LookupParameter("Comments")?.Set(setInfo.Description);

                        createdCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Lỗi khi tạo thép dầm: {setInfo.Description}", ex);
                }
            }

            return createdCount;
        }
    }
}
