using Autodesk.Revit.DB;

namespace JNNTool.Tools.CreateView.Models
{
    public class ViewTemplateItem
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; } = string.Empty;

        public bool IsNone => Id == null || Id == ElementId.InvalidElementId;

        public override string ToString() => Name;
    }
}
