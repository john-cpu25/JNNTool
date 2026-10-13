using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.Login.Models;
using JNNTool.Tools.Login.Services;

namespace JNNTool.Tools.Login.ViewModels
{
    public class LicenseStatusViewModel : ObservableObject
    {
        private ClientLicenseInfo _license;

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _licenseType = string.Empty;
        public string LicenseType
        {
            get => _licenseType;
            set => SetProperty(ref _licenseType, value);
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _expiresAt = string.Empty;
        public string ExpiresAt
        {
            get => _expiresAt;
            set => SetProperty(ref _expiresAt, value);
        }

        private int _daysRemaining;
        public int DaysRemaining
        {
            get => _daysRemaining;
            set => SetProperty(ref _daysRemaining, value);
        }

        private string _deviceName = string.Empty;
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set => SetProperty(ref _isError, value);
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand DeactivateCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public event Action RequestClose;
        public event Action RequestReactivate;

        public LicenseStatusViewModel()
        {
            _license = LicenseClientService.Instance.CurrentLicense;
            LoadInfo();

            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
            DeactivateCommand = new AsyncRelayCommand(ExecuteDeactivateAsync);
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
        }

        private void LoadInfo()
        {
            if (_license != null)
            {
                Email = _license.Email ?? "Unknown";
                LicenseType = _license.LicenseType == "Trial" ? "Trial (30 Days)" : "Annual (1 Year)";
                Status = _license.Status ?? "Active";
                ExpiresAt = _license.ExpiresAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") + $" ({_license.ExpiresAtUtc:yyyy-MM-dd} UTC)";
                DaysRemaining = _license.DaysRemaining;
                DeviceName = _license.DeviceName ?? DeviceFingerprintService.GetDeviceFriendlyName();
            }
            else
            {
                Email = "Not available";
                LicenseType = "Not activated";
                Status = "None";
                ExpiresAt = "-";
                DaysRemaining = 0;
                DeviceName = DeviceFingerprintService.GetDeviceFriendlyName();
            }
        }

        private async Task ExecuteRefreshAsync()
        {
            IsLoading = true;
            IsError = false;
            StatusMessage = "Validating license with server...";

            try
            {
                var (isValid, message) = await LicenseClientService.Instance.ValidateCurrentLicenseAsync();
                _license = LicenseClientService.Instance.CurrentLicense;
                LoadInfo();

                if (isValid)
                {
                    StatusMessage = "License validated successfully.";
                }
                else
                {
                    IsError = true;
                    StatusMessage = message;
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteDeactivateAsync()
        {
            IsLoading = true;
            IsError = false;
            StatusMessage = "Deactivating device...";

            try
            {
                var (success, message) = await LicenseClientService.Instance.DeactivateAsync();
                if (success)
                {
                    StatusMessage = message;
                    await Task.Delay(600);
                    RequestReactivate?.Invoke();
                }
                else
                {
                    IsError = true;
                    StatusMessage = message;
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
