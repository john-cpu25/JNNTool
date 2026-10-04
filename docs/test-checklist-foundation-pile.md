# Checklist Kiểm Thử Thủ Công — Cốt Thép Móng & Cọc (Foundation & Pile Rebar)

Tài liệu hướng dẫn kiểm tra thủ công tính năng **Rebar Footing** (Móng & Đài Cọc) và **Rebar Pile** (Cọc Khoan Nhồi & Cọc Vuông) trong bộ công cụ **JNNTool Rebar Suite** trên Revit.

---

## I. Kiểm Thử Thép Móng & Đài Cọc (`Rebar Footing`)

### 1. Chuẩn bị mô hình Revit mẫu
1. Mở mô hình kết cấu Revit (2024, 2025, 2026, 2027).
2. Đặt các cấu kiện móng (`Structural Foundation`):
   - Móng đơn kích thước 2000x2000x800 mm.
   - Đài cọc kích thước 3000x2500x1200 mm.

### 2. Các bước thực hiện
1. Chọn móng đơn hoặc đài cọc trên mặt bằng hoặc 3D view.
2. Trên Ribbon Revit tab **JNN Rebar**, nhấn nút **Rebar Footing**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Móng & Đài Cọc** hiển thị:
   - **Tab Lưới Thép Đáy**:
     - Thép phương X: `D14` khoảng cách a = `150 mm`.
     - Thép phương Y: `D14` khoảng cách a = `150 mm`.
     - Bật *Uốn móc 90° đứng lên ở 2 đầu thép lưới đáy*, chiều cao móc = `200 mm`.
   - **Tab Lưới Thép Trên (Đài Cọc)**:
     - Chọn đài cọc và tích chọn: *Bố trí lưới cốt thép cấu tạo mặt trên*.
     - Thép trên X và Y: `D12` a = `200 mm`, móc gập xuống `150 mm`.
   - **Tab Thép Chờ Cột & Bảo Vệ**:
     - Bật *Tạo thép chờ cột uốn chân vịt cắm vào móng*.
     - Đường kính: `D20`, số thanh B x H = `3 x 3` (8 thanh).
     - Đoạn chân vịt: `300 mm`.
     - Chiều cao chờ lên mặt móng: `800 mm` (40d).
     - Đai định vị cổ móng: `D8` a = `150 mm`, số lượng `4` đai.
     - Lớp bảo vệ: Đáy `50 mm`, bên hông `50 mm`, mặt trên `50 mm`.
4. Nhấn **Tạo Thép Móng**.

### 3. Kết quả mong đợi
- [ ] Lưới thép đáy phương X và phương Y đan so le chuẩn xác, 2 đầu thanh có móc vuông uốn đứng lên cao 200 mm.
- [ ] Khi bật lưới thép mặt trên, sinh 2 lớp thép trên có móc gập xuống.
- [ ] Thép chờ cột có đoạn chân vịt 300 mm nằm tựa trên lưới thép đáy và vươn cao 800 mm lên trên đỉnh móng, được định vị bởi 4 đai cổ móng.
- [ ] Toàn bộ cốt thép hiển thị rõ ràng, không bị khuất (Unobscured).

---

## II. Kiểm Thử Thép Cọc & Xuất Tọa Độ (`Rebar Pile`)

### 1. Chuẩn bị mô hình Revit mẫu
1. Đặt một số cọc tròn (đường kính D800 hoặc D1000, dài 15m) và cọc vuông (400x400 mm).

### 2. Các bước thực hiện
1. Chọn các cọc cần tạo thép.
2. Trên Ribbon tab **JNN Rebar**, nhấn nút **Rebar Pile**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Cọc & Xuất Tọa Độ** hiển thị:
   - **Tab Loại Cọc & Thép Dọc**:
     - Thép dọc: `D20`, số thanh = `8`.
     - Đoạn neo ngàm vào đài: `800 mm`.
     - Lớp bảo vệ: `50 mm`.
   - **Tab Cốt Đai & Vành Gia Cường**:
     - Cốt đai: `D10`.
     - Đai dày đầu cọc: a = `100 mm`, chiều dài vùng dày = `2000 mm`.
     - Đai thưa thân cọc: a = `200 mm`.
     - Bật *Tạo vành đai gia cường chống bẹp lồng thép*: `D14` khoảng cách `2000 mm`.
4. Nhấn **Tạo Thép Cọc**:
   - Kiểm tra lồng thép tròn, các thanh dọc uốn nhô ngàm vào đài, cốt đai phân vùng dày/thưa và các vành gia cường bên trong lồng.
5. Nhấn nút **Xuất Tọa Độ CSV**:
   - Kiểm tra file `JNN_ToaDoCoc_*.csv` được sinh trên Desktop với đầy đủ tên cọc, tọa độ X, Y, Z đỉnh cọc và chiều dài cọc.

### 3. Kết quả mong đợi
- [ ] Lồng thép cọc tròn gồm 8 thanh bố trí tròn đều bán kính lồng.
- [ ] Đai xoắn/tròn bố trí dày a100 tại 2m đầu cọc và thưa a200 dọc thân cọc.
- [ ] Các vành đai gia cường D14 bố trí đều đặn cách nhau 2m dọc thân cọc.
- [ ] File CSV xuất ra chuẩn định dạng, mở tốt trên Excel.
