using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using JNNTool.Core.UI;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Command Palette Dockable Pane cho JNNTool:
    /// Thiết kế chuẩn bố cục 2 cột (Danh sách lệnh hàng ngang + Thanh Category dọc bên phải)
    /// kèm Footer tích hợp Theme Color Preview, nút Settings (UI Manager) và License.
    /// Được lazy-load bởi PaletteHost khi pane Show lần đầu.
    /// </summary>
    public partial class CommandPalettePage : UserControl
    {
        private static JNNTool.Tools.UIManager.UI.UIManagerWindow? _uiManagerWindow;
        private readonly List<PaletteItemVm> _allItems;
        private readonly HashSet<string> _favorites;
        private readonly ObservableCollection<PaletteItemVm> _displayedItems = new ObservableCollection<PaletteItemVm>();
        private readonly ObservableCollection<CategoryTabVm> _categoryTabs = new ObservableCollection<CategoryTabVm>();
        private CategoryTabVm? _currentSelectedCategory;
        private PaletteItemVm? _lastHighlightedItem;
        private DispatcherTimer? _highlightTimer;

        public CommandPalettePage()
        {
            InitializeComponent();

            try
            {
                _favorites = FavoriteStore.Load();
                var currentTheme = JNNTool.Tools.UIManager.Models.RibbonThemeSettings.Load()
                                ?? new JNNTool.Tools.UIManager.Models.RibbonThemeSettings();

                _allItems = CommandRegistry.Commands.Select(c => new PaletteItemVm(c, currentTheme)).ToList();
                foreach (PaletteItemVm item in _allItems)
                {
                    item.IsFavorite = _favorites.Contains(item.Id);
                }

                ToolsList.ItemsSource = _displayedItems;
                CategoryTabsList.ItemsSource = _categoryTabs;

                InitCategoryTabs();
                ApplyThemeColors(currentTheme);

                CommandRunner.StatusChanged += OnStatusChanged;
                JNNTool.Tools.UIManager.Services.RibbonThemeService.ThemeApplied += OnThemeApplied;
                PreviewKeyDown += Page_PreviewKeyDown;

                // Mặc định chọn tab "Modeling" hoặc "General"
                SelectCategoryById("Modeling");
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi khởi tạo CommandPalettePage", ex);
            }
        }

        // ── Khởi tạo các Tab Category dọc bên phải ────────────────────────
        private void InitCategoryTabs()
        {
            _categoryTabs.Clear();

            _categoryTabs.Add(new CategoryTabVm(
                "Recent", "Recent", "RECENT TOOLS",
                "M12,2 C6.5,2 2,6.5 2,12 C2,17.5 6.5,22 12,22 C17.5,22 22,17.5 22,12 C22,6.5 17.5,2 12,2 Z M12.5,13 L8.5,13 L8.5,11.5 L11,11.5 L11,7 L12.5,7 L12.5,13 Z",
                "Recent"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "General", "General", "GENERAL",
                "M3,3 L10,3 L10,10 L3,10 Z M14,3 L21,3 L21,10 L14,10 Z M3,14 L10,14 L10,21 L3,21 Z M14,14 L21,14 L21,21 L14,21 Z",
                "General"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "Modeling", "Model", "MODELING",
                "M3,5 L21,5 L21,10 L3,10 Z M3,14 L21,14 L21,19 L3,19 Z M7,10 L7,14 M17,10 L17,14",
                "Modeling"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "CSI", "CSI", "CSI x REVIT",
                "M4,21 L4,5.5 L12,2.5 L20,5.5 L20,21 Z M4,9.5 L20,9.5 M4,15 L20,15 M12,2.5 L12,21",
                "CSI x Revit"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "Layout", "Layout", "LAYOUT & DRAFTING",
                "M3,3 L21,3 L21,21 L3,21 Z M6,6 L13,6 L13,12 L6,12 Z M15,6 L18,6 M15,9 L18,9 M6,15 L18,15 M6,18 L14,18",
                "Layout"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "Stair", "Stair", "STAIR",
                "M3,20 L8,20 L8,16 L12,16 L12,12 L16,12 L16,8 L21,8 L21,4 L16,4 L16,8 L12,8 L12,12 L8,12 L8,16 L3,16 Z",
                "Stair"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "Quantity", "QTO", "QUANTITY TAKEOFF",
                "M3,13 L7,13 L7,21 L3,21 Z M10,8 L14,8 L14,21 L10,21 Z M17,3 L21,3 L21,21 L17,21 Z",
                "Quantity"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "Rebar", "Rebar", "REBAR SUITE",
                "M3.5,3.5 L20.5,3.5 L20.5,20.5 L3.5,20.5 Z M7,7 L17,7 L17,17 L7,17 Z M7,9.5 L9.5,7",
                "Rebar Suite"
            ));

            _categoryTabs.Add(new CategoryTabVm(
                "Favorites", "Starred", "FAVORITE TOOLS",
                "M12,2 L15.09,8.26 L22,9.27 L17,14.14 L18.18,21.02 L12,17.77 L5.82,21.02 L7,14.14 L2,9.27 L8.91,8.26 Z",
                "Favorites"
            ));
        }

        // ── Chọn Category theo Id ─────────────────────────────────────────
        private void SelectCategoryById(string id)
        {
            var tab = _categoryTabs.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
                   ?? _categoryTabs.FirstOrDefault();
            if (tab == null) return;

            foreach (var t in _categoryTabs)
            {
                t.IsSelected = (t == tab);
            }
            _currentSelectedCategory = tab;
            TxtCategoryTitle.Text = tab.FullName;

            Rebuild();
        }



        // ── Lọc và tái tạo danh sách công cụ hiển thị ─────────────────────
        private void Rebuild()
        {
            if (_allItems == null || ToolsList == null) return;

            string query = SearchBox.Text?.Trim() ?? string.Empty;
            bool isSearching = !string.IsNullOrEmpty(query);

            List<PaletteItemVm> itemsToShow = new List<PaletteItemVm>();

            if (isSearching)
            {
                // Khi tìm kiếm: lọc trên toàn bộ danh mục để kỹ sư tìm nhanh nhất
                itemsToShow = _allItems.Where(i => TextSearch.Matches(i.SearchKey, query)).ToList();
                TxtCategoryTitle.Text = $"SEARCH RESULTS ({itemsToShow.Count})";
            }
            else
            {
                // Lọc theo Category Tab đang chọn
                if (_currentSelectedCategory != null)
                {
                    TxtCategoryTitle.Text = _currentSelectedCategory.FullName;

                    if (string.Equals(_currentSelectedCategory.GroupKey, "Recent", StringComparison.OrdinalIgnoreCase))
                    {
                        var recentIds = RecentStore.Load();
                        var map = _allItems.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
                        foreach (var rid in recentIds)
                        {
                            if (map.TryGetValue(rid, out var found)) itemsToShow.Add(found);
                        }
                    }
                    else if (string.Equals(_currentSelectedCategory.GroupKey, "Favorites", StringComparison.OrdinalIgnoreCase))
                    {
                        itemsToShow = _allItems.Where(i => i.IsFavorite).ToList();
                    }
                    else
                    {
                        itemsToShow = _allItems.Where(i => string.Equals(i.Group, _currentSelectedCategory.GroupKey, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                }
            }

            _displayedItems.Clear();
            foreach (var item in itemsToShow)
            {
                _displayedItems.Add(item);
            }

            bool empty = _displayedItems.Count == 0;
            EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            if (empty)
            {
                if (isSearching)
                {
                    EmptyText.Text = "No tools match your search.";
                }
                else if (_currentSelectedCategory?.GroupKey == "Recent")
                {
                    EmptyText.Text = "No recent tools.\nClick any tool to start.";
                }
                else if (_currentSelectedCategory?.GroupKey == "Favorites")
                {
                    EmptyText.Text = "No favorite tools yet.\nClick ☆ on any tool to pin.";
                }
                else
                {
                    EmptyText.Text = "No tools in this category.";
                }
            }

            ClearSearchButton.Visibility = isSearching ? Visibility.Visible : Visibility.Collapsed;
            UpdatePlaceholder();
        }

        private PaletteItemVm? FirstVisibleItem() => _displayedItems.FirstOrDefault();

        // ── Sự kiện click hàng công cụ (Run Command) ──────────────────────
        private void ToolRow_Click(object sender, RoutedEventArgs e)
        {
            // Tránh kích hoạt lệnh khi click vào nút ngôi sao yêu thích
            if (!ReferenceEquals(e.OriginalSource, sender) && e.OriginalSource is DependencyObject dep)
            {
                DependencyObject curr = dep;
                while (curr != null && !ReferenceEquals(curr, sender))
                {
                    if (curr is System.Windows.Controls.Primitives.ToggleButton) return;
                    curr = VisualTreeHelper.GetParent(curr);
                }
            }

            if ((sender as FrameworkElement)?.DataContext is PaletteItemVm item)
            {
                // Ghi nhận vào lịch sử gần đây (Recent)
                RecentStore.Record(item.Id);

                // Highlight nút công cụ
                HighlightItem(item);

                // Chạy lệnh Revit an toàn
                CommandRunner.Run(item.Info);
            }
        }

        private void HighlightItem(PaletteItemVm item)
        {
            if (_lastHighlightedItem != null)
            {
                _lastHighlightedItem.IsHighlighted = false;
            }
            item.IsHighlighted = true;
            _lastHighlightedItem = item;

            _highlightTimer?.Stop();
            _highlightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _highlightTimer.Tick += (s, e) =>
            {
                _highlightTimer?.Stop();
                if (_lastHighlightedItem != null)
                {
                    _lastHighlightedItem.IsHighlighted = false;
                    _lastHighlightedItem = null;
                }
            };
            _highlightTimer.Start();
        }

        // ── Sự kiện chuyển đổi Category Tab ───────────────────────────────
        private void CategoryTab_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not CategoryTabVm tab) return;

            SearchBox.Clear();

            foreach (var t in _categoryTabs)
            {
                t.IsSelected = (t == tab);
            }
            _currentSelectedCategory = tab;
            TxtCategoryTitle.Text = tab.FullName;

            Rebuild();
        }

        // ── Sự kiện ngôi sao ghim yêu thích ───────────────────────────────
        private void Star_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if ((sender as FrameworkElement)?.DataContext is not PaletteItemVm item) return;

            if (item.IsFavorite)
            {
                _favorites.Add(item.Id);
            }
            else
            {
                _favorites.Remove(item.Id);
            }
            FavoriteStore.Save(_favorites);

            // Rebuild nếu đang ở tab Favorites
            if (_currentSelectedCategory?.GroupKey == "Favorites")
            {
                Dispatcher.BeginInvoke(new Action(Rebuild));
            }
        }

        // ── Xử lý tìm kiếm ────────────────────────────────────────────────
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Rebuild();

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PaletteItemVm? first = FirstVisibleItem();
                if (first != null)
                {
                    RecentStore.Record(first.Id);
                    HighlightItem(first);
                    CommandRunner.Run(first.Info);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SearchBox.Clear();
                e.Handled = true;
            }
        }

        private void SearchBox_FocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            SearchBorder.BorderBrush = SearchBox.IsKeyboardFocused
                ? (Brush)FindResource("BrushBorderFocus")
                : (Brush)FindResource("BrushBorderInput");
            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
        }

        // ── Sự kiện Footer: Theme, Settings (UI Manager) & License ────────
        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            OpenUIManager();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Yêu cầu: "setting là phần UI manager"
            OpenUIManager();
        }

        private void BtnLicense_Click(object sender, RoutedEventArgs e)
        {
            // Yêu cầu: "hình 3 hiện license của ng dùng"
            OpenLicense();
        }

        private static void OpenUIManager()
        {
            try
            {
                WpfWindowHelper.EnsureApplicationContext();
                if (_uiManagerWindow != null && _uiManagerWindow.IsLoaded)
                {
                    _uiManagerWindow.Activate();
                    if (_uiManagerWindow.WindowState == WindowState.Minimized)
                    {
                        _uiManagerWindow.WindowState = WindowState.Normal;
                    }
                    return;
                }

                var handler = new JNNTool.Tools.UIManager.UIManagerExternalEventHandler();
                var viewModel = new JNNTool.Tools.UIManager.ViewModels.UIManagerViewModel(handler);
                _uiManagerWindow = new JNNTool.Tools.UIManager.UI.UIManagerWindow(viewModel);
                _uiManagerWindow.Closed += (s, e) => _uiManagerWindow = null;
                _uiManagerWindow.Show();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("UI Manager", $"Không thể mở UI Manager:\n{ex.Message}");
            }
        }

        private static void OpenLicense()
        {
            try
            {
                WpfWindowHelper.EnsureApplicationContext();
                // Mở cửa sổ Account & License (Dark Theme hiển thị đầy đủ thông tin bản quyền người dùng)
                JNNTool.Tools.Login.UI.LoginWindow.ShowWindow(null);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("License", $"Không thể mở License Manager:\n{ex.Message}");
            }
        }

        // ── Đồng bộ Theme màu Ribbon & Palette ────────────────────────────
        private void OnThemeApplied(JNNTool.Tools.UIManager.Models.RibbonThemeSettings newTheme)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyThemeColors(newTheme);
                foreach (var item in _allItems)
                {
                    item.RefreshIcon(newTheme);
                }
            }));
        }

        private void ApplyThemeColors(JNNTool.Tools.UIManager.Models.RibbonThemeSettings theme)
        {
            try
            {
                Color mainColor = (Color)ColorConverter.ConvertFromString(theme.PrimaryColorHex);
                SolidColorBrush brushMain = new SolidColorBrush(mainColor);
                brushMain.Freeze();

                Color lightColor = Color.FromArgb(35, mainColor.R, mainColor.G, mainColor.B);
                SolidColorBrush brushLight = new SolidColorBrush(lightColor);
                brushLight.Freeze();

                Color highlightBgColor = Color.FromArgb(50, mainColor.R, mainColor.G, mainColor.B);
                SolidColorBrush brushHighlightBg = new SolidColorBrush(highlightBgColor);
                brushHighlightBg.Freeze();

                Resources["BrushPrimary"] = brushMain;
                Resources["BrushBorderFocus"] = brushMain;
                Resources["BrushPrimaryLight"] = brushLight;
                Resources["BrushHighlightBg"] = brushHighlightBg;
                Resources["BrushHighlightBorder"] = brushMain;

                ThemeColorCircle.Fill = brushMain;
            }
            catch { }
        }

        private void OnStatusChanged(string message, bool isError)
        {
            // Cập nhật tooltip hoặc trạng thái nếu cần
        }
    }
}
