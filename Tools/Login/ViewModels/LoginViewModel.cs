using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.Login.Services;

namespace JNNTool.Tools.Login.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        private string _machineId = string.Empty;
        public string MachineId
        {
            get => _machineId;
            set => SetProperty(ref _machineId, value);
        }

        private string _activationCode = string.Empty;
        public string ActivationCode
        {
            get => _activationCode;
            set => SetProperty(ref _activationCode, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    ActivateCommand?.NotifyCanExecuteChanged();
                    RegisterRequestCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        private string _statusMessage = "Sẵn sàng";
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

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        private string _language = "EN";
        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        public string DeviceName => DeviceFingerprintService.GetDeviceFriendlyName();

        public IAsyncRelayCommand ActivateCommand { get; }
        public IAsyncRelayCommand RegisterRequestCommand { get; }
        public IRelayCommand CopyMachineIdCommand { get; }
        public IRelayCommand ToggleLanguageCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public event Action<bool> RequestClose;

        public LoginViewModel()
        {
            _machineId = DeviceFingerprintService.GetDisplayMachineId();

            // Pre-fill email & status from saved license if available
            var existing = LicenseClientService.Instance.CurrentLicense;
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(existing.Email))
                {
                    _email = existing.Email;
                }

                if (LicenseClientService.Instance.IsLicensed)
                {
                    _statusMessage = $"License Active: {existing.LicenseType} (Còn {existing.DaysRemaining} ngày)";
                }
                else if (existing.IsExpired)
                {
                    _statusMessage = "Bản quyền đã hết hạn. Vui lòng liên hệ quản trị viên để gia hạn.";
                }
            }

            ActivateCommand = new AsyncRelayCommand(ExecuteActivateAsync, () => !IsLoading);
            RegisterRequestCommand = new AsyncRelayCommand(ExecuteRegisterRequestAsync, () => !IsLoading);
            CopyMachineIdCommand = new RelayCommand(ExecuteCopyMachineId);
            ToggleLanguageCommand = new RelayCommand(ExecuteToggleLanguage);
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        }

        private void ExecuteCopyMachineId()
        {
            try
            {
                if (!string.IsNullOrEmpty(MachineId))
                {
                    Clipboard.SetText(MachineId);
                    StatusMessage = "Đã sao chép Machine ID vào bộ nhớ tạm!";
                    IsError = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không thể sao chép: {ex.Message}";
            }
        }

        private void ExecuteToggleLanguage()
        {
            Language = Language == "EN" ? "VI" : "EN";
            StatusMessage = Language == "EN" ? "Ready" : "Sẵn sàng";
        }

        private async Task ExecuteRegisterRequestAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Vui lòng nhập Email để gửi yêu cầu đăng ký.");
                return;
            }

            IsLoading = true;
            IsError = false;
            IsSuccess = false;
            StatusMessage = "Đang gửi thông tin đăng ký lên hệ thống máy chủ...";

            try
            {
                var (success, message, code) = await LicenseClientService.Instance.RegisterAccountRequestAsync(Email.Trim(), FullName, PhoneNumber, MachineId);

                if (success)
                {
                    IsSuccess = true;
                    if (!string.IsNullOrEmpty(code))
                    {
                        ActivationCode = code;
                        StatusMessage = "Tài khoản đã được Quản trị viên phê duyệt! Mã Key Code đã được tự động điền. Bấm Login để tiếp tục.";
                    }
                    else
                    {
                        StatusMessage = "Đã gửi yêu cầu đăng ký lên server! Quản trị viên trên Web sẽ phê duyệt và cấp Key Code cho bạn.";
                    }
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi kết nối máy chủ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteActivateAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Vui lòng nhập Email tài khoản.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ActivationCode))
            {
                ShowError("Vui lòng nhập mã Key Code (GUID Activation Code).");
                return;
            }

            IsLoading = true;
            IsError = false;
            IsSuccess = false;
            StatusMessage = "Đang kết nối tới máy chủ để xác thực bản quyền...";

            try
            {
                var (success, message) = await LicenseClientService.Instance.ActivateAsync(Email.Trim(), ActivationCode.Trim());

                if (success)
                {
                    IsSuccess = true;
                    StatusMessage = "Kích hoạt bản quyền thành công!";
                    await Task.Delay(800);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi xác thực: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowError(string message)
        {
            IsError = true;
            IsSuccess = false;
            StatusMessage = message;
        }
    }
}
