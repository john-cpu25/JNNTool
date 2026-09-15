using System.Collections.Generic;
using System.Windows;

namespace JNNTool
{
    public partial class FloorByRoomWindow : Window
    {
        // ── Thuộc tính trả kết quả ra lệnh ──────────────────────────────────
        public string SelectedFloorTypeName => cmbFloorTypes.Text;

        public double ThicknessMm
        {
            get { return double.TryParse(txtThickness.Text, out double v) ? v : 200; }
        }

        public double OffsetMm
        {
            get { return double.TryParse(txtOffset.Text, out double v) ? v : 0; }
        }

        // ── Flags trả về cho command ─────────────────────────────────────────
        public bool PickRoomsRequested { get; set; }
        public bool CreateRequested    { get; set; }

        // ── Danh sách tên Room hiển thị ─────────────────────────────────────
        private readonly List<string> _roomDisplayNames = new List<string>();

        public FloorByRoomWindow(List<string> floorTypeNames)
        {
            InitializeComponent();
            cmbFloorTypes.ItemsSource = floorTypeNames;
            if (floorTypeNames.Count > 0)
                cmbFloorTypes.SelectedIndex = 0;
        }

        // ── Khôi phục giá trị từ lần trước (khi dialog được tạo lại) ────────
        public void PresetValues(string typeName, string thicknessMm, string offsetMm, List<string> roomNames)
        {
            if (!string.IsNullOrWhiteSpace(typeName))
                cmbFloorTypes.Text = typeName;
            if (!string.IsNullOrWhiteSpace(thicknessMm))
                txtThickness.Text = thicknessMm;
            if (!string.IsNullOrWhiteSpace(offsetMm))
                txtOffset.Text = offsetMm;
            if (roomNames != null && roomNames.Count > 0)
                SetRoomNames(roomNames);
        }

        // ── Cập nhật danh sách Room đã chọn ─────────────────────────────────
        public void SetRoomNames(List<string> names)
        {
            _roomDisplayNames.Clear();
            _roomDisplayNames.AddRange(names);
            lstRooms.ItemsSource = null;
            lstRooms.ItemsSource = _roomDisplayNames;
        }

        // ── Button: Chọn Room ────────────────────────────────────────────────
        // FIX: Dùng Close() thay vì Hide() để ShowDialog() trả về bình thường.
        // Command sẽ tạo lại dialog mới với state đã lưu.
        private void BtnPickRooms_Click(object sender, RoutedEventArgs e)
        {
            PickRoomsRequested = true;
            this.Close(); // Không set DialogResult → Close() trả về null từ ShowDialog
        }

        // ── Button: Xóa Room ─────────────────────────────────────────────────
        private void BtnClearRooms_Click(object sender, RoutedEventArgs e)
        {
            _roomDisplayNames.Clear();
            lstRooms.ItemsSource = null;
        }

        // ── Button: Tạo Sàn ─────────────────────────────────────────────────
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cmbFloorTypes.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập Loại Sàn.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_roomDisplayNames.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một Room.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
