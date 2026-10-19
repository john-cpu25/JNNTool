using System.ComponentModel;
using System.Windows;

namespace JNNToolInstaller.Models;

public class RevitVersionModel : INotifyPropertyChanged
{
    private bool _isSelectedForInstall = true;
    private bool _isInstalledInMachine;

    public string Year { get; set; } = "";

    public bool IsInstalledInMachine
    {
        get => _isInstalledInMachine;
        set
        {
            if (_isInstalledInMachine != value)
            {
                _isInstalledInMachine = value;
                NotifyAll();
            }
        }
    }

    public bool IsSelectedForInstall
    {
        get => _isSelectedForInstall;
        set
        {
            if (_isSelectedForInstall != value)
            {
                _isSelectedForInstall = value;
                NotifyAll();
            }
        }
    }

    public string StatusText => IsInstalledInMachine ? "Đã cài đặt" : "Chưa cài đặt";
    public string StatusColor => IsInstalledInMachine ? "#10B981" : "#475569";

    public string YearColor => _isSelectedForInstall
        ? (IsInstalledInMachine ? "#34D399" : "#60A5FA")
        : "#64748B";

    public string CardBackground => _isSelectedForInstall
        ? (IsInstalledInMachine ? "#1A10B981" : "#1A3B82F6")
        : "#08FFFFFF";

    public string CardBorder => _isSelectedForInstall
        ? (IsInstalledInMachine ? "#5910B981" : "#593B82F6")
        : "#14FFFFFF";

    public string CheckboxBackground => _isSelectedForInstall
        ? (IsInstalledInMachine ? "#10B981" : "#3B82F6")
        : "Transparent";

    public string CheckboxBorder => _isSelectedForInstall
        ? "Transparent"
        : "#334155";

    public Visibility CheckmarkVisibility => _isSelectedForInstall ? Visibility.Visible : Visibility.Collapsed;

    public Visibility GreenDotVisibility => IsInstalledInMachine ? Visibility.Visible : Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(IsSelectedForInstall));
        OnPropertyChanged(nameof(IsInstalledInMachine));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(YearColor));
        OnPropertyChanged(nameof(CardBackground));
        OnPropertyChanged(nameof(CardBorder));
        OnPropertyChanged(nameof(CheckboxBackground));
        OnPropertyChanged(nameof(CheckboxBorder));
        OnPropertyChanged(nameof(CheckmarkVisibility));
        OnPropertyChanged(nameof(GreenDotVisibility));
    }

    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
