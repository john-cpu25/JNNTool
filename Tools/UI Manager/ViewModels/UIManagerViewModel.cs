using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.UIManager.Models;
using JNNTool.Tools.UIManager.Services;

namespace JNNTool.Tools.UIManager.ViewModels
{
    public class UIManagerViewModel : ObservableObject
    {
        private readonly UIManagerExternalEventHandler? _handler;

        private string _selectedColorHex = RibbonThemeSettings.DefaultColor;
        public string SelectedColorHex
        {
            get => _selectedColorHex;
            set
            {
                if (SetProperty(ref _selectedColorHex, value))
                {
                    UpdateColorBrush();
                    UpdatePreviews();
                }
            }
        }

        private Brush _selectedColorBrush = Brushes.DodgerBlue;
        public Brush SelectedColorBrush
        {
            get => _selectedColorBrush;
            set => SetProperty(ref _selectedColorBrush, value);
        }

        private RibbonIconStyle _selectedStyle = RibbonIconStyle.Badge;
        public RibbonIconStyle SelectedStyle
        {
            get => _selectedStyle;
            set
            {
                if (SetProperty(ref _selectedStyle, value))
                {
                    OnPropertyChanged(nameof(IsBadgeStyle));
                    OnPropertyChanged(nameof(IsGlyphStyle));
                    OnPropertyChanged(nameof(IsCircleStyle));
                    OnPropertyChanged(nameof(IsOutlineStyle));
                    UpdatePreviews();
                }
            }
        }

        public bool IsBadgeStyle => SelectedStyle == RibbonIconStyle.Badge;
        public bool IsGlyphStyle => SelectedStyle == RibbonIconStyle.Glyph;
        public bool IsCircleStyle => SelectedStyle == RibbonIconStyle.Circle;
        public bool IsOutlineStyle => SelectedStyle == RibbonIconStyle.Outline;

        private string _selectedPanelFilter = "All";
        public string SelectedPanelFilter
        {
            get => _selectedPanelFilter;
            set
            {
                if (SetProperty(ref _selectedPanelFilter, value))
                {
                    FilterPreviewList();
                }
            }
        }

        private string _statusMessage = "Select a color preset or enter a hex code to customize Ribbon icons.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public List<ColorPreset> PresetColors { get; }

        public List<string> Panels { get; } = new List<string>
        {
            "All", "Modeling", "CSI x Revit", "Layout", "Stair", "Quantity", "General"
        };

        private List<RibbonButtonItem> _allButtons = new List<RibbonButtonItem>();

        private ObservableCollection<RibbonButtonItem> _filteredButtons = new ObservableCollection<RibbonButtonItem>();
        public ObservableCollection<RibbonButtonItem> FilteredButtons
        {
            get => _filteredButtons;
            set => SetProperty(ref _filteredButtons, value);
        }

        public event Action? RequestClose;

        public ICommand SelectPresetCommand { get; }
        public ICommand SelectStyleCommand { get; }
        public ICommand SetFilterCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CloseCommand { get; }

        public UIManagerViewModel(UIManagerExternalEventHandler? handler)
        {
            _handler = handler;
            if (_handler != null)
            {
                _handler.ViewModel = this;
            }

            // 12 curated vibrant modern palettes
            PresetColors = new List<ColorPreset>
            {
                new ColorPreset("Apex Blue", "#2563EB"),
                new ColorPreset("Royal Indigo", "#4F46E5"),
                new ColorPreset("Ocean Cyan", "#0284C7"),
                new ColorPreset("Deep Teal", "#0D9488"),
                new ColorPreset("Emerald Green", "#059669"),
                new ColorPreset("Amber Gold", "#D97706"),
                new ColorPreset("Sunset Orange", "#EA580C"),
                new ColorPreset("Crimson Red", "#DC2626"),
                new ColorPreset("Vivid Rose", "#E11D48"),
                new ColorPreset("Electric Purple", "#9333EA"),
                new ColorPreset("Dark Slate", "#334155"),
                new ColorPreset("Midnight Black", "#1E293B")
            };

            // Commands
            SelectPresetCommand = new RelayCommand<string>(colorHex =>
            {
                if (!string.IsNullOrWhiteSpace(colorHex))
                {
                    SelectedColorHex = colorHex;
                }
            });

            SelectStyleCommand = new RelayCommand<RibbonIconStyle>(style =>
            {
                SelectedStyle = style;
            });

            SetFilterCommand = new RelayCommand<string>(panelName =>
            {
                if (!string.IsNullOrWhiteSpace(panelName))
                {
                    SelectedPanelFilter = panelName;
                }
            });

            ApplyCommand = new RelayCommand(ApplyTheme);
            ResetCommand = new RelayCommand(ResetTheme);
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());

            // Load saved settings
            var saved = RibbonThemeSettings.Load();
            _selectedColorHex = saved.PrimaryColorHex ?? RibbonThemeSettings.DefaultColor;
            _selectedStyle = saved.IconStyle;
            UpdateColorBrush();

            // Load buttons
            _allButtons = RibbonThemeService.GetKnownButtons().ToList();
            UpdatePreviews();
        }

        private void UpdateColorBrush()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(SelectedColorHex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                SelectedColorBrush = brush;
            }
            catch
            {
                SelectedColorBrush = Brushes.DodgerBlue;
            }
        }

        private void UpdatePreviews()
        {
            if (_allButtons == null) return;

            foreach (var btn in _allButtons)
            {
                btn.ImagePreview = RibbonThemeService.CreateRibbonImage(btn.Icon, SelectedColorHex, SelectedStyle, 32);
            }

            FilterPreviewList();
        }

        private void FilterPreviewList()
        {
            if (_allButtons == null) return;

            FilteredButtons.Clear();
            var matches = SelectedPanelFilter == "All"
                ? _allButtons
                : _allButtons.Where(b => string.Equals(b.PanelName, SelectedPanelFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var b in matches)
            {
                FilteredButtons.Add(b);
            }
        }

        private void ApplyTheme()
        {
            StatusMessage = "Applying icons to Revit Ribbon...";

            var settings = new RibbonThemeSettings
            {
                PrimaryColorHex = SelectedColorHex,
                IconStyle = SelectedStyle,
                UseVectorTheme = true
            };

            // Lưu cài đặt vào %AppData%\JNNTool\Settings\RibbonThemeSettings.json
            settings.Save();

            // Notify Revit thread qua ExternalEvent
            if (_handler != null)
            {
                _handler.Settings = settings;
                _handler.Raise();
            }
        }

        private void ResetTheme()
        {
            SelectedColorHex = RibbonThemeSettings.DefaultColor;
            SelectedStyle = RibbonIconStyle.Badge;

            RibbonThemeSettings.Reset();

            var defaultSettings = new RibbonThemeSettings();
            if (_handler != null)
            {
                _handler.Settings = defaultSettings;
                _handler.Raise();
            }

            StatusMessage = "Reset to default theme.";
        }
    }
}
