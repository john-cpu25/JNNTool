using System;

namespace JNNTool.Tools.AutoDimColumn.Models
{
    public enum ColumnPlacementSide
    {
        Auto = 0,
        TopLeft = 1,
        BottomRight = 2
    }

    public enum ColumnScopeMode
    {
        ActiveView = 0,
        SelectedElements = 1
    }

    /// <summary>
    /// Cài đặt cấu hình cho JNN Auto Column Dimension.
    /// Lưu trữ tự động tại %AppData%\JNNTool\Settings\AutoDimColumnSettings.json.
    /// </summary>
    public class AutoDimColumnSettings
    {
        public ColumnScopeMode ScopeMode { get; set; } = ColumnScopeMode.ActiveView;

        public bool DimWidth { get; set; } = true;
        public bool DimLength { get; set; } = true;
        public bool DimGridToColumn { get; set; } = true;
        public bool IncludeOppositeFace { get; set; } = true;
        public bool SkipExisting { get; set; } = true;

        public string SelectedDimensionTypeId { get; set; } = string.Empty;
        public ColumnPlacementSide PlacementSide { get; set; } = ColumnPlacementSide.Auto;

        public double BaseOffsetMm { get; set; } = 350.0;
        public double ChainSpacingMm { get; set; } = 250.0;
        public double GridSearchRangeMm { get; set; } = 2500.0;
        public double AngleToleranceDeg { get; set; } = 5.0;
    }
}
