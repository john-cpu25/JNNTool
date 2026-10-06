using System.Collections.Generic;

namespace JNNTool.Tools.IFCEtabs.Mapping.Models
{
    public class SectionMappingEntry
    {
        public string SourceSectionName { get; set; } = string.Empty;
        public string RevitFamilyName { get; set; } = string.Empty;
        public string RevitTypeName { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public bool IsAutoCreated { get; set; }
    }

    public class MaterialMappingEntry
    {
        public string SourceMaterialName { get; set; } = string.Empty;
        public string RevitMaterialName { get; set; } = string.Empty;
    }

    public class MappingConfig
    {
        public double ElevationToleranceMm { get; set; } = 2.0;
        public double CoordinateToleranceMm { get; set; } = 2.0;

        // Tên Family mẫu mặc định để tự động nhân bản Type (Duplicate) nếu chưa có
        public string DefaultConcreteBeamFamily { get; set; } = "M_Concrete-Rectangular Beam";
        public string DefaultConcreteColumnFamily { get; set; } = "M_Concrete-Rectangular-Column";

        public Dictionary<string, SectionMappingEntry> SectionMappings { get; set; } = new();
        public Dictionary<string, MaterialMappingEntry> MaterialMappings { get; set; } = new();
    }
}
