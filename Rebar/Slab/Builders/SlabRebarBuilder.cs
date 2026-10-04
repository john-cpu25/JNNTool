using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Slab.Builders
{
    /// <summary>
    /// Thực hiện trích xuất dữ liệu từ Revit Floor và tạo các Rebar / RebarSet vào Revit Document.
    /// </summary>
    public static class SlabRebarBuilder
    {
        /// <summary>
        /// Trích xuất SlabModel từ một đối tượng Floor trong Revit (chuyển đổi feet sang mm).
        /// </summary>
        public static SlabModel ExtractSlabModel(Floor floor)
        {
            var doc = floor.Document;
            var slabModel = new SlabModel
            {
                ElementId = floor.Id.GetIdValue(),
                HostMark = floor.LookupParameter("Mark")?.AsString() ?? $"Slab_{floor.Id.GetIdValue()}",
                LevelName = doc.GetElement(floor.LevelId)?.Name ?? ""
            };

            // 1. Chiều dày sàn
            var type = doc.GetElement(floor.GetTypeId()) as FloorType;
            if (type != null)
            {
                double thicknessFeet = type.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() ?? 0.4;
                slabModel.ThicknessMm = GeometryHelper.FeetToMm(thicknessFeet);
            }

            // 2. BoundingBox & Cao độ
            var bbox = floor.get_BoundingBox(null);
            if (bbox != null)
            {
                slabModel.TopElevationMm = GeometryHelper.FeetToMm(bbox.Max.Z);
                double minX = GeometryHelper.FeetToMm(bbox.Min.X);
                double minY = GeometryHelper.FeetToMm(bbox.Min.Y);
                double maxX = GeometryHelper.FeetToMm(bbox.Max.X);
                double maxY = GeometryHelper.FeetToMm(bbox.Max.Y);

                // Dựng biên dạng chữ nhật từ BoundingBox sàn
                slabModel.OuterBoundary.Add(new Point2D(minX, minY));
                slabModel.OuterBoundary.Add(new Point2D(maxX, minY));
                slabModel.OuterBoundary.Add(new Point2D(maxX, maxY));
                slabModel.OuterBoundary.Add(new Point2D(minX, maxY));
            }

            // 3. Quét dầm đỡ tiếp xúc (OST_StructuralFraming)
            try
            {
                var beamFilter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming);
                var beams = new FilteredElementCollector(doc)
                    .WherePasses(beamFilter)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .ToList();

                foreach (var beam in beams)
                {
                    var beamBox = beam.get_BoundingBox(null);
                    if (beamBox == null || bbox == null) continue;

                    // Kiểm tra dầm có nằm trong phạm vi cao độ sàn không
                    if (beamBox.Max.Z >= bbox.Min.Z - 2.0 && beamBox.Min.Z <= bbox.Max.Z)
                    {
                        var curve = (beam.Location as LocationCurve)?.Curve;
                        if (curve != null)
                        {
                            var p0 = curve.GetEndPoint(0);
                            var p1 = curve.GetEndPoint(1);

                            slabModel.SupportingBeams.Add(new BeamSupportInfo
                            {
                                ElementId = beam.Id.GetIdValue(),
                                StartPoint = new Point2D(GeometryHelper.FeetToMm(p0.X), GeometryHelper.FeetToMm(p0.Y)),
                                EndPoint = new Point2D(GeometryHelper.FeetToMm(p1.X), GeometryHelper.FeetToMm(p1.Y))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Không thể quét toàn bộ dầm đỡ: {ex.Message}");
            }

            return slabModel;
        }

        /// <summary>
        /// Tạo các RebarSet từ kết quả tính toán LayoutResult vào sàn Revit trong một Transaction.
        /// </summary>
        public static int BuildRebar(Document doc, Floor floor, SlabLayoutResult layoutResult)
        {
            int createdCount = 0;
            var settings = layoutResult.Settings;

            foreach (var setInfo in layoutResult.RebarSets)
            {
                if (setInfo.CurvePoints.Count < 2) continue;

                try
                {
                    // Chuyển đổi các điểm sang Revit XYZ (feet)
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

                    // Tìm RebarBarType phù hợp
                    var barInfo = BarCatalog.Parse(setInfo.Diameter);
                    int dia = barInfo?.NominalDiameter ?? 10;
                    var barType = RebarTypeResolver.ResolveBarType(doc, dia);
                    if (barType == null)
                    {
                        Logger.Warning($"Không tìm thấy RebarBarType cho {setInfo.Diameter}");
                        continue;
                    }

                    // Tìm RebarHookType nếu có
                    var startHook = RebarTypeResolver.ResolveHookType(doc, setInfo.StartHook);
                    var endHook = RebarTypeResolver.ResolveHookType(doc, setInfo.EndHook);

                    // Tạo thanh thép từ curves
                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        startHook,
                        endHook,
                        floor,
                        new XYZ(0, 0, 1),
                        curves,
                        JNNTool.Core.Compat.JnnHookOrientation.Right,
                        JNNTool.Core.Compat.JnnHookOrientation.Right,
                        true,
                        true);

                    if (rebar != null)
                    {
                        // Thiết lập rải số thanh và khoảng cách (RebarSet)
                        if (setInfo.BarCount > 1 && setInfo.SpacingMm > 0)
                        {
                            var accessor = rebar.GetShapeDrivenAccessor();
                            if (accessor != null)
                            {
                                double spacingFeet = GeometryHelper.MmToFeet(setInfo.SpacingMm);
                                accessor.SetLayoutAsNumberWithSpacing(
                                    setInfo.BarCount,
                                    spacingFeet,
                                    true,
                                    true,
                                    true);
                            }
                        }

                        // Hiển thị Unobscured trong view hiện tại
                        try
                        {
                            if (doc.ActiveView != null)
                            {
                                rebar.SetUnobscuredInView(doc.ActiveView, true);
                            }
                        }
                        catch { }

                        // Gán Partition và Comments
                        string partition = settings.AssignPartitionFromHost && !string.IsNullOrWhiteSpace(floor.LookupParameter("Mark")?.AsString())
                            ? floor.LookupParameter("Mark")!.AsString()
                            : settings.DefaultPartition;

                        rebar.LookupParameter("Partition")?.Set(partition);
                        rebar.LookupParameter("Comments")?.Set(setInfo.Description);

                        createdCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Lỗi khi tạo RebarSet: {setInfo.Description}", ex);
                }
            }

            return createdCount;
        }
    }
}
