# NhanStr - Revit Plugin Suite

Dự án quản lý và phát triển bộ công cụ hỗ trợ Revit (Add-in) cho NhanStr.

## 🛠 Cấu trúc Ribbon (Giao diện người dùng)

Tất cả các công cụ được tập hợp trong Tab **NhanStr** trên thanh Ribbon của Revit.

| Panel | Công cụ | Trạng thái | Mô tả |
| :--- | :--- | :---: | :--- |
| **General** | Advanced Filter | ✅ | Bộ lọc đối tượng nâng cao. |
| | Reload CAD Links | ✅ | Tải lại các link CAD bị đổi tên/đường dẫn. |
| | Concrete Lining | ✅ | Tạo bê tông lót tự động. |
| **Modeling** | Disallow Wall Joins | ✅ | Quản lý nối tường hàng loạt. |
| | Link To Model | ✅ | Công cụ hỗ trợ Link. |
| | Add NS-EW | ✅ | Thêm ký hiệu phương hướng. |
| | Floor Separation | ✅ | Chia sàn thành nhiều mảng nhỏ. |
| **Views - Sheets** | Add Views To Sheets | ✅ | Thêm nhiều View vào nhiều Sheet. |
| | Rename Views | ✅ | Đổi tên View/Sheet hàng loạt. |
| | Title Location | ✅ | Căn chỉnh vị trí tiêu đề View. |
| | Arrange Views | ✅ | Sắp xếp View tự động trên Sheet. |

## 🏗 Kế hoạch hiện tại (Task Tracking)

### 1. Hoàn thiện bộ cài đặt (Installer)
- [ ] Cấu hình file `Package.wxs` cho MSI.
- [ ] Tích hợp logo thương hiệu vào giao diện cài đặt.
- [ ] Loại bỏ các bước không cần thiết (License Agreement) để cài đặt nhanh.
- [ ] Kiểm tra tính năng Gỡ cài đặt (Uninstall).

### 2. Tinh chỉnh UI/UX
- [x] Cải thiện giao diện Tool Reload CAD.
- [ ] Đồng bộ hóa icon cho tất cả các nút (đang cập nhật trong thư mục Resources).
- [ ] Thêm Tooltip chi tiết tiếng Việt cho các nút còn lại.

### 3. Nghiên cứu & Phát triển (R&D)
- [ ] [Tính năng mới] ... (đang chờ yêu cầu)

## 📁 Cấu trúc thư mục
- `Core/`: Chứa `App.cs` (khởi tạo Ribbon) và các logic chính.
- `Tools/`: Mỗi thư mục con là một tính năng riêng biệt.
- `Resources/`: Chứa các icon (.png) của các công cụ.
- `Installer/`: Chứa mã nguồn cho bộ cài đặt WiX Toolset.

---
*Cập nhật lần cuối: 12/04/2026*
