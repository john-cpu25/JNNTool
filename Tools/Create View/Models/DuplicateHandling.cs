namespace JNNTool.Tools.CreateView.Models
{
    public enum DuplicateHandling
    {
        /// <summary>
        /// Bỏ qua không tạo nếu View cùng tên đã tồn tại
        /// </summary>
        SkipIfExists,

        /// <summary>
        /// Tự động thêm hậu tố số tăng dần (ví dụ .1, .2) nếu trùng tên
        /// </summary>
        AddSuffixNumber,

        /// <summary>
        /// Nếu View đã tồn tại, áp dụng lại View Template và cập nhật thông số cho view đó
        /// </summary>
        UpdateExisting
    }
}
