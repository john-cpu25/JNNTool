# Test Checklist — Quantity Takeoff & 3D Section Box (Bóc Tách Khối Lượng)

> Agent không chạy được Revit trực tiếp. File checklist này giúp người dùng test tay nhanh và toàn diện tính năng **Quantity Takeoff**.
> Vị trí nút lệnh: Ribbon Tab riêng **Quantity** → Panel **Takeoff** → **Quantity Takeoff**.

---

## 0. Chuẩn bị mô hình test

- Một mô hình Revit có đầy đủ các cấu kiện kết cấu & kiến trúc:
  - **Levels**: Tối thiểu 2–3 tầng (ví dụ: `Tầng 1`, `Tầng 2`, `Mái` hoặc `Level 1`, `Level 2`).
  - **Structural Columns**: Cột bê tông hoặc thép.
  - **Structural Framing**: Dầm bê tông hoặc dầm thép.
  - **Floors**: Sàn bê tông cốt thép.
  - **Walls**: Vách thang máy, tường ngoài hoặc tường ngăn.
  - **Structural Foundation**: Móng đơn, móng băng hoặc đài cọc.
  - **Structural Rebar** (nếu có): Thép sàn, thép dầm hoặc thép cột.
  - **Stairs / Roofs / Ceilings** (tùy chọn).

---

## 1. Mở cửa sổ & Giao diện (UI)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 1.1 | Bấm nút **Quantity Takeoff** trên Tab riêng `Quantity` (panel `Takeoff`) | Cửa sổ Modeless mở lên nhanh, hiển thị chuẩn Light Theme (nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, chữ `#2C3E50`, font chữ thường thanh thoát, không có banner tối màu ở đầu). |
| 1.2 | Kiểm tra 4 Thẻ chỉ số tổng quan (Top Metric Cards) | Hiển thị chính xác tổng Bê tông ($m^3$), Ván khuôn ($m^2$), Thép / Rebar (Tấn), và Tổng số lượng cấu kiện. |
| 1.3 | Click ra ngoài canvas Revit hoặc xoay view Revit | Cửa sổ Modeless vẫn mở và ghim trên Revit (`Owner = Revit`), người dùng vẫn thao tác được trên view Revit bình thường. |
| 1.4 | Đóng cửa sổ và bấm mở lại nhiều lần | Cửa sổ mở lại bình thường, **không bao giờ bị lỗi** `The Application object is being shut down`. |

---

## 2. Thống kê theo Tầng (Tab: By Level)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 2.1 | Chọn tab **By Level (Theo Tầng)** | Bảng hiển thị danh sách các Tầng trong dự án kèm Cao độ ($m$), Số lượng cấu kiện, Khối lượng bê tông ($m^3$), Diện tích cốp pha ($m^2$), Trọng lượng thép (Tấn), Tổng thể tích. |
| 2.2 | Click chọn 1 dòng Tầng bất kỳ (ví dụ: `Tầng 1`) | Bảng con bên dưới (Category Breakdown) lập tức cập nhật danh mục cấu kiện thuộc tầng đó (Dầm, Cột, Sàn, Vách...) với số lượng và khối lượng riêng. |
| 2.3 | Bấm nút **Box 3D** trên dòng của Tầng đó (hoặc Double-click dòng Tầng) | Revit tự động chuyển sang view 3D (hoặc tạo `{3D - JNN QTO}`), tự động co Section Box vừa vặn bao quanh toàn bộ cấu kiện của Tầng đó và zoom tới đối tượng! |

---

## 3. Thống kê theo Cấu kiện (Tab: By Category)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 3.1 | Chọn tab **By Category (Theo Cấu Kiện)** | Bảng liệt kê tổng hợp theo từng Category (Structural Framing, Structural Columns, Floors, Walls, Structural Foundation, Rebar...) với đầy đủ Số lượng, Thể tích ($m^3$), Diện tích ($m^2$), Chiều dài ($m$), Trọng lượng (Tấn). |
| 3.2 | Bấm nút **Box 3D** trên dòng của một Category (ví dụ: `Structural Columns`) | Revit tự động bật Section Box bao quanh toàn bộ các cột trong dự án/view. |

---

## 4. Chi tiết cấu kiện & Tìm kiếm (Tab: Element Details)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 4.1 | Chọn tab **Element Details (Chi Tiết Cấu Kiện)** | Bảng hiển thị chi tiết từng cấu kiện: Element ID, Category, Tầng, Family, Type Name, Mark, Vật liệu, Thể tích, Diện tích, Chiều dài, Thép (Tấn). |
| 4.2 | Gõ vào ô tìm kiếm (ví dụ: tên Type `300x600` hoặc Mark `C1` hoặc ID) | Danh sách DataGrid lọc tức thì theo từ khóa tìm kiếm. |
| 4.3 | Chọn Category trong dropdown hoặc Level trong dropdown | Dữ liệu lọc chính xác theo Category và Level được chọn. |
| 4.4 | Double-click vào một dòng cấu kiện (hoặc bấm **Box 3D**) | Section Box 3D lập tức thu hẹp bao quanh đúng cấu kiện đó với khoảng đệm nhìn rõ nét. |
| 4.5 | Tick chọn nhiều checkbox cấu kiện → Bấm nút **Select in Model** | Cấu kiện được highlight chọn trực tiếp trên Revit canvas. |

---

## 5. Xuất báo cáo Excel (.xls / .csv)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 5.1 | Bấm nút **Export Excel** | Hộp thoại lưu file mở ra với tên mặc định `JNN_QuantityTakeoff_YYYYMMDD_HHmm.xls`. |
| 5.2 | Bấm Save lưu file | Xuất file thành công, hiển thị hộp thoại hỏi *"Bạn có muốn mở file ngay bây giờ không?"*. |
| 5.3 | Bấm **Yes** mở file trong Microsoft Excel | File Excel mở lên với **3 Sheet riêng biệt**:
- Sheet 1: **By Level (Theo Tang)** — Đầy đủ tiêu đề, ngày giờ, bảng tổng hợp theo tầng, dòng Tổng cộng in đậm viền kép.
- Sheet 2: **By Category (Theo Cau Kien)** — Bảng tổng hợp theo loại cấu kiện kèm tổng thể tích, diện tích, thép.
- Sheet 3: **Element Details (Chi Tiet)** — Chi tiết tất cả cấu kiện với định dạng số chuẩn $0.00$ và $0.000$. |
| 5.4 | Thử lưu với đuôi `.csv` | Xuất file CSV định dạng chuẩn UTF-8, mở trên Excel không bị lỗi font tiếng Việt. |

---

## 6. Lọc Scope & Categories Filter

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 6.1 | Đổi Scope: `Entire Project` ↔ `Active View` ↔ `Current Selection` | Dữ liệu khối lượng và số lượng cấu kiện cập nhật tương ứng theo phạm vi đã chọn. |
| 6.2 | Bấm menu **Categories Filter ▼**:
- Thử chọn *All Structural Categories*
- Thử tick/bỏ tick từng category (như bỏ Windows/Doors) | Bảng dữ liệu tự động re-calculate và cập nhật đúng các categories được chọn. |

---

## 7. Đồ thị Trực quan hóa Khối lượng (Visual Preview Dashboard)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 7.1 | Mở công cụ Quantity Takeoff | Panel **Visual Preview** tích hợp sẵn ở bên phải cửa sổ, hiển thị đồ thị trực quan song song với bảng số liệu. |
| 7.2 | Bấm nút **[📊 Visual Preview]** trên thanh Toolbar trên cùng | Bảng Preview đóng/mở (toggle) linh hoạt; khi ẩn thì bảng số liệu tự động dàn đều toàn bộ chiều rộng cửa sổ. |
| 7.3 | Chế độ **By Level (Elevation Stack)**:
- Xem danh sách các tầng từ trên xuống
- Xem các thanh Bar tiến độ tỷ lệ khối lượng $m^3$ và phần trăm % | - Danh sách các tầng được sắp xếp tự nhiên theo cao độ tòa nhà từ trên xuống (MAI → T7 → ... → HAM).<br>- Độ dài thanh Bar phản ánh trực quan độ chênh lệch thể tích giữa các tầng.<br>- Hover vào thanh hiển thị Tooltip chi tiết (Bê tông, Cốp pha, Thép). |
| 7.4 | Click chọn một tầng trên biểu đồ By Level | Tầng đó được highlight viền xanh, đồng thời tự động đồng bộ và cập nhật bảng chi tiết cấu kiện (Category Breakdown) ở tab By Level bên trái! |
| 7.5 | Bấm nút **Box 3D** nhanh trên biểu đồ By Level | Revit lập tức tạo/cắt Section Box 3D của đúng tầng vừa bấm trên Preview! |
| 7.6 | Chuyển sang chế độ **By Category (Category Share)** | - Hiển thị biểu đồ phân bổ thể tích theo cấu kiện với bảng màu hiện đại (Floors: Xanh dương, Framing: Xanh lá, Columns: Cam, Walls: Tím...).<br>- Thanh bar mini và tỷ lệ % thể tích của từng loại cấu kiện.<br>- Có nút Box 3D nhanh cho từng Category. |

