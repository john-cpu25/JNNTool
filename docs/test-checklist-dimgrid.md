# Test Checklist — Tool Tự Động Dim Grid (JNNTool)

## 1. Mô tả tính năng
Tool **Dim Grid** (Auto Dim Grid) tự động nhận diện toàn bộ hệ lưới trục (Grids) hiển thị trên mặt bằng (Floor Plan / Structural Plan), phân nhóm các trục song song thành trục dọc / trục ngang / trục xiên, sắp xếp theo thứ tự và tự động tạo các đường Dimension bao quanh hệ trục.

Các tính năng chính:
- **Phân loại 4 phía**:
  - Trục dọc (Vertical): Đặt Dim ở phía **Trên (Top)** và/hoặc phía **Dưới (Bottom)**.
  - Trục ngang (Horizontal): Đặt Dim ở phía **Trái (Left)** và/hoặc phía **Phải (Right)**.
- **Tùy chọn 2 lớp Dimension**:
  - **Dim chi tiết (Segment Dim)**: Đo khoảng cách giữa từng cặp trục kế tiếp (1-2, 2-3, 3-4...).
  - **Dim tổng (Overall Dim)**: Đo khoảng cách bao ngoài giữa trục đầu tiên và trục cuối cùng (1-n).
- **Tùy chỉnh khoảng cách linh hoạt**:
  - Khoảng cách từ đầu trục ra đường Dim chi tiết (mặc định 12 mm trên giấy).
  - Khoảng cách giữa 2 lớp Dim chi tiết và Dim tổng (mặc định 8 mm trên giấy).
- **Phạm vi áp dụng**:
  - Toàn bộ trục hiển thị trong View hiện hành.
  - Hoặc quét chọn trực tiếp các trục trên màn hình Revit.
- **Tự động lưu cấu hình**:
  - Tự động lưu lựa chọn vào `%AppData%\JNNTool\Settings\DimGridSettings.json`.

---

## 2. Các bước kiểm thử thủ công trên Revit

### Bước 1: Mở mô hình mẫu và mặt bằng
1. Khởi động Revit (2022, 2023, 2024, 2025, 2026 hoặc 2027).
2. Mở một dự án có hệ lưới trục (ít nhất 2 trục dọc và 2 trục ngang).
3. Mở một mặt bằng kết cấu hoặc kiến trúc (Floor Plan hoặc Structural Plan, ví dụ `Level 1`).

### Bước 2: Mở công cụ Dim Grid
1. Trên Ribbon tab **JNNTool**, tìm panel **Modeling**.
2. Bấm nút **Dim Grid** (nằm cạnh nút *Dim Beam*).
3. Cửa sổ **JNN - Tự Động Dim Grid** xuất hiện:
   - Dòng thông tin hiển thị số lượng trục phát hiện trong View.
   - Dropdown **Dimension Type** tự động chọn Linear Dimension Type của dự án.
   - Các checkbox vị trí (Trên, Dưới, Trái, Phải) và lớp Dim (Chi tiết, Tổng) đang được bật mặc định.

### Bước 3: Tạo Dim tự động cho toàn bộ hệ trục
1. Giữ nguyên các thiết lập mặc định:
   - 4 phía: Trên, Dưới, Trái, Phải
   - 2 lớp: Dim chi tiết (12 mm) + Dim tổng (8 mm)
2. Bấm nút **Tạo Dim Grid**.
3. **Kết quả mong đợi**:
   - Revit tạo ngay lập tức 4 chuỗi Dimension (hoặc 8 chuỗi nếu bật cả chi tiết và tổng ở 4 phía).
   - Các đường Dim đặt ngay ngắn, thẳng hàng, vuông góc với hệ trục, không bị đè lên bubble trục.
   - Thông báo trên thanh trạng thái: *"Đã tạo thành công X chuỗi kích thước cho Y trục."*

### Bước 4: Kiểm thử quét chọn trục cụ thể (Pick Grids)
1. Trong cửa sổ tool, bấm nút **Quét Chọn Trục...**
2. Quét chọn 3 trục bất kỳ trên mặt bằng, bấm **Finish** trên thanh Options Bar.
3. Bấm **Tạo Dim Grid**.
4. **Kết quả mong đợi**:
   - Chỉ tạo Dimension cho 3 trục vừa được quét chọn.

### Bước 5: Kiểm thử tắt/bật vị trí và lớp Dim
1. Bỏ chọn **Phía Dưới** và **Phía Phải**, chỉ giữ **Phía Trên** và **Phía Trái**.
2. Bỏ chọn **Dim tổng**, chỉ giữ **Dim chi tiết**.
3. Bấm **Tạo Dim Grid**.
4. **Kết quả mong đợi**:
   - Chỉ xuất hiện 1 chuỗi Dim chi tiết ở phía Trên và 1 chuỗi Dim chi tiết ở phía Trái.
