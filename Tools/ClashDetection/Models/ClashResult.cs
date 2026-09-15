using Autodesk.Revit.DB;

namespace JNNTool.Tools.ClashDetection.Models
{
    public class ClashResult
    {
        public ElementId HostElementId { get; set; }
        public string HostCategory { get; set; }
        public string HostName { get; set; }
        public ElementId LinkElementId { get; set; }
        public string LinkCategory { get; set; }
        public string LinkName { get; set; }
        public string LinkDocumentName { get; set; }
        
        public bool IsLinkClash => LinkElementId != null && LinkElementId != ElementId.InvalidElementId;
    }
}

