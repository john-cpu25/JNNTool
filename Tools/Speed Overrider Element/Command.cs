using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.SpeedOverriderElement.UI;
using JNNTool.Tools.SpeedOverriderElement.ViewModels;

namespace JNNTool.Tools.SpeedOverriderElement
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Create External Event Handler
                var handler = new SpeedOverriderExternalEventHandler();
                var externalEvent = ExternalEvent.Create(handler);

                SpeedOverriderWindow window = null;
                var viewModel = new SpeedOverriderViewModel(doc, externalEvent);

                // Pass context to handler
                handler.Doc = doc;
                handler.ViewModel = viewModel;

                // Show UI (non-blocking: dùng Show() để Revit có thể dispatch ExternalEvent)
                window = new SpeedOverriderWindow(viewModel);
                handler.CloseWindowAction = () => window?.Close();
                window.Show();

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
