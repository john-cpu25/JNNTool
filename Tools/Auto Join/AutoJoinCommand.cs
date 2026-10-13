using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace JNNTool.Tools.AutoJoin
{
    /// <summary>
    /// Command chính: Hiển thị TaskDialog 2 chế độ (Active View / Entire Project) để người dùng chọn nhanh.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class AutoJoinCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            var uiApp = commandData.Application;
            var uidoc = uiApp.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null) return Result.Cancelled;

            var doc = uidoc.Document;
            var activeView = uidoc.ActiveView;

            TaskDialog dialog = new TaskDialog("JNNTool — Auto Join Elements")
            {
                MainInstruction = "Chọn phạm vi tự động Join & Đảo thứ tự ưu tiên",
                MainContent = "Quy tắc ưu tiên cấu kiện (TCVN / BIM):\n  1. Cột (Column) > 2. Dầm (Beam) > 3. Sàn (Floor)\n\n• Cột cắt Dầm và Sàn\n• Dầm cắt Sàn\n• Tự động đảo thứ tự (Switch Order) nếu đang bị ngược.",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                CommonButtons = TaskDialogCommonButtons.Close
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                "Chỉ trong View hiện hành (Active View)",
                "Quét và liên kết các cấu kiện Cột - Dầm - Sàn đang hiển thị trong view này.");

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                "Toàn bộ dự án (Entire Project)",
                "Quét và liên kết tự động cho toàn bộ mô hình trong file Revit.");

            var result = dialog.Show();

            AutoJoinScope scope;
            if (result == TaskDialogResult.CommandLink1)
            {
                scope = AutoJoinScope.ActiveView;
            }
            else if (result == TaskDialogResult.CommandLink2)
            {
                scope = AutoJoinScope.EntireProject;
            }
            else
            {
                return Result.Cancelled;
            }

            try
            {
                var summary = AutoJoinEngine.Execute(doc, activeView, scope);
                ShowSummary(summary, scope);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JNNTool — Lỗi", "Có lỗi xảy ra khi thực thi Auto Join:\n\n" + ex);
                return Result.Failed;
            }
        }

        internal static void ShowSummary(AutoJoinSummary summary, AutoJoinScope scope)
        {
            string scopeText = (scope == AutoJoinScope.ActiveView) ? "View hiện hành" : "Toàn bộ dự án";
            string msg = $"Phạm vi xử lý: {scopeText}\n" +
                         $"Thời gian thực thi: {summary.ElapsedTime.TotalSeconds:F2} giây\n\n" +
                         $"✔ Đã Join mới: {summary.JoinedCount} cặp\n" +
                         $"✔ Đã đảo thứ tự đúng ưu tiên (Switch Order): {summary.SwitchedCount} cặp\n" +
                         $"✔ Đã đúng thứ tự từ trước: {summary.AlreadyCorrectCount} cặp\n" +
                         $"Tổng số cặp liên kết xử lý: {summary.TotalProcessedPairs}";

            TaskDialog.Show("JNNTool — Hoàn Thành Auto Join", msg);
        }
    }

    /// <summary>
    /// Chạy trực tiếp 1 click cho View hiện hành (Active View).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class AutoJoinActiveViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            var uiApp = commandData.Application;
            var uidoc = uiApp.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null) return Result.Cancelled;

            try
            {
                var summary = AutoJoinEngine.Execute(uidoc.Document, uidoc.ActiveView, AutoJoinScope.ActiveView);
                AutoJoinCommand.ShowSummary(summary, AutoJoinScope.ActiveView);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JNNTool — Lỗi", "Có lỗi xảy ra khi thực thi Auto Join:\n\n" + ex);
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Chạy trực tiếp 1 click cho toàn bộ Dự án (Entire Project).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class AutoJoinProjectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            var uiApp = commandData.Application;
            var uidoc = uiApp.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null) return Result.Cancelled;

            try
            {
                var summary = AutoJoinEngine.Execute(uidoc.Document, uidoc.ActiveView, AutoJoinScope.EntireProject);
                AutoJoinCommand.ShowSummary(summary, AutoJoinScope.EntireProject);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JNNTool — Lỗi", "Có lỗi xảy ra khi thực thi Auto Join:\n\n" + ex);
                return Result.Failed;
            }
        }
    }
}
