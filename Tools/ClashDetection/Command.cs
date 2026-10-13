using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.ClashDetection.UI;
using JNNTool.Tools.ClashDetection.ViewModels;

namespace JNNTool.Tools.ClashDetection
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Initialize Handler and ViewModel
            var handler = new ClashDetectionExternalEventHandler();
            var viewModel = new ClashDetectionViewModel(handler, doc);
            
            // Show UI
            var window = new ClashDetectionWindow(viewModel);
            window.Show();

            return Result.Succeeded;
        }
    }
}

