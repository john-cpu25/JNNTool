# Checklist Kiểm Thử - Giai Đoạn 0: Nền Tảng Core

## 1. Kiểm thử Unit Test (Tự động)
- Chạy lệnh: `dotnet test Tests/JNNTool.Tests/JNNTool.Tests.csproj`
- Kết quả mong đợi: Toàn bộ **23/23 unit tests pass 100%**:
  - `BarCatalogTests`: Tra cứu D6–D32, diện tích, khối lượng, phân tích chuỗi (`D10`, `phi 16`, `T12`).
  - `GeometryHelperTests`: Làm tròn bội số 50mm, tính toán số thanh rải và bước rải thực tế, chuyển đổi mm <-> feet.
  - `AnchorAndLapTests`: Tính neo cơ sở, neo tính toán và nối chồng theo TCVN 5574:2018; tra cứu đoạn móc và lớp bảo vệ.

## 2. Kiểm thử Thực tế trên Revit (Thủ công)
### Mục tiêu:
Xác nhận Ribbon JNNTool khởi động bình thường, ActionEventHandler và Logger hoạt động không gây lỗi.

### Các bước thực hiện:
1. Mở Autodesk Revit (bản 2024, 2025 hoặc 2026).
2. Kiểm tra tab **JNNTool** trên Ribbon xuất hiện đầy đủ các nút công cụ hiện có.
3. Kiểm tra file log:
   - Vào thư mục `%AppData%\JNNTool\Logs\`
   - Xác nhận có file `JNNTool_yyyyMMdd.log` được tạo và không có log lỗi Exception.
4. Kiểm tra thư mục cấu hình:
   - Vào thư mục `%AppData%\JNNTool\Settings\`
   - Thư mục được tạo sẵn sàng nhận cấu hình JSON của các tool Rebar tiếp theo.
