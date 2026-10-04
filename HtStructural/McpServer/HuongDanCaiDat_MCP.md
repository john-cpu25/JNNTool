# Hướng dẫn Cài đặt & Sử dụng MCP Scripts cho Người Dùng Cuối

Để chạy các công cụ tự động hóa (Automations) được triển khai qua hệ thống MCP (Model Context Protocol), máy tính của bạn cần đáp ứng một số yêu cầu môi trường cơ bản sau đây.

## 1. Yêu cầu Hệ Thống (Bắt buộc)
Mã nguồn MCP của HtStructural được thực thi bởi **Node.js**. Bạn cần đảm bảo đã cài đặt Node.js trên máy tính:

1. Tải bộ cài Node.js tại trang chủ: [https://nodejs.org/](https://nodejs.org/) (Chọn bản **LTS - Recommended For Most Users**).
2. Chạy file cài đặt, nhấn **Next** liên tục để hoàn tất (Đảm bảo đã tích chọn mục "Add to PATH" trong quá trình cài đặt).
3. Sau khi cài xong, mở **Command Prompt** (cmd) và gõ lệnh sau để kiểm tra:
   ```cmd
   node -v
   ```
   *Nếu hiển thị phiên bản (ví dụ: `v20.11.0`), bạn đã cài đặt thành công.*

## 2. Lần Chạy Đầu Tiên trên Revit
- Khi bạn bấm vào bất kỳ công cụ MCP nào lần đầu tiên trên thanh Ribbon của Revit (Ví dụ: Vẽ thép dầm, Cắt dầm...), hệ thống Add-in sẽ **tự động khởi tạo môi trường và tải về các thư viện nền tảng (ws, uuid)**.
- Quá trình này sẽ diễn ra ngầm trong khoảng **10 đến 20 giây**.
- Sẽ có một bảng thông báo nhỏ hiện lên báo hiệu việc khởi tạo. Vui lòng chờ đợi cho đến khi công cụ xuất hiện.

*(Những lần bấm sau này, công cụ sẽ tự khởi động ngay lập tức vì môi trường đã được thiết lập).*

## 3. Quản lý Máy Chủ MCP Cục Bộ
Các lệnh (Scripts) khi chạy sẽ gửi yêu cầu trực tiếp đến Add-in Revit của bạn thông qua một "Máy Chủ Ảo" (MCP Server) chạy ngay bên trong phần mềm Revit.

- Trong Ribbon Add-in, bạn có thể tìm thấy nút **Bật/Tắt MCP Server**. 
- Nút này sẽ mở ra cửa sổ cho biết trạng thái của cổng kết nối (Port 7777). 
- Thông thường, bạn **không cần thao tác** gì với cửa sổ này, nhưng bạn có thể dùng nó để xem Log (Lịch sử xử lý) khi mã nguồn đang phân tích kết cấu.

## 4. Bảo mật và Cập nhật Thuật Toán
- Toàn bộ thuật toán JS bạn chạy đều được **tải trực tiếp từ máy chủ Cloud (VPS)** của HtStructural khi có mạng internet.
- Các file `.js` sau khi chạy xong sẽ tự động bị **xoá sạch** khỏi máy tính của bạn.
- Khi có bản nâng cấp hoặc sửa lỗi thuật toán, đội ngũ Kỹ thuật sẽ cập nhật trên máy chủ, và bạn sẽ **tự động nhận được bản sửa lỗi ngay trong lần bấm tiếp theo** mà không cần tải lại file cài đặt (Installer) mới.

## 5. (Dành cho Lập trình viên) Kết nối AI IDE với Revit
Nếu bạn muốn sử dụng các công cụ AI (như **Cursor, Windsurf, Claude Desktop, Antigravity**) để tự động ra lệnh điều khiển Revit thông qua Chat/Prompt, bạn cần thiết lập như sau:

1. Add-in Revit tích hợp sẵn máy chủ ở địa chỉ `ws://127.0.0.1:7777`.
2. Do các phần mềm AI sử dụng giao thức luồng văn bản (STDIO) thay vì mạng, bạn sẽ cần sử dụng file **"Cầu nối" (Proxy)**.
3. File này đã được **tích hợp sẵn** khi bạn cài đặt Add-in HtStructural. Đường dẫn mặc định của file thường nằm tại:
   `%AppData%\HtStructural\McpServer\revit-mcp-proxy.exe`
4. Tiến hành cấu hình AI theo 1 trong các hướng dẫn bên dưới:

### A. Cấu hình cho Cursor / Windsurf
- Mở IDE, vào **Settings -> MCP (Model Context Protocol) -> Add New MCP Server**.
- Điền các thông tin:
  - Tên: `HtStructural-Revit`
  - Type: `command`
  - Command: `%AppData%\HtStructural\McpServer\revit-mcp-proxy.exe` (Copy dán nguyên đường dẫn này)
- Bấm Save/Refresh. AI sẽ tự động đọc được toàn bộ các lệnh API của Revit.

- **Dành cho ai thích thao tác file Config (JSON):** Nếu file cấu hình MCP của Cursor/Windsurf mở ra dưới dạng JSON, bạn có thể copy đoạn này (nhớ đổi `<Tên_User>` thành tên người dùng Windows của bạn):
```json
"HtStructural-Revit": {
  "command": "C:\\Users\\<Tên_User>\\AppData\\Roaming\\HtStructural\\McpServer\\revit-mcp-proxy.exe",
  "args": []
}
```

### B. Cấu hình cho Claude Desktop
- **Tuyệt vời! Bạn không cần làm gì cả.** 
- Quá trình cấu hình đã được tự động hóa hoàn toàn. Trong lúc cài đặt Add-in, hệ thống đã tự động tìm và khai báo đường dẫn Proxy vào file `claude_desktop_config.json` của bạn.
- Bạn chỉ cần **Khởi động lại Claude Desktop**. Nếu thấy biểu tượng cái Búa (Tools) hiện lên danh sách công cụ của Revit là quá trình cài đặt đã thành công.

### C. Cấu hình cho Antigravity
- Trong giao diện thiết lập MCP Server của Antigravity, bạn chỉ cần cấu hình:
  - Chế độ: `STDIO`
  - Lệnh thực thi: `%AppData%\HtStructural\McpServer\revit-mcp-proxy.exe`

- Hoặc nếu bạn muốn dán trực tiếp vào file `mcp_config.json` (nhanh nhất), hãy copy đoạn code mẫu sau và đổi chữ `<Tên_User>` thành tên người dùng Windows của bạn:
```json
"HtStructural-Revit": {
  "command": "C:\\Users\\<Tên_User>\\AppData\\Roaming\\HtStructural\\McpServer\\revit-mcp-proxy.exe",
  "args": [],
  "disabled": false,
  "disabledTools": []
}
```

- Antigravity sẽ tự động kết nối và có thể tương tác 2 chiều với mô hình Revit.

Sau khi kết nối, bạn có thể gõ vào Chat: *"Hãy lấy thông tin các cấu kiện tôi đang chọn"* hoặc *"Nối các dầm lại với nhau"*, AI sẽ tự gọi lệnh thông qua Proxy và tác động thẳng vào bản vẽ Revit.

## 6. (Dành cho Lập trình viên) Kết nối Gemini Docs MCP Server
Để Trợ lý AI (Antigravity, Cursor, Claude Code) luôn tra cứu được tài liệu API, SDK và Schema mới nhất của Google Gemini trực tiếp trong phiên làm việc:

1. **Cài đặt nhanh qua NPX:**
   ```bash
   npx add-mcp "https://gemini-api-docs-mcp.dev"
   ```
2. **Hoặc cấu hình thủ công trong file `mcp_config.json`:**
   ```json
   "gemini-docs": {
     "command": "npx",
     "args": ["-y", "@google/gemini-docs-mcp"],
     "disabled": false
   }
   ```
Sau khi cài đặt, AI sẽ có công cụ `search_documentation` để tự tra cứu mọi endpoint, Structured Outputs và Function Calling của Gemini theo thời gian thực mà không bị giới hạn bởi Cut-off date.

---
**Lưu ý Khắc phục Sự Cố:**
Nếu nhận được lỗi: `MODULE_NOT_FOUND`, nguyên nhân chính là do mạng bị nghẽn trong lúc Add-in tự động cài đặt gói `ws` và `uuid`. Để xử lý thủ công:
1. Mở Command Prompt (cmd)
2. Chạy lệnh: `cd %AppData%\HtStructural\McpEnvironment`
3. Chạy lệnh: `npm install ws uuid`

