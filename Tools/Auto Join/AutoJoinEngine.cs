using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;

namespace JNNTool.Tools.AutoJoin
{
    public enum AutoJoinScope
    {
        ActiveView,
        EntireProject
    }

    public class AutoJoinSummary
    {
        public int JoinedCount { get; set; }
        public int SwitchedCount { get; set; }
        public int AlreadyCorrectCount { get; set; }
        public int SkippedCount { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public int TotalProcessedPairs => JoinedCount + SwitchedCount + AlreadyCorrectCount;
    }

    /// <summary>
    /// Engine xử lý tự động Join cấu kiện và đảo thứ tự ưu tiên (Switch Join Order)
    /// theo tiêu chuẩn BIM / TCVN: Column > Beam > Floor (Cột > Dầm > Sàn).
    /// </summary>
    public static class AutoJoinEngine
    {
        private enum PairProcessResult
        {
            Joined,
            SwitchedOrder,
            AlreadyCorrect,
            Skipped
        }

        /// <summary>
        /// Thực thi thuật toán Auto Join theo mức ưu tiên: Cột > Dầm > Sàn
        /// </summary>
        public static AutoJoinSummary Execute(Document doc, View activeView, AutoJoinScope scope)
        {
            var summary = new AutoJoinSummary();
            var stopwatch = Stopwatch.StartNew();

            if (doc == null)
            {
                stopwatch.Stop();
                summary.ElapsedTime = stopwatch.Elapsed;
                return summary;
            }

            // Thu thập các cấu kiện Cột và Dầm theo Scope
            var colCategories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_Columns
            };
            var colMultiFilter = new ElementMulticategoryFilter(colCategories);

            IList<Element> columns;
            IList<Element> beams;

            bool isViewScope = (scope == AutoJoinScope.ActiveView && activeView != null);

            if (isViewScope)
            {
                columns = new FilteredElementCollector(doc, activeView.Id)
                    .WherePasses(colMultiFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                beams = new FilteredElementCollector(doc, activeView.Id)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .ToElements();
            }
            else
            {
                columns = new FilteredElementCollector(doc)
                    .WherePasses(colMultiFilter)
                    .WhereElementIsNotElementType()
                    .ToElements();

                beams = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType()
                    .ToElements();
            }

            // HashSet lưu trữ cặp Id đã xử lý để tránh trùng lặp
            var processedPairs = new HashSet<string>();
            const double tolerance = 0.01; // ~3mm mở rộng bounding box bắt tiếp xúc

            using (Transaction trans = new Transaction(doc, "JNN - Auto Join Elements"))
            {
                FailureHandlingOptions failOptions = trans.GetFailureHandlingOptions();
                failOptions.SetFailuresPreprocessor(new DismissWarningsPreprocessor());
                trans.SetFailureHandlingOptions(failOptions);

                trans.Start();

                // ─────────────────────────────────────────────────────────────
                // 1. CỘT CẮT DẦM (Column cuts Beam) & CỘT CẮT SÀN (Column cuts Floor)
                // ─────────────────────────────────────────────────────────────
                foreach (Element col in columns)
                {
                    if (col == null || !col.IsValidObject) continue;

                    BoundingBoxXYZ bb = isViewScope
                        ? col.get_BoundingBox(activeView) ?? col.get_BoundingBox(null)
                        : col.get_BoundingBox(null);

                    if (bb == null) continue;

                    Outline outline = new Outline(
                        new XYZ(bb.Min.X - tolerance, bb.Min.Y - tolerance, bb.Min.Z - tolerance),
                        new XYZ(bb.Max.X + tolerance, bb.Max.Y + tolerance, bb.Max.Z + tolerance)
                    );
                    var bbFilter = new BoundingBoxIntersectsFilter(outline);

                    // 1.1 Tìm dầm giao với cột
                    var intersectingBeams = isViewScope
                        ? new FilteredElementCollector(doc, activeView.Id)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsNotElementType()
                            .WherePasses(bbFilter)
                            .ToElements()
                        : new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_StructuralFraming)
                            .WhereElementIsNotElementType()
                            .WherePasses(bbFilter)
                            .ToElements();

                    foreach (Element beam in intersectingBeams)
                    {
                        string pairKey = $"{col.Id.GetIdValue()}_{beam.Id.GetIdValue()}";
                        if (!processedPairs.Add(pairKey)) continue;

                        var res = ProcessPair(doc, higher: col, lower: beam);
                        RecordResult(summary, res);
                    }

                    // 1.2 Tìm sàn giao với cột
                    var intersectingFloors = isViewScope
                        ? new FilteredElementCollector(doc, activeView.Id)
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .WhereElementIsNotElementType()
                            .WherePasses(bbFilter)
                            .ToElements()
                        : new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .WhereElementIsNotElementType()
                            .WherePasses(bbFilter)
                            .ToElements();

                    foreach (Element floor in intersectingFloors)
                    {
                        string pairKey = $"{col.Id.GetIdValue()}_{floor.Id.GetIdValue()}";
                        if (!processedPairs.Add(pairKey)) continue;

                        var res = ProcessPair(doc, higher: col, lower: floor);
                        RecordResult(summary, res);
                    }
                }

                // ─────────────────────────────────────────────────────────────
                // 2. DẦM CẮT SÀN (Beam cuts Floor)
                // ─────────────────────────────────────────────────────────────
                foreach (Element beam in beams)
                {
                    if (beam == null || !beam.IsValidObject) continue;

                    BoundingBoxXYZ bb = isViewScope
                        ? beam.get_BoundingBox(activeView) ?? beam.get_BoundingBox(null)
                        : beam.get_BoundingBox(null);

                    if (bb == null) continue;

                    Outline outline = new Outline(
                        new XYZ(bb.Min.X - tolerance, bb.Min.Y - tolerance, bb.Min.Z - tolerance),
                        new XYZ(bb.Max.X + tolerance, bb.Max.Y + tolerance, bb.Max.Z + tolerance)
                    );
                    var bbFilter = new BoundingBoxIntersectsFilter(outline);

                    var intersectingFloors = isViewScope
                        ? new FilteredElementCollector(doc, activeView.Id)
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .WhereElementIsNotElementType()
                            .WherePasses(bbFilter)
                            .ToElements()
                        : new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .WhereElementIsNotElementType()
                            .WherePasses(bbFilter)
                            .ToElements();

                    foreach (Element floor in intersectingFloors)
                    {
                        string pairKey = $"{beam.Id.GetIdValue()}_{floor.Id.GetIdValue()}";
                        if (!processedPairs.Add(pairKey)) continue;

                        var res = ProcessPair(doc, higher: beam, lower: floor);
                        RecordResult(summary, res);
                    }
                }

                trans.Commit();
            }

            stopwatch.Stop();
            summary.ElapsedTime = stopwatch.Elapsed;
            return summary;
        }

        private static PairProcessResult ProcessPair(Document doc, Element higher, Element lower)
        {
            if (higher == null || lower == null || higher.Id == lower.Id)
                return PairProcessResult.Skipped;

            try
            {
                bool areJoined = JoinGeometryUtils.AreElementsJoined(doc, higher, lower);
                if (!areJoined)
                {
                    // Thử Join 2 phần tử
                    JoinGeometryUtils.JoinGeometry(doc, higher, lower);

                    // Kiểm tra thứ tự cắt: Đảm bảo higher cắt lower
                    if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, higher, lower))
                    {
                        JoinGeometryUtils.SwitchJoinOrder(doc, higher, lower);
                    }
                    return PairProcessResult.Joined;
                }
                else
                {
                    // Đã Join trước đó, kiểm tra thứ tự cắt
                    if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, higher, lower))
                    {
                        JoinGeometryUtils.SwitchJoinOrder(doc, higher, lower);
                        return PairProcessResult.SwitchedOrder;
                    }
                    return PairProcessResult.AlreadyCorrect;
                }
            }
            catch
            {
                // Không thể join (do không chạm nhau thực tế hoặc Revit giới hạn cấu kiện)
                return PairProcessResult.Skipped;
            }
        }

        private static void RecordResult(AutoJoinSummary summary, PairProcessResult res)
        {
            switch (res)
            {
                case PairProcessResult.Joined:
                    summary.JoinedCount++;
                    break;
                case PairProcessResult.SwitchedOrder:
                    summary.SwitchedCount++;
                    break;
                case PairProcessResult.AlreadyCorrect:
                    summary.AlreadyCorrectCount++;
                    break;
                case PairProcessResult.Skipped:
                    summary.SkippedCount++;
                    break;
            }
        }
    }
}
