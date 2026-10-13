# Kiến Trúc Bản Quyền JNNTool & Cẩm Nang Xử Lý Sự Cố (Licensing Architecture & Troubleshooting)

> Tài liệu tổng hợp kiến trúc hệ thống cấp phép bản quyền, cơ chế an ninh Node-locked, quy trình khóa tự động khi hết hạn và toàn bộ bài học kinh nghiệm xử lý lỗi thực tế của **JNNTool**.

---

## 1. Tổng Quan Kiến Trúc Hệ Thống (Dual-Tier Architecture)

Hệ thống bản quyền JNNTool bao gồm 2 thành phần hoạt động phối hợp:

```
┌──────────────────────────────────────────────┐
│            REVIT CLIENT (ADD-IN)             │
│  - Ribbon Command (Chốt chặn LicenseGuard)   │
│  - Mã hóa DPAPI (%AppData%\JNNTool\license)  │
│  - Device Fingerprint (SHA-256 Node-locked)  │
└──────────────────────┬───────────────────────┘
                       │ HTTPS (REST API)
                       │ /api/auth/register-request
                       │ /api/auth/activate
                       │ /api/license/validate (15m sync)
┌──────────────────────▼───────────────────────┐
│        WEB SERVER (jnnchecking.onrender)     │
│  - ASP.NET Core 10 / EF Core                 │
│  - Quản lý User, License, Device Binding     │
│  - Web Admin Dashboard (Phê duyệt, Gia hạn)  │
└──────────────────────────────────────────────┘
```

---

## 2. Các Cơ Chế An Ninh Cốt Lõi

### 2.1 Khóa Cứng Theo Phần Cứng (Node-Locked Security)
- **Machine ID**: Được tạo bởi `DeviceFingerprintService.cs` qua thuật toán SHA-256 từ:
  - `MachineGuid` phần cứng từ Windows Registry (`HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`).
  - Tên máy tính (`Environment.MachineName`).
  - Số lượng luồng CPU (`Environment.ProcessorCount`).
  - Nền tảng hệ điều hành (`Environment.OSVersion`).
- **Server Binding**:
  - Khi người dùng bấm **Login** với Key Code, Server sẽ lưu cặp `[Key Code + Machine ID]`.
  - Nếu mang Key Code này kích hoạt trên máy tính khác (Machine ID khác), Server sẽ **từ chối kích hoạt ngay lập tức**.
- **Chống sao chép file tại chỗ (Windows DPAPI)**:
  - File bản quyền lưu tại `%AppData%\JNNTool\license.dat` được mã hóa bằng Windows DPAPI (`DataProtectionScope.CurrentUser`).
  - Chìa khóa giải mã nằm trong chip bảo mật phần cứng / User Profile của Windows. Nếu sao chép file này sang máy tính khác thì máy đó **hoàn toàn không thể đọc hay giải mã được**.

### 2.2 Tự Động Khóa Khi Hết Hạn (Expiration & License Guard)
- **Chốt chặn tại nút bấm (`LicenseGuard.cs`)**:
  - Toàn bộ 100% các lệnh trong JNNTool (Sàn, Dầm, Cột, Vách, Móng, Auto Join, Dim, E2K...) đều gọi ở dòng đầu tiên:
    ```csharp
    if (!LicenseGuard.EnsureLicensed(commandData.Application)) return Result.Cancelled;
    ```
- **Kiểm tra thời gian thực (Offline-proof)**:
  - Điều kiện: `IsExpired => DateTime.UtcNow >= ExpiresAtUtc`.
  - Dù ngắt mạng Internet, khi đến mốc hết hạn, `IsLicensed` tự động trả về `false`, lệnh bị hủy và bảng **License Management** tự động bật lên.
- **Chống chỉnh lùi đồng hồ Windows (Anti-Clock Tampering)**:
  - Cứ mỗi **15 phút**, JNNTool gửi gói tin ngầm lên `jnnchecking.onrender.com`.
  - Server đối soát theo giờ chuẩn quốc tế UTC của Server. Nếu phát hiện hết hạn hoặc Admin đã bấm **Thu Hồi**, Server gửi lệnh khóa và gán `Status = "Expired"`.

---

## 3. Các Bài Học & Sự Cố Thực Tế Đã Khắc Phục

### Sự cố 1: Chưa đăng nhập tại sao có key 355 ngày (`developer@jnntool.vn`)?
- **Nguyên nhân**:
  - Bài kiểm thử tự động `LoginTests.cs` (`SecureStorage_SaveAndLoad_ShouldRoundtripCorrectly`) đã tạo một bản quyền mẫu (`developer@jnntool.vn`, hạn dùng `355 ngày`) để test cơ chế mã hóa.
  - Test case này gọi `SecureStorageService.SaveLicense(original)` ghi thẳng vào file thật `%AppData%\JNNTool\license.dat` nhưng **không có khối xóa dọn dẹp sau khi test**.
  - Khi Revit mở lên, `LicenseClientService` đọc thấy file này có sẵn nên nhận diện là đã có bản quyền Active 355 ngày.
- **Khắc phục**:
  - Xóa sạch file `%AppData%\JNNTool\license.dat`.
  - Sửa `LoginTests.cs`: bọc toàn bộ khối kiểm thử vào `try { ... } finally { SecureStorageService.ClearLicense(); }`.
  - Sửa `LoginViewModel.cs`: xóa email và tên hardcoded mặc định (`nhan.nguyenthanh0894@gmail.com`, `Nhan Nguyen`), trả về chuỗi rỗng.

### Sự cố 2: Chọn Trial 30 ngày nhưng cấp ra 15 ngày hoặc 1 năm?
- **Nguyên nhân**:
  - Giao diện Web Admin (`Index.cshtml`) ghi nhãn `<option value="1">Trial (30 Ngày)</option>`.
  - Nhưng trong mã nguồn Backend `CalendarDurationCalculator.cs` của Server `JNNchecking`, hằng số lại bị gán cứng: `TrialDurationDays = 15`. Khi duyệt, server cộng đúng 15 ngày (`2026-10-13` -> `2026-10-28`).
  - Trong dropdown duyệt, option đầu tiên trước đó là `Annual (1 Năm)`, người duyệt dễ bấm nhầm nếu không chuyển sang Trial.
- **Khắc phục**:
  - Sửa `CalendarDurationCalculator.TrialDurationDays = 30` trong repo `JNNchecking`.
  - Đổi dropdown duyệt trong `Index.cshtml` đặt `Trial (30 Ngày)` làm mặc định (`selected`).
  - Cập nhật hiển thị trong Revit `LicenseStatusViewModel.cs` thành `Trial (30 Days)`.
  - Push lên GitHub `JNNchecking` để Render tự động deploy lại backend.

### Sự cố 3: Nút trên Ribbon Revit bị trắng icon sau khi cài MSI
- **Nguyên nhân**:
  - File `Installer\Package.wxs` của WiX trước đó chỉ khai báo 20 file PNG cũ, thiếu 46 file PNG mới (`Login.png`, `AutoJoin.png`, `DimGrid.png`, `UIManager.png`, `ExtendBeam.png`...).
  - File icon cọc (`RebarPile.png`, `PileLocation.png`) kích thước quá lớn (1024x1024 ~540KB) làm bộ render Ribbon của Revit bị lỗi ô caro (checkerboard).
- **Khắc phục**:
  - Bổ sung toàn bộ 66 file icon vào `Package.wxs`.
  - Resize các icon quá khổ về chuẩn 64x64 (~7.8KB) siêu nét.
  - Tự động đóng gói và phát hành bản cài đặt **v2.0.8** lên GitHub Release.

---

## 4. Quy Trình Vận Hành Dành Cho Quản Trị Viên (Admin Runbook)

### 4.1 Quy trình cấp mới cho người dùng
1. Người dùng mở Revit, bấm **Account License** → Điền Email, Tên, Số điện thoại → Bấm **[ Gửi yêu cầu đăng ký ]**.
2. Quản trị viên truy cập trang web: **https://jnnchecking.onrender.com**.
3. Tại bảng **Yêu Cầu Đăng Ký Chờ Phê Duyệt**:
   - Chọn loại bản quyền: `Trial (30 Ngày)` hoặc `Annual (1 Năm)`.
   - Bấm **[ ✔ Phê Duyệt & Cấp Key ]**.
4. Hệ thống tự động sinh một mã Key Code (GUID) và gán cho yêu cầu đó:
   - Người dùng chỉ cần dán mã Key Code vào ô **Key Code** trên Revit và bấm **[ Login ]**.
   - Trạng thái trên Web sẽ tự động chuyển từ `PENDING` sang `ACTIVE (1 / 1 thiết bị)`.

### 4.2 Quy trình gia hạn (Khi người dùng hết hạn)
- **Cách 1 (Nhanh nhất - Không đổi Key)**: 
  - Admin vào Web, tìm email người dùng tại mục **Danh Sách Bản Quyền Trong Hệ Thống**.
  - Bấm nút **[ Gia Hạn 1 Năm ]**.
  - Server tự động cộng thêm 365 ngày. Revit của người dùng tự động nhận hạn dùng mới mà không cần thao tác gì thêm.
- **Cách 2 (Cấp Key mới)**:
  - Người dùng bấm gửi yêu cầu mới trên Revit → Admin duyệt cấp Key Code mới → Người dùng nhập Key mới và bấm Login.

### 4.3 Quy trình đổi máy tính làm việc (Reset thiết bị)
- Nếu người dùng đổi máy tính hoặc cài lại Windows:
  - Trên Web Admin, tìm bản quyền của người dùng và bấm **[ Thu Hồi ]** hoặc gỡ thiết bị cũ.
  - Sau đó cấp Key mới hoặc cho phép kích hoạt lại trên máy tính mới.
