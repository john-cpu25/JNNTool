using System;
using System.Collections.Generic;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    public class ConversionOptions
    {
        public bool ImportLevels { get; set; } = true;
        public bool ImportGrids { get; set; } = true;
        public bool ImportColumns { get; set; } = true;
        public bool ImportBeams { get; set; } = true;
        public bool ImportWalls { get; set; } = true;
        public bool ImportSlabs { get; set; } = true;
        public bool UpdateExisting { get; set; } = true;
        public bool AutoCreateTypes { get; set; } = true;
        public double ElevationToleranceMm { get; set; } = 2.0;
    }

    public class ConversionReportResult
    {
        public int LevelsCreated { get; set; }
        public int GridsCreated { get; set; }
        public int ColumnsCreated { get; set; }
        public int ColumnsUpdated { get; set; }
        public int BeamsCreated { get; set; }
        public int BeamsUpdated { get; set; }
        public int WallsCreated { get; set; }
        public int SlabsCreated { get; set; }
        public int ErrorsCount { get; set; }
        public List<string> ErrorMessages { get; } = new();

        public override string ToString() =>
            $"Cột (+{ColumnsCreated}, ~{ColumnsUpdated}) | Dầm (+{BeamsCreated}, ~{BeamsUpdated}) | Vách (+{WallsCreated}) | Sàn (+{SlabsCreated}) | Tầng (+{LevelsCreated}) | Lỗi: {ErrorsCount}";
    }
}
