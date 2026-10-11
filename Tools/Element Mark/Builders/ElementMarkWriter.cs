#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;
using JNNTool.Tools.ElementMark.Collectors;
using JNNTool.Tools.ElementMark.Models;

namespace JNNTool.Tools.ElementMark.Builders
{
    internal class WriteReport
    {
        public int Written;
        public int Unchanged;
        public int SkippedHasMark;
        public int Failed;
        public string FirstError = "";
    }

    /// <summary>Ghi Mark vào model trong một Transaction duy nhất (Undo một lần).</summary>
    internal static class ElementMarkWriter
    {
        public static WriteReport Write(Document doc, IEnumerable<MarkAssignment> items, ElementMarkSettings s)
        {
            var report = new WriteReport();

            using (var t = new Transaction(doc, "JNN - Đánh Mark cấu kiện"))
            {
                // Cột chung Mark từ móng lên mái, sàn chung Mark theo tầng → Revit sẽ cảnh báo
                // "duplicate Mark values". Đây là chủ đích nên xoá cảnh báo để không bật hộp thoại.
                var opts = t.GetFailureHandlingOptions();
                opts.SetFailuresPreprocessor(new MarkWarningSwallower());
                t.SetFailureHandlingOptions(opts);

                t.Start();
                foreach (var a in items)
                {
                    try
                    {
                        Element? e = doc.GetElement(ElementIdCompat.Create(a.Id));
                        if (e == null || !e.IsValidObject) { Fail(report, $"Id {a.Id}: không tìm thấy phần tử"); continue; }

                        Parameter? p = MarkParameterHelper.Get(e, s.TargetParameter);
                        if (p == null || p.IsReadOnly || p.StorageType != StorageType.String)
                        {
                            Fail(report, $"Id {a.Id}: tham số '{s.TargetParameter}' không tồn tại, chỉ đọc hoặc không phải kiểu Text");
                            continue;
                        }

                        string old = p.AsString() ?? "";
                        if (s.Overwrite == OverwriteMode.EmptyOnly && old.Trim().Length > 0) { report.SkippedHasMark++; continue; }
                        if (old == a.Mark) { report.Unchanged++; continue; }

                        if (p.Set(a.Mark)) report.Written++;
                        else Fail(report, $"Id {a.Id}: Revit từ chối giá trị '{a.Mark}'");
                    }
                    catch (Exception ex)
                    {
                        Fail(report, $"Id {a.Id}: {ex.Message}");
                    }
                }
                t.Commit();
            }
            return report;
        }

        private static void Fail(WriteReport r, string msg)
        {
            r.Failed++;
            if (r.FirstError.Length == 0) r.FirstError = msg;
        }
    }

    internal class MarkWarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (FailureMessageAccessor f in failuresAccessor.GetFailureMessages())
            {
                if (f.GetSeverity() == FailureSeverity.Warning) failuresAccessor.DeleteWarning(f);
            }
            return FailureProcessingResult.Continue;
        }
    }
}
