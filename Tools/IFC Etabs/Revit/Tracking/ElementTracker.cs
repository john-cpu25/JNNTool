using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Tracking
{
    public class TrackedRevitElement
    {
        public Element Element { get; set; }
        public ElementId Id => Element.Id;
        public string EtabsId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;

        public TrackedRevitElement(Element element)
        {
            Element = element;
            EtabsId = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamEtabsId);
            Source = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamSource);
            Hash = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamSourceHash);
            Section = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamSection);
            Material = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamMaterial);
            Story = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamStory);
        }
    }

    /// <summary>
    /// Thu thập và theo dõi các đối tượng Revit đã được tạo từ JNN Converter.
    /// </summary>
    public class ElementTracker
    {
        private readonly Document _doc;
        public Dictionary<string, TrackedRevitElement> TrackedElements { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ElementTracker()
        {
            _doc = null!;
        }

        public ElementTracker(Document doc)
        {
            _doc = doc;
            ScanExistingElements();
        }

        public void ScanExistingElements()
        {
            TrackedElements.Clear();

            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors
            };

            try
            {
                var filter = new ElementMulticategoryFilter(categories);
                var elements = new FilteredElementCollector(_doc)
                    .WherePasses(filter)
                    .WhereElementIsNotElementType();

                foreach (var el in elements)
                {
                    string etabsId = SharedParameterManager.GetParameterValue(el, SharedParameterManager.ParamEtabsId);
                    if (!string.IsNullOrWhiteSpace(etabsId))
                    {
                        TrackedElements[etabsId] = new TrackedRevitElement(el);
                    }
                }
            }
            catch
            {
                // Fallback: Quét từng category riêng lẻ nếu ElementMulticategoryFilter gặp vấn đề trên một số phiên bản Revit
                foreach (var bic in categories)
                {
                    try
                    {
                        var elements = new FilteredElementCollector(_doc)
                            .OfCategory(bic)
                            .WhereElementIsNotElementType();

                        foreach (var el in elements)
                        {
                            string etabsId = SharedParameterManager.GetParameterValue(el, SharedParameterManager.ParamEtabsId);
                            if (!string.IsNullOrWhiteSpace(etabsId))
                            {
                                TrackedElements[etabsId] = new TrackedRevitElement(el);
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
