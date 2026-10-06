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

                // 1. Process Beams
                foreach (var beam in beams)
                {
                    if (movedElements.Contains(beam.Id)) continue;
                    if (!(beam.Location is LocationCurve locCurve) || !(locCurve.Curve is Line beamLine)) continue;

                    double beamWidthFeet = GetBeamWidthFeet(beam);
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

                    // If a moved beam connected to this column, inherit beam's move vector
                    var nearbyBeamVec = beamMoveVectors.FirstOrDefault(kvp =>
                    {
                        var b = doc.GetElement(kvp.Key) as FamilyInstance;
                        if (b?.Location is LocationCurve lc && lc.Curve is Line bl)
                        {
                            return bl.Distance(colPt) < 1.0; // Near beam end or on beam line
                        }
                        return false;
                    });

                    if (nearbyBeamVec.Value != null && !nearbyBeamVec.Value.IsAlmostEqualTo(XYZ.Zero))
                    {
                        moveVec = nearbyBeamVec.Value;
                    }
                    else
                    {
                        // Calculate move vector directly for column
                        double colExtentFeet = GetColumnThicknessFeet(col);
                        double shiftDist = customOffsetFeet > 0 ? customOffsetFeet : (colExtentFeet / 2.0);
                        if (shiftDist <= 0.001) shiftDist = UnitUtils.ConvertToInternalUnits(100.0, UnitTypeId.Millimeters);

                        moveVec = DetermineColumnMoveVector(colPt, direction, shiftDist, allGrids, structureCenter);
                    }

                    if (!moveVec.IsAlmostEqualTo(XYZ.Zero))
                    {
                        ElementTransformUtils.MoveElement(doc, col.Id, moveVec);
                        movedElements.Add(col.Id);
                        result.MovedColumns++;
                    }
                }

                // 3. Auto-extend/trim intersecting perpendicular beams (avoid disconnected joints)
                if (autoExtendIntersecting && beamMoveVectors.Count > 0)
                {
                    result.AdjustedIntersectingBeams = AdjustIntersectingBeams(doc, beamMoveVectors, movedElements);
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

        private static XYZ DetermineColumnMoveVector(
            XYZ colPt,
            AlignDirection direction,
            double shiftDist,
            List<Grid> grids,
            XYZ structureCenter)
        {
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
                    double dx = structureCenter.X - colPt.X;
                    double dy = structureCenter.Y - colPt.Y;
                    if (Math.Abs(dy) > Math.Abs(dx))
                    {
                        return new XYZ(0, Math.Sign(dy) * shiftDist, 0);
                    }
                    else
                    {
                        return new XYZ(Math.Sign(dx) * shiftDist, 0, 0);
                    }
            }
        }

        private static int AdjustIntersectingBeams(
            Document doc,
            Dictionary<ElementId, XYZ> movedBeams,
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
                bool modified = false;

                foreach (var kvp in movedBeams)
                {
                    var movedBeam = doc.GetElement(kvp.Key) as FamilyInstance;
                    if (movedBeam?.Location is LocationCurve mlc && mlc.Curve is Line mLine)
                    {
                        XYZ originalLinePt0 = mLine.GetEndPoint(0) - kvp.Value;
                        XYZ originalLinePt1 = mLine.GetEndPoint(1) - kvp.Value;
                        Line originalLine = Line.CreateBound(originalLinePt0, originalLinePt1);

                        // Check if start point was connected to the moved beam
                        if (originalLine.Distance(startPt) < 0.2)
                        {
                            startPt = startPt + kvp.Value;
                            modified = true;
                        }
                        // Check if end point was connected to the moved beam
                        if (originalLine.Distance(endPt) < 0.2)
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

        private static double GetBeamWidthFeet(FamilyInstance beam)
        {
            var symbol = beam.Symbol;
            var pB = beam.LookupParameter("b") ?? symbol?.LookupParameter("b")
                  ?? beam.LookupParameter("Width") ?? symbol?.LookupParameter("Width")
                  ?? beam.LookupParameter("B") ?? symbol?.LookupParameter("B")
                  ?? beam.LookupParameter("Chiều rộng") ?? symbol?.LookupParameter("Chiều rộng")
                  ?? beam.LookupParameter("b (Width)") ?? symbol?.LookupParameter("b (Width)");

            if (pB != null && pB.AsDouble() > 0) return pB.AsDouble();
            return UnitUtils.ConvertToInternalUnits(200.0, UnitTypeId.Millimeters);
        }

        private static double GetColumnThicknessFeet(FamilyInstance col)
        {
            var symbol = col.Symbol;
            var pB = col.LookupParameter("b") ?? symbol?.LookupParameter("b")
                  ?? col.LookupParameter("Width") ?? symbol?.LookupParameter("Width")
                  ?? col.LookupParameter("B") ?? symbol?.LookupParameter("B")
                  ?? col.LookupParameter("Chiều rộng") ?? symbol?.LookupParameter("Chiều rộng");

            if (pB != null && pB.AsDouble() > 0) return pB.AsDouble();
            return UnitUtils.ConvertToInternalUnits(200.0, UnitTypeId.Millimeters);
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
