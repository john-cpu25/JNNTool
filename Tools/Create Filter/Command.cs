using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CreateFilter.UI;
using JNNTool.Tools.CreateFilter.ViewModels;

namespace JNNTool.Tools.CreateFilter
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;

                CreateFilterExternalEventHandler handler = new CreateFilterExternalEventHandler();
                ExternalEvent exEvent = ExternalEvent.Create(handler);

                CreateFilterWindow window = null;
                CreateFilterViewModel viewModel = new CreateFilterViewModel(doc, handler, exEvent);
                
                window = new CreateFilterWindow(viewModel);
                handler.CloseWindowAction = () => window?.Close();
                window.Show(); // non-blocking: Revit có thể dispatch ExternalEvent

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
