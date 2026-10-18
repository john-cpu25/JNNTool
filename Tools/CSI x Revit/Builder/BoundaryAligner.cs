using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public enum AlignDirection
    {
        AutoGrid,
        UpY,
        DownY,
        RightX,
        LeftX
    }

    public class AlignResult
    {
        public int MovedBeams { get; set; }
        public int MovedColumns { get; set; }
        public int AdjustedIntersectingBeams { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class BoundaryAligner
    {
        /// <summary>
        /// Aligns selected boundary beams and columns inward to match the grid line or specified offset.
        /// </summary>
        public static AlignResult AlignElements(
            Document doc,
            List<ElementId> elementIds,
            AlignDirection direction,
            double customOffsetFeet,
            bool autoExtendIntersecting)
        {
            var result = new AlignResult();
            if (elementIds == null || elementIds.Count == 0)
            {
                result.Message = "Chưa chọn đối tượng nào.";
                return result;
            }

            var elements = elementIds.Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
            var beams = new List<FamilyInstance>();
            var columns = new List<FamilyInstance>();

            foreach (var elem in elements)
            {
                if (elem is FamilyInstance fi)
                {
                    var cat = fi.Category;
                    if (cat != null)
                    {
                        var framingId = new ElementId(BuiltInCategory.OST_StructuralFraming);
                        var columnId = new ElementId(BuiltInCategory.OST_StructuralColumns);
                        if (cat.Id == framingId)
                        {
                            if (fi.Location is LocationCurve) beams.Add(fi);
                        }
                        else if (cat.Id == columnId)
                        {
                            if (fi.Location is LocationPoint) columns.Add(fi);
                        }
                    }
                }
            }

            if (beams.Count == 0 && columns.Count == 0)
            {
                result.Message = "Không tìm thấy Dầm hoặc Cột kết cấu hợp lệ trong danh sách chọn.";
                return result;
            }

            // Get all Grids in document for auto-alignment
            var allGrids = new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .Where(g => g.Curve is Line)
                .ToList();

            // Calculate bounding box center of the entire structure to determine "inward" direction
            XYZ structureCenter = CalculateStructureCenter(doc);

            using (var tx = new Transaction(doc, "JNN - Căn Lề Biên Dầm Cột"))
            {
                tx.Start();

                var movedElements = new HashSet<ElementId>();
                var beamMoveVectors = new Dictionary<ElementId, XYZ>();
                var colMoveVectors = new Dictionary<ElementId, XYZ>();

                // 1. Process Beams
                foreach (var beam in beams)
                {
                    if (movedElements.Contains(beam.Id)) continue;
                    if (!(beam.Location is LocationCurve locCurve) || !(locCurve.Curve is Line beamLine)) continue;

                    // Sử dụng BeamDimensionHelper hỗ trợ nhận diện bề rộng dầm lớn (400, 550, 600, 800 mm)
                    double beamWidthFeet = BeamDimensionHelper.GetBeamWidthFeet(beam);
                    double shiftDist = customOffsetFeet > 0 ? customOffsetFeet : (beamWidthFeet / 2.0);
                    if (shiftDist <= 0.001) shiftDist = UnitUtils.ConvertToInternalUnits(100.0, UnitTypeId.Millimeters);

                    XYZ moveVec = DetermineMoveVector(beamLine, direction, shiftDist, allGrids, structureCenter);
                    if (moveVec.IsAlmostEqualTo(XYZ.Zero)) continue;

                    ElementTransformUtils.MoveElement(doc, beam.Id, moveVec);
                    movedElements.Add(beam.Id);
                    beamMoveVectors[beam.Id] = moveVec;
                    result.MovedBeams++;
                }

                // 2. Process Columns
                foreach (var col in columns)
                {
                    if (movedElements.Contains(col.Id)) continue;
                    if (!(col.Location is LocationPoint locPt)) continue;

                    XYZ colPt = locPt.Point;
                    XYZ moveVec = XYZ.Zero;

                    // Lấy kích thước phủ bì thực tế của cột theo trục World X và Y (tự động xử lý b, h và góc xoay)
                    ColumnDimensionHelper.GetColumnExtentsFeet(col, out double colExtentXFeet, out double colExtentYFeet);

                    // Kiểm tra dầm biên nối vào cột (đo khoảng cách tới đường tim dầm BAN ĐẦU để không bị tuột với dầm rộng 400, 550, 600, 800 mm)
                    var nearbyBeamVec = beamMoveVectors.FirstOrDefault(kvp =>
                    {
                        var b = doc.GetElement(kvp.Key) as FamilyInstance;
                        if (b?.Location is LocationCurve lc && lc.Curve is Line bl)
                        {
                            // Lấy lại vị trí tim dầm ban đầu trước khi dời
                            XYZ origP0 = bl.GetEndPoint(0) - kvp.Value;
                            XYZ origP1 = bl.GetEndPoint(1) - kvp.Value;
                            Line origLine = Line.CreateBound(origP0, origP1);

                            double distLine = origLine.Distance(colPt);
                            double distP0 = origP0.DistanceTo(colPt);
                            double distP1 = origP1.DistanceTo(colPt);
                            double minDist = Math.Min(distLine, Math.Min(distP0, distP1));

                            // Dung sai an toàn 2.5 ft (~762 mm), bao trọn vẹn cả dầm 600-800 mm và cột
                            return minDist < 2.5;
                        }
                        return false;
                    });

                    XYZ moveDir;
                    if (nearbyBeamVec.Value != null && !nearbyBeamVec.Value.IsAlmostEqualTo(XYZ.Zero))
                    {
                        // Cột kế thừa HƯỚNG di chuyển của dầm biên nối vào nó (Unit Vector)
                        moveDir = nearbyBeamVec.Value.Normalize();
                    }
                    else
                    {
                        // Cột đứng độc lập: xác định hướng theo lựa chọn UI
                        moveDir = DetermineColumnDirection(colPt, direction, structureCenter);
                    }

                    if (!moveDir.IsAlmostEqualTo(XYZ.Zero))
                    {
                        double shiftDist;
                        if (customOffsetFeet > 0)
                        {
                            shiftDist = customOffsetFeet;
                        }
                        else
                        {
                            // Tự động: lấy nửa kích thước phủ bì của cột theo phương di chuyển (B/2 hoặc H/2)
                            bool isMovingAlongY = Math.Abs(moveDir.Y) >= Math.Abs(moveDir.X);
                            shiftDist = isMovingAlongY ? (colExtentYFeet / 2.0) : (colExtentXFeet / 2.0);
                        }

                        if (shiftDist <= 0.001)
                        {
                            shiftDist = UnitUtils.ConvertToInternalUnits(100.0, UnitTypeId.Millimeters);
                        }

                        moveVec = moveDir * shiftDist;
                    }

                    if (!moveVec.IsAlmostEqualTo(XYZ.Zero))
                    {
                        ElementTransformUtils.MoveElement(doc, col.Id, moveVec);
                        movedElements.Add(col.Id);
                        colMoveVectors[col.Id] = moveVec;
                        result.MovedColumns++;
                    }
                }

                // 3. Auto-extend/trim intersecting perpendicular beams (avoid disconnected joints)
                if (autoExtendIntersecting && (beamMoveVectors.Count > 0 || colMoveVectors.Count > 0))
                {
                    result.AdjustedIntersectingBeams = AdjustIntersectingBeams(doc, beamMoveVectors, colMoveVectors, movedElements);
                }

                tx.Commit();
            }

            result.Message = $"Đã căn lề thành công: {result.MovedBeams} dầm, {result.MovedColumns} cột" +
                             (result.AdjustedIntersectingBeams > 0 ? $", đồng bộ {result.AdjustedIntersectingBeams} dầm nối." : ".");
            return result;
        }

        private static XYZ DetermineMoveVector(
            Line beamLine,
            AlignDirection direction,
            double shiftDist,
            List<Grid> grids,
            XYZ structureCenter)
        {
            XYZ p0 = beamLine.GetEndPoint(0);
            XYZ p1 = beamLine.GetEndPoint(1);
            XYZ beamDir = (p1 - p0).Normalize();

            bool isAlongX = Math.Abs(beamDir.X) > 0.8;
            bool isAlongY = Math.Abs(beamDir.Y) > 0.8;

            switch (direction)
            {
                case AlignDirection.UpY:
                    return new XYZ(0, shiftDist, 0);
                case AlignDirection.DownY:
                    return new XYZ(0, -shiftDist, 0);
                case AlignDirection.RightX:
                    return new XYZ(shiftDist, 0, 0);
                case AlignDirection.LeftX:
                    return new XYZ(-shiftDist, 0, 0);

                case AlignDirection.AutoGrid:
                default:
                    // Auto determine inward direction based on beam orientation and structure center
                    if (isAlongX)
                    {
                        // Beam runs along X -> needs shift along Y towards structure center
                        double midY = (p0.Y + p1.Y) / 2.0;
                        double signY = (structureCenter.Y >= midY) ? 1.0 : -1.0;
                        return new XYZ(0, signY * shiftDist, 0);
                    }
                    else if (isAlongY)
                    {
                        // Beam runs along Y -> needs shift along X towards structure center
                        double midX = (p0.X + p1.X) / 2.0;
                        double signX = (structureCenter.X >= midX) ? 1.0 : -1.0;
                        return new XYZ(signX * shiftDist, 0, 0);
                    }
                    else
                    {
                        // Slanted beam: normal vector towards structure center
                        XYZ normal = new XYZ(-beamDir.Y, beamDir.X, 0).Normalize();
                        XYZ mid = (p0 + p1) / 2.0;
                        if ((mid + normal).DistanceTo(structureCenter) > (mid - normal).DistanceTo(structureCenter))
                        {
                            normal = -normal;
                        }
                        return normal * shiftDist;
                    }
            }
        }

        private static XYZ DetermineColumnDirection(
            XYZ colPt,
            AlignDirection direction,
            XYZ structureCenter)
        {
            switch (direction)
            {
                case AlignDirection.UpY:
                    return new XYZ(0, 1, 0);
                case AlignDirection.DownY:
                    return new XYZ(0, -1, 0);
                case AlignDirection.RightX:
                    return new XYZ(1, 0, 0);
                case AlignDirection.LeftX:
                    return new XYZ(-1, 0, 0);

                case AlignDirection.AutoGrid:
                default:
                    double dx = structureCenter.X - colPt.X;
                    double dy = structureCenter.Y - colPt.Y;
                    if (Math.Abs(dy) >= Math.Abs(dx))
                    {
                        double signY = Math.Sign(dy);
                        if (Math.Abs(signY) < 1e-4) signY = 1.0;
                        return new XYZ(0, signY, 0);
                    }
                    else
                    {
                        double signX = Math.Sign(dx);
                        if (Math.Abs(signX) < 1e-4) signX = 1.0;
                        return new XYZ(signX, 0, 0);
                    }
            }
        }

        private static int AdjustIntersectingBeams(
            Document doc,
            Dictionary<ElementId, XYZ> movedBeams,
            Dictionary<ElementId, XYZ> movedColumns,
            HashSet<ElementId> movedElements)
        {
            int adjustedCount = 0;
            var allBeams = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(b => !movedElements.Contains(b.Id) && b.Location is LocationCurve)
                .ToList();

            foreach (var b in allBeams)
            {
                if (!(b.Location is LocationCurve lc) || !(lc.Curve is Line bl)) continue;

                XYZ startPt = bl.GetEndPoint(0);
                XYZ endPt = bl.GetEndPoint(1);
                XYZ bDir = (endPt - startPt).Normalize();
                bool modified = false;

                // 1. Kiểm tra kết nối với các Dầm biên bị dời
                foreach (var kvp in movedBeams)
                {
                    var movedBeam = doc.GetElement(kvp.Key) as FamilyInstance;
                    if (movedBeam?.Location is LocationCurve mlc && mlc.Curve is Line mLine)
                    {
                        XYZ origP0 = mLine.GetEndPoint(0) - kvp.Value;
                        XYZ origP1 = mLine.GetEndPoint(1) - kvp.Value;
                        Line origLine = Line.CreateBound(origP0, origP1);
                        XYZ mDir = (origP1 - origP0).Normalize();

                        // Bỏ qua các dầm chạy song song với dầm biên (tránh làm xiên dầm nối tiếp cùng trục)
                        if (Math.Abs(bDir.DotProduct(mDir)) > 0.85) continue;

                        // Dung sai kết nối động theo bề rộng thực tế của dầm biên:
                        // Dầm 400mm -> tol ~ 340mm (1.11 ft), Dầm 600mm -> tol ~ 510mm (1.67 ft)
                        double movedWidthFt = BeamDimensionHelper.GetBeamWidthFeet(movedBeam);
                        double connectTol = Math.Max(0.8, movedWidthFt * 0.85);

                        // Check if start point was connected to the moved beam
                        double distStart = origLine.Distance(startPt);
                        if (distStart < connectTol && Math.Min(origP0.DistanceTo(startPt), origP1.DistanceTo(startPt)) < (origP0.DistanceTo(origP1) + connectTol))
                        {
                            startPt = startPt + kvp.Value;
                            modified = true;
                        }
                        // Check if end point was connected to the moved beam
                        double distEnd = origLine.Distance(endPt);
                        if (distEnd < connectTol && Math.Min(origP0.DistanceTo(endPt), origP1.DistanceTo(endPt)) < (origP0.DistanceTo(origP1) + connectTol))
                        {
                            endPt = endPt + kvp.Value;
                            modified = true;
                        }
                    }
                }

                // 2. Kiểm tra kết nối với các Cột biên bị dời (chống hở nút giữa dầm ngang và cột biên)
                foreach (var kvp in movedColumns)
                {
                    var movedCol = doc.GetElement(kvp.Key) as FamilyInstance;
                    if (movedCol?.Location is LocationPoint lp)
                    {
                        XYZ origColPt = lp.Point - kvp.Value;

                        // Nếu đầu start ban đầu cắm vào cột này
                        if (origColPt.DistanceTo(startPt) < 1.5)
                        {
                            startPt = startPt + kvp.Value;
                            modified = true;
                        }
                        // Nếu đầu end ban đầu cắm vào cột này
                        if (origColPt.DistanceTo(endPt) < 1.5)
                        {
                            endPt = endPt + kvp.Value;
                            modified = true;
                        }
                    }
                }

                if (modified && startPt.DistanceTo(endPt) > 0.1)
                {
                    try
                    {
                        lc.Curve = Line.CreateBound(startPt, endPt);
                        adjustedCount++;
                    }
                    catch { }
                }
            }

            return adjustedCount;
        }



        private static XYZ CalculateStructureCenter(Document doc)
        {
            var allCols = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(c => c.Location is LocationPoint)
                .Select(c => (c.Location as LocationPoint).Point)
                .ToList();

            if (allCols.Count > 0)
            {
                double avgX = allCols.Average(p => p.X);
                double avgY = allCols.Average(p => p.Y);
                double avgZ = allCols.Average(p => p.Z);
                return new XYZ(avgX, avgY, avgZ);
            }

            return XYZ.Zero;
        }
    }
}
