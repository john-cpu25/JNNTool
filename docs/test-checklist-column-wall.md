# Checklist Kiểm Thử Thủ Công — Cốt Thép Cột & Vách (Column & Wall Rebar)

Tài liệu hướng dẫn kiểm tra thủ công tính năng **Rebar Column** và **Rebar Wall** trong bộ công cụ **JNNTool Rebar Suite** trên Revit.

---

## I. Kiểm Thử Thép Cột (`Rebar Column`)

### 1. Chuẩn bị mô hình Revit mẫu
1. Mở Revit (2024, 2025, 2026 hoặc 2027).
2. Tạo 3 tầng: Level 1, Level 2, Level 3 (chiều cao mỗi tầng khoảng 3300 - 3600 mm).
3. Đặt một chuỗi cột liên tục qua các tầng tại cùng một vị trí tim trục:
   - Tầng 1: Cột 500x600 mm.
   - Tầng 2: Cột 400x500 mm (thu hẹp tiết diện để kiểm tra bẻ cổ chai cranked lap).
   - Tầng 3: Cột 400x500 mm (tầng mái / trên cùng).
4. Dựng dầm giao đỡ sàn tại đỉnh cột mỗi tầng (chiều cao dầm khoảng 500 mm).

### 2. Các bước thực hiện
1. Quét chọn chuỗi cột từ Level 1 đến Level 3.
2. Trên Ribbon Revit, tab **JNN Rebar**, nhấn nút **Rebar Column**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Cột Theo Chuỗi Tầng** hiển thị:
   - **Tab Thép Dọc & Cổ Chai**:
     - Chọn đường kính: `D20` hoặc `D22`.
     - Số thanh cạnh B: `3`, số thanh cạnh H: `4`.
     - Kiểm tra tùy chọn: *Bẻ cổ chai (Cranked lap) tỉ lệ 1:6*.
   - **Tab Cốt Đai & Gia Cường**:
     - Đường kính đai: `D8` hoặc `D10`.
     - Khoảng cách đai dày: `100 mm` (vùng gối L/6 và nút khung).
     - Khoảng cách đai thưa: `200 mm` (giữa cột).
     - Kiểu đai: `CN` (Chữ nhật), `AB` hoặc `C-Tie`.
   - **Tab Mặt Cắt Dọc & Ngang**:
     - Bật *Tự động tạo MCD & MCN cột*.
4. Nhấn **Tạo Thép Cột**.

### 3. Kết quả mong đợi
- [ ] Thép dọc được tạo bao quanh chuỗi cột theo đúng số thanh đã cài đặt (3 thanh theo B, 4 thanh theo H).
- [ ] Tại vị trí giao giữa Tầng 1 (500x600) lên Tầng 2 (400x500), cốt thép dọc được bẻ dốc cổ chai chuẩn 1:6 neo thẳng vào thân cột tầng trên.
- [ ] Tại tầng trên cùng, thép dọc kết thúc phẳng neo trong dầm/mũ cột, không nhô nối chồng lên trời.
- [ ] Vùng gối và nút dầm cột có mật độ đai dày a100, vùng thân giữa có đai thưa a200.
- [ ] View Mặt Cắt Dọc (MCD_Col_C1) được tạo trong Project Browser hiển thị rõ toàn bộ cốt thép và nhãn chi tiết.

---

## II. Kiểm Thử Thép Vách (`Rebar Wall`)

### 1. Chuẩn bị mô hình Revit mẫu
1. Vẽ 1 bức vách thẳng bê tông (Basic Wall dày 250 mm hoặc 300 mm, dài khoảng 3000 - 4000 mm, cao 3300 mm).
2. Vẽ thêm 1 bức vách tầng hầm có độ dày 350 mm.

### 2. Các bước thực hiện
1. Chọn bức vách trong mặt bằng hoặc 3D view.
2. Trên Ribbon **JNN Rebar**, nhấn nút **Rebar Wall**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Vách 2 Lớp** hiển thị:
   - **Tab Thép Đứng**:
     - Đường kính: `D12`, khoảng cách a = `150 mm`.
     - Hệ số nối chồng: `40d` (TCVN 5574:2018).
   - **Tab Thép Ngang & Đai Biên**:
     - Đường kính: `D10`, khoảng cách a = `150 mm`.
     - Neo uốn đầu vách: `250 mm`.
     - Bật *Bố trí đai C ghim 2 đầu biên vách*, đường kính `D8` a = `150 mm`.
   - **Tab Bảo Vệ & Bản Vẽ**:
     - Tùy chọn *Vách tầng hầm / bể nước (chống thấm)*: Thử nghiệm bật và tắt để xem độ dày lớp bảo vệ (20 mm vs 35 mm).
     - Bật *Tự động tạo MCD vách*.
4. Nhấn **Tạo Thép Vách**.

### 3. Kết quả mong đợi
- [ ] Sinh 2 lớp cốt thép đứng đối xứng, chia đều theo chiều dài vách. Đỉnh thép đứng nhô lên đoạn 40d = 480 mm cho tầng tiếp theo.
- [ ] Sinh 2 lớp cốt thép ngang rải đều theo chiều cao vách, 2 đầu thanh ngang có móc vuông 90° dài 250 mm neo quay vào trong lòng vách.
- [ ] Hai đầu mép biên của vách có các móc C ôm giữ lấy 2 lớp thép đứng và ngang ngoài cùng.
- [ ] Đối với vách bật chế độ chống thấm, lớp bảo vệ dày 35 mm ôm sâu hơn vào thân vách.
- [ ] View Mặt Cắt Dọc (MCD_Wall_...) được tạo tự động với khung nhìn chuẩn ôm trọn bức vách.
