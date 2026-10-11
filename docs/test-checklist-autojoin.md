# Checklist Kiểm Thử Thủ Công — Tool Auto Join (JNNTool)

## 1. Mục đích
Xác nhận tính năng tự động Join và đảo thứ tự liên kết hình học theo thứ tự ưu tiên:
$$\text{Column (Cột)} > \text{Beam (Dầm)} > \text{Floor (Sàn)}$$
hoạt động chính xác, ổn định và mượt mà trên Revit (2022 – 2027).

---

## 2. Mô hình mẫu chuẩn bị
1. Tạo một dự án Revit mẫu với:
   - 2 Level (Level 1, Level 2).
   - 4 Cột bê tông (`Structural Column` hoặc `Column`).
   - 4 Dầm bê tông (`Structural Framing`) nối giữa các cột.
   - 1 Tấm Sàn bê tông (`Floor`) phủ qua vùng dầm và cột.
2. Cố tình tạo một số lỗi giao cắt:
   - Một số dầm và cột chưa được Join với sàn (vết cắt bị trùng hoặc sàn ăn vào cột).
   - Dùng lệnh *Switch Join Order* thủ công của Revit để sàn cắt dầm hoặc sàn cắt cột (thứ tự ngược).

---

## 3. Các bước kiểm thử

### Test Case 1: Bấm nút Auto Join (Chế độ Active View)
1. Mở mặt bằng Level 2 hoặc view 3D.
2. Trên thanh Ribbon Revit, chuyển tới Tab **JNNTool**.
3. Tại Panel **CSI x Revit**, click trực tiếp vào nút **Auto Join (Active View)**.
4. **Kết quả mong đợi**:
   - Tool thực thi ngay lập tức trong 1 transaction `"JNN - Auto Join Elements"`.
   - Bảng thông báo hoàn thành hiện lên tóm tắt:
     - Số cặp đã Join mới.
     - Số cặp đã đảo thứ tự đúng ưu tiên (Switch Order).
     - Số cặp đã đúng thứ tự sẵn.
     - Thời gian chạy (~0.1s - 1s).
   - Không xuất hiện bất kỳ popup warning nào của Revit (đã được lọc tự động bởi `DismissWarningsPreprocessor`).

### Test Case 2: Kiểm tra thứ tự cắt hình học (Geometry Cut Order)
1. Cắt một mặt cắt (Section) đi qua Cột, Dầm và Sàn.
2. Kiểm tra giao diện mặt cắt:
   - **Cột vs Dầm**: Bê tông Cột xuyên suốt, nét dầm bị ngắt tại mép cột (Cột cắt Dầm).
   - **Cột vs Sàn**: Bê tông Cột xuyên suốt qua chiều dày sàn (Cột cắt Sàn).
   - **Dầm vs Sàn**: Bê tông Dầm ăn lên hết chiều cao dầm, bản sàn bị ngắt tại mép dầm (Dầm cắt Sàn).

### Test Case 3: Chế độ Toàn bộ Dự án (Entire Project)
1. Click vào mũi tên nhỏ bên dưới nút **Auto Join** trên Panel **CSI x Revit**.
2. Chọn **Auto Join (Entire Project)**.
3. **Kết quả mong đợi**: Tool quét toàn bộ mô hình trong mọi tầng/view và thực hiện liên kết hoàn tất, hiển thị bảng tổng kết.

### Test Case 4: Kiểm thử Undo
1. Nhấn `Ctrl + Z` (Undo) một lần duy nhất.
2. **Kết quả mong đợi**: Toàn bộ thao tác Join được hoàn tác sạch sẽ về trạng thái trước đó trong 1 bước duy nhất.
