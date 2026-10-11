using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public sealed class ScopeBoxBuildResult
    {
        public bool Found { get; set; }
        public bool Moved { get; set; }
        public bool Rotated { get; set; }
        public string Name { get; set; } = "";
        public ScopeBoxFitResult Fit { get; set; }
        public int GridsAssigned { get; set; }
        public int GridsDetached { get; set; }
        public int Grids3DFitted { get; set; }
        public int LevelsAssigned { get; set; }
        public GridTrimStats Trim { get; set; } = new GridTrimStats();
        public string Error { get; set; } = "";
    }

    public sealed class GridTrimStats
    {
        public int Adjusted { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Views { get; set; }
        public int Snapped { get; set; }
        public string FirstError { get; set; } = "";
    }

    /// <summary>
    /// Quản lý Scope Box cho mô hình ETABS:
    /// - Bước 2: Dời Scope Box "JNN_SB" về tâm công trình (chưa khóa Grid/Level) để người dùng dễ nhìn thấy và kéo tay.
    /// - Bước 3: Khóa toàn bộ Grid & Level khớp khít theo Scope Box "JNN_SB" (gọi từ nút Ribbon Fit Scope Box).
    /// </summary>
    public static class ScopeBoxBuilder
    {
        public const string DefaultName = "JNN_SB";

        /// <summary>Ước lượng đường kính bóng trục trên giấy cho lượt 1; lượt 2 sẽ đo thật và bù cho khít mép.</summary>
        public const double DefaultBubblePaperMm = 10.0;

        /// <summary>
        /// Tìm Scope Box "JNN_SB" và dời về đúng tâm công trình (X, Y, Z).
        /// Không khóa Grid/Level ở bước này để người dùng thoải mái quan sát và kéo rộng Scope Box quanh nhà.
        /// </summary>
        public static ScopeBoxBuildResult CenterScopeBox(
            Document doc,
            IEnumerable<double> xsFt, IEnumerable<double> ysFt,
            double minZFt, double maxZFt,
            double marginMm, string name = DefaultName)
        {
            var result = new ScopeBoxBuildResult { Name = name };
            if (doc == null) return result;

            try
            {
                var sb = FindScopeBox(doc, name);
                if (sb == null) return result;

                result.Found = true;
                result.Name = sb.Name;

                var bb = sb.get_BoundingBox(null);
                if (bb == null) { result.Error = "Cannot read scope box extents."; return result; }

                result.Rotated = IsRotated(sb);

                var current = new BoxMm(ToMm(bb.Min.X), ToMm(bb.Min.Y), ToMm(bb.Min.Z),
                                        ToMm(bb.Max.X), ToMm(bb.Max.Y), ToMm(bb.Max.Z));

                var fit = ScopeBoxPlanner.Plan(
                    (xsFt ?? Enumerable.Empty<double>()).Select(ToMm),
                    (ysFt ?? Enumerable.Empty<double>()).Select(ToMm),
                    ToMm(minZFt), ToMm(maxZFt), current, marginMm);
                result.Fit = fit;
                if (!fit.HasBuilding) return result;

                // Dời tâm Scope Box về đúng tâm công trình (X, Y, Z) để người dùng mở view là thấy ngay
                bool wasPinned = sb.Pinned;
                try
                {
                    if (wasPinned) sb.Pinned = false;

                    var move = new XYZ(ToFt(fit.MoveX), ToFt(fit.MoveY), ToFt(fit.MoveZ));
                    if (!move.IsZeroLength())
                    {
                        try
                        {
                            ElementTransformUtils.MoveElement(doc, sb.Id, move);
                        }
                        catch
                        {
                            var moveXY = new XYZ(move.X, move.Y, 0);
                            if (!moveXY.IsZeroLength()) ElementTransformUtils.MoveElement(doc, sb.Id, moveXY);
                        }
                        result.Moved = true;
                    }
                }
                finally
                {
                    try { if (wasPinned) sb.Pinned = true; } catch { }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi dời tâm Scope Box", ex);
            }

            return result;
        }

        /// <summary>
        /// Khóa toàn bộ Lưới trục (Grid) và Cao độ (Level) trong dự án khớp khít theo ranh giới của Scope Box "JNN_SB".
        /// Gọi độc lập từ nút Ribbon sau khi người dùng đã kéo bao quanh nhà.
        /// </summary>
        public static ScopeBoxBuildResult FitGridsAndLevels(Document doc, string name = DefaultName)
        {
            var result = new ScopeBoxBuildResult { Name = name };
            if (doc == null) return result;

            try
            {
                var sb = FindScopeBox(doc, name);
                if (sb == null) return result;

                result.Found = true;
                result.Name = sb.Name;

                var gridIds = new FilteredElementCollector(doc).OfClass(typeof(Grid)).ToElementIds();
                var levelIds = new FilteredElementCollector(doc).OfClass(typeof(Level)).ToElementIds();

                // Grid gắn Scope Box thì Revit khóa extents theo hộp và bỏ qua mọi lệnh chỉnh đầu trục qua API.
                // Vì vậy gỡ Grid khỏi Scope Box, tự cắt 3D + 2D theo biên hộp. Level vẫn gắn bình thường.
                result.GridsDetached = Assign(doc, gridIds, ElementId.InvalidElementId);
                result.LevelsAssigned = Assign(doc, levelIds, sb.Id);
                doc.Regenerate();

                var bb = sb.get_BoundingBox(null);
                if (bb != null) result.Grids3DFitted = FitModelExtentsToBox(doc, bb);
                doc.Regenerate();

                result.Trim = TrimGridEndsToScopeBox(doc, sb);
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi Fit Scope Box cho Grid & Level", ex);
            }

            return result;
        }

        /// <summary>
        /// Đặt 3D extents (Model) của mỗi Grid trùng đúng biên Scope Box, để trục không lòi ra ngoài
        /// ở các view chưa chỉnh 2D (mặt cắt, view mới tạo sau...).
        /// </summary>
        private static int FitModelExtentsToBox(Document doc, BoundingBoxXYZ bb)
        {
            var planViews = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                .Where(v => !v.IsTemplate).ToList();

            int count = 0;
            foreach (var grid in new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>())
            {
                foreach (var view in planViews)
                {
                    try
                    {
                        var curves = grid.GetCurvesInView(DatumExtentType.Model, view);
                        if (curves == null || curves.Count == 0 || !(curves[0] is Line line)) continue;
                        if (!TryComputeInsetSegment(line, bb, 0, out XYZ s, out XYZ e)) break;

                        var newLine = Line.CreateBound(s, e);
                        if (!grid.IsCurveValidInView(DatumExtentType.Model, view, newLine)) continue;
                        grid.SetCurveInView(DatumExtentType.Model, view, newLine);
                        count++;
                        break; // 3D extents dùng chung mọi view, chỉ cần đặt một lần
                    }
                    catch { }
                }
            }
            return count;
        }

        /// <summary>
        /// Kéo 2 đầu Grid (2D extents) vào BÊN TRONG Scope Box trên mọi mặt bằng,
        /// sao cho bóng trục nằm trọn trong Scope Box, sát mép.
        /// </summary>
        /// <param name="bubblePaperMm">
        /// Khoảng lùi vào trong tính trên giấy (mm) = đường kính bóng trục + khe hở nhỏ.
        /// Nhân với tỷ lệ view để ra khoảng cách thật, nên view 1/100 hay 1/200 đều đúng.
        /// </param>
        public static GridTrimStats TrimGridEndsToScopeBox(Document doc, Element sb, double bubblePaperMm = DefaultBubblePaperMm)
        {
            var stats = new GridTrimStats();
            if (doc == null || sb == null) return stats;
            var bb = sb.get_BoundingBox(null);
            if (bb == null) { stats.FirstError = "Không đọc được kích thước Scope Box."; return stats; }

            var planViews = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate)
                .ToList();

            var grids = new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();

            var placed = new List<(Grid grid, View view, XYZ start, XYZ end, double insetFt)>();

            foreach (var view in planViews)
            {
                int scale = view.Scale > 0 ? view.Scale : 100;
                double insetFt = ToFt(bubblePaperMm * scale);
                bool viewTouched = false;

                foreach (var grid in grids)
                {
                    // Grid không hiện trong view này (khác Level, bị ẩn...) thì bỏ qua, không tính là lỗi
                    IList<Curve> curves;
                    try { curves = grid.GetCurvesInView(DatumExtentType.Model, view); }
                    catch { continue; }
                    if (curves == null || curves.Count == 0 || !(curves[0] is Line line)) continue;

                    try
                    {
                        if (!TryComputeInsetSegment(line, bb, insetFt, out XYZ newStart, out XYZ newEnd))
                        {
                            stats.Skipped++;
                            continue;
                        }

                        try
                        {
                            grid.ShowBubbleInView(DatumEnds.End0, view);
                            grid.ShowBubbleInView(DatumEnds.End1, view);
                        }
                        catch { }

                        grid.SetDatumExtentType(DatumEnds.End0, view, DatumExtentType.ViewSpecific);
                        grid.SetDatumExtentType(DatumEnds.End1, view, DatumExtentType.ViewSpecific);

                        var newLine = Line.CreateBound(newStart, newEnd);
                        if (!grid.IsCurveValidInView(DatumExtentType.ViewSpecific, view, newLine))
                        {
                            stats.Failed++;
                            if (string.IsNullOrEmpty(stats.FirstError))
                                stats.FirstError = $"Grid '{grid.Name}' / view '{view.Name}': Revit từ chối đoạn 2D mới (IsCurveValidInView = false).";
                            continue;
                        }

                        grid.SetCurveInView(DatumExtentType.ViewSpecific, view, newLine);
                        stats.Adjusted++;
                        viewTouched = true;
                        placed.Add((grid, view, newStart, newEnd, insetFt));
                    }
                    catch (Exception ex)
                    {
                        stats.Failed++;
                        if (string.IsNullOrEmpty(stats.FirstError))
                            stats.FirstError = $"Grid '{grid.Name}' / view '{view.Name}': {ex.Message}";
                    }
                }

                if (viewTouched) stats.Views++;
            }

            // Lượt 2: đo bóng thật rồi bù sai số, để mép bóng chạm đúng mép Scope Box
            if (placed.Count > 0)
            {
                doc.Regenerate();
                foreach (var p in placed)
                {
                    try
                    {
                        if (SnapBubbleToEdge(p.grid, p.view, bb, p.start, p.end, p.insetFt)) stats.Snapped++;
                    }
                    catch { /* giữ kết quả lượt 1 nếu không đo được */ }
                }
            }

            if (stats.Failed > 0)
                JNNTool.Core.Logging.Logger.Warning($"CSI x Revit - Fit đầu trục: {stats.Failed} lỗi. {stats.FirstError}");

            return stats;
        }

        /// <summary>
        /// Đọc khung bao của Grid trong view (đã gồm bóng trục) và dịch từng đầu sao cho
        /// mép ngoài bóng trùng mép Scope Box. Chỉ áp cho trục song song X/Y.
        /// </summary>
        private static bool SnapBubbleToEdge(Grid grid, View view, BoundingBoxXYZ sbBox, XYZ start, XYZ end, double insetFt)
        {
            var gb = grid.get_BoundingBox(view);
            if (gb == null) return false;

            XYZ dir = (end - start).Normalize();
            bool vertical = Math.Abs(dir.X) < 1e-6;
            bool horizontal = Math.Abs(dir.Y) < 1e-6;
            if (!vertical && !horizontal) return false;

            // Khoảng hở còn lại ở mỗi phía (dương = bóng còn cách mép, âm = bóng lòi ra ngoài)
            double gapLow = vertical ? gb.Min.Y - sbBox.Min.Y : gb.Min.X - sbBox.Min.X;
            double gapHigh = vertical ? sbBox.Max.Y - gb.Max.Y : sbBox.Max.X - gb.Max.X;

            // Số đo bất thường (khung bao lỗi, view bị crop...) thì không tin
            double limit = Math.Max(insetFt * 1.5, 0.5);
            if (Math.Abs(gapLow) > limit || Math.Abs(gapHigh) > limit) return false;
            if (Math.Abs(gapLow) < 1e-3 && Math.Abs(gapHigh) < 1e-3) return true;

            double axisStart = vertical ? start.Y : start.X;
            double axisEnd = vertical ? end.Y : end.X;
            bool startIsLow = axisStart < axisEnd;

            double newStartAxis = axisStart + (startIsLow ? -gapLow : gapHigh);
            double newEndAxis = axisEnd + (startIsLow ? gapHigh : -gapLow);
            if (Math.Abs(newEndAxis - newStartAxis) < 0.1) return false;

            XYZ s = vertical ? new XYZ(start.X, newStartAxis, start.Z) : new XYZ(newStartAxis, start.Y, start.Z);
            XYZ e = vertical ? new XYZ(end.X, newEndAxis, end.Z) : new XYZ(newEndAxis, end.Y, end.Z);

            var line = Line.CreateBound(s, e);
            if (!grid.IsCurveValidInView(DatumExtentType.ViewSpecific, view, line)) return false;
            grid.SetCurveInView(DatumExtentType.ViewSpecific, view, line);
            return true;
        }

        /// <summary>
        /// Giao đường trục với hình chữ nhật Scope Box đã thu nhỏ vào trong một khoảng insetFt.
        /// Giữ nguyên chiều p0→p1 của Grid để End0/End1 (vị trí bóng) không bị đảo.
        /// </summary>
        private static bool TryComputeInsetSegment(Line line, BoundingBoxXYZ bb, double insetFt, out XYZ newStart, out XYZ newEnd)
        {
            newStart = newEnd = null;

            double minX = bb.Min.X + insetFt, maxX = bb.Max.X - insetFt;
            double minY = bb.Min.Y + insetFt, maxY = bb.Max.Y - insetFt;
            if (maxX - minX < 0.1 || maxY - minY < 0.1) return false;

            XYZ p0 = line.GetEndPoint(0);
            XYZ p1 = line.GetEndPoint(1);
            XYZ dir = (p1 - p0).Normalize();

            // Cắt đường thẳng vô hạn với hộp (thuật toán slab) => [tEnter, tExit]
            double tEnter = double.NegativeInfinity, tExit = double.PositiveInfinity;
            if (!Slab(p0.X, dir.X, minX, maxX, ref tEnter, ref tExit)) return false;
            if (!Slab(p0.Y, dir.Y, minY, maxY, ref tEnter, ref tExit)) return false;
            if (tExit - tEnter < 0.1) return false;

            newStart = new XYZ(p0.X + tEnter * dir.X, p0.Y + tEnter * dir.Y, p0.Z);
            newEnd = new XYZ(p0.X + tExit * dir.X, p0.Y + tExit * dir.Y, p0.Z);
            return true;
        }

        private static bool Slab(double origin, double d, double min, double max, ref double tEnter, ref double tExit)
        {
            if (Math.Abs(d) < 1e-9) return origin >= min && origin <= max;
            double t1 = (min - origin) / d, t2 = (max - origin) / d;
            if (t1 > t2) { var tmp = t1; t1 = t2; t2 = tmp; }
            tEnter = Math.Max(tEnter, t1);
            tExit = Math.Min(tExit, t2);
            return tEnter <= tExit;
        }

        public static Element FindScopeBox(Document doc, string name)
        {
            if (doc == null) return null;
            var boxes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .WhereElementIsNotElementType()
                .ToElements();

            return boxes.FirstOrDefault(e => string.Equals((e.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase))
                ?? boxes.FirstOrDefault(e => (e.Name ?? "").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static int Assign(Document doc, ICollection<ElementId> ids, ElementId scopeBoxId)
        {
            if (ids == null) return 0;
            int count = 0;
            foreach (var id in ids)
            {
                try
                {
                    var el = doc.GetElement(id);
                    var p = el?.get_Parameter(BuiltInParameter.DATUM_VOLUME_OF_INTEREST);
                    if (p != null && !p.IsReadOnly && p.Set(scopeBoxId)) count++;
                }
                catch { }
            }
            return count;
        }

        private static bool IsRotated(Element sb)
        {
            try
            {
                var geom = sb.get_Geometry(new Options());
                if (geom == null) return false;
                foreach (var obj in geom)
                {
                    if (obj is Line line)
                    {
                        var d = line.Direction;
                        bool axis = Math.Abs(d.X) > 0.9999 || Math.Abs(d.Y) > 0.9999 || Math.Abs(d.Z) > 0.9999;
                        if (!axis) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static double ToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
        private static double ToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
    }
}
