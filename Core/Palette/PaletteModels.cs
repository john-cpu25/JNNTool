using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// ViewModel cho từng mục công cụ trong danh sách hàng ngang của Command Palette.
    /// </summary>
    public sealed class PaletteItemVm : ObservableObject
    {
        private bool _isFavorite;
        private bool _isHighlighted;
        private ImageSource? _icon;

        public PaletteItemVm(ToolCommandInfo info, JNNTool.Tools.UIManager.Models.RibbonThemeSettings? themeSettings = null)
        {
            Info = info;
            RefreshIcon(themeSettings);
            Initials = MakeInitials(info.Title);
            SearchKey = TextSearch.Normalize($"{info.Title} {info.Group} {info.Description} {info.Keywords}");
        }

        public ToolCommandInfo Info { get; }
        public string Id => Info.Id;
        public string Title => Info.Title;
        public string Group => Info.Group;
        public string Description => Info.Description;
        public string ToolTipText => string.IsNullOrWhiteSpace(Description) ? $"{Title} ({Group})" : $"{Title} ({Group})\n{Description}";

        public ImageSource? Icon
        {
            get => _icon;
            private set
            {
                if (SetProperty(ref _icon, value))
                {
                    OnPropertyChanged(nameof(HasIcon));
                }
            }
        }

        public bool HasIcon => Icon != null;
        public string Initials { get; }
        public string SearchKey { get; }

        public void RefreshIcon(JNNTool.Tools.UIManager.Models.RibbonThemeSettings? themeSettings = null)
        {
            ImageSource? vectorIcon = JNNTool.Tools.UIManager.Services.RibbonThemeService.CreateCommandIcon(Info.Id, themeSettings, 32);
            Icon = vectorIcon ?? IconLoader.Load(Info.IconFile, 64);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        private static string MakeInitials(string title)
        {
            string[] words = (title ?? "?").Split(new[] { ' ', '-', '_', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            if (words.Length == 1) return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        }
    }

    /// <summary>
    /// ViewModel đại diện cho một thẻ Category trong dải tab dọc bên phải.
    /// </summary>
    public sealed class CategoryTabVm : ObservableObject
    {
        private bool _isSelected;
        private int _count;

        public CategoryTabVm(string id, string name, string fullName, string iconData, string groupKey)
        {
            Id = id;
            Name = name;
            FullName = fullName;
            IconData = iconData;
            GroupKey = groupKey;
        }

        public string Id { get; }
        public string Name { get; }
        public string FullName { get; }
        public string IconData { get; }
        public string GroupKey { get; } // Group tương ứng trong CommandRegistry, hoặc "Recent", "Favorites", "All"

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    /// <summary>
    /// Tìm kiếm Tiếng Việt không dấu tức thì: "dam" khớp "Dầm", "keo dai" khớp "kéo dài".
    /// </summary>
    internal static class TextSearch
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string decomposed = text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Mọi từ khóa (token) trong query đều phải xuất hiện trong search key.
        /// </summary>
        public static bool Matches(string key, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            string[] tokens = Normalize(query).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.All(t => key.IndexOf(t, StringComparison.Ordinal) >= 0);
        }
    }

    /// <summary>
    /// Lưu danh sách lệnh yêu thích vào %AppData%\JNNTool\palette_favorites.txt.
    /// </summary>
    internal static class FavoriteStore
    {
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JNNTool", "palette_favorites.txt");

        public static HashSet<string> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return new HashSet<string>(
                        File.ReadAllLines(FilePath).Select(l => l.Trim()).Where(l => l.Length > 0),
                        StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static void Save(IEnumerable<string> ids)
        {
            try
            {
                string? dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllLines(FilePath, ids.ToArray());
            }
            catch { }
        }
    }

    /// <summary>
    /// Lưu danh sách các lệnh vừa kích hoạt gần đây vào %AppData%\JNNTool\palette_recent.json.
    /// </summary>
    internal static class RecentStore
    {
        private const int MaxRecentItems = 20;

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JNNTool", "palette_recent.json");

        public static List<string> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null) return list;
                }
            }
            catch { }
            return new List<string>();
        }

        public static void Record(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId)) return;
            try
            {
                var list = Load();
                list.RemoveAll(x => string.Equals(x, commandId, StringComparison.OrdinalIgnoreCase));
                list.Insert(0, commandId);
                if (list.Count > MaxRecentItems)
                {
                    list = list.Take(MaxRecentItems).ToList();
                }

                string? dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }
}
