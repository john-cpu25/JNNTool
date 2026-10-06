# Checklist Kiểm Thử: JNN ETABS Converter (Phase 1: Foundation & MVP)

Tài liệu hướng dẫn kiểm thử thủ công công cụ **JNN ETABS Converter** trên Autodesk Revit (2024, 2025, 2026, 2027) kết nối trực tiếp với ETABS.

---

## 1. Mục tiêu kiểm thử
Xác nhận luồng chuyển đổi trực tiếp:
**ETABS (CSi API OAPI) → JNN Data Model → Revit Native Model (Level, Grid, Dầm, Cột)**
- Tự động nhận diện mô hình ETABS đang mở.
- Đọc cao độ tầng (Stories), lưới trục (Grids), cấu kiện dầm (Beams) và cột (Columns).
- Tự động tạo Level theo cao độ mm với dung sai tùy chỉnh (mặc định ±2 mm).
- Tự động gán FamilySymbol hoặc nhân bản (Duplicate) kích thước $b \times h$ tương ứng.
- Đặt tham số chia sẻ `JNN_*` (Traceability: `JNN_Source`, `JNN_ETABS_ID`, `JNN_ETABS_Section`, `JNN_Source_Hash`...).
- Kiểm tra tính năng đối soát biến động (Diffing): New / Updated / Unchanged.
- Khả năng hoàn tác (Undo 1 bước) thông qua TransactionGroup.

---

## 2. Chuẩn bị môi trường
1. Phần mềm **Autodesk Revit** (2024 / 2025 / 2026 / 2027).
2. Phần mềm **CSI ETABS** (v18, v19, v20, v21 hoặc v22+) đang mở sẵn một mô hình kết cấu (hoặc mô hình mẫu gồm vài nhịp dầm và cột).
3. Đã build add-in `JNNTool` thành công.

---

## 3. Các bước thực hiện

### Bước 1: Mở giao diện công cụ
1. Mở Revit, tạo một dự án mới từ template kết cấu (Structural Template) hoặc mở dự án hiện có.
2. Trên Ribbon, chọn Tab **JNNTool** → Panel **CSI x Revit** → bấm nút **ETABS Converter**.
3. Cửa sổ **JNN ETABS CONVERTER** sẽ xuất hiện dạng Modeless.

### Bước 2: Kết nối và đọc dữ liệu ETABS
1. Tại mục **1. NGUỒN DỮ LIỆU**, bấm nút **⚡ Kết Nối & Đọc ETABS Đang Mở**.
2. Kiểm tra thanh trạng thái hiển thị:
   - Thông báo kết nối thành công: `Kết nối thành công tới ETABS: <tên_file>.edb`.
   - Tiến trình đọc Stories, Grids, Materials, Sections, Frames (0% → 100%).
3. Tại mục **2. THÔNG TIN MÔ HÌNH ETABS**, kiểm tra các số liệu:
   - Tên file ETABS.
   - Đơn vị nạp (`N_mm_C`).
   - Số lượng Tầng (Levels) và Lưới trục (Grids).
   - Số lượng Cột (Columns) và Dầm (Beams) phát hiện.

### Bước 3: Thiết lập tùy chọn và Chuyển đổi
1. Tại mục **3. CẤU KIỆN CHUYỂN ĐỔI**, tích chọn:
   - ☑ Cột kết cấu (Columns)
   - ☑ Dầm kết cấu (Beams)
   - ☑ Cao độ tầng (Levels)
   - ☑ Lưới trục (Grids)
2. Tại mục **4. TÙY CHỌN CHUYỂN ĐỔI**:
   - ☑ Tự động nhân bản Family Type nếu chưa có (Auto Create Types).
   - ☑ Cập nhật cấu kiện đã tồn tại (Match ID & Update instead of duplicate).
   - ☑ Gắn tham số nguồn JNN_* phục vụ truy xuất.
3. Bấm nút **🚀 CHUYỂN ĐỔI VÀO REVIT**.

### Bước 4: Kiểm tra kết quả trong Revit
1. **Kiểm tra Cao độ (Levels)**:
   - Mở mặt đứng (Elevation View: South/North), kiểm tra các Level mới được tạo với đúng cao độ như trong ETABS.
2. **Kiểm tra Cột (Columns)**:
   - Mở 3D View, kiểm tra các cột kết cấu được đặt đúng vị trí, Base Level, Top Level và góc xoay.
3. **Kiểm tra Dầm (Beams)**:
   - Kiểm tra dầm kết nối chính xác giữa các cột, cao độ Z khớp với Story trong ETABS.
4. **Kiểm tra Tham số JNN (Traceability)**:
   - Chọn một dầm hoặc cột bất kỳ, xem bảng **Properties**:
     - `JNN_Source`: `ETABS`
     - `JNN_ETABS_ID`: Mã ID từ ETABS (ví dụ `B12`, `C5`)
     - `JNN_ETABS_Story`: Tên tầng
     - `JNN_ETABS_Section`: Tên tiết diện
     - `JNN_Source_Hash`: Mã hash 16 ký tự hexa
     - `JNN_Conversion_Status`: `New` hoặc `Updated`
5. **Kiểm tra Hoàn tác (Undo)**:
   - Nhấn `Ctrl + Z` (Undo). Toàn bộ mô hình vừa import được hoàn tác trong **1 bước duy nhất** (`JNN - Chuyển đổi ETABS sang Revit`).

### Bước 5: Kiểm tra cơ chế Update (Không bị nhân đôi cấu kiện)
1. Trong ETABS, sửa đổi tiết diện của một dầm (ví dụ từ `B300x500` thành `B400x600`).
2. Mở lại cửa sổ **JNN ETABS Converter**, bấm **⟳ Nạp Lại**.
3. Bấm **🚀 CHUYỂN ĐỔI VÀO REVIT**.
4. Kiểm tra trong Revit:
   - Dầm được cập nhật tiết diện mới.
   - Không bị tạo thêm dầm trùng lặp đè lên nhau.
   - Tham số `JNN_Conversion_Status` chuyển thành `Updated`.

### Bước 6: Kiểm thử Mode B — Nhập từ Tệp IFC (.ifc)
1. Trên cửa sổ **JNN ETABS CONVERTER**, chọn Radio button **Tệp IFC (Mode B)**.
2. Bấm nút **📂 Chọn Tệp IFC...** và chọn tệp `.ifc` xuất từ ETABS hoặc phần mềm kết cấu khác.
3. Kiểm tra thống kê: số lượng Tầng, Cột, Dầm, Vách, Sàn và các bộ thuộc tính `Pset_*` được nạp đầy đủ.
4. Tích chọn các cấu kiện cần chuyển đổi (bao gồm cả Vách và Sàn) rồi bấm **🚀 CHUYỂN ĐỔI VÀO REVIT**.
5. Mở 3D View kiểm tra các đối tượng Wall và Floor đã được sinh native, đúng bề dày và đúng lỗ mở.

### Bước 7: Kiểm thử Tra Cứu Nguồn Gốc (Property Viewer)
1. Trên Ribbon tab **JNNTool** → Panel **CSI x Revit** → bấm nút **Property Viewer**.
2. Chọn một cấu kiện kết cấu bất kỳ trong Revit (hoặc bấm nút **🔍 Chọn Cấu Kiện Khác** trên cửa sổ).
3. Kiểm tra bảng hiển thị:
   - Nguồn dữ liệu (`ETABS` hoặc `IFC`).
   - Mã ID gốc (`JNN_ETABS_ID`), GUID IFC (`JNN_IFC_GUID`).
   - Tiết diện, Vật liệu, Tầng và Mã băm `SourceHash`.
   - Danh sách toàn bộ tham số chi tiết và các thuộc tính IFC (`Pset_*`).

### Bước 8: Kiểm thử Báo Cáo Nghiệm Thu & Đối Soát Chất Lượng (QA Reports)
1. Sau khi thực hiện chuyển đổi thành công (Mode A hoặc Mode B), tại thanh công cụ dưới cùng, bấm nút **📄 Xuất Báo Cáo QA**.
2. Kiểm tra cửa sổ thông báo: Báo cáo đã được khởi tạo tự động tại đường dẫn file HTML (mở tự động trên trình duyệt mặc định).
3. Kiểm tra nội dung trang báo cáo HTML:
   - **Header & KPI Grid**: Tổng số cấu kiện nguồn, số lượng tạo mới, cập nhật, cảnh báo, lỗi và Tỷ lệ khớp chuẩn (%).
   - **Bảng 1: Thống kê số lượng**: Kiểm tra số lượng Cột, Dầm, Vách, Sàn, Tầng, Lưới giữa nguồn và Revit.
   - **Bảng 2: Kết quả đối soát hình học & dung sai**: Sai lệch cao độ tầng (±2mm), sai lệch chiều dài dầm cột (±2mm) với huy hiệu `PASS` / `WARN` / `FAIL`.
4. Mở thư mục chứa báo cáo (ví dụ `%TEMP%`), kiểm tra file `.csv` đi kèm:
   - Mở file `.csv` bằng Microsoft Excel hoặc Notepad.
   - Kiểm tra font tiếng Việt hiển thị chính xác (nhờ chuẩn UTF-8 with BOM).
   - Kiểm tra các cột `Nguon, MaID_Nguon, LoaiCauKien, RevitElementId, TietDien, Tang, TrangThai` có đúng mã Revit Element ID đã gán trong dự án.


