using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JNNTool.Core.Logging;

namespace JNNTool.Tools.AutoDimGrid
{
    [Transaction(TransactionMode.Manual)]
    public class AutoDimGridCommand : IExternalCommand
    {
        private static AutoDimGridWindow _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    UIDocument uidoc = commandData.Application.ActiveUIDocument;
                    if (uidoc == null)
                    {
                        TaskDialog.Show("Auto Dim Grid", "Vui lòng mở một dự án Revit trước.");
                        return Result.Cancelled;
                    }
                    Document doc = uidoc.Document;
                    List<DimensionType> dimTypes = new FilteredElementCollector(doc)
                        .OfClass(typeof(DimensionType))
                        .WhereElementIsElementType()
                        .Cast<DimensionType>()
                        .Where(dt => dt.StyleType == DimensionStyleType.Linear)
                        .OrderBy(dt => dt.Name)
                        .ToList();

                    AutoDimGridHandler handler = new AutoDimGridHandler();
                    ExternalEvent exEvent = ExternalEvent.Create(handler);
                    
                    _window = new AutoDimGridWindow(commandData.Application, exEvent, handler, dimTypes);
                    _window.Closed += (s, args) => { _window = null; };
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
                Logger.Error("Lỗi khi mở giao diện Auto Dim Grid", ex);
                TaskDialog.Show("Lỗi Auto Dim Grid", $"Không thể mở giao diện Auto Dim Grid:\n\n{ex.GetType().Name}: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
