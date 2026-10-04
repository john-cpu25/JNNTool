using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Column.Models;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Column.Builders
{
    /// <summary>
    /// Trích xuất dữ liệu chuỗi cột từ Revit và tạo cốt thép cột trong Document.
    /// </summary>
    public static class ColumnRebarBuilder
    {
        public static ColumnStackModel ExtractColumnStack(Document doc, IList<FamilyInstance> columnInstances)
        {
            var stack = new ColumnStackModel();
            if (columnInstances == null || columnInstances.Count == 0) return stack;

            var rawStories = new List<ColumnStoryModel>();

            foreach (var col in columnInstances)
            {
                var bbox = col.get_BoundingBox(null);
                if (bbox == null) continue;

                double botZ = GeometryHelper.FeetToMm(bbox.Min.Z);
                double topZ = GeometryHelper.FeetToMm(bbox.Max.Z);

                double bMm = 400.0;
                double hMm = 500.0;

                var symbol = col.Symbol;
                if (symbol != null)
                {
                    double bFeet = symbol.LookupParameter("b")?.AsDouble() ?? symbol.LookupParameter("Width")?.AsDouble() ?? 1.31;
                    double hFeet = symbol.LookupParameter("h")?.AsDouble() ?? symbol.LookupParameter("Depth")?.AsDouble() ?? 1.64;
                    bMm = GeometryHelper.FeetToMm(bFeet);
                    hMm = GeometryHelper.FeetToMm(hFeet);
                }

                double centerXMm = GeometryHelper.FeetToMm((bbox.Min.X + bbox.Max.X) / 2.0);
                double centerYMm = GeometryHelper.FeetToMm((bbox.Min.Y + bbox.Max.Y) / 2.0);

                // Dò tìm chiều sâu dầm giao tại đỉnh cột
                double beamDepthMm = 500.0;
                try
                {
                    var beamFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming);
                    var intersectingBeams = new FilteredElementCollector(doc)
                        .WherePasses(beamFilter)
                        .WhereElementIsNotElementType()
                        .OfClass(typeof(FamilyInstance))
                        .Cast<FamilyInstance>()
                        .Where(bm =>
                        {
                            var bBox = bm.get_BoundingBox(null);
                            return bBox != null && bBox.Min.Z <= bbox.Max.Z && bBox.Max.Z >= bbox.Max.Z - 3.0;
                        })
                        .ToList();

                    if (intersectingBeams.Count > 0)
                    {
                        double maxD = 0;
                        foreach (var bm in intersectingBeams)
                        {
                            var bSym = bm.Symbol;
                            double dFeet = bSym?.LookupParameter("h")?.AsDouble() ?? bSym?.LookupParameter("Height")?.AsDouble() ?? 1.64;
                            if (dFeet > maxD) maxD = dFeet;
                        }
                        beamDepthMm = GeometryHelper.FeetToMm(maxD);
                    }
                }
                catch { }

                rawStories.Add(new ColumnStoryModel
                {
                    ElementId = col.Id.GetIdValue(),
                    LevelName = doc.GetElement(col.LevelId)?.Name ?? "Level",
                    BottomElevationMm = botZ,
                    TopElevationMm = topZ,
                    WidthBMm = bMm,
                    HeightHMm = hMm,
                    CenterPoint = new Point3D(centerXMm, centerYMm, (botZ + topZ) / 2.0),
                    IntersectingBeamDepthMm = beamDepthMm
                });
            }

            // Sắp xếp chuỗi cột từ tầng dưới lên tầng trên
            var sortedStories = rawStories.OrderBy(s => s.BottomElevationMm).ToList();
            for (int i = 0; i < sortedStories.Count; i++)
            {
                sortedStories[i].StoryIndex = i;
            }

            stack.Stories = sortedStories;
            stack.GroupName = sortedStories.Count > 0 ? $"C_{sortedStories[0].ElementId}" : "C1";

            return stack;
        }

        public static int BuildRebar(Document doc, IList<FamilyInstance> columns, ColumnLayoutResult layoutResult)
        {
            if (columns.Count == 0 || layoutResult.RebarSets.Count == 0) return 0;

            int createdCount = 0;
            var primaryCol = columns[0];

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

                    bool isStirrup = (setInfo.Role == ColumnRebarRole.StirrupBaseDense ||
                                      setInfo.Role == ColumnRebarRole.StirrupMidSparse ||
                                      setInfo.Role == ColumnRebarRole.StirrupTopDense ||
                                      setInfo.Role == ColumnRebarRole.InternalStirrupOrTie);

                    var hookType = isStirrup ? RebarTypeResolver.ResolveHookType(doc, HookAngle.Deg135) : null;

                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        hookType,
                        hookType,
                        primaryCol,
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

                        rebar.LookupParameter("Partition")?.Set("JNN_Column");
                        rebar.LookupParameter("Comments")?.Set(setInfo.Description);

                        createdCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Lỗi khi tạo thép cột: {setInfo.Description}", ex);
                }
            }

            return createdCount;
        }
    }
}
