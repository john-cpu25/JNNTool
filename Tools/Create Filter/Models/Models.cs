using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = System.Windows.Media.Color;

namespace JNNTool.Tools.CreateFilter.Models
{
    public class CategoryModel
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
    }

    public class ParameterModel
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
    }

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

    public enum FilterCondition
    {
        Equals,
        Contains,
        BeginsWith,
        EndsWith,
        DoesNotEqual,
        DoesNotContain,
        DoesNotBeginWith,
        DoesNotEndWith
    }
}
