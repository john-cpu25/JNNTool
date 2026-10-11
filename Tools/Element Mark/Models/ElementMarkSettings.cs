using System.Collections.Generic;

namespace JNNTool.Tools.ElementMark.Models
{
    /// <summary>
    /// Cấu hình tool Element Mark, lưu tại %AppData%\JNNTool\Settings\ElementMarkSettings.json.
    /// Token template: {P} tiền tố, {L} mã tầng, {D} phương, {N} số thứ tự, {S} số nhịp.
    /// Thêm ":00" để đệm số, ví dụ {N:00} → 01.
    /// </summary>
    public class ElementMarkSettings
    {
        // ── Chung ─────────────────────────────────────────────
        public MarkScope Scope { get; set; } = MarkScope.ActiveView;
        public OverwriteMode Overwrite { get; set; } = OverwriteMode.All;
        public string TargetParameter { get; set; } = "Mark";

        // ── Dầm ───────────────────────────────────────────────
        public bool BeamEnabled { get; set; } = true;
        public string BeamPrefix { get; set; } = "DS";
        public string BeamTemplate { get; set; } = "{P}{L}{D}{N:00}";
        /// <summary>Hậu tố chỉ thêm khi dầm liên tục nhiều nhịp: DS1X01 → DS1X01.1, DS1X01.2...</summary>
        public string BeamSpanSuffix { get; set; } = ".{S}";
        public string BeamAxisXCode { get; set; } = "X";
        public string BeamAxisYCode { get; set; } = "Y";
        public int BeamStartNumber { get; set; } = 1;
        public VerticalOrder BeamVertical { get; set; } = VerticalOrder.TopToBottom;
        public HorizontalOrder BeamHorizontal { get; set; } = HorizontalOrder.LeftToRight;
        public bool BeamMergeSpans { get; set; } = true;
        /// <summary>Khe hở tối đa giữa hai nhịp thẳng hàng để coi là dầm liên tục (bề rộng cột lớn nhất).</summary>
        public double BeamChainGapMm { get; set; } = 1000;
        /// <summary>Hai dầm lệch nhau theo phương vuông góc không quá giá trị này thì cùng một hàng.</summary>
        public double BeamRowToleranceMm { get; set; } = 200;

        // ── Cột ───────────────────────────────────────────────
        public bool ColumnEnabled { get; set; } = true;
        public string ColumnPrefix { get; set; } = "C";
        public string ColumnTemplate { get; set; } = "{P}{N}";
        public int ColumnStartNumber { get; set; } = 1;
        public VerticalOrder ColumnVertical { get; set; } = VerticalOrder.TopToBottom;
        public HorizontalOrder ColumnHorizontal { get; set; } = HorizontalOrder.LeftToRight;
        public ColumnPriority ColumnPriority { get; set; } = ColumnPriority.RowFirst;
        /// <summary>Cột tầng trên lệch tâm so với cột tầng dưới không quá giá trị này thì cùng một chuỗi (cùng Mark).</summary>
        public double ColumnStackToleranceMm { get; set; } = 300;
        public double ColumnRowToleranceMm { get; set; } = 300;

        // ── Sàn ───────────────────────────────────────────────
        public bool FloorEnabled { get; set; } = true;
        public string FloorPrefix { get; set; } = "S";
        public string FloorTemplate { get; set; } = "{P}{L}";
        public bool FloorStructuralOnly { get; set; } = true;

        // ── Mã tầng do người dùng sửa (tên Level → mã) ─────────
        public Dictionary<string, string> LevelCodes { get; set; } = new Dictionary<string, string>();
    }
}
