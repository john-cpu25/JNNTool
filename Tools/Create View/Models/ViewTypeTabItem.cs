using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JNNTool.Tools.CreateView.Models
{
    public enum LevelScopeMode
    {
        AllSelectedLevels, // Có đủ các tầng (áp dụng cho toàn bộ các Level được chọn)
        SingleLevel        // Chỉ lấy 1 tầng (Ví dụ tự động lấy tầng MÓNG để sẵn)
    }

    public class ViewTypeTabItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool _isEnabled = true;
        private string _tabId = string.Empty;
        private string _title = string.Empty;
        private string _viewFamilyTypeName = string.Empty;
        private string _browserSubDiscipline = string.Empty;
        private string _defaultTemplateHint = string.Empty;
        private LevelScopeMode _levelScope = LevelScopeMode.AllSelectedLevels;
        private LevelItem? _targetLevel;
        private ViewTypeItem? _selectedViewType;
        private ViewTemplateItem? _selectedViewTemplate;
        private ScopeBoxItem? _selectedScopeBox;
        private ViewRuleItem? _selectedView;
        private bool _autoCreateViewType = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public string TabId
        {
            get => _tabId;
            set => SetField(ref _tabId, value);
        }

        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        public string ViewFamilyTypeName
        {
            get => _viewFamilyTypeName;
            set => SetField(ref _viewFamilyTypeName, value);
        }

        public string BrowserSubDiscipline
        {
            get => _browserSubDiscipline;
            set => SetField(ref _browserSubDiscipline, value);
        }

        public string DefaultTemplateHint
        {
            get => _defaultTemplateHint;
            set => SetField(ref _defaultTemplateHint, value);
        }

        public LevelScopeMode LevelScope
        {
            get => _levelScope;
            set
            {
                if (SetField(ref _levelScope, value))
                {
                    OnPropertyChanged(nameof(IsSingleLevel));
                    OnPropertyChanged(nameof(IsAllLevels));
                }
            }
        }

        public bool IsSingleLevel => LevelScope == LevelScopeMode.SingleLevel;
        public bool IsAllLevels => LevelScope == LevelScopeMode.AllSelectedLevels;

        public LevelItem? TargetLevel
        {
            get => _targetLevel;
            set
            {
                if (SetField(ref _targetLevel, value) && value != null)
                {
                    // Update sample preview for views in this tab
                    foreach (var v in Views)
                    {
                        v.SampleLevelName = value.Name;
                    }
                }
            }
        }

        public bool AutoCreateViewType
        {
            get => _autoCreateViewType;
            set => SetField(ref _autoCreateViewType, value);
        }

        public ViewTypeItem? SelectedViewType
        {
            get => _selectedViewType;
            set => SetField(ref _selectedViewType, value);
        }

        public ViewTemplateItem? SelectedViewTemplate
        {
            get => _selectedViewTemplate;
            set => SetField(ref _selectedViewTemplate, value);
        }

        public ScopeBoxItem? SelectedScopeBox
        {
            get => _selectedScopeBox;
            set => SetField(ref _selectedScopeBox, value);
        }

        public ViewRuleItem? SelectedView
        {
            get => _selectedView;
            set => SetField(ref _selectedView, value);
        }

        public ObservableCollection<ViewRuleItem> Views { get; } = new ObservableCollection<ViewRuleItem>();

        public void AddView(string namePattern, string sampleLevelName = "TANG 1")
        {
            var rule = new ViewRuleItem
            {
                IsSelected = true,
                ViewNamePattern = namePattern,
                BrowserSubDiscipline = this.BrowserSubDiscipline,
                SelectedViewType = this.SelectedViewType,
                SelectedViewTemplate = this.SelectedViewTemplate,
                SelectedScopeBox = this.SelectedScopeBox,
                SampleLevelName = TargetLevel != null ? TargetLevel.Name : sampleLevelName
            };
            Views.Add(rule);
            SelectedView = rule;
        }

        public void DuplicateSelected(string sampleLevelName = "TANG 1")
        {
            if (SelectedView == null) return;
            var clone = SelectedView.Clone();
            clone.ViewNamePattern += "_Copy";
            clone.SampleLevelName = TargetLevel != null ? TargetLevel.Name : sampleLevelName;
            Views.Add(clone);
            SelectedView = clone;
        }

        public void RemoveSelected()
        {
            if (SelectedView == null) return;
            int idx = Views.IndexOf(SelectedView);
            Views.Remove(SelectedView);
            if (Views.Count > 0)
            {
                SelectedView = Views[Math.Min(idx, Views.Count - 1)];
            }
        }
    }
}
