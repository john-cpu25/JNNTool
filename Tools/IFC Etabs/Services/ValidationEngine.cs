using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Tracking;

namespace JNNTool.Tools.IFCEtabs.Services
{
    public enum ValidationStatus
    {
        Pass,
        Warning,
        Fail
    }

    public class ValidationItem
    {
        public string Category { get; set; } = string.Empty;
        public string ElementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public double Difference { get; set; } = 0.0;
        public ValidationStatus Status { get; set; } = ValidationStatus.Pass;
        public string Message { get; set; } = string.Empty;
    }

    public class ValidationSummary
    {
        public int TotalChecks { get; set; }
        public int PassedCount { get; set; }
        public int WarningCount { get; set; }
        public int FailedCount { get; set; }

        public double PassRate => TotalChecks > 0 ? (PassedCount / (double)TotalChecks) * 100.0 : 100.0;

        public List<ValidationItem> Items { get; } = new();

        public override string ToString() =>
            $"Kiểm tra: {TotalChecks} | Đạt: {PassedCount} | Cảnh báo: {WarningCount} | Lỗi: {FailedCount} (Tỷ lệ: {PassRate:F1}%)";
    }

    /// <summary>
    /// Bộ máy kiểm tra và đối soát độ chính xác mô hình (Validation Engine).
    /// Kiểm tra số lượng cấu kiện, cao độ tầng và sai lệch hình học (±2mm).
    /// </summary>
    public class ValidationEngine
    {
        public double GeometryToleranceMm { get; set; } = 2.0;

        public ValidationSummary Validate(Document doc, StructuralModel model, ElementTracker tracker)
        {
            var summary = new ValidationSummary();

            // 1. Kiểm tra đối soát số lượng cấu kiện
            ValidateCount(summary, "Cột (Columns)", model.Columns.Count, CountElements(tracker, "Column"));
            ValidateCount(summary, "Dầm (Beams)", model.Beams.Count, CountElements(tracker, "Beam"));
            ValidateCount(summary, "Tầng (Levels)", model.Levels.Count, new FilteredElementCollector(doc).OfClass(typeof(Level)).GetElementCount());

            // 2. Kiểm tra sai lệch cao độ các tầng
            var revitLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var elvl in model.Levels)
            {
                Level? matched = null;
                double minDiff = double.MaxValue;

                foreach (var rlvl in revitLevels)
                {
                    double diffMm = Math.Abs(UnitConverter.FeetToMm(rlvl.Elevation) - elvl.ElevationMm);
                    if (diffMm < minDiff)
                    {
                        minDiff = diffMm;
                        matched = rlvl;
                    }
                }

                if (matched != null)
                {
                    bool pass = minDiff <= GeometryToleranceMm;
                    summary.Items.Add(new ValidationItem
                    {
                        Category = "Tầng (Level)",
                        ElementId = elvl.Name,
                        ItemName = $"Cao độ tầng {elvl.Name}",
                        ExpectedValue = $"{elvl.ElevationMm:F1} mm",
                        ActualValue = $"{UnitConverter.FeetToMm(matched.Elevation):F1} mm",
                        Difference = minDiff,
                        Status = pass ? ValidationStatus.Pass : ValidationStatus.Warning,
                        Message = pass ? "Khớp cao độ" : $"Lệch cao độ {minDiff:F1}mm (> {GeometryToleranceMm}mm)"
                    });
                }
            }

            // 3. Kiểm tra sai lệch hình học dầm (Beams)
            foreach (var beam in model.Beams)
            {
                if (tracker.TrackedElements.TryGetValue(beam.SourceId, out var tracked) &&
                    tracked.Element is FamilyInstance inst &&
                    inst.Location is LocationCurve locCurve)
                {
                    XYZ p0 = locCurve.Curve.GetEndPoint(0);
                    XYZ p1 = locCurve.Curve.GetEndPoint(1);

                    double revitLengthMm = UnitConverter.FeetToMm(p0.DistanceTo(p1));
                    double diffLengthMm = Math.Abs(revitLengthMm - beam.LengthMm);

                    bool pass = diffLengthMm <= GeometryToleranceMm;
                    summary.Items.Add(new ValidationItem
                    {
                        Category = "Dầm (Beam)",
                        ElementId = beam.SourceId,
                        ItemName = $"Chiều dài dầm {beam.Name}",
                        ExpectedValue = $"{beam.LengthMm:F1} mm",
                        ActualValue = $"{revitLengthMm:F1} mm",
                        Difference = diffLengthMm,
                        Status = pass ? ValidationStatus.Pass : ValidationStatus.Warning,
                        Message = pass ? "Khớp hình học" : $"Sai lệch chiều dài {diffLengthMm:F1}mm"
                    });
                }
            }

            // 4. Tổng kết số lượng Pass/Fail
            summary.TotalChecks = summary.Items.Count;
            foreach (var item in summary.Items)
            {
                if (item.Status == ValidationStatus.Pass) summary.PassedCount++;
                else if (item.Status == ValidationStatus.Warning) summary.WarningCount++;
                else summary.FailedCount++;
            }

            return summary;
        }

        private static void ValidateCount(ValidationSummary summary, string category, int expected, int actual)
        {
            bool pass = expected == actual;
            summary.Items.Add(new ValidationItem
            {
                Category = "Số lượng cấu kiện",
                ItemName = $"Tổng số {category}",
                ExpectedValue = expected.ToString(),
                ActualValue = actual.ToString(),
                Difference = Math.Abs(expected - actual),
                Status = pass ? ValidationStatus.Pass : (Math.Abs(expected - actual) <= 2 ? ValidationStatus.Warning : ValidationStatus.Fail),
                Message = pass ? "Khớp hoàn toàn" : $"Chênh lệch {Math.Abs(expected - actual)} cấu kiện"
            });
        }

        private static int CountElements(ElementTracker tracker, string keyword)
        {
            int count = 0;
            foreach (var kvp in tracker.TrackedElements)
            {
                if (kvp.Value.Element.Category != null &&
                    kvp.Value.Element.Category.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
