using System.ComponentModel;

namespace JNNToolInstaller.Models;

public class RevitVersionModel : INotifyPropertyChanged
{
    private bool _isSelectedForInstall = true;

    public string Year { get; set; } = "";
    public bool IsInstalledInMachine { get; set; }

    public bool IsSelectedForInstall
    {
        get => _isSelectedForInstall;
        set
        {
            if (_isSelectedForInstall != value)
            {
                _isSelectedForInstall = value;
                OnPropertyChanged(nameof(IsSelectedForInstall));
                OnPropertyChanged(nameof(CardBackground));
                OnPropertyChanged(nameof(CardBorder));
            }
        }
    }

    public string StatusText => IsInstalledInMachine ? "Đã cài đặt" : "Chưa cài đặt";
    public string StatusColor => IsInstalledInMachine ? "#16A34A" : "#94A3B8";

    public string CardBackground => _isSelectedForInstall
        ? (IsInstalledInMachine ? "#F0FDF4" : "#F8FAFC")
        : "#F1F5F9";

    public string CardBorder => _isSelectedForInstall
        ? (IsInstalledInMachine ? "#86EFAC" : "#CBD5E1")
        : "#E2E8F0";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
