namespace JNN.Stair.Models
{
    /// <summary>
    /// Các dạng hình học cầu thang chuẩn
    /// </summary>
    public enum StairPreset
    {
        Straight,           // Thang thẳng 1 vế
        LShape,             // Thang chữ L (2 vế vuông góc)
        UShape,             // Thang chữ U (2 vế song song)
        LShapeWinder,       // Thang chữ L có bậc xòe rẻ quạt ở góc
        ThreeFlightUWinder, // Thang 3 vế chữ U có bậc xòe rẻ quạt
        ThreeFlightU,       // Thang 3 vế chữ U có 2 chiếu nghỉ phẳng
        CustomDetailPlan    // Dựng tùy biến từ nét 2D CAD / Detail Lines
    }

    /// <summary>
    /// Phương thức tính toán số bậc và chiều cao bậc
    /// </summary>
    public enum StairInputMode
    {
        ByPreferredRiser,   // Theo chiều cao bậc mong muốn (tự tính số bậc chẵn)
        ByStepCount,        // Theo số bậc cố định (ví dụ 21, 23, 25 bậc)
        ByFlightLength      // Khóa theo chiều dài ô thang thực tế
    }

    /// <summary>
    /// Phân loại góc gập của bản thang
    /// </summary>
    public enum CornerType
    {
        Flat,               // Đoạn thẳng liên tục
        Convex,             // Góc lồi (ôm cua ngoài)
        Concave             // Góc lõm (cua trong - bắt buộc đan chéo thép)
    }

    /// <summary>
    /// Lớp cốt thép cầu thang
    /// </summary>
    public enum StairRebarLayer
    {
        BottomMain,         // Thép dọc lớp dưới (chịu kéo chính)
        TopMain,            // Thép dọc lớp trên (mô-men âm / thép mũ)
        Distribution        // Thép phân bố ngang
    }
}
