using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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

        // Supabase configuration
        public const string DefaultSupabaseUrl = "https://srcijpacycgfkyneoxyu.supabase.co";
        public const string DefaultPublishableKey = "sb_publishable_31xEGAXxKSJcJLXGOb5wgw_IXv9E_KH";
        private static readonly byte[] HmacSecret = Encoding.UTF8.GetBytes("JNN_TOOL_SECRET_SALT_2026_SECURE_KEY");

        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public ClientLicenseInfo CurrentLicense { get; private set; }
        public string ServerBaseUrl { get; set; }

        private LicenseClientService()
        {
            string envUrl = Environment.GetEnvironmentVariable("JNNTOOL_LICENSE_API_URL");
            ServerBaseUrl = !string.IsNullOrWhiteSpace(envUrl)
                ? envUrl.TrimEnd('/')
                : DefaultSupabaseUrl;

            // CHỐNG BẮT GÓI TIN: Tắt Proxy hệ thống để Fiddler / Charles / HTTP Toolkit không chặn được request
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                Proxy = null
            };

            _http = new HttpClient(handler)
            {
                BaseAddress = new Uri(ServerBaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };

            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Add("apikey", DefaultPublishableKey);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DefaultPublishableKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Restore cached license from Windows DPAPI storage
            CurrentLicense = SecureStorageService.LoadLicense();
        }

        public bool IsLicensed
        {
            get
            {
                if (CurrentLicense == null) return false;
                if (CurrentLicense.IsExpired) return false;
                if (CurrentLicense.IsOfflineExpired) return false;
                if (!string.Equals(CurrentLicense.Status, "Active", StringComparison.OrdinalIgnoreCase)) return false;

                // Kiểm tra khóa cứng phần cứng (Node-locked)
                string currentMachineId = DeviceFingerprintService.GetDeviceIdentifierHash();
                if (!string.IsNullOrEmpty(CurrentLicense.DeviceIdentifierHash) &&
                    !string.Equals(CurrentLicense.DeviceIdentifierHash, currentMachineId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Kiểm tra chữ ký số chống chỉnh sửa file tại chỗ
                if (!string.IsNullOrEmpty(CurrentLicense.SignatureHex) && !string.IsNullOrEmpty(CurrentLicense.PayloadJson))
                {
                    if (!VerifySignature(CurrentLicense.PayloadJson, CurrentLicense.SignatureHex))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Xác thực chữ ký số HMAC-SHA256 của gói dữ liệu trả về từ Supabase
        /// </summary>
        public static bool VerifySignature(string payloadJson, string signatureHex)
        {
            if (string.IsNullOrEmpty(payloadJson) || string.IsNullOrEmpty(signatureHex))
                return false;

            try
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
                using (var hmac = new HMACSHA256(HmacSecret))
                {
                    byte[] hash = hmac.ComputeHash(payloadBytes);
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return string.Equals(sb.ToString(), signatureHex, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gọi Supabase RPC `verify_license_rpc` với Nonce duy nhất để chống Replay Attack
        /// </summary>
        public async Task<(bool success, string message, SupabasePayloadData data)> CallVerifyRpcAsync(
            string email, string password, string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Vui lòng nhập Email tài khoản.", null);

            string machineId = DeviceFingerprintService.GetDeviceIdentifierHash();
            string deviceName = DeviceFingerprintService.GetDeviceFriendlyName();
            string nonce = Guid.NewGuid().ToString("N"); // Mã ngẫu nhiên duy nhất cho lượt gửi này

            var req = new
            {
                p_email = email.Trim().ToLowerInvariant(),
                p_password = password ?? string.Empty,
                p_machine_id = machineId,
                p_device_name = deviceName,
                p_nonce = nonce,
                p_license_key = string.IsNullOrWhiteSpace(licenseKey) ? null : licenseKey.Trim().ToUpperInvariant()
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(req);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/rest/v1/rpc/verify_license_rpc", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JsonSerializer.Deserialize<SupabaseVerifyRpcResponse>(responseBody, _jsonOptions);

                if (res != null && res.success)
                {
                    // 1. Kiểm tra chữ ký số chống giả mạo gói tin (Fiddler / MITM)
                    if (!VerifySignature(res.payload_json, res.signature_hex))
                    {
                        return (false, "Phát hiện gói tin bản quyền bị can thiệp hoặc giả mạo (Chữ ký số không khớp)!", null);
                    }

                    // 2. Phân tích nội dung Payload đã được máy chủ chứng thực
                    var payloadData = JsonSerializer.Deserialize<SupabasePayloadData>(res.payload_json, _jsonOptions);
                    if (payloadData == null)
                    {
                        return (false, "Dữ liệu bản quyền từ máy chủ không hợp lệ.", null);
                    }

                    // 3. Kiểm tra Nonce chống tấn công phát lại (Replay Attack)
                    if (payloadData.nonce != nonce)
                    {
                        return (false, "Lỗi bảo mật: Mã phiên Nonce không trùng khớp!", null);
                    }

                    // 4. Kiểm tra khóa cứng phần cứng (Node-locked)
                    if (!string.Equals(payloadData.machine_id, machineId, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "Bản quyền này không dành cho máy tính hiện tại!", null);
                    }

                    // 5. Cập nhật và lưu trữ an toàn bằng Windows DPAPI
                    CurrentLicense = new ClientLicenseInfo
                    {
                        LicenseId = Guid.NewGuid(),
                        Email = payloadData.email,
                        LicenseType = payloadData.license_type,
                        Status = "Active",
                        StartsAtUtc = payloadData.server_time,
                        ExpiresAtUtc = payloadData.expires_at,
                        OfflineUntilUtc = payloadData.offline_until,
                        RuntimeSeed = payloadData.runtime_seed,
                        PayloadJson = res.payload_json,
                        SignatureHex = res.signature_hex,
                        DeviceName = deviceName,
                        DeviceIdentifierHash = machineId,
                        LastValidatedAtUtc = DateTime.UtcNow
                    };

                    SecureStorageService.SaveLicense(CurrentLicense);
                    return (true, res.message ?? "Xác thực bản quyền thành công!", payloadData);
                }

                return (false, res?.message ?? "Không thể xác thực bản quyền.", null);
            }
            catch (TaskCanceledException)
            {
                return (false, "Kết nối tới máy chủ bị quá thời gian (Timeout). Vui lòng kiểm tra lại mạng.", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Không thể kết nối máy chủ Supabase: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xử lý xác thực: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Đăng nhập tài khoản bằng Email + Mật khẩu
        /// </summary>
        public async Task<(bool success, string message)> LoginWithPasswordAsync(string email, string password)
        {
            var (success, msg, _) = await CallVerifyRpcAsync(email, password, licenseKey: null);
            return (success, msg);
        }

        /// <summary>
        /// Kích hoạt mã Key mới cho tài khoản (có thể kèm mật khẩu hoặc không)
        /// </summary>
        public async Task<(bool success, string message)> ActivateAsync(string email, string activationCode, string password = null)
        {
            if (string.IsNullOrWhiteSpace(activationCode))
                return (false, "Vui lòng nhập mã Key bản quyền.");

            var (success, msg, _) = await CallVerifyRpcAsync(email, password, licenseKey: activationCode);
            return (success, msg);
        }

        /// <summary>
        /// Kiểm tra và đồng bộ lại bản quyền hiện tại ngầm từ máy chủ
        /// </summary>
        public async Task<(bool isValid, string message)> ValidateCurrentLicenseAsync()
        {
            if (CurrentLicense == null || string.IsNullOrWhiteSpace(CurrentLicense.Email))
            {
                return (false, "Chưa kích hoạt bản quyền trên máy này.");
            }

            try
            {
                var (success, msg, _) = await CallVerifyRpcAsync(CurrentLicense.Email, password: null, licenseKey: null);

                if (success)
                {
                    return (true, "Bản quyền hợp lệ.");
                }

                // Nếu server báo hết hạn hoặc thu hồi
                CurrentLicense.Status = "Expired";
                SecureStorageService.SaveLicense(CurrentLicense);
                return (false, msg);
            }
            catch (Exception ex)
            {
                // Khi mất mạng: nếu còn trong thời hạn Offline Grace Period thì vẫn cho phép dùng
                if (!CurrentLicense.IsExpired && !CurrentLicense.IsOfflineExpired)
                {
                    return (true, "Bản quyền hợp lệ (Chế độ Offline tạm thời).");
                }

                return (false, $"Không thể xác thực: {ex.Message}");
            }
        }

        /// <summary>
        /// Đăng xuất / Xóa thông tin bản quyền trên máy tính này
        /// </summary>
        public Task<(bool success, string message)> DeactivateAsync()
        {
            SecureStorageService.ClearLicense();
            CurrentLicense = null;
            return Task.FromResult((true, "Đã đăng xuất và xóa thông tin bản quyền trên máy."));
        }

        /// <summary>
        /// Gửi yêu cầu đăng ký (ghi chú cho người dùng liên hệ Admin)
        /// </summary>
        public Task<(bool success, string message, string activationCode)> RegisterAccountRequestAsync(
            string email, string fullName, string phoneNumber, string machineId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Task.FromResult((false, "Vui lòng nhập Email.", (string)null));

            return Task.FromResult((true, 
                "Vui lòng cung cấp Email và Machine ID cho Quản trị viên để được cấp Key kích hoạt!", 
                (string)null));
        }
    }
}
