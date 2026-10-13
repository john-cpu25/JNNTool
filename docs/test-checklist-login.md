# Test Checklist — Account & License Manager (Login Module)

> Agent không thể chạy Revit trực tiếp. File checklist này giúp người dùng test tay nhanh và toàn diện tính năng **Account & License Manager** (`Tools/Login`).
> Vị trí nút lệnh: Ribbon Tab **JNNTool** → Panel **General** → Nút **Account / License**.

---

## 0. Chuẩn bị

- Mở Autodesk Revit (2022, 2023, 2024, 2025, 2026 hoặc 2027) có cài đặt Add-in JNNTool.
- Mở bất kỳ dự án mẫu hoặc dự án trắng nào.
- Tab Ribbon `JNNTool` hiển thị Panel **General** với nút **Account / License** và icon nhận diện.

---

## 1. Mở cửa sổ & Giao diện (UI)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 1.1 | Bấm nút **Account / License** trên Panel **General** của Tab `JNNTool` khi chưa kích hoạt bản quyền | Cửa sổ **LoginWindow** mở lên nhanh, hiển thị chuẩn Light Theme (nền sáng `#F5F7F9`, Card `#FFFFFF`, viền `#DCDDE1`, chữ `#2C3E50`, font chữ thường thanh thoát, **không có banner tối màu** ở đầu). |
| 1.2 | Kiểm tra bo góc các thành phần (Buttons, Textbox, Cards, Borders) | Tất cả các nút bấm, ô nhập Email, ô nhập Activation Code đều có bo góc **5px đồng bộ**, không góc vuông nhọn. |
| 1.3 | Click ra ngoài canvas Revit hoặc xoay view Revit | Cửa sổ Modeless vẫn mở và ghim trên Revit (`Owner = Revit`), người dùng vẫn thao tác được trên view Revit bình thường. |
| 1.4 | Đóng cửa sổ và bấm mở lại nhiều lần | Cửa sổ mở lại bình thường, **không bao giờ bị lỗi** `The Application object is being shut down`. |

---

## 2. Thử nghiệm Form Đăng nhập & Kích hoạt (LoginWindow)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 2.1 | Để trống Email hoặc Mã kích hoạt rồi bấm nút **Activate License** | Hiển thị thông báo hướng dẫn nhập Email và Activation Code, không crash. |
| 2.2 | Nhập Email không hợp lệ (ví dụ: `abc`) rồi bấm **Activate** | Hiển thị thông báo lỗi định dạng Email. |
| 2.3 | Nhập mã không đúng định dạng GUID hoặc mã không tồn tại | Hệ thống báo lỗi kích hoạt thân thiện (hoặc báo không kết nối được server nếu máy offline/chưa bật license server). |
| 2.4 | Click nút **Get Free Trial** | Nút hoạt động và gửi yêu cầu đăng ký dùng thử 30 ngày về server. |
| 2.5 | Click link hỗ trợ / website | Mở trình duyệt ngoài tới trang web hoặc hotline hỗ trợ JNNTool. |

---

## 3. Quản lý trạng thái Bản quyền (LicenseStatusWindow)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 3.1 | Mở **Account / License** khi đã có bản quyền Active trong máy | Cửa sổ **LicenseStatusWindow** tự động mở ra thay vì cửa sổ đăng nhập. |
| 3.2 | Kiểm tra thông tin hiển thị | Hiển thị đúng Email, Loại bản quyền (Trial / Annual / Perpetual), Ngày bắt đầu, Ngày hết hạn, Số ngày còn lại (Days Remaining) và Tên thiết bị (Device Name). |
| 3.3 | Bấm nút **Refresh Status** | Hệ thống đồng bộ lại trạng thái từ server mà không gián đoạn giao diện. |
| 3.4 | Bấm nút **Deactivate on This Machine** | Hiển thị xác nhận; nếu đồng ý, bản quyền được gỡ khỏi máy, xóa cache DPAPI an toàn và chuyển về giao diện Login. |

---

## 4. Xử lý Bản quyền hết hạn (LicenseExpiredWindow)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 4.1 | Bản quyền hết hạn khi khởi động lệnh | Cửa sổ **LicenseExpiredWindow** hiển thị lịch sự thông báo hết hạn và số ngày quá hạn. |
| 4.2 | Bấm nút **Renew / Buy License** | Điều hướng tới kênh gia hạn bản quyền. |
| 4.3 | Bấm nút **Enter New Key** | Mở cửa sổ kích hoạt lại để nhập mã mới. |

---

## 5. An toàn & Lưu trữ bảo mật (Security & Offline Mode)

| # | Bước kiểm tra | Kết quả mong đợi |
|---|---|---|
| 5.1 | Kích hoạt thành công rồi tắt Revit, mở lại khi không có kết nối Internet (Offline) | JNNTool đọc bản quyền đã mã hóa qua Windows DPAPI trong thư mục `%AppData%\JNNTool\License\client_license.dat`, người dùng vẫn dùng bình thường trong thời hạn Offline Grace Period (14 ngày). |
| 5.2 | Copy file `client_license.dat` sang máy tính khác | File không thể giải mã trên máy tính khác (bảo vệ bằng Windows DPAPI cấp CurrentUser/Machine) và hash phần cứng không khớp, ngăn chặn sao chép trái phép. |
