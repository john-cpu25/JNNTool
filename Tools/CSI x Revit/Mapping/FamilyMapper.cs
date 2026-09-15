using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CSIxRevit.Mapping
{
    public class FamilyMapper
    {
        private readonly Document _doc;
        
        public List<FamilySymbol> BeamSymbols { get; private set; }
        public List<FamilySymbol> ColumnSymbols { get; private set; }
        public List<WallType> WallTypes { get; private set; }
        public List<FloorType> FloorTypes { get; private set; }
        public List<Level> Levels { get; private set; }
        
        public FamilyMapper(Document doc)
        {
            _doc = doc;
            LoadFromDocument();
        }

        private void LoadFromDocument()
        {
            BeamSymbols = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Cast<FamilySymbol>()
                .ToList();

            ColumnSymbols = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .Cast<FamilySymbol>()
                .ToList();

            WallTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();

            FloorTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .ToList();

            Levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }
    }
}
