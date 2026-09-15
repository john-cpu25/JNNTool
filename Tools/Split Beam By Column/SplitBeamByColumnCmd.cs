using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JNNTool
{
    // ── Bộ lọc chọn dầm ──────────────────────────────────────────────────
    public class BeamSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) 
            => elem.Category != null && elem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming);

        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    // ── Bộ lọc chọn phần tử ngắt (Cột hoặc Dầm) ──────────────────────────────────────────────
    public class IntersectingElementSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) 
            => elem.Category != null && 
               (elem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns) ||
                elem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming));

        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    [Transaction(TransactionMode.Manual)]
    public class SplitBeamByColumnCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 1. Chọn các dầm cần ngắt
                IList<Reference> beamRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element, 
                    new BeamSelectionFilter(), 
                    "Chọn các dầm cần ngắt (Nhấn Finish)");
                
                if (beamRefs == null || beamRefs.Count == 0) return Result.Cancelled;

                // 2. Chọn các phần tử ngắt (cột hoặc dầm)
                IList<Reference> colRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element, 
                    new IntersectingElementSelectionFilter(), 
                    "Chọn các cột hoặc dầm cắt ngang để ngắt dầm (Nhấn Finish)");

                if (colRefs == null || colRefs.Count == 0) return Result.Cancelled;

                List<FamilyInstance> selectedColumns = new List<FamilyInstance>();
                foreach (Reference colRef in colRefs)
                {
                    if (doc.GetElement(colRef) is FamilyInstance col)
                        selectedColumns.Add(col);
                }

                int beamsProcessed = 0;
                int totalCreated = 0;

                // 3. Thực hiện ngắt dầm tại MẶT PHẦN TỬ CẮT trong Transaction
                using (Transaction tx = new Transaction(doc, "Ngắt dầm tại mặt cắt"))
                {
                    tx.Start();

                    foreach (Reference beamRef in beamRefs)
                    {
                        FamilyInstance originalBeam = doc.GetElement(beamRef) as FamilyInstance;
                        if (originalBeam == null) continue;

                        LocationCurve beamLocCurve = originalBeam.Location as LocationCurve;
                        if (beamLocCurve == null) continue;
                        Curve beamCurve = beamLocCurve.Curve;

                        XYZ startPoint = beamCurve.GetEndPoint(0);
                        XYZ endPoint = beamCurve.GetEndPoint(1);
                        double totalLength = beamCurve.Length;
                        if (totalLength < 1e-4) continue;

                        XYZ dir = (endPoint - startPoint).Normalize();

                        // Thu thập tất cả khoảng tham số [tMin, tMax] của các cột cắt dầm
                        List<(double tMin, double tMax)> colIntervals = new List<(double tMin, double tMax)>();

                        foreach (FamilyInstance col in selectedColumns)
                        {
                            var interval = GetIntersectingFaceInterval(col, beamCurve, startPoint, endPoint, dir, totalLength);
                            if (interval != null)
                            {
                                colIntervals.Add(interval.Value);
                            }
                        }

                        if (colIntervals.Count == 0) continue;

                        // Lọc các khoảng giao với phạm vi [0.0, 1.0] của dầm
                        List<(double tMin, double tMax)> validColIntervals = colIntervals
                            .Where(inv => inv.tMax > 0.0 && inv.tMin < 1.0)
                            .OrderBy(inv => inv.tMin)
                            .ToList();

                        if (validColIntervals.Count == 0) continue;

                        // Gộp các khoảng cột đè hoặc chạm nhau
                        List<(double tMin, double tMax)> mergedCols = new List<(double tMin, double tMax)>();
                        foreach (var curr in validColIntervals)
                        {
                            if (mergedCols.Count == 0)
                            {
                                mergedCols.Add(curr);
                            }
                            else
                            {
                                var prev = mergedCols[mergedCols.Count - 1];
                                if (curr.tMin <= prev.tMax + 1e-4)
                                {
                                    mergedCols[mergedCols.Count - 1] = (prev.tMin, Math.Max(prev.tMax, curr.tMax));
                                }
                                else
                                {
                                    mergedCols.Add(curr);
                                }
                            }
                        }

                        // Tính các đoạn dầm nằm NGOÀI cột trong phạm vi [0.0, 1.0]
                        List<(double tMin, double tMax)> beamSegments = new List<(double tMin, double tMax)>();
                        double currentT = 0.0;

                        foreach (var colInt in mergedCols)
                        {
                            if (colInt.tMin > currentT + 1e-4)
                            {
                                double segStart = Math.Max(0.0, currentT);
                                double segEnd = Math.Min(1.0, colInt.tMin);
                                if ((segEnd - segStart) * totalLength > 0.1) // Dầm dài tối thiểu ~30mm
                                {
                                    beamSegments.Add((segStart, segEnd));
                                }
                            }
                            currentT = Math.Max(currentT, colInt.tMax);
                        }

                        if (currentT < 1.0 - 1e-4)
                        {
                            double segStart = Math.Max(0.0, currentT);
                            double segEnd = 1.0;
                            if ((segEnd - segStart) * totalLength > 0.1)
                            {
                                beamSegments.Add((segStart, segEnd));
                            }
                        }

                        if (beamSegments.Count == 0) continue;

                        Level level = doc.GetElement(originalBeam.LevelId) as Level;
                        FamilySymbol symbol = originalBeam.Symbol;

                        // Tạo các đoạn dầm từ mặt cột đến mặt cột
                        foreach (var seg in beamSegments)
                        {
                            try
                            {
                                XYZ pStart = beamCurve.Evaluate(seg.tMin, true);
                                XYZ pEnd = beamCurve.Evaluate(seg.tMax, true);

                                Line newLine = Line.CreateBound(pStart, pEnd);
                                FamilyInstance newBeam = doc.Create.NewFamilyInstance(newLine, symbol, level, StructuralType.Beam);
                                CopyParameters(originalBeam, newBeam);

                                // Ngăn tự động dãn/nối về tim cột
                                StructuralFramingUtils.DisallowJoinAtEnd(newBeam, 0);
                                StructuralFramingUtils.DisallowJoinAtEnd(newBeam, 1);

                                // Đặt lại LocationCurve để bảo đảm đầu dầm dính đúng vị trí mặt cột pStart, pEnd (tránh bị NewFamilyInstance auto-snap về tim)
                                LocationCurve newLocCurve = newBeam.Location as LocationCurve;
                                if (newLocCurve != null)
                                {
                                    newLocCurve.Curve = newLine;
                                }

                                totalCreated++;
                            }
                            catch { }
                        }

                        // Xóa dầm gốc
                        doc.Delete(originalBeam.Id);
                        beamsProcessed++;
                    }

                    tx.Commit();
                }

                if (beamsProcessed > 0)
                {
                    TaskDialog.Show("Kết quả", $"Đã xử lý {beamsProcessed} dầm, tạo thành {totalCreated} đoạn dầm tại mặt ngắt.");
                }
                else
                {
                    TaskDialog.Show("Thông báo", "Không tìm thấy dầm nào giao với phần tử cắt trong phạm vi cho phép.");
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>
        /// Xác định khoảng tham số [tMin, tMax] của dầm bị chiếm bởi phần tử cắt (cột hoặc dầm)
        /// </summary>
        private (double tMin, double tMax)? GetIntersectingFaceInterval(
            FamilyInstance intersectingElem, 
            Curve beamCurve, 
            XYZ startPoint, 
            XYZ endPoint, 
            XYZ dir, 
            double totalLength)
        {
            Location loc = intersectingElem.Location;
            XYZ refPt = XYZ.Zero;
            if (loc is LocationPoint lp) 
            {
                refPt = lp.Point;
            }
            else if (loc is LocationCurve lc) 
            {
                XYZ p1 = startPoint;
                XYZ p2 = endPoint;
                XYZ p3 = lc.Curve.GetEndPoint(0);
                XYZ p4 = lc.Curve.GetEndPoint(1);

                double x1 = p1.X, y1 = p1.Y;
                double x2 = p2.X, y2 = p2.Y;
                double x3 = p3.X, y3 = p3.Y;
                double x4 = p4.X, y4 = p4.Y;

                double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
                if (Math.Abs(denom) < 1e-6) return null; // Song song

                double px = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / denom;
                double py = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / denom;
                refPt = new XYZ(px, py, startPoint.Z);
            }

            XYZ refPtFlat = new XYZ(refPt.X, refPt.Y, startPoint.Z);
            IntersectionResult projection = beamCurve.Project(refPtFlat);
            if (projection == null || projection.Distance > 4.0) // Khoảng cách tới tim dầm
                return null;

            List<double> faceParams = new List<double>();
            BoundingBoxXYZ elemBBox = intersectingElem.get_BoundingBox(null);

            // 1. Giao cắt hình học Solid của phần tử với đường kéo dài tim dầm
            List<Solid> solids = GetElementSolids(intersectingElem);
            if (solids.Count > 0)
            {
                double zTest = startPoint.Z;
                if (elemBBox != null)
                {
                    zTest = Math.Max(elemBBox.Min.Z + 0.1, Math.Min(elemBBox.Max.Z - 0.1, startPoint.Z));
                }

                XYZ extStart = startPoint - dir * 20.0;
                XYZ extEnd = endPoint + dir * 20.0;
                Line testLine = Line.CreateBound(
                    new XYZ(extStart.X, extStart.Y, zTest), 
                    new XYZ(extEnd.X, extEnd.Y, zTest));

                foreach (Solid solid in solids)
                {
                    foreach (Face face in solid.Faces)
                    {
                        IntersectionResultArray results;
                        SetComparisonResult res = face.Intersect(testLine, out results);
                        if (res == SetComparisonResult.Overlap && results != null)
                        {
                            foreach (IntersectionResult ir in results)
                            {
                                XYZ pt = ir.XYZPoint;
                                double dirDotDir = dir.X * dir.X + dir.Y * dir.Y;
                                if (Math.Abs(dirDotDir) > 1e-6)
                                {
                                    double t = ((pt.X - startPoint.X) * dir.X + (pt.Y - startPoint.Y) * dir.Y) / (dirDotDir * totalLength);
                                    faceParams.Add(t);
                                }
                            }
                        }
                    }
                }
            }

            // 2. Dự phòng (Fallback) nếu Solid Intersect không thu được đủ 2 điểm mặt cắt
            if (faceParams.Count < 2)
            {
                double halfWidth = 0.5; // Mặc định 150mm (~0.5ft)
                
                // Cố gắng đọc tham số bề rộng từ phần tử hoặc Type
                Parameter bParam = intersectingElem.LookupParameter("b") 
                                ?? intersectingElem.LookupParameter("Width")
                                ?? intersectingElem.LookupParameter("bf");
                
                if (bParam != null && bParam.StorageType == StorageType.Double)
                {
                    halfWidth = bParam.AsDouble() / 2.0;
                }
                else if (intersectingElem.Symbol != null)
                {
                    bParam = intersectingElem.Symbol.LookupParameter("b") 
                          ?? intersectingElem.Symbol.LookupParameter("Width")
                          ?? intersectingElem.Symbol.LookupParameter("bf");
                          
                    if (bParam != null && bParam.StorageType == StorageType.Double)
                    {
                        halfWidth = bParam.AsDouble() / 2.0;
                    }
                    else 
                    {
                        // Thử dùng BoundingBox chỉ nếu nó là Cột (cột thường vuông hoặc chữ nhật, không bị dài như dầm)
                        if (intersectingElem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns) && elemBBox != null)
                        {
                            double dx = Math.Abs(elemBBox.Max.X - elemBBox.Min.X);
                            double dy = Math.Abs(elemBBox.Max.Y - elemBBox.Min.Y);
                            halfWidth = Math.Min(dx, dy) / 2.0; // Dùng Min thay vì Max để an toàn hơn
                        }
                    }
                }

                double dirDotDir = dir.X * dir.X + dir.Y * dir.Y;
                if (Math.Abs(dirDotDir) > 1e-6)
                {
                    double tCenter = ((refPt.X - startPoint.X) * dir.X + (refPt.Y - startPoint.Y) * dir.Y) / (dirDotDir * totalLength);
                    double tDelta = halfWidth / totalLength;
                    faceParams.Add(tCenter - tDelta);
                    faceParams.Add(tCenter + tDelta);
                }
            }

            if (faceParams.Count == 0) return null;

            double tMin = faceParams.Min();
            double tMax = faceParams.Max();
            return (tMin, tMax);
        }

        /// <summary>
        /// Lấy tất cả các khối Solid hình học từ phần tử
        /// </summary>
        private List<Solid> GetElementSolids(FamilyInstance elem)
        {
            List<Solid> solids = new List<Solid>();
            Options opt = new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true };
            GeometryElement geomElem = elem.get_Geometry(opt);
            if (geomElem == null) return solids;

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid && solid.Volume > 1e-6)
                {
                    solids.Add(solid);
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    if (instGeom != null)
                    {
                        foreach (GeometryObject instObj in instGeom)
                        {
                            if (instObj is Solid instSolid && instSolid.Volume > 1e-6)
                            {
                                solids.Add(instSolid);
                            }
                        }
                    }
                }
            }
            return solids;
        }

        /// <summary>
        /// Sao chép các tham số instance từ dầm nguồn sang dầm đích
        /// </summary>
        private void CopyParameters(FamilyInstance source, FamilyInstance target)
        {
            foreach (Parameter sourceParam in source.Parameters)
            {
                if (sourceParam.IsReadOnly || !sourceParam.HasValue) continue;

                // Bỏ qua tham số "Mark" vì nó nên là duy nhất
                if (sourceParam.Definition.Name == "Mark") continue;

                Parameter targetParam = target.get_Parameter(sourceParam.Definition);
                if (targetParam != null && !targetParam.IsReadOnly)
                {
                    try
                    {
                        switch (sourceParam.StorageType)
                        {
                            case StorageType.Double:
                                targetParam.Set(sourceParam.AsDouble());
                                break;
                            case StorageType.Integer:
                                targetParam.Set(sourceParam.AsInteger());
                                break;
                            case StorageType.String:
                                targetParam.Set(sourceParam.AsString());
                                break;
                            case StorageType.ElementId:
                                targetParam.Set(sourceParam.AsElementId());
                                break;
                        }
                    }
                    catch { /* Bỏ qua nếu có lỗi set tham số cụ thể */ }
                }
            }
        }
    }
}

