using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.StairDetail.ViewModels;

public class StairDetailViewModel : ObservableObject
{
    private string _selectedThepChu = Settings.Default.ThepChu;
    public string SelectedThepChu
    {
        get => _selectedThepChu;
        set => SetProperty(ref _selectedThepChu, value);
    }

    private string _selectedThepPhu = Settings.Default.ThepPhu;
    public string SelectedThepPhu
    {
        get => _selectedThepPhu;
        set => SetProperty(ref _selectedThepPhu, value);
    }

    private string _thepChuSpacing = Settings.Default.ThepChu_Spacing;
    public string ThepChuSpacing
    {
        get => _thepChuSpacing;
        set => SetProperty(ref _thepChuSpacing, value);
    }

    private string _thepPhuSpacing = Settings.Default.ThepPhu_Spacing;
    public string ThepPhuSpacing
    {
        get => _thepPhuSpacing;
        set => SetProperty(ref _thepPhuSpacing, value);
    }

    private bool _includeRebarLandingTop = true;
    public bool IncludeRebarLandingTop
    {
        get => _includeRebarLandingTop;
        set => SetProperty(ref _includeRebarLandingTop, value);
    }

    private bool _includeRebarLandingBot = false;
    public bool IncludeRebarLandingBot
    {
        get => _includeRebarLandingBot;
        set => SetProperty(ref _includeRebarLandingBot, value);
    }

    public List<string> RebarTypeNames { get; }

    public StairDetailViewModel(List<Element> rebarTypes)
    {
        RebarTypeNames = new List<string>();
        foreach (var item in rebarTypes)
        {
            RebarTypeNames.Add(item.Name);
        }
    }

    /// <summary>
    /// Save current selections to static settings for next use.
    /// </summary>
    public void SaveSettings()
    {
        Settings.Default.ThepChu = SelectedThepChu;
        Settings.Default.ThepPhu = SelectedThepPhu;
        Settings.Default.ThepChu_Spacing = ThepChuSpacing;
        Settings.Default.ThepPhu_Spacing = ThepPhuSpacing;
        Settings.Default.Save();
    }
}
