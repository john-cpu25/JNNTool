using JNNTool.Tools.IFCEtabs.Core.Models;

namespace JNNTool.Tools.IFCEtabs.Core.Services
{
    public interface IElementHashService
    {
        /// <summary>
        /// Tạo chuỗi hash SHA256 nhận diện tính toàn vẹn của cấu kiện.
        /// </summary>
        string ComputeHash(StructuralElement element);
    }
}
