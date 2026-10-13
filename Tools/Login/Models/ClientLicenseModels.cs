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

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

        public int DaysRemaining
        {
            get
            {
                if (IsExpired) return 0;
                return (int)Math.Ceiling((ExpiresAtUtc - DateTime.UtcNow).TotalDays);
            }
        }
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
