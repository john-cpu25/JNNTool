using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.Login.Models;

namespace JNNTool.Tools.Login.ViewModels
{
    public class LicenseExpiredViewModel : ObservableObject
    {
        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _expiredAt = string.Empty;
        public string ExpiredAt
        {
            get => _expiredAt;
            set => SetProperty(ref _expiredAt, value);
        }

        public IRelayCommand ReactivateCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public event Action RequestReactivate;
        public event Action RequestClose;

        public LicenseExpiredViewModel(ClientLicenseInfo info = null)
        {
            if (info != null)
            {
                Email = info.Email ?? "Unknown";
                ExpiredAt = info.ExpiresAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") + $" ({info.ExpiresAtUtc:yyyy-MM-dd} UTC)";
            }
            else
            {
                Email = "Not available";
                ExpiredAt = "-";
            }

            ReactivateCommand = new RelayCommand(() => RequestReactivate?.Invoke());
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
        }
    }
}
