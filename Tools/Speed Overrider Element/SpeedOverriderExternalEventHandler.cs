using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.SpeedOverriderElement.Models;
using JNNTool.Tools.SpeedOverriderElement.ViewModels;

namespace JNNTool.Tools.SpeedOverriderElement
{
    public class SpeedOverriderExternalEventHandler : IExternalEventHandler
    {
        public Document Doc { get; set; }
        public SpeedOverriderViewModel ViewModel { get; set; }
        public System.Action CloseWindowAction { get; set; }

        public void Execute(UIApplication app)
        {
            if (Doc == null || ViewModel == null) return;
            if (ViewModel.SelectedCategory == null) return;

            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;
            View activeView = uidoc.ActiveView;
            if (activeView == null || !activeView.AreGraphicsOverridesAllowed()) return;

            // Get all elements of the selected category and type in the active view
            var collector = new FilteredElementCollector(Doc, activeView.Id)
                .OfCategoryId(ViewModel.SelectedCategory.Id)
                .WhereElementIsNotElementType();

            List<ElementId> elementsToOverride = new List<ElementId>();
            
            if (ViewModel.SelectedType != null)
            {
                // Filter by type
                foreach (var elem in collector)
                {
                    if (elem.GetTypeId() == ViewModel.SelectedType.Id)
                    {
                        elementsToOverride.Add(elem.Id);
                    }
                }
            }
            else
            {
                // If no type is selected, maybe override all elements of this category?
                // The requirements say "Chọn type", so presumably they always select a type.
                // Just in case, we'll override all if none selected.
                elementsToOverride = collector.Select(e => e.Id).ToList();
            }

            if (!elementsToOverride.Any()) return;

            using (Transaction t = new Transaction(Doc, ViewModel.IsResetting ? "Reset Color Override" : "Apply Color Override"))
            {
                t.Start();

                // Get solid fill pattern id
                ElementId solidFillPatternId = new FilteredElementCollector(Doc)
                    .OfClass(typeof(FillPatternElement))
                    .Cast<FillPatternElement>()
                    .FirstOrDefault(f => {
                        try { return f.GetFillPattern()?.IsSolidFill == true; }
                        catch { return false; }
                    })?.Id ?? ElementId.InvalidElementId;

                foreach (var elemId in elementsToOverride)
                {
                    OverrideGraphicSettings ogs = activeView.GetElementOverrides(elemId);
                    
                    if (ViewModel.IsResetting)
                    {
                        // Reset everything
                        ogs.SetProjectionLineColor(Autodesk.Revit.DB.Color.InvalidColorValue);
                        ogs.SetCutLineColor(Autodesk.Revit.DB.Color.InvalidColorValue);

                        ogs.SetSurfaceForegroundPatternId(ElementId.InvalidElementId);
                        ogs.SetSurfaceForegroundPatternColor(Autodesk.Revit.DB.Color.InvalidColorValue);
                        ogs.SetCutForegroundPatternId(ElementId.InvalidElementId);
                        ogs.SetCutForegroundPatternColor(Autodesk.Revit.DB.Color.InvalidColorValue);
                    }
                    else if (ViewModel.SelectedColor != null)
                    {
                        // Apply color
                        ogs.SetProjectionLineColor(ViewModel.SelectedColor.RevitColor);
                        ogs.SetCutLineColor(ViewModel.SelectedColor.RevitColor);

                        if (solidFillPatternId != ElementId.InvalidElementId)
                        {
                            ogs.SetSurfaceForegroundPatternId(solidFillPatternId);
                            ogs.SetSurfaceForegroundPatternColor(ViewModel.SelectedColor.RevitColor);
                            
                            ogs.SetCutForegroundPatternId(solidFillPatternId);
                            ogs.SetCutForegroundPatternColor(ViewModel.SelectedColor.RevitColor);
                        }
                    }

                    activeView.SetElementOverrides(elemId, ogs);
                }

                t.Commit();
            }
            
            // Trigger UI update to active view
            uidoc.RefreshActiveView();

            // Đóng window sau khi execute xong (tránh race condition với ExternalEvent)
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                CloseWindowAction?.Invoke());
        }

        public string GetName()
        {
            return "Speed Overrider External Event";
        }
    }
}
