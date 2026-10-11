# Checklist Kiểm Thử Thủ Công — JNN Auto Structural Stair (Dựng Thang Kết Cấu Bê Tông)

Tài liệu hướng dẫn kiểm tra thủ công tính năng **Auto Structural Stair** trong bộ công cụ **JNNTool** trên Autodesk Revit 2024, 2025, 2026, 2027.

---

## 1. Chuẩn bị mô hình Revit mẫu
1. Mở bất kỳ dự án Revit nào (mới tạo hoặc có sẵn các tầng: Level 1 (0.00), Level 2 (+3.60m)).
2. Đảm bảo dự án có sẵn ít nhất một loại sàn kết cấu (Floor Type) và dầm bê tông (Framing FamilySymbol như 200x350 mm).

---

## 2. Các kịch bản kiểm thử

### Kịch bản 1: Thang chữ U (U-Shaped 2 vế đảo chiều 180° có chiếu nghỉ giữa)
1. Trên Ribbon tab **JNNTool**, panel **Modeling**, click nút **Stair Model**.
2. Cửa sổ **JNN Auto Structural Stair** mở ra ở chế độ modeless, không che khuất màn hình:
   - Dạng cầu thang: Chọn `Thang chữ U (2 vế 180° đảo chiều)`.
   - Hướng rẽ: Chọn `Rẽ Trái (Left - CCW)`.
   - Tầng chân (Base Level): Chọn `Level 1`, Offset = `0`.
   - Tầng đỉnh (Top Level): Chọn `Level 2`, Offset = `0` (Tổng chiều cao = 3600 mm).
   - Tổng số cổ bậc: `21` bậc -> Chiều cao bậc R = `171.4 mm`.
   - Số bậc vế 1 / vế 2: `11 / 10`.
   - Mặt bậc T: `280 mm`, Bề rộng vế W: `1100 mm`.
   - Dài chiếu nghỉ: `1200 mm`, Khe hở giữa 2 vế: `100 mm`.
   - Dày bản thang: `120 mm`, Dày chiếu nghỉ: `120 mm`.
   - Dầm đỡ: `Chỉ tạo dầm chiếu nghỉ` (hoặc `Tạo đầy đủ dầm`).
3. Quan sát khung **Interactive 2D Plan Preview** bên phải:
   - Bản thang vế 1, chiếu nghỉ và vế 2 hiển thị sắc nét, tỉ lệ chuẩn xác.
   - Các vạch bậc thang hiển thị đầy đủ, mũi tên dốc màu cam chỉ hướng UP.
   - Vị trí dầm đỡ nét đứt màu đỏ cam xuất hiện đúng chân chiếu nghỉ.
4. Click **Pick...** để click chọn 1 điểm trên mặt bằng Revit (hoặc để mặc định X=0, Y=0).
5. Click **Tạo Cầu Thang (Create Stair)**.
6. **Kết quả mong đợi**:
   - [ ] Hộp thoại thông báo tạo thành công hiển thị.
   - [ ] Mô hình 3D xuất hiện: Vế 1 nghiêng dốc từ Level 1 lên cao độ +1885 mm.
   - [ ] Chiếu nghỉ phẳng tại cao độ +1885 mm.
   - [ ] Vế 2 nghiêng dốc từ +1885 mm lên chạm mép sàn Level 2 (+3600 mm).
   - [ ] Bản vế thang và chiếu nghỉ tự động Join với nhau không có đường phân cách thừa.
   - [ ] Dầm bê tông đặt đúng mép chiếu nghỉ đỡ toàn bộ vế thang.
   - [ ] Nhấn `Ctrl + Z` (Undo) trên Revit: toàn bộ thang hoàn tác sạch sẽ trong 1 bước duy nhất (nhờ `TransactionGroup`).

---

### Kịch bản 2: Thang chữ L (L-Shaped 2 vế 90° vuông góc có chiếu nghỉ góc)
1. Trên giao diện tool, đổi Dạng cầu thang thành `Thang chữ L (2 vế 90° vuông góc)`.
2. Hướng rẽ: thử đổi giữa `Rẽ Trái` và `Rẽ Phải`.
3. Quan sát Preview 2D: vế 2 xoay vuông góc 90° theo đúng hướng rẽ.
4. Nhấn **Tạo Cầu Thang (Create Stair)**.
5. **Kết quả mong đợi**:
   - [ ] Tạo đúng bản thang chữ L với chiếu nghỉ vuông tại góc đổi hướng.

---

### Kịch bản 3: Thang thẳng 1 vế (Straight Flight)
1. Đổi Dạng cầu thang thành `Thang thẳng (Straight Flight)`.
2. Quan sát Preview: hiển thị 1 vế thang thẳng duy nhất chạy suốt nhịp.
3. Nhấn **Tạo Cầu Thang (Create Stair)**.
4. **Kết quả mong đợi**:
   - [ ] Tạo đúng bản thang thẳng có độ dốc chuẩn xác nối giữa 2 tầng.

---

### Kịch bản 4: Kiểm tra tính toán công thức Blondel (2R + T)
1. Điều chỉnh số bậc từ 21 xuống 18 bậc (R tăng lên 200 mm):
   - Chỉ số 2R + T đổi thành màu cảnh báo và nhãn hiển thị `Cần điều chỉnh bậc` hoặc `Lệch tiêu chuẩn`.
2. Điều chỉnh lại 21 hoặc 22 bậc (R ~ 163-171 mm, 2R + T ~ 607-622 mm):
   - Nhãn hiển thị màu xanh lá cây `Chuẩn TCVN (Lý tưởng)`.
