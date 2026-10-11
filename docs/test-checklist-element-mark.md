# Test checklist — Element Mark (Dầm · Cột · Sàn)

> Agent không chạy được Revit. Các bước dưới đây cần người dùng test tay.
> Nút: tab **JNNTool** → panel **Layout** → **Element Mark**.

## 0. Mô hình mẫu

- 3 Level: `Móng` (-1500), `Tầng 1` (0), `Tầng 2` (+3600), thêm `Tầng hầm 1` (-3000) nếu có.
- Lưới 3×3 trục, nhịp 6000. Cột 300×300 ở mọi giao điểm, từ Móng lên Tầng 2 (mỗi tầng một instance).
- Tầng 2: cột biên thu tiết diện 300×250, lệch tâm ~25–100 mm.
- Dầm Tầng 1, Tầng 2 theo cả 2 phương, **mỗi nhịp một instance** (bị cột cắt).
- Một dầm xiên ~30° và một dầm xiên ~60°.
- Sàn kết cấu: 2 tấm ở Tầng 1, 1 tấm ở Tầng 2, 1 sàn kiến trúc (Structural = No).

## 1. Mở cửa sổ

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 1.1 | Bấm **Element Mark** | Cửa sổ mở, không che khi click vào view (Owner = Revit) |
| 1.2 | Bấm lại nút | Không mở cửa sổ thứ 2, chỉ kích hoạt cửa sổ cũ |
| 1.3 | Tab **Mã tầng {L}** | `Tầng 1 → 1`, `Tầng 2 → 2`, `Tầng hầm 1 → B1`, `Móng → M` |
| 1.4 | Sửa mã `Móng` thành `F` | Ví dụ ở tab Sàn cập nhật ngay |

## 2. Dầm

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 2.1 | Mở mặt bằng Tầng 1, phạm vi **View hiện hành**, mặc định, **Xem trước** | Dầm X hàng trên cùng: `DS1X01.1`, `DS1X01.2`, … (liên tục qua cột). Dầm Y cột trái: `DS1Y01.1`, … |
| 2.2 | Hướng **Dưới → Trên**, **Phải → Trái**, Xem trước | Hàng dưới cùng thành `DS1X01`, nhịp đánh từ phải sang |
| 2.3 | Dầm xiên 30° và 60° | 30° nằm trong nhóm X, 60° nằm trong nhóm Y |
| 2.4 | Bỏ tick **Gộp nhịp** | Mỗi nhịp một số: `DS1X01`, `DS1X02`, … không có `.1` |
| 2.5 | Xoá một nhịp giữa của hàng (tạo khe > 1000) | Hàng đó tách thành 2 dầm khác số |
| 2.6 | Xoay view (Crop region rotate hoặc True North) rồi Xem trước | Thứ tự vẫn đúng như nhìn trên màn hình |
| 2.7 | Đổi mẫu thành `{P}{L}-{D}{N:000}` | Ví dụ `DS1-X001`. Mẫu thiếu `{N}` → hiện cảnh báo trùng Mark |

## 3. Cột

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 3.1 | Phạm vi **Toàn dự án**, chỉ bật Cột, Xem trước | Cùng vị trí từ Móng đến Tầng 2 cùng Mark (`C1` ở cả 3 tầng) |
| 3.2 | Cột biên thu tiết diện lệch ≤ 300 mm | Vẫn cùng Mark với cột tầng dưới |
| 3.3 | Ưu tiên **Theo cột dọc**, **Phải → Trái**, **Dưới → Trên** | `C1` ở góc phải dưới, tăng dần lên trên rồi sang trái |
| 3.4 | Đánh Mark | Không bật cảnh báo "duplicate Mark values" |

## 4. Sàn

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 4.1 | Chỉ bật Sàn, Toàn dự án, Xem trước | 2 tấm Tầng 1 → `S1`, Tầng 2 → `S2` |
| 4.2 | Sàn kiến trúc | Không có trong danh sách khi tick "Chỉ sàn kết cấu" |

## 5. Ghi, Undo, ghi đè

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 5.1 | **Đánh Mark** | Hộp thoại tổng kết số đã ghi / không đổi / lỗi / thời gian. Cột "Mark cũ" cập nhật |
| 5.2 | `Ctrl+Z` một lần | Toàn bộ Mark trở về như cũ |
| 5.3 | Chọn **Chỉ phần tử chưa có Mark**, Xem trước | Phần tử đã có Mark hiện "Giữ nguyên", màu xám |
| 5.4 | Tham số ghi = `Comments` | Ghi vào Comments, Mark không đổi |
| 5.5 | Tham số ghi = tên không tồn tại | Báo lỗi trong tổng kết, Revit không crash |
| 5.6 | Bấm một dòng trong bảng xem trước | Phần tử tương ứng được chọn trong Revit |
| 5.7 | Lọc **Sẽ ghi** | Chỉ còn các dòng có thay đổi |

## 6. Ổn định

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 6.1 | Mở Sheet rồi Xem trước với phạm vi View hiện hành | Thông báo cần mở mặt bằng, không crash |
| 6.2 | Đóng dự án khi cửa sổ đang mở, bấm Xem trước | Báo "Không có dự án đang mở" |
| 6.3 | Đóng và mở lại cửa sổ | Giữ thông số lần trước. **Mặc định** đưa về DS / C / S |
| 6.4 | Mô hình lớn (> 2000 dầm), Toàn dự án | Xong trong vài giây, con trỏ chờ hiển thị |
| 6.5 | Test trên Revit 2024, 2025, 2026, 2027 | Như trên |
