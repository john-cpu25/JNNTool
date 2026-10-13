using System;
using JNNTool.Tools.Login.Models;
using JNNTool.Tools.Login.Services;
using Xunit;

namespace JNNTool.Tests
{
    public class LoginTests
    {
        [Fact]
        public void DeviceFingerprint_ShouldReturnValid64CharHex()
        {
            string hash = DeviceFingerprintService.GetDeviceIdentifierHash();

            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length);
            // Verify hex format
            Assert.Matches("^[a-f0-9]{64}$", hash);
        }

        [Fact]
        public void DeviceFriendlyName_ShouldNotBeEmpty()
        {
            string name = DeviceFingerprintService.GetDeviceFriendlyName();

            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.Equal(Environment.MachineName, name);
        }

        [Fact]
        public void ClientLicenseInfo_IsExpired_ShouldReturnTrueForPastDate()
        {
            var license = new ClientLicenseInfo
            {
                Email = "test@jnntool.vn",
                Status = "Active",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            Assert.True(license.IsExpired);
            Assert.Equal(0, license.DaysRemaining);
        }

        [Fact]
        public void ClientLicenseInfo_IsExpired_ShouldReturnFalseForFutureDate()
        {
            var license = new ClientLicenseInfo
            {
                Email = "test@jnntool.vn",
                Status = "Active",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
            };

            Assert.False(license.IsExpired);
            Assert.True(license.DaysRemaining >= 29 && license.DaysRemaining <= 31);
        }

        [Fact]
        public void SecureStorage_SaveAndLoad_ShouldRoundtripCorrectly()
        {
            var original = new ClientLicenseInfo
            {
                LicenseId = Guid.NewGuid(),
                Email = "developer@jnntool.vn",
                LicenseType = "Annual",
                Status = "Active",
                StartsAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(355),
                AccessToken = "test-token-jwt-secret-payload",
                DeviceName = DeviceFingerprintService.GetDeviceFriendlyName(),
                DeviceIdentifierHash = DeviceFingerprintService.GetDeviceIdentifierHash(),
                LastValidatedAtUtc = DateTime.UtcNow
            };

            try
            {
                SecureStorageService.SaveLicense(original);
                var loaded = SecureStorageService.LoadLicense();

                Assert.NotNull(loaded);
                Assert.Equal(original.LicenseId, loaded.LicenseId);
                Assert.Equal(original.Email, loaded.Email);
                Assert.Equal(original.LicenseType, loaded.LicenseType);
                Assert.Equal(original.Status, loaded.Status);
                Assert.Equal(original.AccessToken, loaded.AccessToken);
                Assert.Equal(original.DeviceIdentifierHash, loaded.DeviceIdentifierHash);
            }
            finally
            {
                SecureStorageService.ClearLicense();
            }
        }
    }
}
