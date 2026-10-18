using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JNNTool.Tools.CreateView.Models
{
    public class ViewRuleItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isSelected = true;
        private string _viewNamePattern = string.Empty;
        private ViewTypeItem? _selectedViewType;
        private ViewTemplateItem? _selectedViewTemplate;
        private ScopeBoxItem? _selectedScopeBox;
        private string _browserSubDiscipline = string.Empty;
        private string _sampleLevelName = "TANG 1";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ViewNamePattern
        {
            get => _viewNamePattern;
            set
            {
                if (_viewNamePattern != value)
                {
                    _viewNamePattern = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PreviewName));
                }
            }
        }

        public ViewTypeItem? SelectedViewType
        {
            get => _selectedViewType;
            set
            {
                if (_selectedViewType != value)
                {
                    _selectedViewType = value;
                    OnPropertyChanged();
                }
            }
        }

        public ViewTemplateItem? SelectedViewTemplate
        {
            get => _selectedViewTemplate;
            set
            {
                if (_selectedViewTemplate != value)
                {
                    _selectedViewTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        public ScopeBoxItem? SelectedScopeBox
        {
            get => _selectedScopeBox;
            set
            {
                if (_selectedScopeBox != value)
                {
                    _selectedScopeBox = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BrowserSubDiscipline
        {
            get => _browserSubDiscipline;
            set
            {
                if (_browserSubDiscipline != value)
                {
                    _browserSubDiscipline = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TemplateSearchHint { get; set; } = string.Empty;

        public string SampleLevelName
        {
            get => _sampleLevelName;
            set
            {
                if (_sampleLevelName != value)
                {
                    _sampleLevelName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PreviewName));
                }
            }
        }

        public string PreviewName => ResolveName(_sampleLevelName);

        public string ResolveName(string levelName)
        {
            if (string.IsNullOrWhiteSpace(ViewNamePattern))
                return levelName ?? string.Empty;

            string name = ViewNamePattern;
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\{Level\}", levelName ?? "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return name.Trim();
        }

        public ViewRuleItem Clone()
        {
            return new ViewRuleItem
            {
                IsSelected = this.IsSelected,
                ViewNamePattern = this.ViewNamePattern,
                SelectedViewType = this.SelectedViewType,
                SelectedViewTemplate = this.SelectedViewTemplate,
                SelectedScopeBox = this.SelectedScopeBox,
                BrowserSubDiscipline = this.BrowserSubDiscipline,
                TemplateSearchHint = this.TemplateSearchHint,
                SampleLevelName = this.SampleLevelName
            };
        }
    }
}
