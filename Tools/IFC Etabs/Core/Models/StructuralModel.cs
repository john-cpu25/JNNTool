using System;
using System.Collections.Generic;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Container chứa toàn bộ mô hình kết cấu trung gian JNN được trích xuất từ ETABS hoặc IFC.
    /// </summary>
    public class StructuralModel
    {
        public string ModelName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string SourceUnits { get; set; } = "kN_m_C";
        public DateTime ExtractedAt { get; set; } = DateTime.Now;

        // Danh sách cấu kiện kết cấu
        public List<ETABSLevel> Levels { get; } = new();
        public List<ETABSGrid> Grids { get; } = new();
        public List<ETABSBeam> Beams { get; } = new();
        public List<ETABSColumn> Columns { get; } = new();
        public List<ETABSWall> Walls { get; } = new();
        public List<ETABSSlab> Slabs { get; } = new();

        // Từ điển tra cứu tiết diện và vật liệu
        public Dictionary<string, ETABSSection> Sections { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, ETABSMaterial> Materials { get; } = new(StringComparer.OrdinalIgnoreCase);

        // Tổng hợp thống kê
        public int TotalElements => Levels.Count + Grids.Count + Beams.Count + Columns.Count + Walls.Count + Slabs.Count;

        public override string ToString() =>
            $"Model: {ModelName} (Levels={Levels.Count}, Grids={Grids.Count}, Columns={Columns.Count}, Beams={Beams.Count}, Walls={Walls.Count}, Slabs={Slabs.Count})";
    }
}
