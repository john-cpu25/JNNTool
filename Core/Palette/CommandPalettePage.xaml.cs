using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.UI;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Command Palette Dockable Pane cho JNNTool:
    /// Quản lý tập trung toàn bộ công cụ, tìm kiếm tiếng Việt không dấu, ghim yêu thích (★).
    /// </summary>
    public partial class CommandPalettePage : Page, IDockablePaneProvider
    {
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(new Guid("D5C7B6E2-3A19-4F58-9A2E-8E14B927CF01"));

        public const string PaneTitle = "JNN Command Palette";

        private readonly List<PaletteItemVm> _allItems;
        private readonly HashSet<string> _favorites;
        private readonly ObservableCollection<PaletteGroupVm> _groups = new ObservableCollection<PaletteGroupVm>();

        public CommandPalettePage()
        {
            InitializeComponent();

            _favorites = FavoriteStore.Load();
            _allItems = CommandRegistry.Commands.Select(c => new PaletteItemVm(c)).ToList();
            foreach (PaletteItemVm item in _allItems)
            {
                item.IsFavorite = _favorites.Contains(item.Id);
            }

            GroupsList.ItemsSource = _groups;
            TotalCommandsText.Text = $"{_allItems.Count} lệnh";

            CommandRunner.StatusChanged += OnStatusChanged;
            PreviewKeyDown += Page_PreviewKeyDown;

            Rebuild();
        }

        // ── IDockablePaneProvider ─────────────────────────────────────────
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Left,
                MinimumWidth = 240
            };
        }

        // ── Lọc và tái tạo danh sách nhóm lệnh ─────────────────────────────
        private void Rebuild()
        {
            if (_allItems == null || GroupsList == null) return;

            string query = SearchBox.Text ?? string.Empty;
            bool favOnly = FavoritesOnlyToggle.IsChecked == true;

            List<PaletteItemVm> matched = _allItems.Where(i => TextSearch.Matches(i.SearchKey, query)).ToList();

            _groups.Clear();

            List<PaletteItemVm> favs = matched.Where(i => i.IsFavorite).ToList();
            if (favs.Count > 0)
            {
                _groups.Add(new PaletteGroupVm("★ Yêu thích", favs, isFavorites: true));
            }

            if (!favOnly)
            {
                // Nhóm theo tên panel (Group) theo đúng thứ tự khai báo trong CommandRegistry
                foreach (var g in matched.GroupBy(i => i.Group))
                {
                    _groups.Add(new PaletteGroupVm(g.Key, g));
                }
            }

            bool empty = _groups.Count == 0;
            EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            if (empty)
            {
                EmptyText.Text = favOnly && string.IsNullOrWhiteSpace(query)
                    ? "Chưa có lệnh yêu thích.\nDi chuột vào ô lệnh và bấm ☆ để ghim."
                    : "Không tìm thấy lệnh phù hợp với từ khóa.";
            }

            ClearSearchButton.Visibility = string.IsNullOrEmpty(query) ? Visibility.Collapsed : Visibility.Visible;
            UpdatePlaceholder();
        }

        private PaletteItemVm? FirstVisibleItem()
            => _groups.SelectMany(g => g.Items).FirstOrDefault();

        // ── Sự kiện giao diện ─────────────────────────────────────────────
        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            // Tránh kích hoạt lệnh khi bấm vào nút ngôi sao con bên trong card
            if (!ReferenceEquals(e.OriginalSource, sender) && e.OriginalSource is DependencyObject dep)
            {
                DependencyObject curr = dep;
                while (curr != null && !ReferenceEquals(curr, sender))
                {
                    if (curr is ToggleButton) return;
                    curr = VisualTreeHelper.GetParent(curr);
                }
            }

            if ((sender as FrameworkElement)?.DataContext is PaletteItemVm item)
            {
                CommandRunner.Run(item.Info);
            }
        }

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

            // Rebuild bất đồng bộ để tránh gỡ bỏ Visual Tree khi đang xử lý click
            Dispatcher.BeginInvoke(new Action(Rebuild));
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Rebuild();

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PaletteItemVm? first = FirstVisibleItem();
                if (first != null)
                {
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

        private void FavoritesOnly_Changed(object sender, RoutedEventArgs e)
        {
            if (IsInitialized) Rebuild();
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

        private void OnStatusChanged(string message, bool isError)
        {
            StatusText.Text = message;
            StatusText.ToolTip = message;
            StatusDot.Fill = (Brush)FindResource(isError ? "BrushDanger" : "BrushSuccess");
        }
    }
}
