namespace JNNTool.Tools.AutoDimGrid.Models
{
    /// <summary>
    /// Cài đặt Auto Dim Grid, lưu tại %AppData%\JNNTool\Settings\AutoDimGridSettings.json.
    /// </summary>
    public class AutoDimGridSettings
    {
        /// <summary>0 = Dim thủ công, 1 = Dim tự động 4 cạnh.</summary>
        public int LastTabIndex { get; set; } = 1;

        /// <summary>2 = Trục + Tổng, 3 = Tường + Trục + Tổng.</summary>
        public int LayersCount { get; set; } = 2;

        public bool IsOffsetFromGridEndpoint { get; set; } = false;

        /// <summary>mm trên bản in.</summary>
        public double DistanceToFirstDimMm { get; set; } = 15.0;

        /// <summary>mm trên bản in.</summary>
        public double DistanceBetweenDimsMm { get; set; } = 8.0;

        /// <summary>Lưu theo tên vì ElementId khác nhau giữa các dự án.</summary>
        public string DimTypeName { get; set; } = string.Empty;
    }
}
