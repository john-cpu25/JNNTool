namespace JNNTool.Tools.StairModel.Engine
{
    /// <summary>
    /// Các dạng hình học cầu thang kết cấu.
    /// </summary>
    public enum StairType
    {
        Straight = 0,   // Thang thẳng (1 vế hoặc 2 vế thẳng có chiếu nghỉ)
        UShaped = 1,    // Thang chữ U (2 vế song song đảo chiều 180° có chiếu nghỉ giữa)
        LShaped = 2     // Thang chữ L (2 vế vuông góc 90° có chiếu nghỉ góc)
    }

    /// <summary>
    /// Hướng rẽ của thang chữ L hoặc chiều quay của thang chữ U.
    /// </summary>
    public enum TurnDirection
    {
        Left = 0,       // Rẽ trái (ngược chiều kim đồng hồ)
        Right = 1       // Rẽ phải (thuận chiều kim đồng hồ)
    }

    /// <summary>
    /// Tùy chọn bố trí dầm đỡ kết cấu cho thang.
    /// </summary>
    public enum SupportBeamOption
    {
        None = 0,               // Không tạo dầm
        LandingBeamOnly = 1,    // Chỉ tạo dầm đỡ chiếu nghỉ giữa
        FullBeams = 2           // Tạo đủ dầm chân thang, dầm chiếu nghỉ và dầm đỉnh thang
    }
}
