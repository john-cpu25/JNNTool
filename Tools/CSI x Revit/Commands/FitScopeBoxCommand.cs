using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CSIxRevit.Builder;

namespace JNNTool.Tools.CSIxRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class FitScopeBoxCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            var uiapp = commandData.Application;
            var doc = uiapp?.ActiveUIDocument?.Document;
            if (doc == null)
            {
                message = "Hãy mở một dự án Revit trước khi chạy lệnh.";
                return Result.Failed;
            }

            try
            {
                using (Transaction tx = new Transaction(doc, "JNN - Fit Scope Box cho Grid & Level"))
                {
                    tx.Start();
                    var res = ScopeBoxBuilder.FitGridsAndLevels(doc);
                    tx.Commit();

                    if (!res.Found)
                    {
                        TaskDialog.Show("JNN - Fit Scope Box",
                            $"⚠️ Không tìm thấy Scope Box tên '{ScopeBoxBuilder.DefaultName}' trong dự án.\n\n" +
                            $"Vui lòng tạo một Scope Box với tên là '{ScopeBoxBuilder.DefaultName}', điều chỉnh bao quanh nhà, rồi bấm lại nút này.");
                        return Result.Failed;
                    }

                    var t = res.Trim;
                    string msg = $"✅ Scope Box '{res.Name}': gán {res.LevelsAssigned} Level, gỡ khóa {res.GridsDetached} Grid để tự cắt đầu trục.\n" +
                                 $"📏 Cắt 3D Grid theo biên Scope Box: {res.Grids3DFitted} trục.\n\n" +
                                 $"📍 Kéo bóng vào trong Scope Box: {t.Adjusted} lượt (trục × mặt bằng) trên {t.Views} mặt bằng.\n" +
                                 $"🎯 Đo bóng thật và ép sát mép: {t.Snapped}/{t.Adjusted}\n" +
                                 (t.Failed > 0 ? $"⚠️ Không chỉnh được: {t.Failed}\n   Lỗi đầu tiên: {t.FirstError}\n" : "") +
                                 (!string.IsNullOrEmpty(res.Error) ? $"❌ Lỗi: {res.Error}\n" : "") +
                                 "\n💡 Nếu sau này kéo lại Scope Box, hãy bấm nút này lần nữa để cập nhật đầu trục.";
                    TaskDialog.Show("JNN - Fit Scope Box", msg);
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi khi Fit Scope Box", ex);
                TaskDialog.Show("JNN - Fit Scope Box", "Đã xảy ra lỗi: " + ex.Message);
                return Result.Failed;
            }
        }
    }
}
