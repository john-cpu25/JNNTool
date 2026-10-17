using System;
using System.Collections.Generic;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using JNNTool.Core.Settings;

namespace JNNTool.Tools.UIManager.Models
{
    /// <summary>
    /// Các phong cách hiển thị đồ họa của icon trên thanh Ribbon.
    /// </summary>
    public enum RibbonIconStyle
    {
        /// <summary>Thẻ bo góc mềm mại đổ gradient màu chủ đạo với biểu tượng vector trắng nổi bật.</summary>
        Badge,

        /// <summary>Biểu tượng vector thuần nét theo màu chủ đạo trên nền trong suốt (tối giản, tinh tế).</summary>
        Glyph,

        /// <summary>Khung tròn gradient màu chủ đạo với biểu tượng vector trắng bên trong.</summary>
        Circle,

        /// <summary>Thẻ nền màu nhạt (12% tint), viền bo góc cùng màu và biểu tượng vector cùng màu.</summary>
        Outline
    }

    /// <summary>
    /// Cấu hình lưu trữ theme giao diện Ribbon của JNNTool (lưu tại %AppData%\JNNTool\Settings\RibbonThemeSettings.json).
    /// </summary>
    public class RibbonThemeSettings
    {
        /// <summary>Màu mặc định chuẩn thiết kế JNN Blue BIM (#087BC1).</summary>
        public const string DefaultColor = "#087BC1";

        /// <summary>Mã màu Hex chủ đạo (ví dụ "#087BC1").</summary>
        public string PrimaryColorHex { get; set; } = DefaultColor;

        /// <summary>Kiểu hiển thị icon được chọn.</summary>
        public RibbonIconStyle IconStyle { get; set; } = RibbonIconStyle.Badge;

        /// <summary>Sử dụng bộ Icon Vector phẳng tùy biến màu (Mặc định: true - Đồng bộ sắc nét toàn bộ thanh Ribbon).</summary>
        public bool UseVectorTheme { get; set; } = true;

        /// <summary>Ghi đè màu theo từng Panel (Tùy chọn: PanelName -> HexColor).</summary>
        public Dictionary<string, string> PanelColors { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Nạp cấu hình từ đĩa thông qua SettingsService.
        /// </summary>
        public static RibbonThemeSettings Load()
        {
            return SettingsService<RibbonThemeSettings>.Load();
        }

        /// <summary>
        /// Lưu cấu hình hiện tại xuống đĩa.
        /// </summary>
        public void Save()
        {
            SettingsService<RibbonThemeSettings>.Save(this);
        }

        /// <summary>
        /// Khôi phục cài đặt mặc định.
        /// </summary>
        public static RibbonThemeSettings Reset()
        {
            return SettingsService<RibbonThemeSettings>.Reset();
        }
    }

    /// <summary>
    /// Thông tin mô tả một nút bấm trên thanh Ribbon JNNTool.
    /// </summary>
    public class RibbonButtonItem : ObservableObject
    {
        private ImageSource? _imagePreview;

        public string CommandId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PanelName { get; set; } = string.Empty;
        public JNNIcon Icon { get; set; } = JNNIcon.Settings;
        public string ToolTip { get; set; } = string.Empty;

        public ImageSource? ImagePreview
        {
            get => _imagePreview;
            set => SetProperty(ref _imagePreview, value);
        }
    }

    /// <summary>
    /// Bảng màu mẫu dựng sẵn để người dùng chọn nhanh.
    /// </summary>
    public class ColorPreset
    {
        public string Name { get; set; }
        public string HexCode { get; set; }
        public Brush Brush { get; set; }

        public ColorPreset(string name, string hexCode)
        {
            Name = name;
            HexCode = hexCode;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexCode);
                var solid = new SolidColorBrush(color);
                solid.Freeze();
                Brush = solid;
            }
            catch
            {
                Brush = Brushes.DodgerBlue;
            }
        }
    }
}
