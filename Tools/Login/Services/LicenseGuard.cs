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
        /// Validates current workstation license status.
        /// If not licensed or expired, opens the Login/Activation window modelessly and returns false.
        /// Unrelated Revit commands will not crash.
        /// </summary>
        public static bool EnsureLicensed(UIApplication uiapp = null)
        {
            var client = LicenseClientService.Instance;

            // Fast local check
            if (!client.IsLicensed)
            {
                ShowActivationPrompt(uiapp);
                return false;
            }

            // Periodically re-validate with server if cache is older than 15 minutes
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
