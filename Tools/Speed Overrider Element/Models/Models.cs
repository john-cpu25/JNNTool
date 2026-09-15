using System.Windows.Media;

namespace JNNTool.Tools.SpeedOverriderElement.Models
{
    public class ColorModel
    {
        public string Name { get; set; }
        public Color WpfColor { get; set; }
        public Autodesk.Revit.DB.Color RevitColor { get; set; }

        public ColorModel(string name, byte r, byte g, byte b)
        {
            Name = name;
            WpfColor = Color.FromRgb(r, g, b);
            RevitColor = new Autodesk.Revit.DB.Color(r, g, b);
        }
    }

    public class TypeModel
    {
        public string Name { get; set; }
        public Autodesk.Revit.DB.ElementId Id { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TypeModel other)
                return Id == other.Id;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class CategoryModel
    {
        public string Name { get; set; }
        public Autodesk.Revit.DB.ElementId Id { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is CategoryModel other)
                return Id == other.Id;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
