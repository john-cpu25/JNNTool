using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace JNNTool
{
    /// <summary>
    /// Một mục xem trước trong bảng DataGrid.
    /// </summary>
    public class SectionPreviewItem
    {
        public string TypeName { get; set; }
        public double B { get; set; }
        public double H { get; set; }
        public string Status { get; set; }
    }

    public partial class CreateSectionWindow : Window
    {
        // ── Dữ liệu từ Command ──────────────────────────────────────────────
        private readonly Dictionary<string, List<string>> _beamFamilies;   // Family → List<TypeName>
        private readonly Dictionary<string, List<string>> _columnFamilies; // Family → List<TypeName>
        private readonly HashSet<string> _existingTypeNames;               // Tất cả tên type đã tồn tại

        // ── Kết quả trả về cho Command ───────────────────────────────────────
        public bool CreateRequested { get; private set; }
        public string SelectedCategory => (cmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        public string SelectedFamily => cmbFamily.SelectedItem?.ToString() ?? "";
        public string SelectedBaseType => cmbBaseType.SelectedItem?.ToString() ?? "";
        public List<SectionPreviewItem> ParsedSections { get; private set; } = new List<SectionPreviewItem>();

        // ── Constructor ──────────────────────────────────────────────────────
        public CreateSectionWindow(
            Dictionary<string, List<string>> beamFamilies,
            Dictionary<string, List<string>> columnFamilies,
            HashSet<string> existingTypeNames)
        {
            _beamFamilies = beamFamilies ?? new Dictionary<string, List<string>>();
            _columnFamilies = columnFamilies ?? new Dictionary<string, List<string>>();
            _existingTypeNames = existingTypeNames ?? new HashSet<string>();

            InitializeComponent();

            // Mặc định chọn Dầm — force populate vì SelectionChanged có thể đã fire
            cmbCategory.SelectedIndex = 0;
            CmbCategory_SelectionChanged(null, null);
        }

        // ── Khi đổi loại (Dầm / Cột) ────────────────────────────────────────
        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFamily == null || _beamFamilies == null) return;

            var families = IsBeamCategory() ? _beamFamilies : _columnFamilies;

            cmbFamily.ItemsSource = families.Keys.OrderBy(x => x).ToList();
            if (cmbFamily.Items.Count > 0)
                cmbFamily.SelectedIndex = 0;
            else
                cmbBaseType.ItemsSource = null;
        }

        // ── Khi đổi Family ──────────────────────────────────────────────────
        private void CmbFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBaseType == null || cmbFamily.SelectedItem == null) return;

            string familyName = cmbFamily.SelectedItem.ToString();
            var families = IsBeamCategory() ? _beamFamilies : _columnFamilies;

            if (families.TryGetValue(familyName, out var types))
            {
                cmbBaseType.ItemsSource = types.OrderBy(x => x).ToList();
                if (cmbBaseType.Items.Count > 0)
                    cmbBaseType.SelectedIndex = 0;
            }
            else
            {
                cmbBaseType.ItemsSource = null;
            }
        }

        // ── Parse kích thước từ TextBox ──────────────────────────────────────
        private void BtnParse_Click(object sender, RoutedEventArgs e)
        {
            ParseDimensions();
        }

        private void ParseDimensions()
        {
            ParsedSections.Clear();

            string text = txtDimensions.Text ?? "";
            // Tách theo dòng, dấu phẩy, dấu chấm phẩy, hoặc khoảng trắng liên tiếp
            string[] lines = text.Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            // Regex: cho phép b×h, bxh, bXh, b*h (có hoặc không có khoảng trắng)
            var regex = new Regex(@"^\s*(\d+(?:\.\d+)?)\s*[xX×\*]\s*(\d+(?:\.\d+)?)\s*$");

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                Match match = regex.Match(trimmed);
                if (!match.Success) continue;

                double b = double.Parse(match.Groups[1].Value);
                double h = double.Parse(match.Groups[2].Value);

                // Tạo tên type: B{b}x{h} cho dầm, C{b}x{h} cho cột (giống family gốc)
                string prefix = IsBeamCategory() ? "B" : "C";
                string typeName = $"{prefix}{FormatDim(b)}x{FormatDim(h)}";

                // Kiểm tra trùng trong danh sách parse hiện tại
                if (ParsedSections.Any(s => s.TypeName == typeName))
                    continue;

                string status = _existingTypeNames.Contains(typeName) ? "✓ Đã có" : "⊕ Tạo mới";

                ParsedSections.Add(new SectionPreviewItem
                {
                    TypeName = typeName,
                    B = b,
                    H = h,
                    Status = status
                });
            }

            dgPreview.ItemsSource = null;
            dgPreview.ItemsSource = ParsedSections;
        }

        // ── Format số: bỏ .0 nếu là số nguyên ──────────────────────────────
        private static string FormatDim(double value)
        {
            return value == Math.Floor(value) ? ((int)value).ToString() : value.ToString("0.#");
        }

        // ── Button: Tạo Section ─────────────────────────────────────────────
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // Parse lại trước khi tạo (đề phòng user sửa text mà chưa nhấn Parse)
            ParseDimensions();

            if (string.IsNullOrWhiteSpace(SelectedFamily))
            {
                MessageBox.Show("Vui lòng chọn Family.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedBaseType))
            {
                MessageBox.Show("Vui lòng chọn Type gốc để duplicate.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chỉ giữ lại các mục thực sự cần tạo mới
            var toCreate = ParsedSections.Where(s => s.Status == "⊕ Tạo mới").ToList();
            if (toCreate.Count == 0)
            {
                MessageBox.Show("Không có section mới nào cần tạo.\nTất cả type đã tồn tại trong project.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CreateRequested = true;
            this.DialogResult = true;
            this.Close();
        }

        // ── Button: Huỷ ─────────────────────────────────────────────────────
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // ── Helper ──────────────────────────────────────────────────────────
        private bool IsBeamCategory()
        {
            return cmbCategory.SelectedIndex == 0;
        }
    }
}
