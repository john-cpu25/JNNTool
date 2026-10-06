namespace JNNTool.Tools.IFCEtabs.Core.Enums
{
    /// <summary>
    /// Loại cấu kiện kết cấu trong mô hình JNN.
    /// </summary>
    public enum StructuralElementType
    {
        Level,
        Grid,
        Column,
        Beam,
        Wall,
        Slab,
        Foundation
    }

    /// <summary>
    /// Nguồn trích xuất mô hình.
    /// </summary>
    public enum SourceType
    {
        EtabsApi,
        IfcFile
    }

    /// <summary>
    /// Trạng thái đồng bộ đối tượng với mô hình Revit.
    /// </summary>
    public enum ConversionStatus
    {
        New,
        Updated,
        Unchanged,
        Deleted,
        Conflict,
        Error
    }

    /// <summary>
    /// Hình dạng tiết diện cấu kiện.
    /// </summary>
    public enum SectionShapeType
    {
        Rectangular,
        Circular,
        ISection,
        TSection,
        Angle,
        Channel,
        Box,
        General,
        Custom
    }

    /// <summary>
    /// Điểm tim đặt dầm/cột (Cardinal Point theo chuẩn CSI / IFC).
    /// </summary>
    public enum CardinalPoint
    {
        BottomLeft = 1,
        BottomCenter = 2,
        BottomRight = 3,
        MiddleLeft = 4,
        MiddleCenter = 5,
        MiddleRight = 6,
        TopLeft = 7,
        TopCenter = 8,
        TopRight = 9,
        Centroid = 10,
        ShearCenter = 11
    }
}
