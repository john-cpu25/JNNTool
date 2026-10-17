using System;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using JNNTool.Tools.Login.UI;

namespace JNNTool.Tools.Login.Services
{
    public static class LicenseGuard
    {
        private static DateTime _lastCheckedUtc = DateTime.MinValue;
        private static bool _lastCheckResult = false;
        private static readonly TimeSpan CacheInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Khóa nghiệp vụ RuntimeSeed được giải mã từ chữ ký số máy chủ Supabase.
        /// Áp dụng kỹ thuật Data Dependency: nếu kẻ xấu dùng dnSpy xóa dòng EnsureLicensed, 
        /// biến này sẽ mang giá trị rỗng/mặc định khiến các hàm dựng thép tính sai thông số.
        /// </summary>
        public static string RuntimeSeed => LicenseClientService.Instance.CurrentLicense?.RuntimeSeed ?? "JNN_DEFAULT_SEED";

        /// <summary>
        /// Kiểm tra trạng thái bản quyền của máy trạm.
        /// Nếu chưa kích hoạt hoặc hết hạn, hiển thị bảng đăng nhập và trả về false để chặn lệnh.
        /// </summary>
        public static bool EnsureLicensed(UIApplication uiapp = null)
        {
            var client = LicenseClientService.Instance;

            // Kiểm tra cục bộ tức thì qua DPAPI và HMAC-SHA256
            if (!client.IsLicensed)
            {
                ShowActivationPrompt(uiapp);
                return false;
            }

            // Đồng bộ ngầm với Supabase định kỳ mỗi 15 phút (Anti-Clock Tampering)
            if (DateTime.UtcNow - _lastCheckedUtc > CacheInterval)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var (valid, _) = await client.ValidateCurrentLicenseAsync();
                        _lastCheckResult = valid;
                        _lastCheckedUtc = DateTime.UtcNow;
                    }
                    catch { }
                });
            }

            return true;
        }

        public static void ShowActivationPrompt(UIApplication uiapp = null)
        {
            try
            {
                LoginWindow.ShowWindow(uiapp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LicenseGuard] Error showing login window: {ex.Message}");
            }
        }
    }
}
