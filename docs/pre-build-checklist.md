# Pre-Build & Release Quality Checklist — JNNTool

> **Tiêu chuẩn chất lượng vàng trước khi Build & Đóng gói sản phẩm (Production-Ready)**  
> Mọi tính năng, công cụ mới hoặc nâng cấp trong JNNTool đều phải được đối soát theo bảng kiểm tra này trước khi phát hành release.

---

## 1. 🪟 Modeless Window & Threading Safety (Cửa sổ Nổi & Đa luồng)

- [ ] **Window Owner Handle**: Đã gán `WindowInteropHelper.Owner = uiApp.MainWindowHandle` ngay khi khởi tạo Window để cửa sổ luôn gắn kết với cửa sổ chính Revit, không bị chìm mất hút ra sau khi click vào View.
- [ ] **ExternalEvent Context (BẮT BUỘC)**: Mọi thao tác ghi dữ liệu (Transaction) vào Revit từ cửa sổ Modeless **100% phải đẩy qua `ExternalEvent`** (hoặc `ActionEventHandler`).
  - *Cấm kỵ*: Mở `Transaction` trực tiếp từ các hàm sự kiện UI WPF (`Button_Click`, `RelayCommand`...). Điều này sẽ gây ngoại lệ crash: *"Starting a transaction from an external application running outside of API context is not allowed"*.
- [ ] **Document Lifecycle**: Xử lý an toàn khi người dùng đóng Active Document, đổi Document, hoặc đóng Revit:
  - Cửa sổ Modeless phải đóng lại hoặc cập nhật lại context Document, tránh thao tác trên Document đã bị dispose.

---

## 2. 🛡️ Crash Prevention & Exception Safety (Chống Sập Revit)

- [ ] **Top-Level Try-Catch**:
  - Toàn bộ thân hàm `IExternalCommand.Execute()` và `IExternalEventHandler.Execute()` phải được bọc trong khối `try-catch (Exception ex)`.
  - Không để bất kỳ unhandled exception nào văng ra ngoài Revit Core làm sập phần mềm đột ngột.
- [ ] **Logger & Dialog thân thiện**:
  - Ghi chi tiết stack trace vào `JNNTool.Core.Logging.Logger.Error(...)`.
  - Hiển thị `TaskDialog` giải thích nguyên nhân rõ ràng và hướng dẫn người dùng cách khắc phục thay vì để văng thông báo kỹ thuật khó hiểu.
- [ ] **Null & Validity Checks**:
  - Luôn kiểm tra `uiApp.ActiveUIDocument != null`, `doc != null`, `doc.ActiveView != null`.
  - Kiểm tra các phần tử Revit trước khi truy cập: `elem != null && elem.IsValidObject`.

---

## 3. 🔄 Transaction & Undo/Redo Integrity (Quản lý Giao dịch)

- [ ] **1 TransactionGroup duy nhất**:
  - Gom toàn bộ chuỗi các transaction tạo hình học / thép / annotation vào **1 `TransactionGroup`** (hoặc 1 `Transaction`).
  - Đảm bảo người dùng chỉ cần nhấn `Ctrl + Z` **đúng 1 lần** là hoàn tác sạch sẽ toàn bộ cấu kiện vừa tạo.
- [ ] **Tự động xử lý Warning (`FailuresPreprocessor`)**:
  - Gắn `IFailuresPreprocessor` (ví dụ `DismissWarningsPreprocessor`) vào Transaction để tự động xóa/bỏ qua các warning ngầm của Revit (như trùng hình học, overlap, không thể join...) tránh làm đơ tiến trình hoặc bật popup làm phiền người dùng.
- [ ] **Đặt tên Transaction chuẩn**:
  - Đặt tên Transaction tiếng Việt có dấu với tiền tố nhận diện dự án, ví dụ: `"JNN - Auto Join Elements"`, `"JNN - Tạo thép sàn"`.
- [ ] **Using Block & Rollback**:
  - Luôn dùng `using (Transaction t = new Transaction(doc, "..."))` để tự động Rollback an toàn nếu có lỗi xảy ra giữa chừng.

---

## 4. 🧩 Multi-Version & API Compatibility (Tương thích 2022 – 2027)

- [ ] **ElementId Compatibility**:
  - Sử dụng phương thức mở rộng `ElementIdCompat.GetIdValue()` từ `JNNTool.Core.Compat`.
  - *Không gọi trực tiếp*: `.IntegerValue` (đã bị xóa từ Revit 2026) hay `.Value` (chưa có ở Revit 2022-2023).
- [ ] **ForgeTypeId & Đổi đơn vị**:
  - Sử dụng `UnitTypeId` / `SpecTypeId` kết hợp `UnitUtils.ConvertToInternalUnits(...)`.
  - *Không dùng*: `DisplayUnitType` hay `ParameterType` cũ đã bị Autodesk khai tử.
- [ ] **Biên dịch đa nền tảng**:
  - Kiểm tra build thành công cả 3 Target Frameworks:
    - `net48` (Revit 2022, 2023, 2024)
    - `net8.0-windows` (Revit 2025, 2026)
    - `net10.0-windows` (Revit 2027)

---

## 5. ⚡ Performance & Memory Optimization (Tối ưu Hiệu năng)

- [ ] **Kiểm soát `doc.Regenerate()`**:
  - Không gọi `doc.Regenerate()` bên trong vòng lặp lớn (duyệt hàng trăm/hàng nghìn phần tử). Chỉ gọi 1 lần khi kết thúc toàn bộ batch xử lý.
- [ ] **Bộ lọc không gian (Spatial Filters)**:
  - Sử dụng `BoundingBoxIntersectsFilter` (chạy ở tầng C++ native trong Revit) để tìm nhanh các cấu kiện lân cận thay vì duyệt 2 vòng lặp lồng $O(N^2)$.
- [ ] **Chống lặp cặp (Duplicate Pair Checking)**:
  - Dùng `HashSet` lưu trữ cặp Id phần tử đã xử lý để tránh double-processing.
- [ ] **Giải phóng tài nguyên WPF**:
  - Hủy đăng ký Event, dừng `DispatcherTimer`, dọn dẹp các ObservableCollection khi đóng Window để tránh rò rỉ RAM (Memory Leak).

---

## 6. 🎨 UI/UX & User Experience (Trải nghiệm Người dùng)

- [ ] **Hỗ trợ màn hình độ phân giải cao (DPI Scaling)**:
  - Giao diện WPF không bị vỡ font, co chữ, hoặc mất góc khi người dùng để Display Scaling 125%, 150% trên màn hình 2K/4K.
- [ ] **Con trỏ chờ (Wait Cursor) & Feedback**:
  - Khi chạy các tác vụ nặng trên nhiều cấu kiện, hiển thị `Mouse.OverrideCursor = Cursors.Wait` hoặc Progress Bar.
  - Sau khi hoàn thành, hiển thị thông báo tổng kết rõ ràng: Số lượng cấu kiện đã xử lý thành công, số lượng bỏ qua, và thời gian thực thi.
- [ ] **Lưu cấu hình người dùng (Settings Persistence)**:
  - Lưu các thông số nhập gần nhất vào file JSON (`%AppData%\JNNTool\Settings\`) để lần mở sau người dùng không phải gõ lại. Có nút `Mặc định` để reset.

---

## 7. 📦 Packaging, Bundle & Clean-Room Standard

- [ ] **Đầy đủ Biểu tượng (Icon 16px & 32px)**:
  - Icon định dạng PNG sắc nét, đặt đúng tên `<CommandName>.png`, đã được copy vào thư mục `Resources` của `JNNTool.bundle`.
- [ ] **Ribbon ToolTip & Description**:
  - Có đầy đủ ToolTip tiếng Việt giải thích công năng, đầu vào và tiêu chuẩn áp dụng.
- [ ] **Tuân thủ Clean-Room**:
  - Toàn bộ tên class, file, family, type tự tạo sử dụng tiền tố **`JNN_`**.
  - Tuyệt đối không dùng code dịch ngược, không sao chép family hay resource của bên thứ ba.
- [ ] **Cập nhật Phiên bản & Tài liệu**:
  - Cập nhật số phiên bản trong `version.json` và `JNNTool.bundle\version.json`.
  - Cập nhật mục mô tả tính năng mới trong `CHANGELOG.md`.
  - Có file hướng dẫn kiểm thử thủ công `docs/test-checklist-<module>.md`.
