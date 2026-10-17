namespace JNNTool.Tools.UIManager.Models
{
    /// <summary>
    /// Danh mục 50 biểu tượng vector đại diện cho từng công cụ trong JNNTool theo bộ thiết kế JNN BIM Vector Icons.
    /// </summary>
    public enum JNNIcon
    {
        // 1. General & Base Elements (34)
        Slab,
        Beam,
        Column,
        Wall,
        Foundation,
        Pile,
        Stair,
        Floor,
        Section,
        Cad,
        ScopeBox,
        Align,
        Join,
        Connection,
        Clash,
        Filter,
        Palette,
        Dim,
        Extend,
        ViewSheet,
        Mark,
        Quantity,
        Check,
        Refresh,
        Settings,
        Key,
        Split,
        DisallowJoin,
        Flash,
        Target,
        Eye,
        CommandPalette,
        DimColumn,
        StairDesigner,

        // 2. JNN Rebar Suite (12 - TCVN 5574:2018)
        RebarBeam,
        RebarColumn,
        RebarSlab,
        RebarWall,
        RebarFooting,
        RebarPile,
        RebarStair,
        RebarVolume,
        RebarNumber,
        RebarUnobscured,
        RebarIsolate,
        RebarTypeManager,

        // 3. Extended Modeling & CSI Analysis (4)
        FloorByRoom,
        DimBeam,
        EtabsConverter,
        PropertyViewer
    }
}
