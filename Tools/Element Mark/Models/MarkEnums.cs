namespace JNNTool.Tools.ElementMark.Models
{
    /// <summary>Phạm vi lấy phần tử.</summary>
    public enum MarkScope
    {
        ActiveView,
        Project
    }

    /// <summary>Cách xử lý phần tử đã có Mark.</summary>
    public enum OverwriteMode
    {
        All,
        EmptyOnly
    }

    /// <summary>Thứ tự theo phương đứng của màn hình (trục Up của view).</summary>
    public enum VerticalOrder
    {
        TopToBottom,
        BottomToTop
    }

    /// <summary>Thứ tự theo phương ngang của màn hình (trục Right của view).</summary>
    public enum HorizontalOrder
    {
        LeftToRight,
        RightToLeft
    }

    /// <summary>Cột: đánh hết một hàng rồi mới sang hàng kế tiếp, hay đánh hết một cột dọc.</summary>
    public enum ColumnPriority
    {
        RowFirst,
        ColumnFirst
    }

    public enum BeamAxis
    {
        X,
        Y
    }

    public enum MarkCategory
    {
        Beam,
        Column,
        Floor
    }
}
