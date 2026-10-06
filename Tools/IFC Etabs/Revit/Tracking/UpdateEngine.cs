using System;
using System.Collections.Generic;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Models;

namespace JNNTool.Tools.IFCEtabs.Revit.Tracking
{
    public class ConversionStatistics
    {
        public int NewCount { get; set; }
        public int UpdatedCount { get; set; }
        public int UnchangedCount { get; set; }
        public int DeletedCount { get; set; }
        public int ErrorCount { get; set; }

        public override string ToString() =>
            $"Mới: {NewCount} | Cập nhật: {UpdatedCount} | Giữ nguyên: {UnchangedCount} | Đã xóa: {DeletedCount} | Lỗi: {ErrorCount}";
    }

    /// <summary>
    /// Bộ máy so sánh biến động mô hình (Diffing Engine).
    /// Phân loại đối tượng thành NEW, UPDATED, UNCHANGED, DELETED dựa trên mã băm Hash và ID.
    /// </summary>
    public class UpdateEngine
    {
        public ConversionStatistics Differentiate(StructuralModel model, ElementTracker tracker)
        {
            var stats = new ConversionStatistics();
            var processedEtabsIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Evaluate(StructuralElement el)
            {
                processedEtabsIds.Add(el.SourceId);

                if (!tracker.TrackedElements.TryGetValue(el.SourceId, out var tracked))
                {
                    el.Status = ConversionStatus.New;
                    stats.NewCount++;
                }
                else if (string.Equals(tracked.Hash, el.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    el.Status = ConversionStatus.Unchanged;
                    stats.UnchangedCount++;
                }
                else
                {
                    el.Status = ConversionStatus.Updated;
                    stats.UpdatedCount++;
                }
            }

            foreach (var col in model.Columns) Evaluate(col);
            foreach (var bm in model.Beams) Evaluate(bm);
            foreach (var wl in model.Walls) Evaluate(wl);
            foreach (var sb in model.Slabs) Evaluate(sb);
            foreach (var lv in model.Levels) Evaluate(lv);
            foreach (var gd in model.Grids) Evaluate(gd);

            // Kiểm tra các phần tử có trong Revit nhưng không còn trong file ETABS
            foreach (var kvp in tracker.TrackedElements)
            {
                if (!processedEtabsIds.Contains(kvp.Key))
                {
                    stats.DeletedCount++;
                }
            }

            return stats;
        }
    }
}
