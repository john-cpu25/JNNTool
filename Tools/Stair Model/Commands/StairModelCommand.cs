#nullable enable
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.Tools.StairModel.ViewModels;
using JNNTool.Tools.StairModel.Views;

namespace JNNTool.Tools.StairModel.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class StairModelCommand : IExternalCommand
    {
        private static StairModelWindow? _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                    return Result.Cancelled;

                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                if (uiDoc == null)
                {
                    message = "Vui lòng mở một dự án Revit trước khi chạy công cụ.";
                    return Result.Failed;
                }

                var doc = uiDoc.Document;

                if (_window == null || !_window.IsLoaded)
                {
                    var viewModel = new StairModelViewModel();
                    viewModel.LoadRevitData(doc);

                    _window = new StairModelWindow(viewModel);
                    _window.Closed += (s, e) => _window = null;
                    _window.Show();
                }
                else
                {
                    _window.Activate();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khi mở công cụ Dựng Thang Kết Cấu (Stair Model)", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
