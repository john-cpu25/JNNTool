using System;

namespace JNNTool.Tools.Login.Models
{
    public class ClientLicenseInfo
    {
        public Guid LicenseId { get; set; }
        public string Email { get; set; }
        public string LicenseType { get; set; } // "Trial" or "Annual"
        public string Status { get; set; } // "Active", "Expired", etc.
        public DateTime StartsAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public string AccessToken { get; set; }
        public string DeviceName { get; set; }
        public string DeviceIdentifierHash { get; set; }
        public DateTime LastValidatedAtUtc { get; set; }

        // Cryptographic Signed Ticket properties (Anti-Tamper & Anti-Fiddler)
        public string PayloadJson { get; set; }
        public string SignatureHex { get; set; }
        public DateTime OfflineUntilUtc { get; set; }
        public string RuntimeSeed { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public bool IsOfflineExpired => OfflineUntilUtc != default && DateTime.UtcNow >= OfflineUntilUtc;

        public int DaysRemaining
        {
            get
            {
                if (IsExpired) return 0;
                return (int)Math.Ceiling((ExpiresAtUtc - DateTime.UtcNow).TotalDays);
            }
        }
    }

    public class SupabaseVerifyRpcResponse
    {
        public bool success { get; set; }
        public string payload_json { get; set; }
        public string signature_hex { get; set; }
        public int? days_remaining { get; set; }
        public string message { get; set; }
    }

    public class SupabasePayloadData
    {
        public string email { get; set; }
        public string machine_id { get; set; }
        public string license_type { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime offline_until { get; set; }
        public DateTime server_time { get; set; }
        public string nonce { get; set; }
        public string runtime_seed { get; set; }
    }

    public class ApiActivateRequest
    {
        public string Email { get; set; }
        public string ActivationCode { get; set; }
        public string DeviceIdentifierHash { get; set; }
        public string DeviceName { get; set; }
    }

    public class ApiTrialRequest
    {
        public string Email { get; set; }
        public string DeviceIdentifierHash { get; set; }
        public string DeviceName { get; set; }
    }

    public class ApiValidateRequest
    {
        public Guid LicenseId { get; set; }
        public string DeviceIdentifierHash { get; set; }
    }

    public class ApiDeactivateRequest
    {
        public Guid LicenseId { get; set; }
        public string DeviceIdentifierHash { get; set; }
    }

    public class ApiActivateResponse
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public Guid? LicenseId { get; set; }
        public int? LicenseType { get; set; }
        public int? LicenseStatus { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string Message { get; set; }
    }

    public class ApiValidateResponse
    {
        public bool IsValid { get; set; }
        public int? LicenseType { get; set; }
        public int? LicenseStatus { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public int DaysRemaining { get; set; }
        public string Message { get; set; }
    }

    public class ApiDeactivateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
