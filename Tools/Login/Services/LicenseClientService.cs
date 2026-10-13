using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JNNTool.Tools.Login.Models;

namespace JNNTool.Tools.Login.Services
{
    public class LicenseClientService
    {
        private static readonly Lazy<LicenseClientService> _instance = new Lazy<LicenseClientService>(() => new LicenseClientService());
        public static LicenseClientService Instance => _instance.Value;

        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public ClientLicenseInfo CurrentLicense { get; private set; }
        public string ServerBaseUrl { get; set; }

        private LicenseClientService()
        {
            // Base API URL can be configured through environment variable JNNTOOL_LICENSE_API_URL
            string envUrl = Environment.GetEnvironmentVariable("JNNTOOL_LICENSE_API_URL");
            ServerBaseUrl = !string.IsNullOrWhiteSpace(envUrl) 
                ? envUrl.TrimEnd('/') 
                : "https://jnnchecking.onrender.com";

            _http = new HttpClient
            {
                BaseAddress = new Uri(ServerBaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Restore cached license from DPAPI storage
            CurrentLicense = SecureStorageService.LoadLicense();
        }

        public bool IsLicensed
        {
            get
            {
                if (CurrentLicense == null) return false;
                if (CurrentLicense.IsExpired) return false;
                return string.Equals(CurrentLicense.Status, "Active", StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task<(bool success, string message)> ActivateAsync(string email, string activationCode)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Vui lòng nhập Email.");
            if (string.IsNullOrWhiteSpace(activationCode))
                return (false, "Vui lòng nhập mã kích hoạt (GUID Activation Code).");

            string deviceHash = DeviceFingerprintService.GetDeviceIdentifierHash();
            string deviceName = DeviceFingerprintService.GetDeviceFriendlyName();

            var req = new ApiActivateRequest
            {
                Email = email.Trim(),
                ActivationCode = activationCode.Trim(),
                DeviceIdentifierHash = deviceHash,
                DeviceName = deviceName
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(req);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/api/auth/activate", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JsonSerializer.Deserialize<ApiActivateResponse>(responseBody, _jsonOptions);

                if (res != null && res.Success && res.LicenseId.HasValue)
                {
                    CurrentLicense = new ClientLicenseInfo
                    {
                        LicenseId = res.LicenseId.Value,
                        Email = email.Trim(),
                        LicenseType = res.LicenseType == 1 ? "Trial" : "Annual",
                        Status = "Active",
                        StartsAtUtc = res.StartsAtUtc ?? DateTime.UtcNow,
                        ExpiresAtUtc = res.ExpiresAtUtc ?? DateTime.UtcNow.AddYears(1),
                        AccessToken = res.AccessToken,
                        DeviceName = deviceName,
                        DeviceIdentifierHash = deviceHash,
                        LastValidatedAtUtc = DateTime.UtcNow
                    };

                    SecureStorageService.SaveLicense(CurrentLicense);
                    return (true, res.Message ?? "Kích hoạt bản quyền thành công!");
                }

                return (false, res?.Message ?? "Kích hoạt không thành công. Vui lòng kiểm tra lại thông tin.");
            }
            catch (TaskCanceledException)
            {
                return (false, "Kết nối tới máy chủ cấp phép bị quá thời gian (Timeout). Vui lòng kiểm tra lại mạng.");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Không thể kết nối tới máy chủ license ({ServerBaseUrl}): {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xử lý kích hoạt: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> StartTrialAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Vui lòng nhập Email để nhận bản quyền dùng thử.");

            string deviceHash = DeviceFingerprintService.GetDeviceIdentifierHash();
            string deviceName = DeviceFingerprintService.GetDeviceFriendlyName();

            var req = new ApiTrialRequest
            {
                Email = email.Trim(),
                DeviceIdentifierHash = deviceHash,
                DeviceName = deviceName
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(req);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/api/auth/trial", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JsonSerializer.Deserialize<ApiActivateResponse>(responseBody, _jsonOptions);

                if (res != null && res.Success && res.LicenseId.HasValue)
                {
                    CurrentLicense = new ClientLicenseInfo
                    {
                        LicenseId = res.LicenseId.Value,
                        Email = email.Trim(),
                        LicenseType = "Trial",
                        Status = "Active",
                        StartsAtUtc = res.StartsAtUtc ?? DateTime.UtcNow,
                        ExpiresAtUtc = res.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(15),
                        AccessToken = res.AccessToken,
                        DeviceName = deviceName,
                        DeviceIdentifierHash = deviceHash,
                        LastValidatedAtUtc = DateTime.UtcNow
                    };

                    SecureStorageService.SaveLicense(CurrentLicense);
                    return (true, "Đã kích hoạt bản quyền dùng thử 15 ngày thành công!");
                }

                return (false, res?.Message ?? "Không thể tạo bản quyền dùng thử.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối máy chủ: {ex.Message}");
            }
        }

        public async Task<(bool isValid, string message)> ValidateCurrentLicenseAsync()
        {
            if (CurrentLicense == null)
            {
                return (false, "Chưa kích hoạt bản quyền trên máy này.");
            }

            string deviceHash = DeviceFingerprintService.GetDeviceIdentifierHash();
            var req = new ApiValidateRequest
            {
                LicenseId = CurrentLicense.LicenseId,
                DeviceIdentifierHash = deviceHash
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(req);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Attach token if present
                if (!string.IsNullOrEmpty(CurrentLicense.AccessToken))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentLicense.AccessToken);
                }

                var response = await _http.PostAsync("/api/license/validate", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JsonSerializer.Deserialize<ApiValidateResponse>(responseBody, _jsonOptions);

                if (res != null && res.IsValid)
                {
                    CurrentLicense.Status = "Active";
                    if (res.ExpiresAtUtc.HasValue) CurrentLicense.ExpiresAtUtc = res.ExpiresAtUtc.Value;
                    CurrentLicense.LastValidatedAtUtc = DateTime.UtcNow;
                    SecureStorageService.SaveLicense(CurrentLicense);

                    return (true, "Bản quyền hợp lệ.");
                }

                // If expired or deactivated
                if (res != null)
                {
                    CurrentLicense.Status = res.LicenseStatus == 3 ? "Expired" : "Invalid";
                    if (res.ExpiresAtUtc.HasValue) CurrentLicense.ExpiresAtUtc = res.ExpiresAtUtc.Value;
                    SecureStorageService.SaveLicense(CurrentLicense);

                    return (false, res.Message ?? "Bản quyền đã hết hạn hoặc không còn hiệu lực.");
                }

                return (false, "Xác thực bản quyền không thành công.");
            }
            catch (Exception ex)
            {
                // Strict 0-day offline grace rule: network failure cannot authorize protected operations
                return (false, $"Không thể kết nối đến máy chủ để xác thực bản quyền: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeactivateAsync()
        {
            if (CurrentLicense == null)
            {
                return (true, "Không có bản quyền nào để hủy kích hoạt.");
            }

            string deviceHash = DeviceFingerprintService.GetDeviceIdentifierHash();
            var req = new ApiDeactivateRequest
            {
                LicenseId = CurrentLicense.LicenseId,
                DeviceIdentifierHash = deviceHash
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(req);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(CurrentLicense.AccessToken))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentLicense.AccessToken);
                }

                var response = await _http.PostAsync("/api/license/deactivate", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JsonSerializer.Deserialize<ApiDeactivateResponse>(responseBody, _jsonOptions);

                // Clear local storage on deactivation
                SecureStorageService.ClearLicense();
                CurrentLicense = null;

                return (true, res?.Message ?? "Thiết bị đã được hủy kích hoạt thành công.");
            }
            catch (Exception ex)
            {
                // Fallback: Clear locally even if offline
                SecureStorageService.ClearLicense();
                CurrentLicense = null;
                return (true, $"Đã xóa thông tin bản quyền trên máy (Lưu ý: Máy chủ có thể chưa cập nhật do lỗi mạng: {ex.Message}).");
            }
        }

        public async Task<(bool success, string message, string activationCode)> RegisterAccountRequestAsync(string email, string fullName, string phoneNumber, string machineId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Vui lòng nhập Email.", null);

            var req = new
            {
                email = email.Trim(),
                fullName = fullName?.Trim() ?? string.Empty,
                phoneNumber = phoneNumber?.Trim() ?? string.Empty,
                machineId = machineId ?? DeviceFingerprintService.GetDisplayMachineId(),
                deviceName = DeviceFingerprintService.GetDeviceFriendlyName()
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(req);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/api/auth/register-request", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                bool success = root.GetProperty("success").GetBoolean();
                string message = root.GetProperty("message").GetString() ?? string.Empty;
                string actCode = null;
                if (root.TryGetProperty("activationCode", out var codeElem) && codeElem.ValueKind == JsonValueKind.String)
                {
                    actCode = codeElem.GetString();
                }

                return (success, message, actCode);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối máy chủ: {ex.Message}", null);
            }
        }
    }
}
