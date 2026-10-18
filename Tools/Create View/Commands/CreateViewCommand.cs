using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CreateView.Handlers;
using JNNTool.Tools.CreateView.UI;
using JNNTool.Tools.CreateView.ViewModels;
using JNNTool.Tools.Login.Services;

namespace JNNTool.Tools.CreateView.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreateViewCommand : IExternalCommand
    {
        private static CreateViewWindow? _windowInstance;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!LicenseGuard.EnsureLicensed(commandData.Application))
                    return Result.Cancelled;

                var uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    message = "Vui lòng mở một dự án Revit trước khi chạy lệnh.";
                    return Result.Failed;
                }

                Document doc = uidoc.Document;

                if (_windowInstance != null && _windowInstance.IsLoaded)
                {
                    _windowInstance.Activate();
                    return Result.Succeeded;
                }

                var handler = new CreateViewHandler();
                var viewModel = new CreateViewViewModel(handler, doc);

                _windowInstance = new CreateViewWindow(viewModel);
                _windowInstance.Closed += (s, e) => _windowInstance = null;
                _windowInstance.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
