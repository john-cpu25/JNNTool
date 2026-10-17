# Lịch Sử Code Review & Cập Nhật Hệ Thống JNNTool (v2.1.0+)

- **Ngày thực hiện:** 2026-10-14
- **Phạm vi:** Toàn bộ dự án `JNNTool` (`Core`, `Tools/Login`, `Rebar`, `Installer`, `server/LicenseManagement.Api`, `Tests/JNNTool.Tests`)
- **Trạng thái kiểm thử:**
  - `JNNTool.csproj` (`net48`, `net8.0-windows`, `net10.0-windows` — Revit 2022–2027): **0 Errors**
  - `JNNToolInstaller.csproj`: **0 Warnings, 0 Errors**
  - `LicenseManagement.Api.csproj`: **0 Warnings, 0 Errors**
  - `JNNTool.Tests.csproj`: **176 / 176 Passed (0 Failed)**

---

## 1. Bảo Mật, Quản Lý Bản Quyền & Cô Lập Unit Test (`P0` / `P1`)

### 1.1. Cô lập Unit Test khỏi file License thật (`%APPDATA%\JNNTool\license.dat`)
- **Vấn đề trước khi sửa:**
  - Bài test `LoginTests.SecureStorage_SaveAndLoad_ShouldRoundtripCorrectly()` gọi trực tiếp `SecureStorageService.SaveLicense()` và `ClearLicense()` không có tham số đường dẫn riêng, dẫn tới việc chạy `dotnet test` ghi đè dữ liệu giả (`test@jnn.vn`) và xóa mất file license bản quyền thật của người dùng tại `%APPDATA%\JNNTool\license.dat`.
- **Giải pháp đã áp dụng:**
  - **`Tools/Login/Services/SecureStorageService.cs`**: Bổ sung tham số tùy chọn `string customFilePath = null` cho cả 3 phương thức `SaveLicense(LicenseInfo info, string customFilePath = null)`, `LoadLicense(string customFilePath = null)`, và `ClearLicense(string customFilePath = null)`.
  - **`Tests/JNNTool.Tests/LoginTests.cs`**: Cập nhật bài test sử dụng đường dẫn tạm độc lập trong `Path.GetTempPath()` (`jnn_test_license_<guid>.dat`) bọc trong khối `try / finally`, đảm bảo không bao giờ chạm tới `%APPDATA%\JNNTool\license.dat`.

### 1.2. Thread-safety cho `HttpClient` trong `LicenseClientService`
- **Vấn đề trước khi sửa:**
  - `ValidateCurrentLicenseAsync` và `DeactivateAsync` gán trực tiếp `_http.DefaultRequestHeaders.Authorization` trên biến `static readonly HttpClient _http`, gây nguy cơ xung đột (race condition) khi gọi bất đồng bộ.
- **Giải pháp đã áp dụng:**
  - **`Tools/Login/Services/LicenseClientService.cs`**: Tạo `HttpRequestMessage` riêng cho từng request và gắn `request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.JwtToken)`.

### 1.3. Bảo vệ các Endpoint quản lý License trên Server API
- **Vấn đề trước khi sửa:**
  - Các endpoint `POST /api/v1/license/validate`, `POST /api/v1/license/deactivate`, `POST /api/v1/license/renew` thiếu thuộc tính `[Authorize]`.
- **Giải pháp đã áp dụng:**
  - **`server/LicenseManagement.Api/Controllers/LicenseController.cs`**: Bổ sung `[Authorize]` cho `Validate`, `Deactivate` và `Renew` (chỉ giữ `[AllowAnonymous]` cho `Activate` và `Trial`).

---

## 2. JNN Updater (`JNNToolInstaller`), WiX MSI & Quy Trình Release (`P0`)

### 2.1. Đồng bộ kiến trúc cài đặt Per-User (`%APPDATA%`)
- **Vấn đề trước khi sửa:**
  - Bộ cài WiX (`Package.wxs`) đã chuyển sang cài đặt Per-User (`Scope="perUser"` vào `%APPDATA%\Autodesk\ApplicationPlugins\JNNTool.bundle` và ghi Registry vào `HKCU`), nhưng ứng dụng `JNNToolInstaller` vẫn kiểm tra phiên bản ở `C:\ProgramData` (`SpecialFolder.CommonApplicationData`), tìm mã gỡ cài đặt ở `HKLM`, và ép quyền Admin (`requireAdministrator`, `Verb = "runas"`).
- **Giải pháp đã áp dụng:**
  - **`Installer/JNNToolInstaller/Services/VersionChecker.cs`**:
    - Đổi `InstallRoot` sang ưu tiên kiểm tra `%APPDATA%\Autodesk\ApplicationPlugins\JNNTool.bundle` trước, nếu chưa có mới fallback kiểm tra `C:\ProgramData\Autodesk\ApplicationPlugins\JNNTool.bundle`.
    - Nâng cấp fallback manifest lên `v2.1.0` hỗ trợ đầy đủ Revit `2022` đến `2027`.
  - **`Installer/JNNToolInstaller/Services/InstallerService.cs`**:
    - Đổi `InstallRoot` sang `%APPDATA%\Autodesk\ApplicationPlugins\JNNTool.bundle`.
    - Bỏ `Verb = "runas"` khi gọi `msiexec.exe`.
    - Bọc thư mục tải file tạm `tempDir` trong khối `try / finally` để luôn tự động xóa file `.msi` tạm sau khi cài xong.
    - Cập nhật `FindProductCode()` đọc trực tiếp giá trị `ProductCode` tại `HKCU\Software\JNN\JNNTool`, quét `HKCU` Uninstall và fallback sang `HKLM` Uninstall cho các bản cài cũ.
  - **`Installer/JNNToolInstaller/app.manifest`**:
    - Đổi `requestedExecutionLevel` từ `requireAdministrator` sang `asInvoker` và cập nhật tên định danh `JNNToolInstaller.app`.
  - **`Installer/JNNToolInstaller/MainWindow.xaml.cs`**:
    - Chuyển lệnh đăng ký sự kiện `_svc.StatusChanged` và `_svc.ProgressChanged` vào hàm khởi tạo `MainWindow()` để tránh nhân bản event handler khi bấm nút nhiều lần.

### 2.2. Làm sạch `Package.wxs`, `JNNTool.csproj` & `JNNTool.bundle`
- **Vấn đề trước khi sửa:**
  - `Package.wxs` vẫn đóng gói các DLL cũ (`NhanStr.dll`, `RincoModeling.dll`, `Material.Icons*`, `Microsoft.Expression.Drawing.dll`, `Microsoft.Xaml.Behaviors.dll`) và 13 icon decompile không còn sử dụng.
  - `JNNTool.csproj` dùng đường dẫn `$(ProjectDir)Installer\version.json` không tồn tại và copy sang `C:\ProgramData` gây cảnh báo `MSB3021`.
- **Giải pháp đã áp dụng:**
  - **`Installer/Package.wxs`**: Xóa toàn bộ Component chứa DLL rác ở cả 6 phiên bản Revit (`2022–2027`) và xóa 13 Component icon decompile (`Ico_mcn_diagram`, `Ico_qrcode`, `Ico_stair_cas_1..8`, `Ico_stirrup*_v2`, `Ico_StairDetail`).
  - **`JNNTool.csproj`**:
    - Loại trừ `Tools\StairDetail\**` khỏi `Compile`, `Page`, `EmbeddedResource`.
    - Loại trừ `Tools\Tool\**\*.png` và `Tools\StairDetail\**\*.png` khỏi `ToolIcons`.
    - Sửa đường dẫn copy `$(ProjectDir)version.json` vào `$(BundleFolder)` và bỏ lệnh copy sang `C:\ProgramData`.
  - **`JNNTool.bundle/Contents`**: Dọn sạch các DLL cũ và 13 file icon thừa trong thư mục bundle cục bộ.

### 2.3. Bảo vệ mã nguồn khi phát hành (`ReleaseToGitHub.ps1`)
- **Vấn đề trước khi sửa:**
  - `ReleaseToGitHub.ps1` dùng `git add -A`, có nguy cơ đẩy toàn bộ mã nguồn C# lên nhánh `main` công khai trên GitHub nếu chạy nhầm nhánh.
- **Giải pháp đã áp dụng:**
  - **`Installer/ReleaseToGitHub.ps1`**: Áp dụng chính sách **Clean Distribution Repo** — chỉ stage đúng các file phân phối (`version.json`, `README.md`, `.gitignore`, `Logo_JNN.png`) khi ở nhánh `main`, hoặc sử dụng `git worktree` tạm thời để cập nhật `version.json` trên nhánh `main` nếu đang đứng ở nhánh `source-backup`.

---

## 3. Core Architecture, UI Dispatcher & Command Palette (`P1`)

### 3.1. Khởi tạo Auto-Update & Thread-safety trong `App.cs`
- **Vấn đề trước khi sửa:**
  - `App.OnStartup` đăng ký sự kiện `ApplicationInitialized` bên trong một `Task.Run` sau khi chờ `Task.Delay(3000)` (lúc này sự kiện `ApplicationInitialized` của Revit đã bắn xong từ trước nên không bao giờ chạy), đồng thời gọi `TaskDialog.Show()` từ background thread.
- **Giải pháp đã áp dụng:**
  - **`Core/App.cs`**: Đăng ký `application.ControlledApplication.ApplicationInitialized += OnApplicationInitialized` đồng bộ ngay trong `OnStartup`, và bọc lệnh hiển thị `TaskDialog.Show()` qua `ActionEventHandler.Instance.Post(...)` để luôn thực thi an toàn trên luồng chính của Revit.

### 3.2. Chống Deadlock trong `UiDispatcher.cs`
- **Vấn đề trước khi sửa:**
  - `UiDispatcher.RunOnUi` gọi `d.Invoke(action)` đồng bộ khi đang ở ngoài UI thread, dễ gây deadlock nếu UI thread đang đợi Revit API thread.
- **Giải pháp đã áp dụng:**
  - **`Core/UI/UiDispatcher.cs`**: Đổi `d.Invoke(action)` sang `d.BeginInvoke(action)` khi khác luồng.

### 3.3. Sửa định danh SplitButton & `ExternalCommandData` trong Command Palette
- **Vấn đề trước khi sửa:**
  - Hai lệnh `cmdAutoJoinActiveView` và `cmdAutoJoinProject` nằm trong `SplitButton` (`cmdAutoJoinSplit`) nhưng `GetPostableId` định dạng như `PushButton` thường khiến `PostCommand` không khớp ID của Revit; кроме đó `CommandRunner.CreateCommandData` chỉ gán mỗi `Application` mà thiếu `View` và `JournalData`.
- **Giải pháp đã áp dụng:**
  - **`Core/Palette/CommandRegistry.cs`**: Thêm thuộc tính `ParentSplitId` cho `ToolCommandInfo`, khai báo `"cmdAutoJoinSplit"` cho 2 lệnh Auto Join và cập nhật `GetPostableId()` sinh đúng chuỗi định danh cho nút con trong `SplitButton`.
  - **`Core/Palette/CommandRunner.cs`**: Gán đầy đủ `Application`, `View` (`app.ActiveUIDocument.ActiveView`) và `JournalData` khi khởi tạo `ExternalCommandData`.

---

## 4. Hệ Thống Rebar Suite — Hình Học 3D & Builders (`P0` / `P1`)

### 4.1. Thép Sàn (`SlabRebarBuilder.cs`)
- **Vấn đề trước khi sửa:**
  - Tìm dầm đỡ sàn chỉ kiểm tra cao độ `Z` mà không kiểm tra giao cắt hình chiếu bằng `X/Y`, và truyền `XYZ.BasisZ` làm vector pháp tuyến `normal` trong `CreateFromCurves` cho thanh thép thẳng nằm ngang (khiến thanh thép không rải được theo phương vuông góc).
- **Giải pháp đã áp dụng:**
  - **`Rebar/Slab/Builders/SlabRebarBuilder.cs`**: Bổ sung kiểm tra giao cắt BoundingBox `X/Y` khi lọc dầm đỡ sàn và truyền vector phân bố `setInfo.DistributionVector` đã chuẩn hóa làm `normal` cho `Rebar.CreateFromCurves`.

### 4.2. Thép Cột (`ColumnLayoutEngine.cs` & `ColumnRebarBuilder.cs`)
- **Vấn đề trước khi sửa:**
  - Tọa độ thanh thép trong `ColumnLayoutEngine` tính quanh gốc `(0,0)` mà chưa cộng tâm cột `story.CenterPoint`; công thức nhấn cổ chai dùng `Math.Sign(pos.X)` làm mất độ lệch của các thanh nằm giữa cạnh (`pos.X == 0` hoặc `pos.Y == 0`); cao độ `Z` của các vùng đai chưa được gán vào `CurvePoints`; `ColumnRebarBuilder` dùng `RebarStyle.Standard` cho cả đai cột và dùng `XYZ.BasisX` cố định làm pháp tuyến mặt phẳng.
- **Giải pháp đã áp dụng:**
  - **`Rebar/Column/Engine/ColumnLayoutEngine.cs`**: Cộng tọa độ `story.CenterPoint` cho toàn bộ thép chủ và thép đai; sửa vector nhấn cổ chai hướng vào tâm cho cả thanh góc và thanh giữa cạnh; tính chính xác cao độ `Z` bắt đầu cho từng vùng đai (`StirrupBottomZone`, `StirrupMidZone`, `StirrupTopZone`, `StirrupJointZone`).
  - **`Rebar/Column/Builders/ColumnRebarBuilder.cs`**: Kiểm tra giao cắt BoundingBox `X/Y` khi tìm dầm giao cột; dùng `RebarStyle.StirrupTie` cho thanh đai và bổ sung hàm `ComputeCurvePlaneNormal(List<Curve> curves)` tự động tính vector pháp tuyến vuông góc với mặt phẳng thanh thép chủ/nhấn cổ chai.

### 4.3. Thép Dầm (`BeamLayoutEngine.cs` & `BeamRebarBuilder.cs`)
- **Vấn đề trước khi sửa:**
  - `BeamLayoutEngine` tạo các thanh thép cùng nằm tại `Y = 0, Z = 0`, còn `BeamRebarBuilder` dựng `Line` trực tiếp từ tọa độ cục bộ mà chưa biến đổi theo `StartPoint` và vector phương dầm `Direction`, đồng thời truyền `XYZ.BasisZ` cho tất cả các thanh.
- **Giải pháp đã áp dụng:**
  - **`Rebar/Beam/Engine/BeamLayoutEngine.cs`**: Tính toán đầy đủ tọa độ mặt cắt ngang (`Y`, `Z`) và khoảng phân bố dọc dầm (`X`) cho thép chủ trên/dưới, thép gia cường gối/nhịp, thép giá và từng vùng đai dầm.
  - **`Rebar/Beam/Builders/BeamRebarBuilder.cs`**: Xây dựng hàm `TransformBeamLocalPoint` chuyển đổi từ hệ tọa độ cục bộ của nhịp dầm sang hệ tọa độ 3D của Revit theo `span.StartPoint`, trục dọc dầm `dirX`, trục ngang `dirY` và trục đứng `dirZ`; dùng `RebarStyle.StirrupTie` với pháp tuyến `dirX` cho cốt đai và `RebarStyle.Standard` với pháp tuyến `dirY` cho cốt dọc.

### 4.4. Thép Vách (`WallRebarBuilder.cs`)
- **Vấn đề trước khi sửa:**
  - Thanh thép đứng (`IsVertical`) sử dụng `Wall.Normal` làm hướng rải thay vì rải dọc theo chiều dài vách.
- **Giải pháp đã áp dụng:**
  - **`Rebar/Wall/Builders/WallRebarBuilder.cs`**: Dùng `wallGeo.Direction` làm vector pháp tuyến/phân bố cho lớp thép đứng và `XYZ.BasisZ` cho lớp thép ngang.

### 4.5. Thép Móng (`FoundationRebarBuilder.cs`)
- **Vấn đề trước khi sửa:**
  - Thanh thép chờ cột (`ColumnDowel` hình chữ L nằm trong mặt phẳng đứng) được truyền `normal = XYZ.BasisZ`, song song với đoạn thân đứng của thanh thép khiến `Rebar.CreateFromCurves` báo lỗi mặt phẳng.
- **Giải pháp đã áp dụng:**
  - **`Rebar/Foundation/Builders/FoundationRebarBuilder.cs`**: Bổ sung hàm `ComputeDowelPlaneNormal(List<Curve> curves)` dùng tích có hướng (`CrossProduct`) giữa đoạn ngang và đoạn đứng để tìm đúng pháp tuyến mặt phẳng chữ L.

### 4.6. Thép Cọc (`PileLayoutEngine.cs` & `PileRebarBuilder.cs`)
- **Vấn đề trước khi sửa:**
  - `PileRebarBuilder` truyền `XYZ.BasisZ` làm hướng rải cho đai cọc khiến đai bị rải ngược lên trên đỉnh cọc (`+Z`), và `PileLayoutEngine` thiếu vùng đai dày ở mũi cọc cho cọc khoan nhồi.
- **Giải pháp đã áp dụng:**
  - **`Rebar/Pile/Engine/PileLayoutEngine.cs`**: Bổ sung vùng đai dày mũi cọc (`StirrupDenseToe`) với bước đai thu nhỏ cho cọc khoan nhồi (`BoredPile`).
  - **`Rebar/Pile/Builders/PileRebarBuilder.cs`**: Sử dụng `setInfo.DistributionVector` (`(0, 0, -1)`) làm vector pháp tuyến rải đai đi xuống dọc thân cọc.

---

## 5. Hệ Thống Icon Vector 3x, Ribbon Theme & Command Palette (`P0` / `P1` / `P2`)

### 5.1. Đăng ký đầy đủ 100% Vector Geometry (`JNNIcon.cs` & `VectorIconCatalog.cs`)
- **Vấn đề trước khi sửa:**
  - 5 biểu tượng ở cuối `enum JNNIcon` (`Split`, `DisallowJoin`, `Flash`, `Target`, `Eye`) chưa được đăng ký trong `VectorIconCatalog`, dẫn đến việc bị fallback âm thầm về hình bánh răng `Settings`.
- **Giải pháp đã áp dụng:**
  - **`Tools/UI Manager/Models/JNNIcon.cs`**: Bổ sung thêm 3 biểu tượng chuyên biệt `CommandPalette`, `DimColumn`, `StairDesigner` (tổng cộng 34 biểu tượng Vector).
  - **`Tools/UI Manager/Services/VectorIconCatalog.cs`**: Thiết kế và đăng ký đầy đủ Path Geometry chuẩn **Solid Filled + Rounded Corners** (`F0`, EvenOdd rule) cho cả 8 biểu tượng (`Split`, `DisallowJoin`, `Flash`, `Target`, `Eye`, `CommandPalette`, `DimColumn`, `StairDesigner`) và bổ sung hàm `HasGeometry(JNNIcon)` để kiểm chứng bằng Unit Test.

### 5.2. Xóa bỏ trùng lặp Icon trên thanh Ribbon & Đồng bộ Command Palette (`RibbonThemeService.cs`, `PaletteModels.cs`, `CommandPalettePage.xaml.cs`)
- **Vấn đề trước khi sửa:**
  - Nhiều công cụ nằm cạnh nhau dùng chung một icon (`Command Palette` trùng `Ribbon Theme`, `Split Beam` trùng `Rebar Beam`, `Disallow Join` trùng `Auto Join`, `Speed Overrider` trùng `Ribbon Theme`, `Pile Location` trùng `Rebar Pile`, `DimCol/Wall` trùng `Dim Grid`, `Stair Designer` trùng `Stair Model`).
  - `Command Palette` nạp file PNG tĩnh cũ từ thư mục `Resources/` thay vì dùng bộ Vector Icon 3x theo màu Theme của người dùng.
- **Giải pháp đã áp dụng:**
  - **`Tools/UI Manager/Services/RibbonThemeService.cs`**: Gán biểu tượng `JNNIcon` riêng biệt cho toàn bộ 36 lệnh của `JNNTool`, mở khóa toàn bộ danh mục trong `_knownButtons`, bổ sung hàm `CreateCommandIcon()` và sự kiện `ThemeApplied`.
  - **`Core/Palette/PaletteModels.cs` & `Core/Palette/CommandPalettePage.xaml.cs`**: Kết nối `PaletteItemVm` trực tiếp với `RibbonThemeService.CreateCommandIcon()` và lắng nghe sự kiện `ThemeApplied` để toàn bộ thẻ lệnh trong **Command Palette** luôn hiển thị icon Vector 3x sắc nét và đổi màu tức thì khi người dùng cập nhật Theme.
  - **`Tools/UI Manager/UI/UIManagerWindow.xaml` & `ViewModels/UIManagerViewModel.cs`**: Bổ sung hiệu ứng highlight nút kiểu hiển thị đang chọn (`Badge`, `Glyph`, `Circle`, `Outline`) và thêm bộ lọc nhóm `Rebar Suite`.

### 5.3. Tối ưu hóa dung lượng tài nguyên ảnh PNG (`JNNTool.bundle/Contents/Resources`)
- **Giải pháp đã áp dụng:**
  - Thu gọn các file PNG `1024×1024` quá khổ (`PileLocation.png` từ `529 KB` xuống `16.8 KB`, `SplitBeam.png` từ `409 KB` xuống `14.7 KB`, và 8 ảnh minh họa `SteelConnection` từ `~4.1 MB` xuống `~450 KB`), giúp giảm ~4 MB dung lượng bộ cài MSI và tiết kiệm bộ nhớ RAM.

---

## 6. Danh Sách File Đã Cập Nhật

1. `Core/App.cs`
2. `Core/Palette/CommandPalettePage.xaml.cs`
3. `Core/Palette/CommandRegistry.cs`
4. `Core/Palette/CommandRunner.cs`
5. `Core/Palette/PaletteModels.cs`
6. `Core/UI/UiDispatcher.cs`
7. `Installer/JNNToolInstaller/MainWindow.xaml.cs`
8. `Installer/JNNToolInstaller/Services/InstallerService.cs`
9. `Installer/JNNToolInstaller/Services/VersionChecker.cs`
10. `Installer/JNNToolInstaller/app.manifest`
11. `Installer/Package.wxs`
12. `Installer/ReleaseToGitHub.ps1`
13. `JNNTool.csproj`
14. `Rebar/Beam/Builders/BeamRebarBuilder.cs`
15. `Rebar/Beam/Engine/BeamLayoutEngine.cs`
16. `Rebar/Column/Builders/ColumnRebarBuilder.cs`
17. `Rebar/Column/Engine/ColumnLayoutEngine.cs`
18. `Rebar/Foundation/Builders/FoundationRebarBuilder.cs`
19. `Rebar/Pile/Builders/PileRebarBuilder.cs`
20. `Rebar/Pile/Engine/PileLayoutEngine.cs`
21. `Rebar/Slab/Builders/SlabRebarBuilder.cs`
22. `Rebar/Wall/Builders/WallRebarBuilder.cs`
23. `Tests/JNNTool.Tests/LoginTests.cs`
24. `Tests/JNNTool.Tests/RibbonThemeTests.cs`
25. `Tools/Login/Services/LicenseClientService.cs`
26. `Tools/Login/Services/SecureStorageService.cs`
27. `Tools/UI Manager/Models/JNNIcon.cs`
28. `Tools/UI Manager/Models/RibbonIconThemeModel.cs`
29. `Tools/UI Manager/Services/RibbonThemeService.cs`
30. `Tools/UI Manager/Services/VectorIconCatalog.cs`
31. `Tools/UI Manager/UI/UIManagerWindow.xaml`
32. `Tools/UI Manager/ViewModels/UIManagerViewModel.cs`
33. `server/LicenseManagement.Api/Controllers/LicenseController.cs`
