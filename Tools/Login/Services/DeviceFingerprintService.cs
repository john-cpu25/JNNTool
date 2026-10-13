using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace JNNTool.Tools.Login.Services
{
    public static class DeviceFingerprintService
    {
        private static string _cachedHash;

        /// <summary>
        /// Generates a privacy-preserving SHA-256 hash representing this physical workstation.
        /// No identifiable raw hardware details are ever sent to or stored on the server.
        /// </summary>
        public static string GetDeviceIdentifierHash()
        {
            if (!string.IsNullOrEmpty(_cachedHash))
                return _cachedHash;

            try
            {
                string machineGuid = GetMachineGuid();
                string machineName = Environment.MachineName ?? "UnknownHost";
                int processorCount = Environment.ProcessorCount;
                string osVersion = Environment.OSVersion.Platform.ToString();

                string rawSeed = $"{machineGuid}|{machineName}|{processorCount}|{osVersion}";
                byte[] bytes = Encoding.UTF8.GetBytes(rawSeed);

                using (var sha = SHA256.Create())
                {
                    byte[] hashBytes = sha.ComputeHash(bytes);
                    var sb = new StringBuilder(hashBytes.Length * 2);
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    _cachedHash = sb.ToString();
                }
            }
            catch
            {
                // Fallback deterministic fallback if registry is blocked by group policy
                string fallback = Environment.MachineName + "_" + Environment.ProcessorCount;
                using (var sha = SHA256.Create())
                {
                    byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    _cachedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }

            return _cachedHash;
        }

        public static string GetDeviceFriendlyName()
        {
            try
            {
                return Environment.MachineName ?? "Workstation";
            }
            catch
            {
                return "Revit Workstation";
            }
        }

        public static string GetDisplayMachineId()
        {
            try
            {
                string guid = GetMachineGuid();
                if (!string.IsNullOrEmpty(guid) && guid.Length >= 16)
                {
                    return guid.ToUpperInvariant();
                }
            }
            catch { }

            string hash = GetDeviceIdentifierHash();
            if (hash.Length >= 20)
            {
                return $"{hash.Substring(0, 8)}-{hash.Substring(8, 4)}-{hash.Substring(12, 4)}-{hash.Substring(16, 4)}".ToUpperInvariant();
            }
            return hash.ToUpperInvariant();
        }

        private static string GetMachineGuid()
        {
            try
            {
                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var subKey = key.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    var val = subKey?.GetValue("MachineGuid");
                    if (val != null)
                    {
                        return val.ToString();
                    }
                }
            }
            catch { }

            return Environment.MachineName;
        }
    }
}
