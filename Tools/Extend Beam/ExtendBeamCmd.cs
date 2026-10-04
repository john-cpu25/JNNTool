using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JNNTool.Tools.ExtendBeam
{
    /// <summary>
    /// Extend Beam (nâng cấp của Trim/Extend Multiple Elements):
    /// - Chọn 1 hoặc nhiều dầm (hoặc chọn sẵn trước khi bấm lệnh).
    /// - Với từng đầu dầm (Start / End), tự dò cấu kiện gối phía trước theo phương dầm
    ///   (Cột, Dầm, Tường, Sàn, Móng) và nhận diện đầu đó đang gối vào cái gì.
    /// - Tự đưa TIM DẦM (đường định vị) ở 2 đầu tới đúng MẶT GẦN của cấu kiện gối (giống Trim/Extend):
    ///     + Đầu dầm còn hở  → kéo dài tới mặt gối.
    ///     + Đầu dầm nằm trong gối (vd. tới tim tường) → đưa về đúng mặt gối.
    ///     + Dầm xuyên qua hẳn cấu kiện → không cắt.
    /// - Dầm được giữ Disallow Join ở 2 đầu.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ExtendBeamCmd : IExternalCommand
    {
        /// <summary>Khoảng dò tìm tối đa phía trước mỗi đầu dầm (mm).</summary>
        public const double MaxSearchMm = 3000.0;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                List<FamilyInstance> beams = GetBeams(uidoc, doc);
                if (beams.Count == 0)
                {
                    TaskDialog.Show("Extend Beam", "Không tìm thấy dầm nào trong view hiện hành.");
                    return Result.Cancelled;
                }

                var results = new List<BeamResult>();

                using (Transaction tx = new Transaction(doc, "JNN - Extend Beam"))
                {
                    tx.Start();

                    FailureHandlingOptions fho = tx.GetFailureHandlingOptions();
                    fho.SetFailuresPreprocessor(new WarningSwallower());
                    tx.SetFailureHandlingOptions(fho);

                    var extender = new BeamExtender(doc, MaxSearchMm / 304.8);
                    foreach (FamilyInstance beam in beams)
                    {
                        BeamResult r;
                        try
                        {
                            r = extender.Process(beam);
                        }
                        catch (Exception ex)
                        {
                            r = new BeamResult { BeamId = beam.Id, Error = ex.Message };
                        }
                        results.Add(r);
                    }

                    tx.Commit();
                }

                ShowReport(results);
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

        // ─────────────────────────────────────────────────────────────
        //  Selection
        // ─────────────────────────────────────────────────────────────
        private static List<FamilyInstance> GetBeams(UIDocument uidoc, Document doc)
        {
            var filter = new ExtendBeamSelectionFilter();

            // 1. Nếu đã chọn sẵn dầm → chỉ xử lý các dầm đó (để chạy cho 1 vùng nhỏ khi cần)
            List<FamilyInstance> preSelected = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(filter.AllowElement)
                .OfType<FamilyInstance>()
                .ToList();
            if (preSelected.Count > 0) return preSelected;

            // 2. Mặc định: toàn bộ dầm nhìn thấy trong view hiện hành (tôn trọng crop / section box / ẩn)
            return new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Where(filter.AllowElement)
                .OfType<FamilyInstance>()
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        //  Report
        // ─────────────────────────────────────────────────────────────
        private static void ShowReport(List<BeamResult> results)
        {
            int beamsExtended = results.Count(r => r.Error == null && (r.Start.Kind == EndKind.Extended || r.End.Kind == EndKind.Extended));
            var ends = results.Where(r => r.Error == null).SelectMany(r => new[] { r.Start, r.End }).ToList();
            int endsExtended = ends.Count(e => e.Kind == EndKind.Extended && e.Extension > 0);
            int endsPulled = ends.Count(e => e.Kind == EndKind.Extended && e.Extension < 0);
            int endsConnected = ends.Count(e => e.Kind == EndKind.Connected);
            int endsFree = ends.Count(e => e.Kind == EndKind.Free);
            int errors = results.Count(r => r.Error != null);

            var bySupport = ends.Where(e => e.Kind == EndKind.Extended)
                                .GroupBy(e => e.SupportCategory)
                                .Select(g => $"{g.Key} {g.Count()}");

            var content = new StringBuilder();
            content.AppendLine($"• Đầu dầm đã kéo dài tới mặt gối: {endsExtended}");
            content.AppendLine($"• Đầu dầm nằm trong gối → đưa về mặt gối: {endsPulled}");
            if (endsExtended + endsPulled > 0)
                content.AppendLine($"   (Gối: {string.Join(" · ", bySupport)})");
            content.AppendLine($"• Tim dầm đã đúng mặt gối: {endsConnected}");
            content.AppendLine($"• Đầu tự do (không thấy gối trong {MaxSearchMm:0} mm): {endsFree}");
            if (errors > 0) content.AppendLine($"• Dầm bị bỏ qua: {errors}");

            var details = new StringBuilder();
            const int maxLines = 40;
            foreach (BeamResult r in results.Take(maxLines))
            {
                string id = $"#{GetIdValue(r.BeamId)}";
                if (r.Error != null)
                    details.AppendLine($"{id}: ⚠ {r.Error}");
                else
                    details.AppendLine($"{id}:  Start → {r.Start.Describe()}   |   End → {r.End.Describe()}");
            }
            if (results.Count > maxLines)
                details.AppendLine($"... và {results.Count - maxLines} dầm khác.");

            var dlg = new TaskDialog("Extend Beam")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                MainInstruction = $"Đã Extend {beamsExtended}/{results.Count} dầm."
            };

            // Ít dầm → hiện chi tiết luôn cho dễ kiểm tra
            if (results.Count <= 5)
            {
                dlg.MainContent = content.ToString().TrimEnd() + "\n\n" + details.ToString().TrimEnd();
            }
            else
            {
                dlg.MainContent = content.ToString().TrimEnd();
                dlg.ExpandedContent = details.ToString().TrimEnd();
            }
            dlg.Show();
        }

        internal static long GetIdValue(ElementId id)
        {
#if NET48
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Core engine
    // ═════════════════════════════════════════════════════════════════
    internal sealed class BeamExtender
    {
        /// <summary>Dung sai 1 mm (feet).</summary>
        private const double Tol = 1.0 / 304.8;

        /// <summary>
        /// Các cao độ tia dò trong mặt cắt dầm (tỉ lệ theo chiều cao). Tất cả tia đều nằm trên
        /// mặt phẳng đứng chứa TIM DẦM → giống Trim/Extend của Revit: tim dầm chạm mặt gối.
        /// (Dầm xiên gặp gối: góc dầm có thể cắm vào gối, tim dầm vẫn còn hở.)
        /// </summary>
        private static readonly double[] HeightFractions = { 0.1, 0.3, 0.5, 0.7, 0.9 };

        private static readonly BuiltInCategory[] SupportCategories =
        {
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_Walls,
            BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_StructuralFoundation
        };

        private readonly Document _doc;
        private readonly double _maxSearch;
        private readonly Options _geoOptions;
        private readonly ElementMulticategoryFilter _catFilter;
        private readonly SolidCurveIntersectionOptions _sciOptions;

        private readonly Dictionary<long, List<Solid>> _solidCache = new Dictionary<long, List<Solid>>();
        private readonly HashSet<long> _dirty = new HashSet<long>();

        public BeamExtender(Document doc, double maxSearchFeet)
        {
            _doc = doc;
            _maxSearch = maxSearchFeet;
            _geoOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = false,
                IncludeNonVisibleObjects = false
            };
            _catFilter = new ElementMulticategoryFilter(SupportCategories.ToList());
            _sciOptions = new SolidCurveIntersectionOptions
            {
                ResultType = SolidCurveIntersectionMode.CurveSegmentsInside
            };
        }

        public BeamResult Process(FamilyInstance beam)
        {
            var result = new BeamResult { BeamId = beam.Id };

            if (beam.Location is not LocationCurve lc || lc.Curve is not Line line)
            {
                result.Error = "Dầm cong / không có đường định vị thẳng.";
                return result;
            }

            XYZ p0 = line.GetEndPoint(0);
            XYZ p1 = line.GetEndPoint(1);
            double length = line.Length;
            if (length < 10 * Tol)
            {
                result.Error = "Dầm quá ngắn.";
                return result;
            }

            // Hệ trục mặt cắt: dir (dọc dầm), side (ngang), up (đứng, vuông góc dầm)
            XYZ dir = (p1 - p0).Normalize();
            XYZ side = XYZ.BasisZ.CrossProduct(dir);
            if (side.GetLength() < 1e-6)
            {
                result.Error = "Dầm thẳng đứng.";
                return result;
            }
            side = side.Normalize();
            XYZ up = dir.CrossProduct(side).Normalize();

            List<Solid> own = GetSolids(beam);
            if (own.Count == 0)
            {
                result.Error = "Không đọc được hình học dầm.";
                return result;
            }

            if (!TryGetSection(own, line, up, side,
                               out double vMin, out double vMax, out double sMin, out double sMax))
            {
                result.Error = "Không xác định được tiết diện dầm.";
                return result;
            }

            // Vị trí ngang của tim dầm (đường định vị). Nếu đường định vị nằm ngoài tiết diện
            // (y-justification lệch) thì dùng giữa bề rộng.
            double lateral = (sMin + Tol <= 0 && 0 <= sMax - Tol) ? 0.0 : (sMin + sMax) / 2;

            // Các điểm dò trong mặt cắt (tương đối với đường định vị)
            var offsets = new List<XYZ>();
            foreach (double fv in HeightFractions)
                offsets.Add(up * (vMin + (vMax - vMin) * fv) + side * lateral);

            double radius = new[] { vMin, vMax, sMin, sMax }.Max(x => Math.Abs(x)) + 0.5;

            // Nhận diện cấu kiện gối ở từng đầu (dùng hình học gốc trước khi sửa)
            result.Start = AnalyzeEnd(beam, own, p0, dir.Negate(), length, offsets, radius);
            result.End = AnalyzeEnd(beam, own, p1, dir, length, offsets, radius);

            double e0 = result.Start.Kind == EndKind.Extended ? result.Start.Extension : 0;
            double e1 = result.End.Kind == EndKind.Extended ? result.End.Extension : 0;

            if (e0 != 0 || e1 != 0)
            {
                XYZ n0 = p0 - dir * e0;
                XYZ n1 = p1 + dir * e1;

                // An toàn: không để dầm bị lật chiều / quá ngắn
                if ((n1 - n0).DotProduct(dir) < 10 * Tol)
                {
                    result.Error = "Hai đầu dầm cùng nằm trong gối – bỏ qua.";
                    return result;
                }

                EnsureJoinDisallowed(beam);
                lc.Curve = Line.CreateBound(n0, n1);
                EnsureJoinDisallowed(beam);
                MarkDirty(beam.Id);
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────
        //  End analysis (ray casting)
        // ─────────────────────────────────────────────────────────────
        private EndResult AnalyzeEnd(FamilyInstance beam, List<Solid> own, XYZ endPt, XYZ outDir,
                                     double length, List<XYZ> offsets, double radius)
        {
            var res = new EndResult { Kind = EndKind.Free };

            List<Element> candidates = CollectCandidates(beam, endPt, outDir, radius);
            if (candidates.Count == 0) return res;

            double back = length + 1.0;            // tia kéo lùi qua hết thân dầm
            double alongLimit = 0.5 * length;      // phần tử chạy dọc theo dầm (vd. sàn phía trên) → bỏ qua

            // Mọi khoảng cách đo từ ĐẦU ĐƯỜNG ĐỊNH VỊ (tim dầm), dương = hướng ra ngoài.
            // Mặt gối (face) có thể:
            //   > 0 : đầu dầm còn hở → kéo dài tới mặt gối
            //   < 0 : đầu dầm đang nằm TRONG gối (vd. tới tim tường) → đưa về đúng mặt gối
            // Gối chính: Cột / Dầm / Tường / Móng. Sàn chỉ là gối dự phòng khi không có gối chính.
            double bestFace = double.MaxValue;
            Element bestElem = null;
            double bestFloorFace = double.MaxValue;
            Element bestFloor = null;

            foreach (XYZ off in offsets)
            {
                XYZ origin = endPt + off;
                Line ray;
                try
                {
                    ray = Line.CreateBound(origin - outDir * back, origin + outDir * _maxSearch);
                }
                catch { continue; }

                // Đầu dầm thực tế theo hình học (chỉ dùng để nhận ra sàn bị dầm khoét)
                List<(double s0, double s1)> ownSegs = Segments(own, ray, origin, outDir);
                double geoExit = ownSegs.Count > 0 ? ownSegs.Max(s => s.s1) : 0.0;

                foreach (Element cand in candidates)
                {
                    bool isFloor = IsFloor(cand);

                    foreach (var (s0, s1) in Segments(GetSolids(cand), ray, origin, outDir))
                    {
                        if (s1 - s0 < Tol) continue;          // chỉ sượt qua
                        if (s1 <= Tol) continue;              // nằm hẳn phía sau đầu dầm (dầm xuyên qua) → không cắt
                        if (s0 < -alongLimit) continue;       // chạy dọc theo dầm (sàn, tường song song...)

                        if (isFloor)
                        {
                            // Sàn Join với dầm bị khoét đúng chỗ dầm → phần sàn còn lại bắt đầu
                            // ngay tại mặt đầu dầm. Đó KHÔNG phải gối → bỏ qua.
                            if (s0 <= Math.Max(0.0, geoExit) + Tol) continue;
                            if (s0 < bestFloorFace) { bestFloorFace = s0; bestFloor = cand; }
                            continue;
                        }

                        if (s0 < bestFace)
                        {
                            bestFace = s0;
                            bestElem = cand;
                        }
                    }
                }
            }

            if (bestElem == null && bestFloor != null)
            {
                bestElem = bestFloor;
                bestFace = bestFloorFace;
            }

            if (bestElem == null || bestFace > _maxSearch) return res;

            SetSupport(res, bestElem);
            if (Math.Abs(bestFace) <= Tol)
            {
                res.Kind = EndKind.Connected;          // tim dầm đã nằm đúng mặt gối
            }
            else
            {
                res.Kind = EndKind.Extended;
                res.Extension = bestFace;              // có dấu: + kéo dài, − đưa về mặt gối
            }
            return res;
        }

        private List<Element> CollectCandidates(FamilyInstance beam, XYZ endPt, XYZ outDir, double radius)
        {
            XYZ a = endPt - outDir * 0.5;
            XYZ b = endPt + outDir * _maxSearch;

            XYZ min = new XYZ(Math.Min(a.X, b.X) - radius, Math.Min(a.Y, b.Y) - radius, Math.Min(a.Z, b.Z) - radius);
            XYZ max = new XYZ(Math.Max(a.X, b.X) + radius, Math.Max(a.Y, b.Y) + radius, Math.Max(a.Z, b.Z) + radius);

            var bbFilter = new BoundingBoxIntersectsFilter(new Outline(min, max));

            return new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()
                .WherePasses(_catFilter)
                .WherePasses(bbFilter)
                .Excluding(new List<ElementId> { beam.Id })
                .ToElements()
                .Where(e => GetSolids(e).Count > 0)
                .ToList();
        }

        /// <summary>
        /// Xác định khoảng tiết diện dầm (so với đường định vị) theo phương đứng (up) và ngang (side).
        /// </summary>
        private bool TryGetSection(List<Solid> own, Line line, XYZ up, XYZ side,
                                   out double vMin, out double vMax, out double sMin, out double sMax)
        {
            vMin = vMax = sMin = sMax = 0;

            foreach (double t in new[] { 0.5, 0.3, 0.7, 0.15, 0.85 })
            {
                XYZ m = line.Evaluate(t, true);

                // Đo chiều cao trước, sau đó đo bề rộng tại giữa chiều cao (tránh sượt mặt trên)
                if (TryRange(own, m, up, out vMin, out vMax) &&
                    TryRange(own, m + up * ((vMin + vMax) / 2), side, out sMin, out sMax))
                    return true;

                // Đường định vị nằm ngoài tiết diện theo phương đứng → đo bề rộng trước
                if (TryRange(own, m, side, out sMin, out sMax) &&
                    TryRange(own, m + side * ((sMin + sMax) / 2), up, out vMin, out vMax))
                    return true;
            }
            return false;
        }

        private bool TryRange(List<Solid> solids, XYZ origin, XYZ axis, out double min, out double max)
        {
            min = max = 0;
            Line probe = Line.CreateBound(origin - axis * 50.0, origin + axis * 50.0);
            List<(double s0, double s1)> segs = Segments(solids, probe, origin, axis);
            if (segs.Count == 0) return false;

            min = segs.Min(s => s.s0);
            max = segs.Max(s => s.s1);
            return max - min > Tol;
        }

        /// <summary>Các đoạn nằm trong solid, quy về khoảng cách có dấu dọc theo dir tính từ origin.</summary>
        private List<(double s0, double s1)> Segments(List<Solid> solids, Line ray, XYZ origin, XYZ dir)
        {
            var list = new List<(double, double)>();
            foreach (Solid solid in solids)
            {
                SolidCurveIntersection sci;
                try
                {
                    sci = solid.IntersectWithCurve(ray, _sciOptions);
                }
                catch { continue; }
                if (sci == null) continue;

                for (int i = 0; i < sci.SegmentCount; i++)
                {
                    Curve c = sci.GetCurveSegment(i);
                    double a = (c.GetEndPoint(0) - origin).DotProduct(dir);
                    double b = (c.GetEndPoint(1) - origin).DotProduct(dir);
                    list.Add((Math.Min(a, b), Math.Max(a, b)));
                }
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────
        //  Geometry cache
        // ─────────────────────────────────────────────────────────────
        private List<Solid> GetSolids(Element e)
        {
            long key = ExtendBeamCmd.GetIdValue(e.Id);
            if (_solidCache.TryGetValue(key, out List<Solid> cached)) return cached;

            // Phần tử vừa bị sửa → regenerate để lấy hình học mới
            if (_dirty.Contains(key))
            {
                _doc.Regenerate();
                _dirty.Clear();
            }

            var solids = new List<Solid>();
            GeometryElement geo = e.get_Geometry(_geoOptions);
            if (geo != null) CollectSolids(geo, solids);

            _solidCache[key] = solids;
            return solids;
        }

        private void MarkDirty(ElementId id)
        {
            long key = ExtendBeamCmd.GetIdValue(id);
            _solidCache.Remove(key);
            _dirty.Add(key);
        }

        private static void CollectSolids(GeometryElement geo, List<Solid> solids)
        {
            foreach (GeometryObject obj in geo)
            {
                if (obj is Solid s && s.Faces.Size > 0 && s.Volume > 1e-9)
                {
                    solids.Add(s);
                }
                else if (obj is GeometryInstance gi)
                {
                    GeometryElement instGeo = gi.GetInstanceGeometry();
                    if (instGeo != null) CollectSolids(instGeo, solids);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────
        private static void EnsureJoinDisallowed(FamilyInstance beam)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    if (StructuralFramingUtils.IsJoinAllowedAtEnd(beam, i))
                        StructuralFramingUtils.DisallowJoinAtEnd(beam, i);
                }
                catch { /* một số family không hỗ trợ join */ }
            }
        }

        private void SetSupport(EndResult res, Element support)
        {
            res.SupportId = support.Id;
            res.SupportCategory = CategoryLabel(support);
            string typeName = _doc.GetElement(support.GetTypeId())?.Name ?? support.Name;
            res.SupportLabel = $"{res.SupportCategory} [{typeName}] #{ExtendBeamCmd.GetIdValue(support.Id)}";
        }

        private static bool IsFloor(Element e)
            => e is Floor
               || (e.Category != null && e.Category.Id == new ElementId(BuiltInCategory.OST_Floors));

        private static string CategoryLabel(Element e)
        {
            if (e.Category == null) return "Khác";
            ElementId c = e.Category.Id;
            if (c == new ElementId(BuiltInCategory.OST_StructuralColumns)) return "Cột";
            if (c == new ElementId(BuiltInCategory.OST_StructuralFraming)) return "Dầm";
            if (c == new ElementId(BuiltInCategory.OST_Walls)) return "Tường";
            if (c == new ElementId(BuiltInCategory.OST_Floors)) return "Sàn";
            if (c == new ElementId(BuiltInCategory.OST_StructuralFoundation)) return "Móng";
            return e.Category.Name;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Result models
    // ═════════════════════════════════════════════════════════════════
    internal enum EndKind
    {
        /// <summary>Không tìm thấy gối trong phạm vi dò.</summary>
        Free,
        /// <summary>Đầu dầm đã chạm / cắm vào gối sẵn → không cần kéo.</summary>
        Connected,
        /// <summary>Đã kéo dài tới mặt gối.</summary>
        Extended
    }

    internal sealed class EndResult
    {
        public EndKind Kind { get; set; } = EndKind.Free;
        public double Extension { get; set; }               // feet
        public ElementId SupportId { get; set; }
        public string SupportCategory { get; set; } = "";
        public string SupportLabel { get; set; } = "";

        public string Describe()
        {
            switch (Kind)
            {
                case EndKind.Extended:
                    return Extension > 0
                        ? $"{SupportLabel} +{Extension * 304.8:0} mm"
                        : $"{SupportLabel} về mặt gối −{-Extension * 304.8:0} mm";
                case EndKind.Connected:
                    return $"đúng mặt {SupportLabel}";
                default:
                    return "tự do";
            }
        }
    }

    internal sealed class BeamResult
    {
        public ElementId BeamId { get; set; }
        public EndResult Start { get; set; } = new EndResult();
        public EndResult End { get; set; } = new EndResult();
        public string Error { get; set; }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Selection filter & failure handling
    // ═════════════════════════════════════════════════════════════════
    /// <summary>Chỉ cho phép chọn dầm (Structural Framing) có đường định vị.</summary>
    internal class ExtendBeamSelectionFilter : ISelectionFilter
    {
        private static readonly ElementId FramingCatId = new ElementId(BuiltInCategory.OST_StructuralFraming);

        public bool AllowElement(Element elem)
            => elem is FamilyInstance fi
               && fi.Category != null
               && fi.Category.Id == FramingCatId
               && fi.Location is LocationCurve;

        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    /// <summary>Tự bỏ qua các cảnh báo (warning) phát sinh khi kéo dài dầm.</summary>
    internal class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (FailureMessageAccessor f in failuresAccessor.GetFailureMessages())
            {
                if (f.GetSeverity() == FailureSeverity.Warning)
                    failuresAccessor.DeleteWarning(f);
            }
            return FailureProcessingResult.Continue;
        }
    }
}
