using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JNNTool.Tools.PileLocation.UI;
using JNNTool.Tools.PileLocation.ViewModels;

namespace JNNTool.Tools.PileLocation
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
                // Bước 1: Chọn cọc mẫu trước khi mở UI
                Reference pickedRef;
                try
                {
                    pickedRef = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        new PileSelectionFilter(),
                        "Chọn cọc mẫu (Structural Foundation có chữ Pile)");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                Element refPile = doc.GetElement(pickedRef.ElementId);
                if (refPile == null) return Result.Failed;

                // Bước 2: Khởi tạo ExternalEvent + ViewModel + Window
                var handler      = new PileLocationExternalEventHandler();
                var externalEvent = ExternalEvent.Create(handler);

                PileLocationWindow window = null;
                var viewModel = new PileLocationViewModel(
                    doc, refPile, externalEvent, () => window?.Close());

                handler.ViewModel = viewModel;

                window = new PileLocationWindow(viewModel);
                window.Show();   // Modeless – Revit idle → ExternalEvent fires ngay

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>Chỉ cho phép chọn Structural Foundation có chữ "Pile".</summary>
    internal class PileSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is not FamilyInstance fi) return false;
#if NET48
            if (elem.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_StructuralFoundation) return false;
#else
            if (elem.Category?.Id.Value != (long)BuiltInCategory.OST_StructuralFoundation) return false;
#endif
            string fam  = fi.Symbol?.Family?.Name ?? string.Empty;
            string type = fi.Symbol?.Name          ?? string.Empty;
            return fam.IndexOf("Pile",  StringComparison.OrdinalIgnoreCase) >= 0
                || type.IndexOf("Pile", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
