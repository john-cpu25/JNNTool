# Checklist Kiểm Thử Thủ Công — JNN Auto Column Dimension

Tài liệu hướng dẫn kỹ sư kết cấu kiểm thử tính năng **Auto Column Dimension** trên Autodesk Revit (2024, 2025, 2026, 2027).

---

## 1. Chuẩn Bị Mô Hình Test
1. Mở một dự án Revit có sẵn các cột kết cấu (`Structural Columns`) trên mặt bằng (`Structural Plan` hoặc `Floor Plan`).
2. Bố trí tối thiểu:
   - 1 Cột chữ nhật thẳng (ví dụ $400 \times 600\text{ mm}$).
   - 1 Cột vuông (ví dụ $500 \times 500\text{ mm}$).
   - 1 Cột chữ nhật xoay góc $30^\circ$ hoặc $45^\circ$.
   - 1 Cột tròn ($\varnothing 400\text{ mm}$).
   - Lưới trục (`Grid`) chạy cắt qua cột hoặc nằm gần cạnh cột (cách $500 - 1500\text{ mm}$).
   - 1 Grid ở xa ngoài bán kính $3000\text{ mm}$.

---

## 2. Các Bước Kiểm Thử

### Test 1: Khởi động Tool và Giao diện Đồng bộ
- [ ] Mở mặt bằng kết cấu `Level 1` hoặc `Level 2`.
- [ ] Trên Ribbon Tab **JNNTool**, tìm nhóm công cụ `Auto Layout` và nhấn nút **Dim Column** (icon hình cột kèm đường dim màu cam).
- [ ] Cửa sổ **JNN — Auto Column Dimension** mở lên:
  - Không có banner tối màu ở đỉnh.
  - Font chữ thường thanh thoát, không in đậm.
  - Bo góc $5\text{px}$ đồng bộ trên toàn bộ GroupBox, Textbox, Combobox, Button, DataGrid.
  - Tự động chạy Preview ban đầu và nạp danh sách các cột tìm thấy vào bảng DataGrid bên phải.

### Test 2: Dimension Tiết Diện Cột Thẳng và Xoay
- [ ] Tích chọn `Column Width (b)` và `Column Length (h)`.
- [ ] Bỏ tích `Grid to Column Face` (để test riêng phần tiết diện).
- [ ] Kiểm tra bảng Preview:
  - Cột $400 \times 600$ hiển thị đúng kích thước `400 × 600 mm`, góc xoay `0°`.
  - Cột xoay hiển thị đúng góc xoay (ví dụ `30°` hoặc `45°`).
- [ ] Nhấn nút **Create Dimensions**:
  - Revit tự động tạo 2 chuỗi dim vuông góc đo đúng chiều rộng $b$ và chiều dài $h$.
  - Đối với cột xoay: Đường dim nằm nghiêng đúng theo phương xoay thực tế của cột, không bị ép theo trục X/Y toàn cục.
  - Khoảng cách offset cách mép cột đúng bằng giá trị `Base Offset` ($350\text{ mm}$).

### Test 3: Dimension từ Lưới Trục đến Cột (Grid-to-Column)
- [ ] Tích chọn `Grid to Column Face`.
- [ ] Tích chọn `Include Opposite Face (Chain)`:
  - Chuỗi dim được tạo gồm 3 điểm: `Grid -> Mặt gần cột -> Mặt xa cột`.
- [ ] Nhấn **Create Dimensions**:
  - Xuất hiện chuỗi dim định vị từ Lưới trục gần nhất đến các mặt của cột.
  - Chuỗi dim Grid nằm ở lớp bên ngoài (cách lớp dim tiết diện một khoảng `Chain Spacing`).
  - Grid ở quá xa ngoài khoảng `Grid Search Range` ($2500\text{ mm}$) bị bỏ qua, không bị dim tùy tiện.

### Test 4: Chống Trùng Dimension (Duplicate Prevention)
- [ ] Giữ nguyên các thiết lập và nhấn lại nút **Create Dimensions**:
  - Do có tùy chọn `Skip Existing Dimensions`, tool kiểm tra các Reference đã tồn tại và bỏ qua, không tạo thêm dim đè nét lên dim cũ.
  - Thanh trạng thái hiển thị rõ: `Created 0 dimensions (X skipped)`.

### Test 5: Cột Tròn và Cột Không Hợp Lệ
- [ ] Quan sát hàng của Cột tròn trên DataGrid:
  - Cột tròn không có mặt phẳng đứng đối diện $\rightarrow$ Cột có trạng thái `Circular / No planar faces`.
  - Cột này tự động không được tick chọn và bỏ qua an toàn, không làm crash hoặc đứng Revit.

### Test 6: Undo Một Chạm
- [ ] Bấm phím tắt `Ctrl + Z` trên Revit:
  - Toàn bộ các dimension vừa được tạo bởi tool được hoàn tác cùng lúc nhờ bọc trong 1 `TransactionGroup` duy nhất.
