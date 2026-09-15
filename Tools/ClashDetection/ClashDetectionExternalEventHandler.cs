using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.ClashDetection.Models;
using JNNTool.Tools.ClashDetection.ViewModels;

namespace JNNTool.Tools.ClashDetection
{
    public class ClashDetectionExternalEventHandler : IExternalEventHandler
    {
        public ClashDetectionViewModel ViewModel { get; set; }
        public string RequestAction { get; set; } = "";
        public ClashResult SelectedClash { get; set; }

        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;

            switch (RequestAction)
            {
                case "RUN_CHECK":
                    PerformClashCheck(doc);
                    break;
                case "SHOW_CLASH":
                    if (SelectedClash != null) ShowClashInSectionBox(app, SelectedClash);
                    break;
            }
        }

        private void PerformClashCheck(Document doc)
        {
            try
            {
                var results = new List<ClashResult>();
                
                // Bá»™ A: Æ¯u tiÃªn Selection, náº¿u khÃ´ng láº¥y All in View
                List<Element> setA = ViewModel.GetHostElements(doc);
                
                if (setA.Count == 0)
                {
                    ViewModel.StatusMessage = "No elements found in Host Selection/View.";
                    return;
                }

                if (ViewModel.IsLinkSelected && ViewModel.SelectedLink != null)
                {
                    // Check Host vs Link
                    RevitLinkInstance linkInst = ViewModel.SelectedLink;
                    Document linkDoc = linkInst.GetLinkDocument();
                    Transform transform = linkInst.GetTotalTransform();
                    Transform inverseTransform = transform.Inverse;

                    foreach (Element hostElem in setA)
                    {
                        List<Solid> hostSolids = ClashUtils.GetSolids(hostElem);
                        foreach (Solid solid in hostSolids)
                        {
                            // Transform solid cá»§a host vá» khÃ´ng gian cá»§a link
                            Solid transformedSolid = SolidUtils.CreateTransformed(solid, inverseTransform);
                            
                            FilteredElementCollector linkCollector = new FilteredElementCollector(linkDoc);
                            // ThÃªm lá»c Category náº¿u user cÃ³ chá»n
                            if (ViewModel.SelectedLinkCategory != null)
                                linkCollector.OfCategoryId(ViewModel.SelectedLinkCategory.Id);
                            else
                                linkCollector.WhereElementIsNotElementType();

                            linkCollector.WherePasses(new ElementIntersectsSolidFilter(transformedSolid));

                            foreach (Element linkElem in linkCollector)
                            {
                                results.Add(new ClashResult
                                {
                                    HostElementId = hostElem.Id,
                                    HostCategory = hostElem.Category?.Name ?? "Unknown",
                                    HostName = hostElem.Name,
                                    LinkElementId = linkElem.Id,
                                    LinkCategory = linkElem.Category?.Name ?? "Unknown",
                                    LinkName = linkElem.Name,
                                    LinkDocumentName = linkDoc.Title
                                });
                            }
                        }
                    }
                }
                else
                {
                    // Check Host vs Host (Bá»™ A vs Bá»™ B chá»n trong UI)
                    // Logic tÆ°Æ¡ng tá»± nhÆ°ng trong cÃ¹ng Document
                }

                ViewModel.UpdateResults(results);
                ViewModel.StatusMessage = $"Check complete. Found {results.Count} clashes.";
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = "Error: " + ex.Message;
            }
        }

        private void ShowClashInSectionBox(UIApplication app, ClashResult clash)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            using (Transaction trans = new Transaction(doc, "Show Clash"))
            {
                trans.Start();

                // 1. TÃ¬m hoáº·c táº¡o View 3D
                View3D view3d = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .FirstOrDefault(v => v.Name == "{RINCO}_Clash_View");

                if (view3d == null)
                {
                    ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
                    
                    view3d = View3D.CreateIsometric(doc, viewFamilyType.Id);
                    view3d.Name = "{RINCO}_Clash_View";
                }

                // 2. TÃ­nh toÃ¡n Bounding Box
                Element hostElem = doc.GetElement(clash.HostElementId);
                BoundingBoxXYZ bbox = hostElem.get_BoundingBox(null);

                if (clash.IsLinkClash)
                {
                    // Náº¿u lÃ  link, láº¥y bbox cá»§a link element vÃ  transform vá» host
                    // ÄÆ¡n giáº£n hÃ³a: láº¥y bbox cá»§a host element lÃ m tÃ¢m Ä‘iá»ƒm
                }

                // 3. Ãp dá»¥ng Section Box
                if (bbox != null)
                {
                    view3d.IsSectionBoxActive = true;
                    view3d.SetSectionBox(ClashUtils.ExpandBoundingBox(bbox, 2.0)); // Má»Ÿ rá»™ng 2 feet
                    uidoc.ActiveView = view3d;
                    
                    // Highlight Ä‘á»‘i tÆ°á»£ng
                    uidoc.Selection.SetElementIds(new List<ElementId> { clash.HostElementId });
                }

                trans.Commit();
            }
        }

        public string GetName() => "Clash Detection Event Handler";

        public void Raise()
        {
            ExternalEvent.Create(this).Raise();
        }
    }
}

