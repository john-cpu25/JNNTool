using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace JNNTool
{
    public class RebarBeamCreator
    {
        private Document _doc;
        private List<Rebar> _createdRebars = new List<Rebar>();

        public RebarBeamCreator(Document doc)
        {
            _doc = doc;
        }

        public void CreateRebars(List<Element> selectedBeams, RebarBeamViewModel config, View activeView)
        {
            if (selectedBeams == null || selectedBeams.Count == 0) return;

            // Sort beams by geometry so we can figure out continuity
            List<Element> beams = SortBeamsByPosition(selectedBeams);

            Curve totalCurve = GetContinuousCurve(beams);
            if (totalCurve == null) return;

            FamilyInstance firstBeam = beams[0] as FamilyInstance;
            if (firstBeam == null) return;

            double width = GetBeamWidth(firstBeam);
            double height = GetBeamHeight(firstBeam);
            double cover = GetBeamCover(firstBeam);

            _createdRebars.Clear();

            using (Transaction t = new Transaction(_doc, "Create Beam Rebars"))
            {
                t.Start();

                // Create Top Rebars
                if (config.SelectedTopRebarType != null && config.TopRebarQuantity > 0)
                {
                    CreateLongitudinalRebars(beams, totalCurve, config.SelectedTopRebarType, config.TopRebarQuantity, width, height, cover, true, config.TopAnchorageD, config.SelectedTopHookType);
                }

                // Create Bottom Rebars
                if (config.SelectedBottomRebarType != null && config.BottomRebarQuantity > 0)
                {
                    CreateLongitudinalRebars(beams, totalCurve, config.SelectedBottomRebarType, config.BottomRebarQuantity, width, height, cover, false, config.BottomAnchorageD, config.SelectedBottomHookType);
                }

                // Create Stirrups for each span
                if (config.SelectedStirrupRebarType != null)
                {
                    foreach (var beam in beams)
                    {
                        CreateStirrups(beam as FamilyInstance, config.SelectedStirrupRebarType, config.SelectedStirrupHookType, width, height, cover, config.StirrupSupportSpacing, config.StirrupMidSpacing);
                    }
                }

                // Create Additional Rebars
                var familyBeams = beams.Cast<FamilyInstance>().ToList();
                if (config.EnableTopAdd && config.SelectedTopAddRebarType != null && config.TopAddQuantity > 0)
                {
                    CreateAdditionalRebars(familyBeams, totalCurve, config.SelectedTopAddRebarType, config.SelectedTopHookType, config.TopAddQuantity, width, height, cover, config.TopAnchorageD, true);
                }
                
                if (config.EnableBottomAdd && config.SelectedBottomAddRebarType != null && config.BottomAddQuantity > 0)
                {
                    CreateAdditionalRebars(familyBeams, totalCurve, config.SelectedBottomAddRebarType, null, config.BottomAddQuantity, width, height, cover, 0, false);
                }

                // Set solid in view for 3D views
                if (activeView is View3D view3D)
                {
                    foreach (var rebar in _createdRebars)
                    {
                        rebar.SetUnobscuredInView(view3D, true);
                    }
                }

                t.Commit();
            }
        }

        private List<Element> SortBeamsByPosition(List<Element> beams)
        {
            return beams.OrderBy(b => 
            {
                LocationCurve loc = b.Location as LocationCurve;
                return loc != null ? loc.Curve.GetEndPoint(0).X + loc.Curve.GetEndPoint(0).Y : 0;
            }).ToList();
        }

        private Curve GetContinuousCurve(List<Element> beams)
        {
            List<XYZ> points = new List<XYZ>();
            foreach (var beam in beams)
            {
                LocationCurve loc = beam.Location as LocationCurve;
                if (loc != null)
                {
                    points.Add(loc.Curve.GetEndPoint(0));
                    points.Add(loc.Curve.GetEndPoint(1));
                }
            }

            if (points.Count < 2) return null;

            double maxDist = -1;
            XYZ bestStart = points[0];
            XYZ bestEnd = points[1];

            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    double d = points[i].DistanceTo(points[j]);
                    if (d > maxDist)
                    {
                        maxDist = d;
                        bestStart = points[i];
                        bestEnd = points[j];
                    }
                }
            }

            return Line.CreateBound(bestStart, bestEnd);
        }

        private double GetBeamWidth(FamilyInstance beam)
        {
            Parameter param = beam.Symbol.LookupParameter("b");
            if (param == null) param = beam.Symbol.LookupParameter("Width");
            return param != null ? param.AsDouble() : 1.0; // fallback to 1 ft if not found
        }

        private double GetBeamHeight(FamilyInstance beam)
        {
            Parameter param = beam.Symbol.LookupParameter("h");
            if (param == null) param = beam.Symbol.LookupParameter("Height");
            return param != null ? param.AsDouble() : 2.0;
        }

        private double GetBeamCover(FamilyInstance beam)
        {
            Parameter coverParam = beam.get_Parameter(BuiltInParameter.CLEAR_COVER_OTHER);
            if (coverParam != null && coverParam.AsElementId() != ElementId.InvalidElementId)
            {
                RebarCoverType coverType = _doc.GetElement(coverParam.AsElementId()) as RebarCoverType;
                if (coverType != null) return coverType.CoverDistance;
            }
            return 30.0 / 304.8; // default 30mm
        }

        private void CreateLongitudinalRebars(List<Element> beams, Curve totalCurve, RebarBarType barType, int quantity, double width, double height, double cover, bool isTop, double anchorageD, RebarHookType hookType)
        {
            Line line = totalCurve as Line;
            if (line == null || beams == null || beams.Count == 0) return;

            FamilyInstance firstBeam = beams[0] as FamilyInstance;
            FamilyInstance lastBeam = beams[beams.Count - 1] as FamilyInstance;
            FamilyInstance hostBeam = firstBeam;

            XYZ dir = line.Direction;
            XYZ up = XYZ.BasisZ;
            XYZ right = dir.CrossProduct(up).Normalize();

            double barDiameter = barType.BarModelDiameter;
            
            // Assuming totalCurve (LocationCurve) is at the TOP CENTER of the beam
            double zOffset = -cover - (barDiameter / 2.0);
            if (!isTop)
            {
                zOffset = -height + cover + (barDiameter / 2.0);
            }

            // Transverse spacing
            double startYOffset = (width / 2.0) - cover - (barDiameter / 2.0);
            double spacingY = quantity > 1 ? (startYOffset * 2) / (quantity - 1) : 0;

            double anchorageLength = anchorageD * barDiameter;
            
            GetBeamGeometryExtents(firstBeam, line, out double startCutback, out double dummy1);
            GetBeamGeometryExtents(lastBeam, line, out double dummy2, out double endCutback);
            
            double innerStartT = startCutback;
            double innerEndT = line.Length - endCutback;
            
            // Find column outer bounds to prevent poking out
            double maxStartExtend = GetColumnOuterFaceDistance(firstBeam, line.GetEndPoint(0), dir, true);
            double maxEndExtend = GetColumnOuterFaceDistance(lastBeam, line.GetEndPoint(1), dir, false);
            
            // Bound it by the column's outer face plus cover
            double startBoundT = maxStartExtend + cover;
            double endBoundT = line.Length + maxEndExtend - cover;
            
            if (startBoundT >= endBoundT)
            {
                startBoundT = 0;
                endBoundT = line.Length;
            }

            XYZ startPt = line.GetEndPoint(0) + dir * startBoundT;
            XYZ endPt = line.GetEndPoint(0) + dir * endBoundT;
            
            double achievedStartStraight = innerStartT - startBoundT;
            double achievedEndStraight = endBoundT - innerEndT;
            
            double legStartLength = 0;
            double legEndLength = 0;
            
            if (hookType != null)
            {
                if (achievedStartStraight < anchorageLength)
                {
                    legStartLength = anchorageLength - achievedStartStraight;
                    if (legStartLength > 0) legStartLength = Math.Max(legStartLength, 10 * barDiameter);
                }
                if (achievedEndStraight < anchorageLength)
                {
                    legEndLength = anchorageLength - achievedEndStraight;
                    if (legEndLength > 0) legEndLength = Math.Max(legEndLength, 10 * barDiameter);
                }
            }
            
            XYZ legVector = isTop ? -up : up;

            for (int i = 0; i < quantity; i++)
            {
                double currentYOffset = quantity == 1 ? 0 : startYOffset - (i * spacingY);
                XYZ offsetVector = (up * zOffset) + (right * currentYOffset);
                Transform t = Transform.CreateTranslation(offsetVector);
                
                List<Curve> curves = new List<Curve>();
                
                if (legStartLength > 0)
                {
                    XYZ p1 = startPt + legVector * legStartLength;
                    curves.Add(Line.CreateBound(p1, startPt).CreateTransformed(t));
                }
                
                curves.Add(Line.CreateBound(startPt, endPt).CreateTransformed(t));
                
                if (legEndLength > 0)
                {
                    XYZ p2 = endPt + legVector * legEndLength;
                    curves.Add(Line.CreateBound(endPt, p2).CreateTransformed(t));
                }
                
                // Normal is 'right' because curve is in XZ plane, so normal must be Y axis.
                XYZ rebarNormal = right;

                try
                {
                    Rebar rebar = Rebar.CreateFromCurves(_doc, RebarStyle.Standard, barType, null, null,
                        hostBeam, rebarNormal, curves, RebarHookOrientation.Left, RebarHookOrientation.Left,
                        true, true);
                        
                    _createdRebars.Add(rebar);
                }
                catch (Exception)
                {
                    // Ignore rebar creation failures to avoid crashing the whole transaction
                }
            }
        }

        private double GetColumnOuterFaceDistance(FamilyInstance beam, XYZ point, XYZ dir, bool isStart)
        {
            double s = 2.0; // 2 ft bounding box to find column
            Outline outline = new Outline(point - new XYZ(s, s, s), point + new XYZ(s, s, s));
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
            
            var columns = new FilteredElementCollector(beam.Document)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WherePasses(filter)
                .ToElements();
                
            double maxDist = 0;
            foreach (var col in columns)
            {
                Options opt = new Options { DetailLevel = ViewDetailLevel.Coarse };
                var geom = col.get_Geometry(opt);
                if (geom != null)
                {
                    foreach (GeometryObject obj in geom)
                    {
                        if (obj is GeometryInstance inst)
                        {
                            foreach (GeometryObject instObj in inst.GetInstanceGeometry())
                            {
                                if (instObj is Solid solid && solid.Faces.Size > 0)
                                {
                                    foreach (Face face in solid.Faces)
                                    {
                                        Mesh mesh = face.Triangulate();
                                        foreach (XYZ pt in mesh.Vertices)
                                        {
                                            double t = (pt - point).DotProduct(dir);
                                            if (isStart)
                                            {
                                                if (t < maxDist) maxDist = t; // Will be negative
                                            }
                                            else
                                            {
                                                if (t > maxDist) maxDist = t; // Will be positive
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return maxDist;
        }

        private void GetBeamGeometryExtents(FamilyInstance beam, Line locationLine, out double startCutback, out double endCutback)
        {
            startCutback = 0;
            endCutback = 0;
            XYZ dir = locationLine.Direction;
            XYZ start = locationLine.GetEndPoint(0);
            
            Options opt = new Options();
            opt.DetailLevel = ViewDetailLevel.Coarse;
            GeometryElement geomElem = beam.get_Geometry(opt);
            
            if (geomElem == null) return;

            double minT = double.MaxValue;
            double maxT = double.MinValue;
            
            bool found = false;
            foreach (GeometryObject geomObj in geomElem)
            {
                GeometryInstance geomInst = geomObj as GeometryInstance;
                if (geomInst != null)
                {
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    foreach (GeometryObject obj in instGeom)
                    {
                        Solid solid = obj as Solid;
                        if (solid != null && solid.Faces.Size > 0)
                        {
                            foreach (Face face in solid.Faces)
                            {
                                Mesh mesh = face.Triangulate();
                                foreach (XYZ pt in mesh.Vertices)
                                {
                                    double t = (pt - start).DotProduct(dir);
                                    if (t < minT) minT = t;
                                    if (t > maxT) maxT = t;
                                    found = true;
                                }
                            }
                        }
                    }
                }
                else if (geomObj is Solid solid && solid.Faces.Size > 0)
                {
                    foreach (Face face in solid.Faces)
                    {
                        Mesh mesh = face.Triangulate();
                        foreach (XYZ pt in mesh.Vertices)
                        {
                            double t = (pt - start).DotProduct(dir);
                            if (t < minT) minT = t;
                            if (t > maxT) maxT = t;
                            found = true;
                        }
                    }
                }
            }
            
            if (found)
            {
                startCutback = minT;
                endCutback = locationLine.Length - maxT;
            }
        }

        private void CreateStirrups(FamilyInstance beam, RebarBarType barType, RebarHookType stirrupHook, double width, double height, double cover, double supportSpacingMm, double midSpacingMm)
        {
            LocationCurve loc = beam.Location as LocationCurve;
            if (loc == null) return;

            Line line = loc.Curve as Line;
            if (line == null) return;
            
            XYZ dir = line.Direction;
            double spanLength = line.Length;
            XYZ start = line.GetEndPoint(0);
            
            double stirrupWidth = width - 2 * cover;
            double stirrupHeight = height - 2 * cover;

            // Assuming start is at the TOP CENTER of the beam
            // Center of the stirrup shape is half the height down
            XYZ shapeCenterStart = start - XYZ.BasisZ * (height / 2.0);

            List<Curve> profile = new List<Curve>();
            XYZ right = dir.CrossProduct(XYZ.BasisZ).Normalize();
            XYZ up = XYZ.BasisZ;
            XYZ p1 = shapeCenterStart - right * (stirrupWidth / 2.0) + up * (stirrupHeight / 2.0);
            XYZ p2 = shapeCenterStart + right * (stirrupWidth / 2.0) + up * (stirrupHeight / 2.0);
            XYZ p3 = shapeCenterStart + right * (stirrupWidth / 2.0) - up * (stirrupHeight / 2.0);
            XYZ p4 = shapeCenterStart - right * (stirrupWidth / 2.0) - up * (stirrupHeight / 2.0);

            profile.Add(Line.CreateBound(p1, p2));
            profile.Add(Line.CreateBound(p2, p3));
            profile.Add(Line.CreateBound(p3, p4));
            profile.Add(Line.CreateBound(p4, p1));

            double supportSpacing = supportSpacingMm / 304.8;
            double midSpacing = midSpacingMm / 304.8;
            double offset50 = 50.0 / 304.8;

            List<Tuple<double, double>> spans = GetClearSpans(new List<FamilyInstance> { beam }, line);

            foreach (var span in spans)
            {
                double physicalStart = span.Item1;
                double physicalEnd = span.Item2;
                double clearSpan = physicalEnd - physicalStart;
                
                if (clearSpan < 2.0 * offset50) continue; // Too short to place stirrups
                
                double l0_4 = clearSpan / 4.0;
                
                // Zone 1
                double z1Start = physicalStart + offset50;
                double z1Length = l0_4 - offset50;
                if (z1Length < 0) z1Length = 0;
                int q1 = (int)(z1Length / supportSpacing) + 1;
                
                CreateStirrupZone(beam, barType, stirrupHook, profile, dir, supportSpacing, z1Start, q1, true);
                
                double z1ActualEnd = z1Start + (q1 - 1) * supportSpacing;
                
                // Zone 3
                double z3End = physicalEnd - offset50;
                double z3Length = l0_4 - offset50;
                if (z3Length < 0) z3Length = 0;
                int q3 = (int)(z3Length / supportSpacing) + 1;
                
                CreateStirrupZone(beam, barType, stirrupHook, profile, dir, supportSpacing, z3End, q3, false);
                
                double z3ActualStart = z3End - (q3 - 1) * supportSpacing;
                
                // Zone 2
                double z2Start = z1ActualEnd + midSpacing;
                double z2End = z3ActualStart - midSpacing;
                
                if (z2End >= z2Start - 0.01)
                {
                    int q2 = (int)((z2End - z2Start) / midSpacing) + 1;
                    CreateStirrupZone(beam, barType, stirrupHook, profile, dir, midSpacing, z2Start, q2, true);
                }
            }
        }

        private void CreateStirrupZone(FamilyInstance beam, RebarBarType barType, RebarHookType stirrupHook, List<Curve> profile, XYZ dir, double spacing, double anchorOffset, int quantity, bool layoutForward)
        {
            if (quantity <= 0) return;

            Transform tr = Transform.CreateTranslation(dir * anchorOffset);
            List<Curve> movedProfile = profile.Select(c => c.CreateTransformed(tr)).ToList();

            Rebar stirrupSet = Rebar.CreateFromCurves(_doc, RebarStyle.StirrupTie, barType, stirrupHook, stirrupHook,
                beam, dir, movedProfile, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);

            RebarShapeDrivenAccessor accessor = stirrupSet.GetShapeDrivenAccessor();
            accessor.SetLayoutAsNumberWithSpacing(quantity, spacing, layoutForward, true, true);
            
            _createdRebars.Add(stirrupSet);
        }

        private List<Tuple<double, double>> GetClearSpans(List<FamilyInstance> beams, Line locationLine)
        {
            List<Tuple<double, double>> colIntervals = new List<Tuple<double, double>>();
            XYZ start = locationLine.GetEndPoint(0);
            XYZ dir = locationLine.Direction;
            double length = locationLine.Length;
            
            GetBeamGeometryExtents(beams[0], locationLine, out double beamStartCutback, out double dummy1);
            GetBeamGeometryExtents(beams[beams.Count - 1], locationLine, out double dummy2, out double beamEndCutback);
            double beamMinT = beamStartCutback;
            double beamMaxT = length - beamEndCutback;
            
            BoundingBoxXYZ firstBbox = beams[0].get_BoundingBox(null);
            if (firstBbox == null) return new List<Tuple<double, double>> { new Tuple<double, double>(beamMinT, beamMaxT) };
            
            XYZ min = firstBbox.Min;
            XYZ max = firstBbox.Max;
            
            foreach (var b in beams)
            {
                BoundingBoxXYZ bx = b.get_BoundingBox(null);
                if (bx != null)
                {
                    min = new XYZ(Math.Min(min.X, bx.Min.X), Math.Min(min.Y, bx.Min.Y), Math.Min(min.Z, bx.Min.Z));
                    max = new XYZ(Math.Max(max.X, bx.Max.X), Math.Max(max.Y, bx.Max.Y), Math.Max(max.Z, bx.Max.Z));
                }
            }
            
            Outline outline = new Outline(min, max);
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
            
            var columns = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WherePasses(filter)
                .ToElements();
                
            foreach (var col in columns)
            {
                Options opt = new Options { DetailLevel = ViewDetailLevel.Coarse };
                var geom = col.get_Geometry(opt);
                if (geom == null) continue;
                
                double cMinT = double.MaxValue;
                double cMaxT = double.MinValue;
                bool found = false;
                
                foreach (GeometryObject geomObj in geom)
                {
                    if (geomObj is GeometryInstance geomInst)
                    {
                        foreach (GeometryObject instObj in geomInst.GetInstanceGeometry())
                        {
                            if (instObj is Solid solid && solid.Faces.Size > 0)
                            {
                                foreach (Face face in solid.Faces)
                                {
                                    Mesh mesh = face.Triangulate();
                                    foreach (XYZ pt in mesh.Vertices)
                                    {
                                        double t = (pt - start).DotProduct(dir);
                                        if (t < cMinT) cMinT = t;
                                        if (t > cMaxT) cMaxT = t;
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (geomObj is Solid solid && solid.Faces.Size > 0)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            Mesh mesh = face.Triangulate();
                            foreach (XYZ pt in mesh.Vertices)
                            {
                                double t = (pt - start).DotProduct(dir);
                                if (t < cMinT) cMinT = t;
                                if (t > cMaxT) cMaxT = t;
                                found = true;
                            }
                        }
                    }
                }
                
                if (found)
                {
                    if (cMaxT <= beamMinT || cMinT >= beamMaxT) continue;
                    if (cMinT < beamMinT) cMinT = beamMinT;
                    if (cMaxT > beamMaxT) cMaxT = beamMaxT;
                    colIntervals.Add(new Tuple<double, double>(cMinT, cMaxT));
                }
            }
            
            if (colIntervals.Count == 0)
            {
                return new List<Tuple<double, double>> { new Tuple<double, double>(beamMinT, beamMaxT) };
            }
            
            colIntervals = colIntervals.OrderBy(x => x.Item1).ToList();
            List<Tuple<double, double>> merged = new List<Tuple<double, double>>();
            merged.Add(colIntervals[0]);
            
            for (int i = 1; i < colIntervals.Count; i++)
            {
                var last = merged[merged.Count - 1];
                var current = colIntervals[i];
                if (current.Item1 <= last.Item2)
                {
                    merged[merged.Count - 1] = new Tuple<double, double>(last.Item1, Math.Max(last.Item2, current.Item2));
                }
                else
                {
                    merged.Add(current);
                }
            }
            
            List<Tuple<double, double>> clearSpans = new List<Tuple<double, double>>();
            double currentT = beamMinT;
            
            foreach (var interval in merged)
            {
                if (interval.Item1 > currentT + 0.01)
                {
                    clearSpans.Add(new Tuple<double, double>(currentT, interval.Item1));
                }
                currentT = Math.Max(currentT, interval.Item2);
            }
            
            if (currentT < beamMaxT - 0.01)
            {
                clearSpans.Add(new Tuple<double, double>(currentT, beamMaxT));
            }
            
            return clearSpans;
        }

        private void CreateAdditionalRebars(List<FamilyInstance> beams, Curve totalCurve, RebarBarType barType, RebarHookType hookType, int quantity, double width, double height, double cover, double anchorageD, bool isTop)
        {
            Line line = totalCurve as Line;
            if (line == null || beams == null || beams.Count == 0) return;
            
            FamilyInstance beam = beams[0]; // Host beam
            
            XYZ dir = line.Direction;
            XYZ start = line.GetEndPoint(0);
            
            double barDiameter = barType.BarModelDiameter;
            XYZ up = XYZ.BasisZ;
            XYZ right = dir.CrossProduct(up).Normalize();
            
            double spacing25mm = 25.0 / 304.8;
            double zOffsetFromFace = cover + barDiameter * 1.5 + spacing25mm;
            double zOffset = 0;
            if (isTop)
            {
                zOffset = -zOffsetFromFace;
            }
            else
            {
                zOffset = -height + zOffsetFromFace;
            }
            
            double startYOffset = (width / 2.0) - cover - (barDiameter / 2.0);
            double spacingY = quantity > 1 ? (startYOffset * 2) / (quantity - 1) : 0;
            
            List<Tuple<double, double>> spans = GetClearSpans(beams, line);
            double anchorageLength = anchorageD * barDiameter;
            
            var validSpans = spans.Where(s => s.Item2 - s.Item1 >= 1.0).ToList();
            if (validSpans.Count == 0) return;
            
            if (isTop)
            {
                for (int i = 0; i <= validSpans.Count; i++)
                {
                    if (i == 0) // First support (Left end)
                    {
                        var span = validSpans[i];
                        double physicalStart = span.Item1;
                        double l0_4 = (span.Item2 - span.Item1) / 4.0;
                        double leftDist = GetColumnOuterFaceDistance(beam, start + dir * physicalStart, dir, true);
                        double startExtendT = physicalStart + leftDist + cover;
                        if (startExtendT >= physicalStart) startExtendT = physicalStart;
                        
                        XYZ p1 = start + dir * startExtendT;
                        XYZ p2 = start + dir * (physicalStart + l0_4);
                        CreateAddRebarsCurves(beam, barType, hookType, quantity, p1, p2, dir, up, right, zOffset, startYOffset, spacingY, anchorageLength, true, false, isTop);
                    }
                    else if (i == validSpans.Count) // Last support (Right end)
                    {
                        var span = validSpans[i - 1];
                        double physicalEnd = span.Item2;
                        double l0_4 = (span.Item2 - span.Item1) / 4.0;
                        double rightDist = GetColumnOuterFaceDistance(beam, start + dir * physicalEnd, dir, false);
                        double endExtendT = physicalEnd + rightDist - cover;
                        if (endExtendT <= physicalEnd) endExtendT = physicalEnd;
                        
                        XYZ p3 = start + dir * (physicalEnd - l0_4);
                        XYZ p4 = start + dir * endExtendT;
                        CreateAddRebarsCurves(beam, barType, hookType, quantity, p3, p4, dir, up, right, zOffset, startYOffset, spacingY, anchorageLength, false, true, isTop);
                    }
                    else // Intermediate support
                    {
                        var leftSpan = validSpans[i - 1];
                        var rightSpan = validSpans[i];
                        double leftL0_4 = (leftSpan.Item2 - leftSpan.Item1) / 4.0;
                        double rightL0_4 = (rightSpan.Item2 - rightSpan.Item1) / 4.0;
                        
                        XYZ p1 = start + dir * (leftSpan.Item2 - leftL0_4);
                        XYZ p2 = start + dir * (rightSpan.Item1 + rightL0_4);
                        // No hooks for intermediate supports
                        CreateAddRebarsCurves(beam, barType, hookType, quantity, p1, p2, dir, up, right, zOffset, startYOffset, spacingY, 0, false, false, isTop);
                    }
                }
            }
            else
            {
                foreach (var span in validSpans)
                {
                    double physicalStart = span.Item1;
                    double physicalEnd = span.Item2;
                    double clearSpan = physicalEnd - physicalStart;
                    double l0_10 = clearSpan / 10.0;
                    
                    XYZ p1 = start + dir * (physicalStart + l0_10);
                    XYZ p2 = start + dir * (physicalEnd - l0_10);
                    CreateAddRebarsCurves(beam, barType, null, quantity, p1, p2, dir, up, right, zOffset, startYOffset, spacingY, 0, false, false, isTop);
                }
            }
        }

        private void CreateAddRebarsCurves(FamilyInstance beam, RebarBarType barType, RebarHookType hookType, int quantity, XYZ startPt, XYZ endPt, XYZ dir, XYZ up, XYZ right, double zOffset, double startYOffset, double spacingY, double anchorageLength, bool hookStart, bool hookEnd, bool isTop)
        {
            XYZ legVector = isTop ? -up : up;
            
            for (int i = 0; i < quantity; i++)
            {
                double currentYOffset = quantity == 1 ? 0 : startYOffset - (i * spacingY);
                XYZ offsetVector = (up * zOffset) + (right * currentYOffset);
                Transform t = Transform.CreateTranslation(offsetVector);
                
                List<Curve> curves = new List<Curve>();
                
                if (hookStart && anchorageLength > 0 && hookType != null)
                {
                    XYZ p1 = startPt + legVector * anchorageLength;
                    curves.Add(Line.CreateBound(p1, startPt).CreateTransformed(t));
                }
                
                curves.Add(Line.CreateBound(startPt, endPt).CreateTransformed(t));
                
                if (hookEnd && anchorageLength > 0 && hookType != null)
                {
                    XYZ p2 = endPt + legVector * anchorageLength;
                    curves.Add(Line.CreateBound(endPt, p2).CreateTransformed(t));
                }
                
                try
                {
                    Rebar rebar = Rebar.CreateFromCurves(_doc, RebarStyle.Standard, barType, null, null, beam, right, curves, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                    
                    _createdRebars.Add(rebar);
                }
                catch (Exception)
                {
                    // Ignore rebar creation failures to avoid crashing the whole transaction
                }
            }
        }
    }
}
