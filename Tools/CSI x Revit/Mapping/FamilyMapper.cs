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
        /// <summary>Floor thường (category Floors) – dùng cho sàn S.</summary>
        public List<FloorType> SlabTypes { get; private set; }
        /// <summary>Foundation Slab (category Structural Foundations) – dùng cho sàn móng F.</summary>
        public List<FloorType> FoundationSlabTypes { get; private set; }
        public List<Level> Levels { get; private set; }
        
        public FamilyMapper(Document doc)
        {
            _doc = doc;
            LoadFromDocument();
        }

        public void Reload()
        {
            LoadFromDocument();
        }

        /// <summary>Danh sách type Revit hợp lệ cho từng loại đối tượng ETABS (xem ElementKinds).</summary>
        public List<ElementType> GetTypesForKind(string kind)
        {
            switch (kind)
            {
                case JNNTool.Tools.CSIxRevit.Models.ElementKinds.Beam: return BeamSymbols.Cast<ElementType>().ToList();
                case JNNTool.Tools.CSIxRevit.Models.ElementKinds.Column: return ColumnSymbols.Cast<ElementType>().ToList();
                case JNNTool.Tools.CSIxRevit.Models.ElementKinds.Wall: return WallTypes.Cast<ElementType>().ToList();
                case JNNTool.Tools.CSIxRevit.Models.ElementKinds.Slab: return SlabTypes.Cast<ElementType>().ToList();
                case JNNTool.Tools.CSIxRevit.Models.ElementKinds.FoundationSlab: return FoundationSlabTypes.Cast<ElementType>().ToList();
                default: return new List<ElementType>(); // Sàn ảo: không dựng
            }
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

            // IsFoundationSlab là read-only: type móng chỉ có được bằng cách nhân bản từ type móng có sẵn.
            SlabTypes = FloorTypes.Where(f => !f.IsFoundationSlab).ToList();
            FoundationSlabTypes = FloorTypes.Where(f => f.IsFoundationSlab).ToList();

            Levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }
    }
}
