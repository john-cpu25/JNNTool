using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Structure;

namespace JNNTool
{
    [Transaction(TransactionMode.Manual)]
    public class RebarBeamCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            try
            {
                // Lấy tất cả RebarBarTypes để đưa vào UI
                var rebarTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(RebarBarType))
                    .Cast<RebarBarType>()
                    .OrderBy(r => r.Name)
                    .ToList();

                if (rebarTypes.Count == 0)
                {
                    TaskDialog.Show("Error", "No Rebar Bar Types found in the document. Please load rebar families.");
                    return Result.Failed;
                }

                // Lấy tất cả RebarHookTypes
                var hookTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(RebarHookType))
                    .Cast<RebarHookType>()
                    .OrderBy(h => h.Name)
                    .ToList();

                hookTypes.Insert(0, null);

                // Initialize ViewModel
                var viewModel = new RebarBeamViewModel
                {
                    AvailableRebarTypes = new ObservableCollection<RebarBarType>(rebarTypes),
                    SelectedTopRebarType = rebarTypes.FirstOrDefault(r => r.Name.Contains("16")) ?? rebarTypes.First(),
                    SelectedBottomRebarType = rebarTypes.FirstOrDefault(r => r.Name.Contains("16")) ?? rebarTypes.First(),
                    SelectedStirrupRebarType = rebarTypes.FirstOrDefault(r => r.Name.Contains("8") || r.Name.Contains("10")) ?? rebarTypes.First(),
                    
                    AvailableHookTypes = new ObservableCollection<RebarHookType>(hookTypes),
                    SelectedTopHookType = hookTypes.FirstOrDefault(h => h != null && (h.Name.Contains("90") || h.Name.Contains("Standard"))) ?? hookTypes.FirstOrDefault(h => h != null),
                    SelectedBottomHookType = null,
                    SelectedStirrupHookType = hookTypes.FirstOrDefault(h => h != null && (h.Name.Contains("135") || h.Name.Contains("Seismic"))) ?? hookTypes.FirstOrDefault(h => h != null)
                };

                // Show UI FIRST
                RebarBeamWindow window = new RebarBeamWindow(viewModel);
                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Sau khi cấu hình xong, yêu cầu người dùng chọn dầm
                    IList<Reference> references = null;
                    try
                    {
                        references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select continuous concrete beams");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }

                    if (references == null || references.Count == 0) return Result.Cancelled;

                    List<Element> selectedBeams = new List<Element>();
                    foreach (var r in references)
                    {
                        Element elem = doc.GetElement(r);
                        if (elem is FamilyInstance fi && fi.StructuralType == StructuralType.Beam)
                        {
                            selectedBeams.Add(elem);
                        }
                    }

                    if (selectedBeams.Count == 0)
                    {
                        TaskDialog.Show("Error", "No concrete beams selected. Please select concrete beams.");
                        return Result.Failed;
                    }

                    // Execute creation logic
                    RebarBeamCreator creator = new RebarBeamCreator(doc);
                    creator.CreateRebars(selectedBeams, viewModel, uidoc.ActiveView);

                    TaskDialog.Show("Success", "Rebars created successfully for the selected continuous beams.");
                }

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
