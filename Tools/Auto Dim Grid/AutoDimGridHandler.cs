using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;

namespace JNNTool.Tools.AutoDimGrid
{
    public class AutoDimGridHandler : IExternalEventHandler
    {
        public bool IsAutoAllSides { get; set; }
        public int LayersCount { get; set; } = 2;
        public bool IsOffsetFromGridEndpoint { get; set; } = false;
        public double DistanceToFirstDimMm { get; set; } = 15.0;
        public double DistanceBetweenDimsMm { get; set; } = 8.0;
        public DimensionType SelectedDimType { get; set; }

        /// <summary>Gọi khi chạy xong (thành công hay lỗi) để UI bật lại nút Run.</summary>
        public Action Completed { get; set; }

        public void Execute(UIApplication app)
        {
            try
            {
                UIDocument uidoc = app.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Auto Dim Grid", "Không có tài liệu Revit nào đang mở.");
                    return;
                }

                Document doc = uidoc.Document;
                View view = doc.ActiveView;

                if (!AutoDimGridLogic.IsSupportedView(view))
                {
                    TaskDialog.Show("Auto Dim Grid", "Vui lòng mở mặt bằng, mặt đứng hoặc mặt cắt để chạy lệnh này.");
                    return;
                }

                // DimensionType có thể đã bị xóa trong lúc cửa sổ mở.
                DimensionType dimType = SelectedDimType != null && SelectedDimType.IsValidObject ? SelectedDimType : null;

                if (IsAutoAllSides) RunAuto(doc, view, dimType);
                else RunManual(uidoc, doc, view, dimType);
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khi chạy Auto Dim Grid", ex);
                TaskDialog.Show("Lỗi Auto Dim Grid", ex.Message);
            }
            finally
            {
                Completed?.Invoke();
            }
        }

        private void RunAuto(Document doc, View view, DimensionType dimType)
        {
            if (!AutoDimGridLogic.CollectGridsInView(doc, view).Any())
            {
                TaskDialog.Show("Auto Dim Grid", "Không có lưới trục thẳng nào hiển thị trong view hiện tại.");
                return;
            }

            AutoDimResult result;
            using (var tx = new Transaction(doc, "JNN - Dim lưới trục 4 cạnh"))
            {
                tx.Start();
                SuppressWarnings(tx);
                result = AutoDimGridLogic.CreateDimensionsAutoAllSides(doc, view,
                    DistanceToFirstDimMm, DistanceBetweenDimsMm, LayersCount, IsOffsetFromGridEndpoint, dimType);

                if (result.Created > 0) tx.Commit();
                else tx.RollBack();
            }

            Report("Dim lưới trục 4 cạnh", result);
        }

        private void RunManual(UIDocument uidoc, Document doc, View view, DimensionType dimType)
        {
            var total = new AutoDimResult();
            int walls = 0;

            // Lặp để Dim nhiều tường liên tiếp; ESC để kết thúc. Mỗi tường một transaction để Undo từng cái.
            while (true)
            {
                Wall wall;
                XYZ pickPoint;
                try
                {
                    Reference wallRef = uidoc.Selection.PickObject(
                        Autodesk.Revit.UI.Selection.ObjectType.Element, new WallSelectionFilter(),
                        "Chọn tường để Dim (ESC để kết thúc)");
                    wall = doc.GetElement(wallRef) as Wall;
                    pickPoint = uidoc.Selection.PickPoint("Chọn vị trí đặt đường Dim");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    break;
                }

                if (wall == null) continue;

                using (var tx = new Transaction(doc, "JNN - Dim tường (thủ công)"))
                {
                    tx.Start();
                    SuppressWarnings(tx);
                    var r = AutoDimGridLogic.CreateWallDimension(doc, view, wall, pickPoint, dimType);
                    if (r.Created > 0) tx.Commit();
                    else tx.RollBack();
                    total.Merge(r);
                }
                walls++;

                // Lỗi thì báo ngay, không để người dùng tiếp tục chọn mà không biết.
                if (total.Failed > 0 && total.Created == 0)
                {
                    Report("Dim tường", total);
                    return;
                }
            }

            if (walls > 0 && total.Failed > 0) Report("Dim tường", total);
        }

        private static void Report(string title, AutoDimResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Đã tạo: {r.Created} Dim");
            if (r.Failed > 0) sb.AppendLine($"Lỗi: {r.Failed} Dim");
            if (r.SkippedGroups > 0) sb.AppendLine($"Bỏ qua: {r.SkippedGroups} nhóm trục (ít hơn 2 trục)");
            if (r.Created == 0 && r.Failed == 0) sb.AppendLine("Không có Dim nào được tạo.");
            if (!string.IsNullOrEmpty(r.FirstError)) sb.AppendLine().AppendLine($"Lỗi đầu tiên: {r.FirstError}");

            if (r.Failed > 0) Logger.Error($"Auto Dim Grid - {title}: {r.Failed} lỗi. {r.FirstError}");
            TaskDialog.Show($"Auto Dim Grid - {title}", sb.ToString().TrimEnd());
        }

        private static void SuppressWarnings(Transaction tx)
        {
            FailureHandlingOptions opts = tx.GetFailureHandlingOptions();
            opts.SetFailuresPreprocessor(new WarningSwallower());
            tx.SetFailureHandlingOptions(opts);
        }

        public string GetName()
        {
            return "Auto Dim Grid Handler";
        }
    }

    /// <summary>Xóa cảnh báo (không phải lỗi) để Revit không bật hộp thoại giữa chừng khi tạo hàng loạt Dim.</summary>
    internal class WarningSwallower : IFailuresPreprocessor
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

    public class WallSelectionFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
