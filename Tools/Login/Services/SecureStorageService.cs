using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNNTool.Tools.Login.Models;

namespace JNNTool.Tools.Login.Services
{
    public static class SecureStorageService
    {
        private static readonly string StorageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JNNTool");

        private static readonly string LicenseFilePath = Path.Combine(StorageDirectory, "license.dat");
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("JNNTool_Revit_DPAPI_Entropy_2026");

        public static void SaveLicense(ClientLicenseInfo licenseInfo)
        {
            try
            {
                if (!Directory.Exists(StorageDirectory))
                {
                    Directory.CreateDirectory(StorageDirectory);
                }

                string json = JsonSerializer.Serialize(licenseInfo);
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);

                // Encrypt using Windows DPAPI under CurrentUser scope
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(LicenseFilePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorageService] Error saving license: {ex.Message}");
            }
        }

        public static ClientLicenseInfo LoadLicense()
        {
            try
            {
                if (!File.Exists(LicenseFilePath))
                    return null;

                byte[] encryptedBytes = File.ReadAllBytes(LicenseFilePath);
                if (encryptedBytes.Length == 0)
                    return null;

                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser);

                string json = Encoding.UTF8.GetString(plainBytes);
                return JsonSerializer.Deserialize<ClientLicenseInfo>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorageService] Error loading license: {ex.Message}");
                return null;
            }
        }

        public static void ClearLicense()
        {
            try
            {
                if (File.Exists(LicenseFilePath))
                {
                    File.Delete(LicenseFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorageService] Error clearing license: {ex.Message}");
            }
        }
    }
}
