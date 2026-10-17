using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.Login.Services;
using JNNTool.Tools.UIManager.Models;
using JNNTool.Tools.UIManager.Services;

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

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
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

        private Brush _primaryBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(RibbonThemeSettings.DefaultColor));
        public Brush PrimaryBrush
        {
            get => _primaryBrush;
            set => SetProperty(ref _primaryBrush, value);
        }

        private ImageSource? _logoImage;
        public ImageSource? LogoImage
        {
            get => _logoImage;
            set => SetProperty(ref _logoImage, value);
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
        public IRelayCommand PasteActivationCodeCommand { get; }
        public IRelayCommand ToggleLanguageCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public event Action<bool>? RequestClose;

        public LoginViewModel()
        {
            _machineId = DeviceFingerprintService.GetDisplayMachineId();

            // Đồng bộ màu theme theo UI Manager (Ribbon Theme)
            var currentTheme = RibbonThemeSettings.Load();
            ApplyThemeColor(currentTheme?.PrimaryColorHex);
            RibbonThemeService.ThemeApplied += OnThemeApplied;

            // Nạp Logo JNN chính thức
            LoadLogoImage();

            // Lấy thông tin bản quyền đã lưu nếu có
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
            PasteActivationCodeCommand = new RelayCommand(ExecutePasteActivationCode);
            ToggleLanguageCommand = new RelayCommand(ExecuteToggleLanguage);
            CloseCommand = new RelayCommand(() =>
            {
                Cleanup();
                RequestClose?.Invoke(false);
            });
        }

        public void Cleanup()
        {
            RibbonThemeService.ThemeApplied -= OnThemeApplied;
        }

        private void ApplyThemeColor(string? hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex)) hex = RibbonThemeSettings.DefaultColor;
                var color = (Color)ColorConverter.ConvertFromString(hex.Trim());
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                PrimaryBrush = brush;
            }
            catch
            {
                var fallback = (Color)ColorConverter.ConvertFromString(RibbonThemeSettings.DefaultColor);
                var brush = new SolidColorBrush(fallback);
                brush.Freeze();
                PrimaryBrush = brush;
            }
        }

        private void OnThemeApplied(RibbonThemeSettings settings)
        {
            if (settings != null && !string.IsNullOrWhiteSpace(settings.PrimaryColorHex))
            {
                ApplyThemeColor(settings.PrimaryColorHex);
            }
        }

        private void LoadLogoImage()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/JNNTool;component/Tools/Login/Assets/Logo_JNN.png", UriKind.Absolute);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                if (bmp.CanFreeze) bmp.Freeze();
                LogoImage = bmp;
                return;
            }
            catch { }

            try
            {
                string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                string[] candidates = new[]
                {
                    Path.Combine(asmDir, "Resources", "Logo_JNN.png"),
                    Path.Combine(asmDir, "Logo_JNN.png"),
                    Path.Combine(asmDir, "..", "Resources", "Logo_JNN.png"),
                    Path.Combine(asmDir, "..", "Logo_JNN.png")
                };

                foreach (var p in candidates)
                {
                    if (File.Exists(p))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(p, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        if (bmp.CanFreeze) bmp.Freeze();
                        LogoImage = bmp;
                        return;
                    }
                }
            }
            catch { }
        }

        private void ExecutePasteActivationCode()
        {
            try
            {
                string text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ActivationCode = text.Trim();
                    StatusMessage = "Đã dán mã Key từ bộ nhớ tạm!";
                    IsError = false;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Không thể dán: {ex.Message}");
            }
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

        private Task ExecuteRegisterRequestAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Vui lòng nhập Email để gửi cho Quản trị viên.");
                return Task.CompletedTask;
            }

            StatusMessage = "Vui lòng gửi Email và Machine ID của bạn cho Admin để được cấp Key kích hoạt.";
            IsError = false;
            IsSuccess = true;
            return Task.CompletedTask;
        }

        private async Task ExecuteActivateAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Vui lòng nhập Email tài khoản.");
                return;
            }

            IsLoading = true;
            IsError = false;
            IsSuccess = false;
            StatusMessage = "Đang kết nối Supabase để xác thực bản quyền...";

            try
            {
                // Gọi Supabase RPC: kiểm tra Email + Mật khẩu + Mã Key (nếu có)
                var (success, message, data) = await LicenseClientService.Instance.CallVerifyRpcAsync(
                    Email.Trim(), 
                    Password, 
                    ActivationCode?.Trim());

                if (success)
                {
                    IsSuccess = true;
                    StatusMessage = message ?? "Đăng nhập và xác thực bản quyền thành công!";
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
