# Test Checklist — Ribbon Theme & Icon Color Manager (UI Manager)

> Agent không chạy được Revit trực tiếp. File checklist này giúp người dùng test tay nhanh và toàn diện tính năng **UI Manager / Ribbon Theme**.
> Vị trí nút lệnh: Ribbon Tab **JNNTool** → Panel **General** → Nút **Ribbon Theme**.

---

## 0. Chuẩn bị

- Mở Autodesk Revit (2022, 2023, 2024, 2025, 2026 hoặc 2027) có cài đặt Add-in JNNTool.
- Mở bất kỳ dự án mẫu hoặc dự án trắng nào để thanh Ribbon JNNTool hiển thị đầy đủ các panel:
  - `JNN Rebar`
  - `Modeling`
  - `CSI x Revit`
  - `Layout`
  - `Stair`
  - `Quantity`
  - `General`

---

## 1. Mở cửa sổ & Giao diện (UI)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 1.1 | Bấm nút **Ribbon Theme** trên Panel **General** của Tab `JNNTool` | Cửa sổ Modeless mở lên nhanh, hiển thị chuẩn Light Theme (nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, chữ `#2C3E50`, font chữ thường thanh thoát, **không có banner tối màu** ở đầu). |
| 1.2 | Kiểm tra bo góc các thành phần (Buttons, Textbox, Cards, Preview) | Tất cả các nút bấm, ô nhập liệu, thẻ card màu và preview đều có bo góc **5px đồng bộ**, không góc vuông nhọn. |
| 1.3 | Click ra ngoài canvas Revit hoặc xoay view Revit | Cửa sổ Modeless vẫn mở và ghim trên Revit (`Owner = Revit`), người dùng vẫn thao tác được trên view Revit bình thường. |
| 1.4 | Đóng cửa sổ và bấm mở lại nhiều lần | Cửa sổ mở lại bình thường, **không bao giờ bị lỗi** `The Application object is being shut down`. |

---

## 2. Thử nghiệm Color Presets & Custom Hex

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 2.1 | Click vào các chip màu Preset (Apex Blue, Royal Indigo, Emerald Green, Sunset Orange, Crimson Red, v.v.) | Ô mã màu Hex cập nhật giá trị tương ứng, ô live swatch bên cạnh đổi màu tức thì, toàn bộ thẻ Live Preview ở panel bên phải đổi màu ngay lập tức theo thời gian thực. |
| 2.2 | Nhập thủ công mã Hex tùy ý (ví dụ `#0284C7` hoặc `#10B981`) vào ô Custom Hex Code | Swatch màu và toàn bộ preview bên phải tự động chuyển sang màu mới gõ. |

---

## 3. Thử nghiệm Icon Presentation Styles

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 3.1 | Chọn kiểu **Badge** | Live preview hiển thị icon trong thẻ nền gradient bo góc 5px với glyph trắng ở giữa (phong cách badge hiện đại). |
| 3.2 | Chọn kiểu **Glyph** | Live preview hiển thị biểu tượng vector trực tiếp bằng màu đã chọn trên nền trong suốt (phong cách minimalist thanh lịch). |
| 3.3 | Chọn kiểu **Circle** | Live preview hiển thị biểu tượng vector trắng nằm trong hình tròn gradient. |
| 3.4 | Chọn kiểu **Outline** | Live preview hiển thị thẻ có viền 1.3px màu chủ đạo, nền nhạt 12% tint và glyph cùng màu. |

---

## 4. Bộ lọc Live Ribbon Preview (Filter Tabs)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 4.1 | Click chọn pill filter **JNN Rebar** | Live preview chỉ hiển thị 12 nút của bộ công cụ Rebar (Slab, Beam, Column, Wall, Footing, Pile, Stair, Volume, Number, Unobscured, Isolate, Manager). |
| 4.2 | Click chọn pill filter **Modeling** / **CSI x Revit** / **Layout** / **Quantity** | Live preview lọc chính xác các nút thuộc Panel tương ứng. |
| 4.3 | Click chọn **All** | Hiển thị toàn bộ danh mục nút bấm của JNNTool. |

---

## 5. Áp dụng vào Revit Ribbon & Khôi phục mặc định

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 5.1 | Chọn màu (ví dụ `Emerald Green` hoặc `Royal Indigo`) và kiểu `Badge` → Bấm nút **Apply to Revit Ribbon** | Thanh trạng thái báo *"Applying icons to Revit Ribbon..."* và sau đó *"Updated ... ribbon icons successfully!"*. Toàn bộ các nút PushButton/SplitButton trên thanh Ribbon của Revit đổi màu và kiểu icon ngay lập tức mà không cần khởi động lại Revit! |
| 5.2 | Bấm nút **Reset to Default** | Thanh trạng thái báo *"Reset to default theme."*, cài đặt trở về màu chuẩn `#2563EB` và kiểu `Badge`, Ribbon Revit cập nhật về giao diện mặc định ban đầu. |
| 5.3 | Đóng Revit và mở lại Revit | Add-in tự động nạp cấu hình đã lưu trong `%AppData%\JNNTool\Settings\RibbonThemeSettings.json` khi khởi động (`OnStartup`), thanh Ribbon giữ nguyên màu sắc và kiểu icon đã cấu hình trước đó. |
