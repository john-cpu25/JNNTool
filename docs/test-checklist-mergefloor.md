# Checklist Kiểm Thử — Merge Floor (Chế độ Active View & Selection)

> Tài liệu hướng dẫn kiểm thử tính năng **Merge Floor** (Gộp sàn tự động theo View hiện hành và quét chọn thủ công) cho add-in JNNTool trên Autodesk Revit (2024–2027).

---

## 1. Mục tiêu kiểm thử

1. **Chế độ Active View (Tự động 1-click không cần chọn sàn)**:
   - Tool tự động quét toàn bộ các đối tượng Sàn (Floor) đang hiển thị trong View hiện hành (`doc.ActiveView`).
   - Tự động gom nhóm theo **Loại sàn (FloorType)** và **Cao độ mặt sàn (Top Elevation $\pm 5\text{ mm}$)**.
   - Tự động nhận diện các cụm sàn tiếp giáp nhau / chạm cạnh / gần nhau ($\le 12\text{ mm}$) thông qua thuật toán phân cụm liên thông (`FindConnectedComponents`).
   - Tự động gộp từng cụm $\ge 2$ sàn chạm nhau thành 1 sàn liên tục duy nhất, triệt tiêu mọi đường phân cách thừa bên trong.
   - Các sàn đứng độc lập 1 mình được giữ nguyên 100%, không bị ảnh hưởng hay xóa nhầm.
2. **Chế độ Selection (Quét chọn thủ công)**:
   - Nếu đã chọn trước $\ge 2$ sàn trước khi bấm nút $\rightarrow$ gộp ngay các sàn đã chọn mà không cần hỏi thêm.
   - Nếu chưa chọn sàn $\rightarrow$ cho phép quét chọn bằng chuột (`PickObjects`) hoặc chọn chế độ Active View từ TaskDialog.
3. **Hình học & Khôi phục dữ liệu**:
   - Khôi phục chính xác Level, Height Offset, Structural, Comments, Mark từ sàn lớn nhất trong cụm.
   - Tự động kết nối lại toàn bộ liên kết Join Geometry với Cột / Dầm / Tường xung quanh.
   - Tự động highlight (chọn) các sàn mới vừa được gộp để người dùng dễ kiểm tra.

---

## 2. Mô hình mẫu chuẩn bị trên Revit

- Mở một file dự án Revit bất kỳ có mặt bằng kết cấu (Structural Plan hoặc Floor Plan).
- Chuẩn bị các trường hợp sàn:
  - **Trường hợp 1**: 2 hoặc 3 mảnh sàn cùng loại và cùng cao độ đặt sát cạnh nhau (ví dụ sàn import từ ETABS bị mesh thành nhiều tấm nhỏ, hoặc 2 ô sàn vẽ liền kề).
  - **Trường hợp 2**: 1 ô sàn khác loại hoặc khác cao độ nằm sát cạnh (ví dụ sàn ban công hạ cốt hoặc sàn vệ sinh dày khác).
  - **Trường hợp 3**: 1 sàn độc lập nằm riêng lẻ ở một khu vực khác (không chạm bất kỳ sàn nào).

---

## 3. Các bước kiểm thử chi tiết

### Kịch bản 1: Kiểm thử Active View (1-Click trên Ribbon)
1. Mở mặt bằng kết cấu (Structural Plan Tầng bất kỳ).
2. Nhấp chuột vào nút lớn **`Merge Floor (Active View)`** trên Panel **Modeling** của thanh Ribbon JNNTool (hoặc mở Command Palette gõ `Merge Floor View`).
3. **Kết quả mong đợi**:
   - Lệnh chạy ngay lập tức mà không cần dùng chuột quét chọn sàn.
   - Các mảnh sàn tiếp giáp nhau cùng loại và cùng cao độ được gộp thành 1 sàn duy nhất, các đường phân cách thừa bên trong biến mất hoàn toàn.
   - Sàn hạ cốt / khác loại hoặc sàn đứng độc lập không bị gộp chung, vẫn giữ nguyên.
   - Hiển thị hộp thoại báo cáo `Merge Floor (Active View)` ghi rõ:
     - Số sàn quét được trong View.
     - Số mảnh sàn đã gộp thành bao nhiêu sàn duy nhất.
     - Số sàn đứng riêng lẻ được giữ nguyên an toàn.
   - Các sàn mới vừa gộp được highlight (chọn) trên màn hình.

### Kịch bản 2: Kiểm thử Menu xổ xuống (SplitButton)
1. Bấm vào mũi tên xổ xuống của nút `Merge Floor` trên Ribbon.
2. Kiểm tra xem có 2 lựa chọn:
   - `Merge Floor (Active View)`: Chạy tự động trong view.
   - `Merge Floor (Selection)`: Bật chế độ dùng chuột quét chọn.
3. Chọn `Merge Floor (Selection)`: Bấm chuột quét chọn các sàn cụ thể rồi nhấn **Finish** trên Options Bar $\rightarrow$ gộp thành công các sàn đã chọn.

### Kịch bản 3: Kiểm thử Pre-selection (Chọn trước rồi bấm lệnh)
1. Dùng chuột chọn sẵn 2 mảnh sàn tiếp giáp nhau trên mặt bằng.
2. Bấm vào lệnh `Merge Floor` (Selection hoặc lệnh chính).
3. **Kết quả mong đợi**: Lệnh tự động nhận diện 2 sàn đã chọn và thực hiện gộp ngay lập tức, không bắt người dùng quét chọn lại.

### Kịch bản 4: Kiểm thử an toàn khi không có sàn nào tiếp giáp
1. Mở một mặt bằng chỉ có các sàn đứng độc lập (không có 2 sàn nào chạm nhau).
2. Chạy `Merge Floor (Active View)`.
3. **Kết quả mong đợi**: Tool thông báo rõ ràng: "Đã kiểm tra ... sàn trong View hiện hành nhưng không có mảnh sàn nào tiếp giáp/gần nhau để gộp. Mô hình của bạn đã được giữ nguyên an toàn." Không có lỗi xảy ra.

---

## 4. Bảng kiểm tra đạt / không đạt (Checklist)

| Mục kiểm tra | Tiêu chuẩn đạt | Kết quả |
|---|---|:---:|
| 1-Click Active View | Quét toàn bộ sàn trong view và tự gộp mà không cần chọn tay | [ ] Đạt |
| Giữ nguyên sàn độc lập | Sàn đứng một mình không bị xóa hoặc gộp nhầm | [ ] Đạt |
| Phân loại theo cao độ & loại sàn | Chỉ gộp các sàn có cùng FloorType và cùng cao độ mặt sàn ($\pm 5\text{ mm}$) | [ ] Đạt |
| Báo cáo kết quả chi tiết | Hộp thoại TaskDialog thông báo đầy đủ số lượng sàn quét, số sàn gộp, số sàn giữ nguyên | [ ] Đạt |
| Khôi phục Join Geometry | Sàn gộp mới tự động nối liên kết Join với Cột và Dầm xung quanh | [ ] Đạt |
| Highlight sau khi gộp | Tự động chọn các sàn mới tạo để người dùng dễ kiểm soát | [ ] Đạt |
| SplitButton Ribbon | Nút lớn mặc định là Active View, menu xổ xuống có cả Selection | [ ] Đạt |
| Command Palette & Theme | Xuất hiện trên bảng Command Palette và đổi màu sắc nét theo Theme | [ ] Đạt |
