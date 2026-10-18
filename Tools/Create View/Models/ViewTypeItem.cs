using Autodesk.Revit.DB;

namespace JNNTool.Tools.CreateView.Models
{
    public class ViewTypeItem
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;

        public override string ToString() => string.IsNullOrEmpty(FamilyName) ? Name : $"{Name} ({FamilyName})";
    }
}
