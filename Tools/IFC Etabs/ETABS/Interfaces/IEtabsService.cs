using System;
using JNNTool.Tools.IFCEtabs.Core.Models;

namespace JNNTool.Tools.IFCEtabs.ETABS.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ kết nối và trích xuất dữ liệu từ phần mềm ETABS.
    /// </summary>
    public interface IEtabsService : IDisposable
    {
        /// <summary>
        /// Kiểm tra trạng thái kết nối tới ETABS.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Đường dẫn hoặc tên file mô hình ETABS đang mở.
        /// </summary>
        string ModelPath { get; }

        /// <summary>
        /// Kết nối tới phiên bản ETABS đang chạy trên máy tính.
        /// </summary>
        bool Connect(out string message);

        /// <summary>
        /// Ngắt kết nối khỏi ETABS.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Trích xuất toàn bộ dữ liệu kết cấu sang JNN StructuralModel.
        /// </summary>
        StructuralModel ExtractModel(Action<string, double>? progressCallback = null);
    }
}
