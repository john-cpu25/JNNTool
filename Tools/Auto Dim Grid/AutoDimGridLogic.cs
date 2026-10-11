using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;
using JNNTool.Tools.AutoDimGrid.Engine;

namespace JNNTool.Tools.AutoDimGrid
{
    /// <summary>Kết quả một lần chạy, để báo lại cho người dùng thay vì nuốt lỗi.</summary>
    public sealed class AutoDimResult
    {
        public int Created { get; set; }
        public int Failed { get; set; }
        public int SkippedGroups { get; set; }
        public string FirstError { get; set; }

        internal void AddError(string context, Exception ex)
        {
            Failed++;
            if (FirstError == null) FirstError = $"{context}: {ex.Message}";
        }

        internal void Merge(AutoDimResult other)
        {
            Created += other.Created;
            Failed += other.Failed;
            SkippedGroups += other.SkippedGroups;
            if (FirstError == null) FirstError = other.FirstError;
        }
    }

    /// <summary>Trục kèm đường thể hiện thực tế trong view (đã chiếu lên mặt phẳng view).</summary>
    internal sealed class GridInView
    {
        public Grid Grid { get; set; }
        public Line Line { get; set; }
        public XYZ Dir { get; set; }
    }

    public static class AutoDimGridLogic
    {
        private const double ParallelTol = 1e-4;
        private const double PerpTol = 1e-3;
        private const double MmPerFoot = 304.8;
        // 1 mm: hai tham chiếu gần hơn mức này coi như trùng (tránh đoạn Dim 0).
        private const double DedupTolFt = 1.0 / MmPerFoot;
        // Trục nằm ngoài đầu tường quá 10 mm thì không coi là cắt qua tường.
        private const double WallGridTolFt = 10.0 / MmPerFoot;

        public static bool IsSupportedView(View view)
        {
            switch (view.ViewType)
            {
                case ViewType.FloorPlan:
                case ViewType.CeilingPlan:
                case ViewType.EngineeringPlan:
                case ViewType.AreaPlan:
                case ViewType.Section:
                case ViewType.Elevation:
                case ViewType.Detail:
                    return true;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ Dim thủ công

        public static AutoDimResult CreateWallDimension(Document doc, View view, Wall wall, XYZ pickPoint, DimensionType dimType)
        {
            var result = new AutoDimResult();
            if (!(wall.Location is LocationCurve lc) || !(lc.Curve is Line wallLine))
            {
                result.AddError($"Tường {wall.Id.GetIdValue()}", new InvalidOperationException("chỉ hỗ trợ tường thẳng."));
                return result;
            }

            XYZ wallDir = FlattenToView(wallLine.Direction, view);
            if (wallDir == null)
            {
                result.AddError($"Tường {wall.Id.GetIdValue()}", new InvalidOperationException("tường vuông góc với mặt phẳng view."));
                return result;
            }

            var refs = new List<KeyValuePair<Reference, double>>();
            var opt = new Options { ComputeReferences = true, View = view };
            GeometryElement geom = wall.get_Geometry(opt);
            if (geom != null) ExtractFacesFromGeometry(geom, wallDir, refs);

            double w0 = wallLine.GetEndPoint(0).DotProduct(wallDir);
            double w1 = wallLine.GetEndPoint(1).DotProduct(wallDir);
            double wMin = Math.Min(w0, w1);
            double wMax = Math.Max(w0, w1);
            foreach (var r in refs)
            {
                wMin = Math.Min(wMin, r.Value);
                wMax = Math.Max(wMax, r.Value);
            }

            // Chỉ lấy các trục vuông góc và thực sự cắt qua đoạn tường.
            foreach (var gi in CollectGridsInView(doc, view))
            {
                if (Math.Abs(gi.Dir.DotProduct(wallDir)) > PerpTol) continue;
                double proj = gi.Line.GetEndPoint(0).DotProduct(wallDir);
                if (proj < wMin - WallGridTolFt || proj > wMax + WallGridTolFt) continue;
                refs.Add(new KeyValuePair<Reference, double>(new Reference(gi.Grid), proj));
            }

            TryCreateDimension(doc, view, pickPoint, wallDir, refs, dimType, result, "Dim tường");
            return result;
        }

        // ------------------------------------------------------------------ Dim tự động 4 cạnh

        public static AutoDimResult CreateDimensionsAutoAllSides(Document doc, View view,
            double distanceToFirstDimMm, double distanceBetweenDimsMm, int layersCount,
            bool isOffsetFromGridEndpoint, DimensionType dimType)
        {
            var result = new AutoDimResult();
            var grids = CollectGridsInView(doc, view);
            XYZ viewDir = view.ViewDirection.Normalize();

            double firstFt = AutoDimGridEngine.PaperToModelMm(distanceToFirstDimMm, view.Scale) / MmPerFoot;
            double betweenFt = AutoDimGridEngine.PaperToModelMm(distanceBetweenDimsMm, view.Scale) / MmPerFoot;
            // Lớp Dim tường dựa vào hình học cắt của tường nên chỉ có nghĩa trên mặt bằng.
            bool wantWallLayer = layersCount >= 3 && view is ViewPlan;

            foreach (var group in GroupByDirection(grids))
            {
                XYZ gridDir = CanonicalDirection(group[0].Dir, view);
                XYZ dimDir = viewDir.CrossProduct(gridDir).Normalize();

                var projs = group.Select(g => g.Line.GetEndPoint(0).DotProduct(dimDir)).ToList();
                var order = AutoDimGridEngine.SortAndDedup(projs, DedupTolFt);
                if (order.Count < 2)
                {
                    result.SkippedGroups++;
                    continue;
                }
                var sorted = order.Select(i => group[i]).ToList();

                var gridRefs = sorted
                    .Select(g => new KeyValuePair<Reference, double>(new Reference(g.Grid), g.Line.GetEndPoint(0).DotProduct(dimDir)))
                    .ToList();
                var overallRefs = new List<KeyValuePair<Reference, double>> { gridRefs.First(), gridRefs.Last() };

                // Mốc dọc theo phương trục.
                var endProjs = group.SelectMany(g => new[] { g.Line.GetEndPoint(0), g.Line.GetEndPoint(1) })
                    .Select(p => p.DotProduct(gridDir)).ToList();
                double anchorMin = endProjs.Min();
                double anchorMax = endProjs.Max();
                bool insideFromEndpoint = isOffsetFromGridEndpoint;

                if (!isOffsetFromGridEndpoint)
                {
                    var perpProjs = grids
                        .Where(o => Math.Abs(o.Dir.DotProduct(gridDir)) < PerpTol)
                        .Select(o => o.Line.GetEndPoint(0).DotProduct(gridDir))
                        .ToList();
                    if (perpProjs.Count > 0)
                    {
                        anchorMin = perpProjs.Min();
                        anchorMax = perpProjs.Max();
                    }
                    else
                    {
                        // Không có trục vuông góc (vd. mặt cắt): đặt Dim ra ngoài đầu trục sẽ đè lên bóng trục,
                        // nên đặt vào trong tính từ đầu trục.
                        insideFromEndpoint = true;
                    }
                }

                XYZ basePt = sorted[0].Line.GetEndPoint(0);
                double baseProj = basePt.DotProduct(gridDir);

                foreach (int side in new[] { 1, -1 })
                {
                    var layers = new List<KeyValuePair<string, List<KeyValuePair<Reference, double>>>>();

                    if (wantWallLayer)
                    {
                        var wallRefs = GetWallAndOpeningReferences(doc, view, gridDir, dimDir, side > 0);
                        if (wallRefs.Count > 0)
                            layers.Add(new KeyValuePair<string, List<KeyValuePair<Reference, double>>>("Dim tường", gridRefs.Concat(wallRefs).ToList()));
                    }
                    layers.Add(new KeyValuePair<string, List<KeyValuePair<Reference, double>>>("Dim trục", gridRefs));
                    if (sorted.Count > 2)
                        layers.Add(new KeyValuePair<string, List<KeyValuePair<Reference, double>>>("Dim tổng", overallRefs));

                    double anchor = side > 0 ? anchorMax : anchorMin;
                    double[] positions = AutoDimGridEngine.LayerPositions(anchor, layers.Count, firstFt, betweenFt, side, insideFromEndpoint);

                    for (int k = 0; k < layers.Count; k++)
                    {
                        XYZ pt = basePt + gridDir * (positions[k] - baseProj);
                        TryCreateDimension(doc, view, pt, dimDir, layers[k].Value, dimType, result, layers[k].Key);
                    }
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>
        /// Tạo một Dim; lỗi chỉ làm hỏng Dim này chứ không làm mất cả lần chạy.
        /// </summary>
        private static void TryCreateDimension(Document doc, View view, XYZ point, XYZ dimDir,
            List<KeyValuePair<Reference, double>> refs, DimensionType dimType, AutoDimResult result, string context)
        {
            var projs = refs.Select(r => r.Value).ToList();
            var order = AutoDimGridEngine.SortAndDedup(projs, DedupTolFt);
            if (order.Count < 2) return;

            try
            {
                var arr = new ReferenceArray();
                foreach (int i in order) arr.Append(refs[i].Key);

                XYZ p = ProjectToViewPlane(point, view);
                Line dimLine = Line.CreateBound(p, p + dimDir);

                Dimension dim = dimType != null
                    ? doc.Create.NewDimension(view, dimLine, arr, dimType)
                    : doc.Create.NewDimension(view, dimLine, arr);

                if (dim != null) result.Created++;
                else result.AddError(context, new InvalidOperationException("Revit không tạo được Dim."));
            }
            catch (Exception ex)
            {
                result.AddError(context, ex);
            }
        }

        /// <summary>
        /// Lấy đường trục đang hiển thị trong view (đã kéo đầu 2D), không dùng Grid.Curve 3D,
        /// vì trên mặt cắt/mặt đứng Grid.Curve là đường nằm ngang trên mặt bằng, không phải đường thấy trong view.
        /// </summary>
        internal static List<GridInView> CollectGridsInView(Document doc, View view)
        {
            var result = new List<GridInView>();
            var grids = new FilteredElementCollector(doc, view.Id).OfClass(typeof(Grid)).Cast<Grid>();

            foreach (Grid g in grids)
            {
                Line line = GetLineInView(g, view);
                if (line == null) continue;

                XYZ p0 = ProjectToViewPlane(line.GetEndPoint(0), view);
                XYZ p1 = ProjectToViewPlane(line.GetEndPoint(1), view);
                if (p0.DistanceTo(p1) < 1e-6) continue; // trục nhìn thẳng đầu mút

                result.Add(new GridInView { Grid = g, Line = Line.CreateBound(p0, p1), Dir = (p1 - p0).Normalize() });
            }
            return result;
        }

        private static Line GetLineInView(Grid grid, View view)
        {
            foreach (var extent in new[] { DatumExtentType.ViewSpecific, DatumExtentType.Model })
            {
                try
                {
                    IList<Curve> curves = grid.GetCurvesInView(extent, view);
                    if (curves != null && curves.Count > 0 && curves[0] is Line l) return l;
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // View không hỗ trợ extents kiểu này → thử kiểu tiếp theo.
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    // Như trên.
                }
            }
            return grid.Curve as Line;
        }

        private static List<List<GridInView>> GroupByDirection(List<GridInView> grids)
        {
            var groups = new List<List<GridInView>>();
            foreach (var gi in grids)
            {
                var group = groups.FirstOrDefault(gr => Math.Abs(gr[0].Dir.DotProduct(gi.Dir)) > 1 - ParallelTol);
                if (group != null) group.Add(gi);
                else groups.Add(new List<GridInView> { gi });
            }
            return groups;
        }

        /// <summary>Cố định chiều của phương trục để "phía max/min" luôn nhất quán.</summary>
        private static XYZ CanonicalDirection(XYZ dir, View view)
        {
            double up = dir.DotProduct(view.UpDirection);
            if (up < -1e-9 || (Math.Abs(up) <= 1e-9 && dir.DotProduct(view.RightDirection) < 0))
                return -dir;
            return dir;
        }

        private static XYZ ProjectToViewPlane(XYZ p, View view)
        {
            XYZ n = view.ViewDirection.Normalize();
            return p - n * (p - view.Origin).DotProduct(n);
        }

        /// <summary>Bỏ thành phần theo phương nhìn; null nếu vector song song phương nhìn.</summary>
        private static XYZ FlattenToView(XYZ v, View view)
        {
            XYZ n = view.ViewDirection.Normalize();
            XYZ flat = v - n * v.DotProduct(n);
            return flat.GetLength() < 1e-9 ? null : flat.Normalize();
        }

        private static List<KeyValuePair<Reference, double>> GetWallAndOpeningReferences(Document doc, View view, XYZ gridDir, XYZ dimDir, bool isMaxSide)
        {
            var result = new List<KeyValuePair<Reference, double>>();

            // Tường biên chạy song song phương Dim.
            var parallelWalls = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Where(w => w.Location is LocationCurve lc && lc.Curve is Line l
                            && Math.Abs(l.Direction.Normalize().DotProduct(dimDir)) > 0.99)
                .ToList();
            if (parallelWalls.Count == 0) return result;

            Func<Wall, double> projOf = w => ((Line)((LocationCurve)w.Location).Curve).Origin.DotProduct(gridDir);
            double extreme = isMaxSide ? parallelWalls.Max(projOf) : parallelWalls.Min(projOf);
            var targetWalls = parallelWalls.Where(w => Math.Abs(projOf(w) - extreme) < 1.0).ToList(); // trong 1 ft

            var opt = new Options { ComputeReferences = true, View = view };
            foreach (var w in targetWalls)
            {
                GeometryElement geom = w.get_Geometry(opt);
                if (geom != null) ExtractFacesFromGeometry(geom, dimDir, result);
            }
            return result;
        }

        private static void ExtractFacesFromGeometry(GeometryElement geom, XYZ dimDir, List<KeyValuePair<Reference, double>> result)
        {
            foreach (GeometryObject obj in geom)
            {
                if (obj is Solid solid && solid.Faces.Size > 0)
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (face is PlanarFace pf && pf.Reference != null
                            && Math.Abs(pf.FaceNormal.DotProduct(dimDir)) > 0.99)
                        {
                            result.Add(new KeyValuePair<Reference, double>(pf.Reference, pf.Origin.DotProduct(dimDir)));
                        }
                    }
                }
                else if (obj is GeometryInstance inst)
                {
                    ExtractFacesFromGeometry(inst.GetInstanceGeometry(), dimDir, result);
                }
            }
        }
    }
}
