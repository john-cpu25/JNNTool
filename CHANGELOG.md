# Changelog — JNNTool

All notable changes to this project will be documented in this file.

## [Unreleased]

### Enhanced - **Boundary Align (Căn Lề Biên): Nâng cấp toàn diện cho Dầm có bề rộng lớn (400, 550, 600, 800 mm)**
- **Xử lý chính xác Bề rộng Dầm lớn (`BeamDimensionHelper.cs`)**:
  - Tự động nhận diện bề rộng dầm qua 3 tầng: (1) Tham số Type/Instance (`b`, `Width`, `B`...), (2) Phân tích tên Type bằng Regex (`B40.50` $\rightarrow 400\text{ mm}$, `B60.40` $\rightarrow 600\text{ mm}$, `B50.75` $\rightarrow 500\text{ mm}$, `B80.50` $\rightarrow 800\text{ mm}$, `400x600`, `550x700`), (3) Đo kích thước thực tế từ 3D BoundingBox của dầm.
  - Triệt tiêu hoàn toàn lỗi fallback về 200 mm khi tham số dầm đặt tên khác quy ước.
- **Khắc phục lỗi Cột bị tuột liên kết (`BoundaryAligner.cs`)**:
  - Đo khoảng cách từ Cột đến đường tim dầm BAN ĐẦU trước khi dời thay vì đường dầm mới, nâng dung sai an toàn lên $2.5\text{ ft}$ ($762\text{ mm}$).
  - Cột luôn nhận diện chính xác 100% dầm biên $400, 550, 600, 800\text{ mm}$ nối vào để dời đồng bộ theo cùng hướng.
- **Khắc phục lỗi Hở nút Dầm ngang (`AdjustIntersectingBeams`)**:
  - Dung sai kết nối dầm ngang được tính tỷ lệ theo bề rộng dầm biên ($connectTol = \max(0.8, B \times 0.85) \approx 340 - 510\text{ mm}$), bắt trọn cả dầm ngang cắm ở tim hoặc ở mép dầm biên lớn.
  - Bổ sung bộ lọc chống xiên dầm (bỏ qua dầm song song cùng trục) và tự động đồng bộ co/kéo dầm ngang nối vào Cột biên bị dời.

### Added - **Merge Floor: Bổ sung chế độ Active View (Tự động gộp sàn gần nhau không cần chọn)**
- **Tính năng 1-Click tự động**:
  - Tự động quét toàn bộ sàn đang hiển thị trong View hiện hành (`ActiveView` - Floor Plan, Structural Plan hoặc 3D View).
  - Tự động phân loại theo Loại sàn & Cao độ (`GroupByTypeAndElevation`), nhận diện các cụm sàn tiếp giáp/gần nhau ($\le 12\text{ mm}$) qua `FindConnectedComponents`.
  - Tự động hợp nhất từng cụm có $\ge 2$ sàn chạm nhau thành 1 sàn liên tục duy nhất mà **không cần người dùng phải quét chọn thủ công bằng chuột**.
  - Các sàn đứng độc lập 1 mình được giữ nguyên 100%, không bị ảnh hưởng.
  - Tự động phục hồi liên kết Join Geometry với Cột/Dầm và kế thừa toàn bộ tham số (Level, Offset, Structural, Comments, Mark).
- **Trải nghiệm Ribbon & Palette linh hoạt**:
  - Nâng cấp nút Ribbon thành **SplitButton** `cmdMergeFloorSplit`:
    + Nút chính (nút trên cùng): `Merge Floor (Active View)` (`cmdMergeFloorActiveView`) — 1-click chạy ngay chế độ tự động cho view hiện hành.
    + Nút xổ xuống: `Merge Floor (Selection)` (`cmdMergeFloorSelect`) — quét chọn thủ công các sàn trên màn hình.
  - Lệnh `MergeFloorCmd`: Nếu đã quét chọn trước $\ge 2$ sàn $\rightarrow$ gộp ngay lập tức; nếu chưa chọn sàn nào $\rightarrow$ hiển thị hộp thoại TaskDialog 2 CommandLinks trực quan để chọn phạm vi (Active View hoặc Pick Elements).
  - Đăng ký đầy đủ vào `CommandRegistry` (Command Palette) và `RibbonThemeService` (Vector Icon 3x).

### Changed - **Đồng bộ số lượng công cụ Ribbon và Command Palette (21 tools)**
- **Khớp 1:1 theo Ribbon thực tế**:
  - Ẩn lệnh `Rebar Column` (`cmdRebarColumn`) khỏi Panel **Modeling** trên cả Ribbon ([`App.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/App.cs)) và Command Palette ([`CommandRegistry.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/Palette/CommandRegistry.cs)), chuyển ngầm vào phân hệ `Rebar Suite`.
  - Panel **Modeling** tinh gọn còn đúng **6 công cụ**: `Split Beam`, `Create Section`, `Disallow Join Beam`, `Create Filter`, `Speed Overrider`, `Pile Location`.
  - Tổng số công cụ trên Ribbon và Command Palette đồng bộ chuẩn xác **21 công cụ**.
  - Thống nhất tên gọi tiếng Anh chuẩn mực **`Boundary Align`** cho lệnh `cmdBoundaryAlign` trên Ribbon, Command Palette, Theme Manager và Tiêu đề cửa sổ.

### Fixed & Enhanced - **Code Review Toàn Diện & Nâng Cấp Hệ Thống (2026-10-14)** (Chi tiết tại `History/2026-10-14_CodeReview_And_Update_History.md`)
- **Login, Bảo mật & Cô lập Unit Test**:
  - Bổ sung tham số `customFilePath` cho `SecureStorageService` và cập nhật `LoginTests` dùng file tạm độc lập trong `Path.GetTempPath()`, bảo vệ tuyệt đối file `%APPDATA%\JNNTool\license.dat` thật khi chạy `dotnet test`.
  - Gắn `Authorization: Bearer` theo từng `HttpRequestMessage` trong `LicenseClientService` để đảm bảo thread-safety.
  - Bổ sung `[Authorize]` cho các endpoint `validate`, `deactivate`, `renew` trên `LicenseController` (Server API).
- **JNN Updater (`JNNToolInstaller`), WiX MSI & Release Pipeline**:
  - Đồng bộ `VersionChecker` và `InstallerService` sang kiến trúc cài đặt Per-User (`%APPDATA%\Autodesk\ApplicationPlugins\JNNTool.bundle`), đọc `ProductCode` từ `HKCU\Software\JNN\JNNTool`, bỏ ép quyền Admin (`asInvoker`) và tự động dọn thư mục tải MSI tạm.
  - Loại bỏ toàn bộ DLL cũ (`NhanStr.dll`, `RincoModeling.dll`, `Material.Icons*`, `Microsoft.Expression.Drawing`, `Microsoft.Xaml.Behaviors`) và 13 icon decompile khỏi `Package.wxs` và `JNNTool.bundle`.
  - Áp dụng cơ chế Clean Distribution Repo trong `ReleaseToGitHub.ps1` (thay thế `git add -A`) để bảo vệ mã nguồn C#.
- **Core, Dispatcher & Command Palette**:
  - Đăng ký `ApplicationInitialized` đồng bộ trong `App.OnStartup` và điều phối `TaskDialog.Show()` qua `ActionEventHandler.Instance.Post`.
  - Đổi `d.Invoke` sang `d.BeginInvoke` trong `UiDispatcher.RunOnUi` để chống deadlock giữa UI thread và Revit API thread.
  - Bổ sung `ParentSplitId` trong `CommandRegistry` cho các lệnh con của `SplitButton` và gán đầy đủ `View` + `JournalData` trong `CommandRunner`.
- **Rebar Suite 3D Geometry & Builders**:
  - Hoàn thiện biến đổi tọa độ 3D, kiểm tra giao cắt BoundingBox X/Y và vector pháp tuyến mặt phẳng (`normal` / `RebarStyle.StirrupTie`) cho cả 6 phân hệ: **Sàn** (`SlabRebarBuilder`), **Dầm** (`BeamLayoutEngine`, `BeamRebarBuilder`), **Cột** (`ColumnLayoutEngine`, `ColumnRebarBuilder`), **Vách** (`WallRebarBuilder`), **Móng** (`FoundationRebarBuilder`) và **Cọc** (`PileLayoutEngine`, `PileRebarBuilder`).
- **Hệ thống Icon Vector 3x, Ribbon Theme & Command Palette**:
  - Thiết kế và đăng ký đầy đủ Path Geometry cho toàn bộ 34 biểu tượng trong `VectorIconCatalog` (`Split`, `DisallowJoin`, `Flash`, `Target`, `Eye`, `CommandPalette`, `DimColumn`, `StairDesigner`), loại bỏ hoàn toàn hiện tượng fallback về icon `Settings` và xóa bỏ trùng lặp icon giữa các nút trên cùng một Panel.
  - Đồng bộ trực tiếp bộ **Vector Icon 3x Supersampling** vào **Command Palette** (`PaletteItemVm` + `RibbonThemeService.ThemeApplied`), tự động đổi màu và kiểu hiển thị (`Badge`, `Glyph`, `Circle`, `Outline`) theo thời gian thực khi người dùng tùy chỉnh trong `Ribbon Theme Manager`.
  - Tối ưu hóa các tài nguyên ảnh PNG `1024×1024` quá khổ, giúp giảm dung lượng bộ cài `JNNToolSetup_v2.1.0.msi` từ `10.62 MB` xuống còn **`6.74 MB`**.

### Added - **Command Palette (Dockable Pane)** (panel General): Bảng lệnh tìm kiếm nhanh toàn bộ công cụ JNNTool
- **Kiến trúc Dockable Pane Modeless**:
  - Tích hợp Dockable Pane chuẩn Revit API (`IDockablePaneProvider`), có thể neo cố định hoặc dock bên cạnh màn hình Revit.
  - Nút **Command Palette** trên Panel **General** của Ribbon, hỗ trợ bật/tắt nhanh qua lệnh `TogglePaletteCmd`.
- **Tìm kiếm thông minh & Đa ngôn ngữ**:
  - Hỗ trợ gõ tiếng Việt có dấu và không dấu (chuẩn hóa `NormalizationForm.FormD`) cho toàn bộ công cụ.
  - Hỗ trợ ghim lệnh yêu thích (Favorites ★), tự động lưu trữ cấu hình tại `%AppData%\JNNTool\palette_favorites.txt`.
  - Phím tắt: `Enter` để thực thi ngay lệnh đầu tiên, `Esc` để xóa nhanh từ khóa, `Ctrl + F` để focus vào ô tìm kiếm.
- **Tuân thủ chuẩn thiết kế AGENTS.md**:
  - Không sử dụng banner tối màu, không dùng font in đậm. Nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, chữ `#2C3E50`, bo góc 5px đồng bộ.
- **Tối ưu hóa bố cục Ribbon theo nhu cầu thực tế**:
  - Chuyển công cụ **Stair Model** từ panel Layout sang panel **Stair**.
  - Tạm ẩn các công cụ chưa dùng tới: Floor By Room, Rebar Beam, Steel Connection, Clash Detection, Dim Beam, Extend Beam, Stair Rebar và panel JNN Rebar.

### Added - **Hệ thống Quản Trị & Tạo Key Bản Quyền Tự Động (Admin License Portal & CLI)**
- **SQL Backend RPC trên Supabase (`supabase/admin_license_rpc.sql`)**:
  - `admin_generate_key`: Tự động sinh mã Key chuẩn Base32 Crockford `JNN-XXXX-XXXX-XXXX` (loại bỏ ký tự dễ nhầm lẫn 0, O, 1, I, L), tính toán hạn dùng và lưu vào bảng `public.licenses`.
  - `admin_manage_license`: Xử lý 3 nghiệp vụ quản trị tức thì: Reset máy (`bound_machine_id = NULL`), Gia hạn thêm ngày (`renew`), và Thu hồi bản quyền (`revoke`).
  - `admin_list_licenses`: Truy vấn danh sách khách hàng có phân trang, tính toán số ngày còn lại và hỗ trợ tìm kiếm.
  - Bảng cấu hình bảo mật `admin_config` với RLS bật kín, bảo vệ bằng mật khẩu `Admin Secret`.
- **Giao diện Web Admin Portal (`Tools/Admin/admin-license-portal.html`)**:
  - Giao diện Web tĩnh độc lập (HTML/CSS/JS thuần, không phụ thuộc server hay Node.js), có thể chạy offline hoặc deploy lên web tĩnh.
  - Thiết kế đồng bộ chuẩn JNNTool: nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, chữ `#2C3E50`, font chữ thường thanh thoát, bo góc 5px.
  - Tự động lưu cấu hình `Admin Secret` vào `localStorage`, tích hợp nút 1-click **[ 📋 Copy Key ]** và **[ 💬 Copy Tin Nhắn Gửi Khách ]** soạn sẵn gửi qua Zalo/Email.
  - Bảng quản trị danh sách khách hàng trực quan kèm các nút thao tác nhanh: `[ 🔄 Reset Máy ]`, `[ ➕ Gia Hạn ]`, `[ ⛔ Thu Hồi ]`.
- **Script Dòng Lệnh PowerShell (`scripts/create-key.ps1`)**:
  - Tạo key siêu tốc từ terminal chỉ với 1 lệnh: `.\scripts\create-key.ps1 -Email "user@gmail.com" -Days 365`.
  - Tự động sao chép mã Key sinh ra vào bộ nhớ tạm (Windows Clipboard).
  - Hỗ trợ tham số `-ResetMachine -LicenseKey "JNN-..."` để hủy khóa máy nhanh chóng khi khách đổi máy tính.

### Added - Module **Account & License Manager** (panel General): Quản lý bản quyền, đăng nhập tài khoản và kích hoạt JNNTool
- **Vị trí Ribbon**: Bố trí trên Panel **General** của Tab `JNNTool` (nút **Account / License** kèm icon nhận diện `Login.png`).
- **Tái cấu trúc Clean-room & Rebrand 100%**:
  - Chuyển toàn bộ namespace sang `JNNTool.Tools.Login.*`.
  - Thay thế hoàn toàn phụ thuộc thư viện bên ngoài (`NhanStr.IconLibrary`) bằng vector `Path Geometry` WPF chuẩn, không runtime crash hay thiếu DLL.
  - Tích hợp chuẩn kiến trúc Core: dùng `ActionEventHandler` chung của JNNTool, loại bỏ handler riêng lẻ dư thừa.
- **Tuân thủ chuẩn thiết kế AGENTS.md**:
  - Không sử dụng banner tối màu trên đầu cửa sổ (`Title` chuẩn Windows native).
  - Không dùng font in đậm (`FontWeight="Normal"`, `FontSize="12"`).
  - Nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, chữ `#2C3E50`.
  - Bo góc $5\text{px}$ đồng bộ trên tất cả Button, TextBox, Card, Border.
  - Ngôn ngữ UI chuẩn tiếng Anh kỹ thuật (Email, Activation Code, License Type, Days Remaining, Active, Expired...).
- **Cơ chế xác thực & Bảo mật an toàn (Device Fingerprint & DPAPI)**:
  - Sinh mã định danh phần cứng SHA-256 (`DeviceFingerprintService`) kết hợp bo mạch, CPU và MachineName.
  - Lưu trữ chứng chỉ bản quyền bằng Windows DPAPI (`SecureStorageService`) tại `%AppData%\JNNTool\License\client_license.dat`, mã hóa ở cấp người dùng, chống sao chép trái phép sang máy khác.
  - Hỗ trợ chế độ Offline Grace Period lên tới 14 ngày khi mất kết nối mạng.
- **3 Cửa sổ chức năng chuyên nghiệp**:
  - `LoginWindow`: Đăng nhập, kích hoạt bằng Email và GUID Activation Code, lấy bản dùng thử 30 ngày (Trial).
  - `LicenseStatusWindow`: Xem chi tiết bản quyền (Loại, Ngày hết hạn, Số ngày còn lại, Thiết bị), Refresh trạng thái trực tuyến hoặc Hủy kích hoạt (Deactivate).
  - `LicenseExpiredWindow`: Thông báo hết hạn lịch sự, nút gia hạn (Renew) hoặc nhập Key mới (Enter New Key).
- **Kiểm thử**: Viết mới `Tests/JNNTool.Tests/LoginTests.cs` (5 unit tests pass, tổng 174 tests), checklist kiểm thử tay tại `docs/test-checklist-login.md`.

### Enhanced - **Bộ Icon Vector 3x Supersampling làm mặc định 100% cho toàn bộ thanh Ribbon JNNTool**
- **Sắc nét tuyệt đối & Đồng bộ giao diện**:
  - Chuyển `UseVectorTheme = true` làm mặc định cho toàn bộ thanh Ribbon khi khởi động (`App.OnStartup`), loại bỏ hoàn toàn các icon PNG mờ nhòe, mất hình hoặc viền răng cưa.
  - Bổ sung biểu tượng `JNNIcon.Key` hình chìa khóa sắc nét cho nút **Account / License** (`cmdLicense`).
  - Bao phủ toàn diện 100% các nút trên Ribbon vào danh mục `_knownButtons`: bao gồm `cmdLicense`, `cmdRebarBeam`, `cmdRebarColumn`, `cmdRebarUtilsSuite`, `cmdAutoJoinSplit`.
  - Cập nhật hiển thị biểu tượng vector trực tiếp cho cả nút chính của `PulldownButton` và `SplitButton` cùng toàn bộ nút con bên trong menu xổ xuống.
  - Tự động nạp và giữ nguyên mọi tùy biến màu sắc (12 theme màu) và phong cách đồ họa (Badge, Glyph, Circle, Outline) của người dùng qua `RibbonThemeSettings`.

### Added - Tool **Ribbon Theme & Icon Color Manager** (panel General): Tùy biến màu sắc và kiểu Icon Ribbon JNNTool
- **Vị trí Ribbon**: Bố trí trên Panel **General** của Tab `JNNTool` (nút **Ribbon Theme**).
- **Bộ Icon Vector chuẩn Icons8 (Solid Filled + Rounded Corners)**:
  - Thiết kế lại toàn diện 25 biểu tượng `JNNIcon` theo chuẩn **Solid Filled + Rounded Corners** (hình khối đặc dày dặn, góc bo tròn mềm mại $R=1.5 - 2.5\text{px}$, các đường khe rãnh âm bản EvenOdd thể hiện cốt thép đai/chủ, lưới trục, dầm sàn, bu lông).
  - Không còn hiện tượng nét mảnh 1px nhạt nhòa, không răng cưa, đạt độ nhận diện cấu kiện kết cấu cao nhất.
- **Engine kết xuất đồ họa siêu nét (3x Supersampling - 288 DPI)**:
  - Nâng cấp `RibbonThemeService.CreateRibbonImage` sử dụng kỹ thuật 3x Supersampling: Render ở độ phân giải $96 \times 96$ pixel (nút lớn 32px) và $48 \times 48$ pixel (nút nhỏ 16px) với DPI = 288.
  - Bật chế độ `RenderOptions.BitmapScalingMode="HighQuality"` và `EdgeMode.Unspecified` khử răng cưa mượt mà, giúp icon sắc nét tối đa trên mọi màn hình Full HD, 2K và 4K Retina mà không bị mờ nhòe.
- **Theme Color Presets**: 12 bảng màu hiện đại chuẩn kỹ thuật (Apex Blue, Royal Indigo, Ocean Cyan, Deep Teal, Emerald Green, Amber Gold, Sunset Orange, Crimson Red, Vivid Rose, Electric Purple, Dark Slate, Midnight Black) kèm ô nhập Custom Hex Code và live color swatch.
- **Icon Presentation Styles (4 kiểu đồ họa)**:
  - `Badge`: Thẻ bo góc $5\text{px}$ đổ gradient màu chủ đạo kèm viền sáng tinh tế $0.6\text{px}$ với biểu tượng vector trắng nổi bật.
  - `Glyph`: Biểu tượng vector đặc theo màu chủ đạo trên nền trong suốt (phong cách tối giản, tinh tế).
  - `Circle`: Khung tròn gradient màu chủ đạo với biểu tượng vector trắng bên trong.
  - `Outline`: Thẻ nền màu nhạt (12% tint), viền bo góc $5\text{px}$ và biểu tượng vector cùng màu.
- **Tùy chọn Icon 3D vs Vector linh hoạt (`UseVectorTheme`)**:
  - Thêm cờ `UseVectorTheme = false` mặc định để giữ trọn vẹn bộ Icon 3D ảnh màu chân thực trong `JNNTool.bundle/Contents/Resources`.
  - Khi người dùng mở UI Manager và nhấn **Apply to Revit Ribbon**, hệ thống tự động lưu `UseVectorTheme = true` và nạp theme vector tùy biến.
- **Live Ribbon Preview thời gian thực**:
  - Mô phỏng toàn bộ nút bấm PushButton của thanh Ribbon JNNTool theo từng Panel (JNN Rebar, Modeling, CSI x Revit, Layout, Stair, Quantity, General) hoặc Tất cả (All).
  - Tự động re-render và cập nhật tức thì khi người dùng đổi màu hoặc đổi Style.
- **Áp dụng trực tiếp vào Revit Ribbon (`Apply to Revit Ribbon`)**:
  - Cập nhật trực tiếp `LargeImage` (32x32) và `Image` (16x16) cho toàn bộ PushButton, SplitButton, PulldownButton trên thanh Ribbon của Revit qua `ExternalEvent` an toàn mà không cần khởi động lại Revit.
- **Persistence & Tự động nạp khi khởi động**:
  - Cấu hình lưu trữ tại `%AppData%\JNNTool\Settings\RibbonThemeSettings.json` qua `SettingsService<RibbonThemeSettings>`.
  - Tự động nạp và áp dụng theme cho thanh Ribbon trong `App.OnStartup`.
- **Giao diện chuẩn JNN**: Modeless, không banner tối màu, không dùng font in đậm (`FontWeight="Normal"`, `FontSize="12"`), nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, bo góc $5\text{px}$ đồng bộ. Khắc phục triệt để lỗi WPF Shutdown bằng `WpfWindowHelper`. Checklist: `docs/test-checklist-ui-manager.md` + 5 unit tests mới (tổng 176 tests pass 100%).

### Added - Tool **Quantity Takeoff** (tab Quantity): Bóc tách khối lượng toàn diện đa cấu kiện, Visual Preview Dashboard & 3D Section Box
- **Vị trí Ribbon**: Bố trí trên Tab riêng biệt **Quantity** (panel `Takeoff`) độc lập.
- **Bóc tách đa Category siêu tốc**: Dầm kết cấu, Cột kết cấu, Sàn, Vách/Tường, Móng/Đài/Cọc, Cầu thang, Cốt thép Rebar (Rebar & RebarInSystem), Mái, Trần, Cột kiến trúc, Cấu kiện khác...
- **Chỉ số khối lượng đầy đủ**:
  - Thể tích Bê tông ($m^3$) — ưu tiên đọc parameter `HOST_VOLUME_COMPUTED` siêu nhanh, tự động fallback quét Solid 1 lần duy nhất với mức `Coarse` khi cần.
  - Diện tích Cốp pha ($m^2$) — tự động trích xuất hoặc ước tính mặt biên Solid.
  - Khối lượng Thép / Rebar (Tấn, kg) — tính toán toán học chính xác 100% bằng công thức định mức $0.006165 \times d^2$, không bóc tách Solid, xử lý tức thì cho cả Rebar và RebarInSystem không gây giật lag.
  - Chiều dài ($m$), Số lượng cấu kiện.
- **Visual Preview Dashboard (Trực quan hóa khối lượng)**:
  - Tích hợp đồ thị trực quan song song với bảng số liệu, có nút bật/tắt `[📊 Visual Preview]` linh hoạt trên Toolbar.
  - **By Level (Elevation Stack)**: Mặt đứng cao độ khối lượng các tầng từ trên xuống kèm thanh Progress Bar phẳng thể hiện tỷ lệ $m^3$ và %, click chọn tầng để lọc bảng con bên trái, có nút Box 3D nhanh từng tầng.
  - **By Category (Category Share)**: Biểu đồ phân bổ tỷ lệ khối lượng theo loại cấu kiện với bảng màu hiện đại, thanh progress bar phẳng và nút Box 3D nhanh.
- **Thống kê theo Tầng (By Level)**:
  - Bảng tổng hợp từng tầng kèm cao độ, thể tích bê tông, cốp pha, khối lượng thép.
  - Bảng phân tách chi tiết danh mục cấu kiện trong tầng được chọn (Category Breakdown).
- **Thống kê theo Cấu kiện (By Category)**:
  - Bảng tổng hợp theo loại cấu kiện trong phạm vi chọn.
- **Chi tiết cấu kiện & Tìm kiếm thời gian thực (Element Details)**:
  - Tìm kiếm live filter theo ID, Type, Family, Level, Mark, Vật liệu.
  - Lọc theo Category và Level dạng Dropdown.
- **Bấm Box 3D tức thì (3D Section Box)**:
  - Bấm nút **Box 3D** hoặc Double-click vào bất kỳ dòng nào (Tầng, Loại cấu kiện, hoặc Cấu kiện đơn lẻ): tự động chuyển sang view 3D (hoặc tạo `{3D - JNN QTO}`), tự động co Section Box vừa khít đối tượng kèm khoảng đệm nhìn rõ nét và zoom tới đối tượng.
- **Xuất bảng tính Excel chuyên nghiệp (.xls / .csv)**:
  - Xuất file Excel định dạng XML Spreadsheet 2003 gồm **3 Sheet đầy đủ định dạng** (By Level, By Category, Element Details) kèm tiêu đề, dòng tổng cộng in đậm viền kép và định dạng số chuẩn $0.00$. Mở được ngay trên Microsoft Excel mà không cần cài đặt thư viện COM Interop.
- **Giao diện chuẩn JNN**: Modeless, không banner tối màu, không dùng font in đậm (`FontWeight="Normal"`, `FontSize="12"`), nền sáng `#F5F7F9`, Card trắng `#FFFFFF`, viền `#DCDDE1`, bo góc $5\text{px}$ đồng bộ. Khắc phục triệt để lỗi WPF Shutdown bằng `WpfWindowHelper`. Checklist: `docs/test-checklist-quantity-takeoff.md`.


### Added - Tool **Auto Structural Stair** (panel Modeling): Dựng cầu thang kết cấu bê tông tự động (Revit 2024–2027)
- **3 dạng hình học kết cấu**:
  - `Thang chữ U` (U-Shaped): 2 vế song song đảo chiều 180° có chiếu nghỉ giữa, hỗ trợ rẽ trái/phải và khe hở giữa 2 vế.
  - `Thang chữ L` (L-Shaped): 2 vế vuông góc 90° có chiếu nghỉ góc.
  - `Thang thẳng` (Straight): 1 vế chạy suốt hoặc 2 vế thẳng có chiếu nghỉ nối tiếp.
- **Dựng cấu kiện Native BIM Kết Cấu**:
  - Dựng bản thang nghiêng bằng `Floor` có độ dốc (`slopeArrow` và `slope`), bản chiếu nghỉ bằng `Floor` phẳng, dầm đỡ kết cấu bằng `Structural Framing`.
  - Tự động gộp (`JoinGeometry`) các bản sàn và chiếu nghỉ giao nhau liền mạch.
  - Đóng gói toàn bộ trong 1 `TransactionGroup` để người dùng Undo sạch sẽ trong 1 bước duy nhất.
- **Mặt bằng trực quan 2D thời gian thực (Interactive 2D Plan Preview)**:
  - Tự động scale và hiển thị chi tiết từng vế, chiếu nghỉ, vạch chia mặt bậc, mũi tên dốc (Slope Arrow UP) và dầm đỡ (Support Beam).
  - Tự động tính toán và đánh giá chỉ số bước chân ecgônômi theo công thức Blondel ($2R + T = 600 - 640\text{ mm}$).
- **Giao diện chuẩn JNN**: Modeless, không banner tối màu, không dùng font in đậm (`FontWeight="Normal"`, `FontSize="12"`), bo góc $5\text{px}$ đồng bộ.
- **Engine thuần C#** `StairGeometryEngine`, `StairValidator` + 5 unit tests (tổng 166 pass). Checklist: `docs/test-checklist-stair-model.md`.

### Added - Tool **Auto Dim Column** (panel Layout): Tự động Dimension Cột kết cấu
- **Kích thước cột**: Dim chiều rộng $b$ và chiều dài $h$ cho cột chữ nhật/vuông; hỗ trợ cột xoay góc bất kỳ trên mặt bằng theo hệ trục cục bộ của cột.
- **Định vị từ Lưới trục (Grid-to-Column)**: Tự động tìm Grid gần nhất và song song với từng mặt cột trong phạm vi tìm kiếm (`GridSearchRangeMm`). Tạo chuỗi dim định vị `Grid -> Mép cột gần` hoặc chuỗi liên hoàn `Grid -> Mép gần -> Mép xa`.
- **Chống trùng Dimension**: Quét các dimension hiện hữu trong view và tự động bỏ qua nếu cặp Reference đã tồn tại (`Skip Existing Dimensions`).
- **Giao diện chuẩn JNN**: Không banner tối màu, không in đậm (`FontWeight="Normal"`), bo góc $5\text{px}$ đồng bộ, Preview DataGrid kiểm soát từng cột trước khi tạo, Undo một lần (`TransactionGroup`).
- **Engine thuần C#** `AutoDimColumnEngine` + 11 unit test mới (tổng 161 pass). Checklist: `docs/test-checklist-auto-dim-column.md`.

### Added - Tool **Element Mark** (panel Layout): đánh Mark Dầm / Cột / Sàn
- **Dầm**: mẫu do người dùng đặt, mặc định `{P}{L}{D}{N:00}` → `DS1X01`. Góc ≤ 45° → X, > 45° → Y. Đánh số riêng theo từng tầng và từng phương.
- **Dầm liên tục**: các nhịp thẳng hàng, khe hở ≤ 1000 mm (cấu hình được) gộp thành một dầm `DS1X01.1`, `DS1X01.2`… Có thể tắt.
- **Cột**: `C1`, `C2`… dùng chung Mark cho cả chuỗi từ móng đến mái (chịu lệch tâm ≤ 300 mm khi thu tiết diện). Ưu tiên theo hàng hoặc theo cột.
- **Sàn**: một Mark chung cho mọi sàn của cùng Level (`S1` cho Level 1), tuỳ chọn chỉ sàn kết cấu.
- **Hướng đánh số** Trên/Dưới, Trái/Phải theo hệ trục view mặt bằng đang mở (đúng cả khi view bị xoay).
- **Mã tầng `{L}`** lấy từ tên Level (`Tầng 1` → `1`, `Tầng hầm 1` → `B1`, `Mái` → `M`), sửa được trong bảng và được lưu lại.
- Phạm vi View hiện hành / Toàn dự án; ghi đè tất cả hoặc chỉ phần tử chưa có Mark; ghi vào `Mark` hoặc tham số Text khác.
- Bảng xem trước (Mark cũ → mới, lọc theo loại, bấm dòng để chọn phần tử). Ghi trong một Transaction `"JNN - Đánh Mark cấu kiện"` (Undo một lần), tự xoá cảnh báo trùng Mark.
- Engine thuần C# `ElementMarkEngine` + 26 unit test (tổng 150). Checklist: `docs/test-checklist-element-mark.md`.

## [2.0.6] - 2026-10-10

### Changed - Nút **Dim Grid** chuyển sang tool **Auto Dim Grid**
- Gỡ tool cũ `Tools/Dim Grid` (`JNNTool.Tools.DimGrid`) cùng `GridDimEngineTests` (test còn 115).
- Nút "Dim Grid" trên Ribbon gọi `JNNTool.Tools.AutoDimGrid.AutoDimGridCommand` (đổi namespace từ `NhanARC.Tools.Auto_Dim_Grid`). Icon `DimGrid.png` chuyển vào `Tools/Auto Dim Grid/`.
- Transaction đổi thành `"JNN - Dim lưới trục 4 cạnh"` / `"JNN - Dim tường (thủ công)"`, lỗi được ghi log.
- **Sửa lỗi Auto Dim Grid**:
  - Lấy đường trục **đang hiển thị trong view** (`GetCurvesInView`), không dùng `Grid.Curve` 3D. Dim đúng khi đã kéo đầu trục và đúng trên **mặt cắt / mặt đứng** (không có trục vuông góc thì Dim đặt vào trong tính từ đầu trục để không đè bóng).
  - Mỗi Dim tạo độc lập: một tham chiếu lỗi không còn làm mất cả lần chạy. Hộp thoại báo số Dim đã tạo / lỗi / lỗi đầu tiên. Cảnh báo Revit được tự xóa.
  - Khử tham chiếu trùng vị trí (< 1 mm) để tránh đoạn Dim 0. Dim thủ công chỉ lấy trục thực sự cắt qua tường.
  - Dim thủ công: chọn liên tục nhiều tường, ESC để kết thúc, mỗi tường 1 lần Undo.
  - Cửa sổ không tự đóng khi bấm Chạy, bỏ Topmost (gắn Owner Revit), khóa nút khi đang chạy.
  - Lưu cài đặt (`AutoDimGridSettings.json`): số lớp, khoảng cách, kiểu Dim, tab. Nhập được dấu phẩy thập phân.
  - Tách `AutoDimGridEngine` thuần C# + 9 unit test (tổng 124).

### Added - CSI x Revit: nút **Fit Scope Box** (đầu trục sát mép `JNN_SB`)
- Level gắn Scope Box `JNN_SB`. Grid **gỡ khỏi** Scope Box vì Revit khóa extents của Grid đã gắn Scope Box và bỏ qua mọi lệnh chỉnh đầu trục qua API.
- Grid được cắt 3D đúng biên Scope Box, sau đó kéo đầu 2D vào trong trên từng mặt bằng. Lượt 2 đo khung bao Grid trong view (có cả bóng) rồi bù, để mép bóng chạm sát mép Scope Box ở mọi tỷ lệ view.
- Bật bóng trục cả 2 đầu. Hộp thoại báo số trục đã cắt / đã ép sát / lỗi đầu tiên (không còn nuốt lỗi im lặng).
- Kéo lại Scope Box thì bấm Fit Scope Box lần nữa để cập nhật.

### Changed - CSI x Revit (ETABS E2K → Revit): Tạo tiết diện thiếu & phân loại sàn S / F
- **Template theo loại**: thanh trên Tab 1 có thêm **Template Sàn (S)**, **Template Móng (F)**, **Template Vách** (cạnh Dầm / Cột). Nút "Áp dụng Template" áp đúng template cho từng loại, chỉ trên các dòng chưa có.
- **Tách 3 cột**: **Family** → **Type mẫu** (lọc theo Family) → **Type Revit** (tên sẽ tạo, mặc định = tên ETABS, sửa được).
- **Sửa lỗi UI không khớp sau khi tạo**: sau "Tạo các tiết diện đã chọn", dòng hiển thị đúng type vừa nhân bản (vd. `C60X20`), Tab 2 được gán đúng type đó (ghim, AutoMatch không ghi đè). Kiểm tra "Đã có" chỉ theo **tên chính xác** (trước đây `Contains("100")` khớp nhầm `JNN_SanBT_100`).
- **Phân loại sàn** (`SlabClassifier`, có unit test):
  - **Sàn móng (F)**: tên `F<số>` hoặc `MODELINGTYPE = ShellThick` → dựng bằng type **Foundation Slab** (category *Structural Foundations*). Không bao giờ dựng thành Floor thường; nếu dự án chưa có type Foundation Slab thì cảnh báo và bỏ qua.
  - **Sàn (S)**: các sàn còn lại → Floor thường.
  - **Sàn ảo**: dày ≤ 1 mm (SS1, S1 membrane) → hiển thị nhưng không dựng.
- Parser đọc thêm `MODELINGTYPE`, `SLABTHICKNESS`, `WALLTHICKNESS` (trước đây dựa vào số đầu tiên).
- Hộp thoại kết quả đếm riêng Sàn S / Sàn móng F / Sàn ảo bỏ qua.
- **Modeless**: cửa sổ Import không còn khóa Revit. Tạo tiết diện / Làm mới / Dựng hình đều chạy qua `ExternalEvent` (handler riêng, tạo trong Command nên chạy được cả khi load bằng Add-in Manager). Chặn mở trùng cửa sổ, kiểm tra document còn hợp lệ, con trỏ chờ khi đang chạy.
- **"Đã có" theo kích thước thực**: đọc chiều dày (Floor/Wall, kể cả nhiều lớp) và b×h / D (FamilySymbol) từ type Revit. Có type cùng kích thước (vd. S200 ↔ `JNN_SanBT_200`) → tự chọn sẵn type đó, bỏ tick, Tab 2 tự gán. Cột cho phép đảo b/h, dầm thì không. Trạng thái vàng đổi thành "Chưa có type cùng kích thước".
- Nhân bản Floor/Wall: chỉnh lớp lõi để **tổng** chiều dày đúng bằng ETABS (trước đây chỉ đặt lớp lõi).
- **UI làm lại & chuyển sang tiếng Anh**: thanh template thành card "Default Templates" (lưới 5 cột Beam / Column / Slab (S) / Foundation Slab (F) / Wall), nút bo góc có hover. Toàn bộ nhãn, trạng thái, thông báo, hộp thoại kết quả đều tiếng Anh. Nhãn loại cấu kiện gom về `ElementKinds` (bỏ chuỗi cứng trong Parser / Command).
- **Tên type mới theo kích thước thực (mm)** (`TypeNameSuggester`, có unit test): tiền tố chữ của tên ETABS + b×h, vd. `C50X20` → `C200x500`, `B30X25` → `B300x250`, cột tròn `C-D50` → `CD500`, sàn/vách `S20` → `S200`. Thiếu kích thước thì giữ tên ETABS. Vẫn sửa tay được ở cột Revit Type.
- Bỏ tùy chọn "Align perimeter beams/columns inward" (luôn giữ tim theo E2K).
- **Quy trình 3 bước Scope Box chuẩn**:
  - *Bước 1 (Build model)*: Dựng đầy đủ cấu kiện, lưới trục, cao độ.
  - *Bước 2 (Đặt Scope Box tâm nhà)*: Tool tự động tìm Scope Box `JNN_SB` và đặt tại đúng tâm công trình (X, Y, Z) để người dùng mở view lên là thấy ngay ở giữa nhà, thoải mái dùng chuột kéo 4 góc bao quanh căn nhà (chưa khóa vội Grid/Level).
  - *Bước 3 (Fit Level & Grid)*: Thêm lệnh Ribbon mới **"Fit Scope Box"** (`FitScopeBoxCommand`) trên panel `CSI x Revit`. Sau khi kéo xong Scope Box quanh nhà, bấm nút này là toàn bộ Grid & Level lập tức khóa và cắt khớp khít 100% theo Scope Box.

### Added - Tools: Công cụ 1 nút "Auto Join" (`AutoJoinCommand`)
- **Vị trí Ribbon**: Tạo Panel mới **"General"** trên Ribbon Tab `JNNTool` (đứng đầu tiên trên thanh Ribbon) chứa nút SplitButton **"Auto Join"**.
- **Mức độ ưu tiên cấu kiện (TCVN / BIM)**:
  - **Cột (Column) > Dầm (Beam) > Sàn (Floor)**
  - Cột cắt Dầm (`Column cuts Beam`) và Cột cắt Sàn (`Column cuts Floor`).
  - Dầm cắt Sàn (`Beam cuts Floor`).
  - Tự động kiểm tra và đảo thứ tự ưu tiên (`Switch Join Order`) nếu liên kết trước đó bị ngược (ví dụ Sàn đang cắt Dầm/Cột).
- **2 chế độ thực thi linh hoạt**:
  - **Active View (Mặc định)**: Bấm 1 click vào nút to trên Ribbon để quét và liên kết nhanh toàn bộ cấu kiện trong View hiện hành.
  - **Entire Project**: Bấm mũi tên xổ xuống để quét và liên kết toàn bộ mô hình dự án.
  - Hỗ trợ gọi `AutoJoinCommand` hiển thị hộp thoại TaskDialog 2 CommandLinks trực quan.
- **Tối ưu hiệu năng & Ổn định**:
  - Dùng bộ lọc không gian `BoundingBoxIntersectsFilter` (C++ level trong Revit) để tìm cấu kiện giao cắt siêu tốc.
  - Tích hợp `DismissWarningsPreprocessor` triệt tiêu mọi popup warning của Revit trong lúc Join hàng loạt.
  - Đảm bảo tương thích hoàn hảo đa phiên bản Revit (2022-2027) qua `ElementIdCompat.GetIdValue()`.

### Updated - Auto View & Sheet (`AutoViewSheetCommand`):
- **Luôn luôn bật Collapse**: Cập nhật trạng thái mặc định của Expander thành `IsExpanded="False"` và kích hoạt tự động Collapse khi mở cửa sổ.
- **Chuẩn hóa nhận diện Tầng (T1 → TẦNG 1)**: Cập nhật bộ lọc Regex nhận diện tên Level bắt đầu bằng `T` (ví dụ `T1`, `T2`... `T7`, `T01`) sẽ tự động lược bỏ chữ `T` để gán tên Sheet chuẩn: `MẶT BẰNG KẾT CẤU TẦNG 1` thay vì bị lặp `TẦNG T1`. Đảm bảo tự động cập nhật lại tên Sheet khi đổi Level trong ComboBox.


### Fixed - CSI x Revit: Khắc phục triệt để lỗi căn lề cột (Tự động nhận diện B/2 hoặc H/2 theo hướng dời và góc xoay)
- **Vấn đề đã xử lý**: Cột có tiết diện $250\text{ mm}$ (hoặc các cột chữ nhật $b \ne h$, $200\times 250$, $250\times 500$) khi căn lề theo phương Y bị dời sai khoảng cách ($100\text{ mm}$ thay vì $125\text{ mm}$) do code cũ hardcode chỉ đọc tham số `b`, bỏ qua tham số `h`, không xét hướng di chuyển và kế thừa mù quáng vector dời của dầm biên.
- **Giải pháp kỹ thuật**:
  - Tạo [`ColumnDimensionHelper.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/CSI%20x%20Revit/Builder/ColumnDimensionHelper.cs) tính toán kích thước phủ bì thực tế $W_X$ và $H_Y$ theo công thức hình học tổng quát:
    $$W_X = b \cdot |\cos \theta| + h \cdot |\sin \theta|$$
    $$H_Y = b \cdot |\sin \theta| + h \cdot |\cos \theta|$$
    kết hợp đối soát với `BoundingBoxXYZ` thực tế trong Revit.
  - Tách bạch giữa **Hướng di chuyển (Unit Vector)** và **Khoảng dời (Magnitude)**: Khi cột nối với dầm biên, cột chỉ kế thừa hướng vector của dầm biên, còn khoảng cách dời tự động tính theo nửa kích thước của chính cây cột đó theo phương dời ($H/2$ hoặc $B/2$).
  - Cập nhật [`BoundaryAligner.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/CSI%20x%20Revit/Builder/BoundaryAligner.cs) và giao diện [`BoundaryAlignWindow.xaml`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/CSI%20x%20Revit/UI/BoundaryAlignWindow.xaml).
  - Bổ sung 11 unit test trong [`CSIxRevitTests.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tests/JNNTool.Tests/CSIxRevitTests.cs) kiểm tra đầy đủ các góc xoay $0^\circ, 90^\circ$ và phương di chuyển $X, Y$ (`83/83 tests PASS 100%`).

### Added - Tools: Công cụ "Tự Động Dim Grid" (`AutoDimGridCmd`)
- **Vị trí Ribbon**: Thêm nút **"Dim Grid"** trên Ribbon tab `JNNTool` (Panel `Modeling`, nằm ngay cạnh *Dim Beam*).
- **Tính năng nổi bật**:
  - Tự động nhận diện và phân nhóm các hệ lưới trục (Grid) song song theo trục dọc, trục ngang và góc xiên bất kỳ trên mặt bằng (`ViewPlan`).
  - Hỗ trợ tạo kích thước 4 phía: **Phía Trên (Top)**, **Phía Dưới (Bottom)**, **Phía Trái (Left)**, **Phía Phải (Right)**.
  - Tự động tạo 2 lớp kích thước:
    - **Dim chi tiết (Segment Dim)**: Đo khoảng cách giữa từng cặp trục liên tiếp.
    - **Dim tổng (Overall Dim)**: Đo khoảng cách tổng giữa 2 trục biên ngoài cùng.
  - Tự động lọc bỏ các trục trùng tim (Duplicate Grids) để chống lỗi Revit API.
  - Tùy chỉnh khoảng cách offset từ đầu trục và giữa 2 lớp Dim (theo mm trên giấy, tự nhân theo View Scale).
  - Tự động lưu cấu hình người dùng vào `%AppData%\JNNTool\Settings\DimGridSettings.json`.
  - Hỗ trợ cả 2 chế độ: Tự động chạy cho toàn bộ trục trong View hoặc Quét chọn trục cụ thể trên màn hình.
- **Kiến trúc Clean-Room & Đa phiên bản**:
  - Thuật toán tính toán hình học độc lập `GridDimEngine.cs` (thuần C#, unit test đạt 100%).
  - Tương thích đầy đủ toàn bộ phiên bản Revit **2022, 2023, 2024, 2025, 2026, 2027**.

## [2.0.4] - 2026-10-06

### Added - CSI x Revit: Công cụ độc lập "🎯 Căn Lề Biên" (Boundary Align)
- **Công cụ Ribbon Modeless mới**: Thêm nút bấm **"🎯 Căn Lề Biên"** (`BoundaryAlignCommand`) trên Ribbon tab `JNNTool` (Panel `CSI x Revit`).
- **Giao diện Modeless Window (`BoundaryAlignWindow.xaml`)**:
  - Cho phép người dùng quét chọn Dầm & Cột biên trực tiếp trên mặt bằng Revit mà không cần đóng cửa sổ.
  - Tự động lọc cấu kiện bằng `FrameAndColumnSelectionFilter` (Structural Framing & Structural Columns).
  - Tự động tính toán hướng vào trong lòng nhà (Auto Grid) hoặc chọn hướng thủ công (Dời xuống -Y, Dời lên +Y, Dời sang trái -X, Dời sang phải +X).
  - Tự động offset biên theo $B/2$ ($100\text{ mm}$ cho dầm $200\text{ mm}$) hoặc khoảng cách tùy chỉnh.
- **Thuật toán co/kéo dầm ngang (`BoundaryAligner.cs`)**:
  - Tự động co ngắn hoặc kéo dài các dầm ngang vuông góc kết nối vào dầm biên, đảm bảo không bị hở nút kết cấu sau khi dời biên.
  - Cơ chế Anti-Skew Beam Protection chống tuyệt đối hiện tượng xiên dầm.

### Fixed - CSI x Revit: Khắc phục lỗi UI bị chìm sau Revit & Tinh gọn cửa sổ Import
- **Chống chìm UI ra sau Revit**:
  - Gán `WindowInteropHelper.Owner = uiapp.MainWindowHandle;` cho cửa sổ `ImportWindow`.
  - Cấu hình `WindowStartupLocation="CenterOwner"` giúp cửa sổ luôn xuất hiện ngay trung tâm màn hình Revit.
  - Tích hợp `RevitWin32Window` cho `OpenFileDialog.ShowDialog(...)` để Revit không giành focus khi đóng dialog chọn file e2k.
- **Tinh gọn cửa sổ Import**:
  - Loại bỏ hoàn toàn Tab 3 (Căn lề biên) khỏi `ImportWindow`, quy trình chỉ tập trung vào 2 tab cốt lõi: 1. Tạo tiết diện thiếu & 2. Khớp tiết diện.
  - Sau khi bấm "Bắt đầu Dựng Hình", cửa sổ sẽ tự động đóng và thông báo kết quả.

### Fixed - CSI x Revit: Khắc phục triệt để lỗi góc xoay cột biên & cột dẹt
- **Nguyên nhân cốt lõi**: Trong công thức cũ của `ColumnBuilder.cs`, `targetAngleDeg = angleEtabsLong - angleRevitLong = 90 - 90 = 0`, khiến các cột biên chữ nhật (ví dụ C50X20, C60X20) không được xoay mà giữ nguyên góc đặt mặc định của Revit (cạnh dài $h = 500/600\text{ mm}$ đâm ngang vào lòng nhà).
- **Giải pháp**: Xây dựng [`ColumnRotationHelper.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/CSI%20x%20Revit/Builder/ColumnRotationHelper.cs) tự động đối soát chính xác giữa quy ước kích thước $D$ (theo trục X) & $B$ (theo trục Y) của ETABS với các tham số $b$ và $h$ của Family Revit:
  - Cột biên dọc trục X (`C50X20`, `C60X20`): tự động xoay $90^\circ$ để cạnh dài $500/600\text{ mm}$ chạy dọc theo vách biên/dầm biên (phương X) đúng 100% như mô hình ETABS.
  - Cột đầu hồi (`C20X30`): tự động giữ nguyên $0^\circ$ để cạnh $300\text{ mm}$ chạy dọc tường đầu hồi (phương Y).
  - Cột vách thang (`C15X40`): tự động giữ nguyên $0^\circ$ để cạnh $400\text{ mm}$ chạy dọc vách thang (phương Y).
  - Cột có `ANG 90` (như cột thép `SC1` hoặc `C8`): tự động xoay $90^\circ$.
  - Đồng bộ logic tính toán bao hình biên trong [`EdgeAlignmentHelper.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/CSI%20x%20Revit/Builder/EdgeAlignmentHelper.cs).
- **Kiểm thử**: Bổ sung Unit Tests kiểm tra đầy đủ 7 trường hợp cột (`69/69 tests PASS 100%`). Đã build và deploy thành công cho toàn bộ các phiên bản Revit 2022 - 2027.

### Changed - Ribbon Ribbon Bar
- **Tạm ẩn Module `Tools/IFC Etabs`**: Tạm ẩn 2 nút `ETABS Converter` và `Property Viewer` trên Ribbon theo yêu cầu do module đang trong giai đoạn tiếp tục hoàn thiện.

### Added - JNN ETABS Converter (Phase 3: Validation Engine & Báo Cáo Nghiệm Thu QA)
- **Bộ Máy Đối Soát Mô Hình (Validation Engine - `ValidationEngine.cs`)**:
  - Tự động kiểm tra đối soát số lượng cấu kiện (Cột, Dầm, Tầng) giữa mô hình nguồn (ETABS / IFC) và mô hình Revit thực tế.
  - Kiểm tra sai lệch cao độ tầng (Levels) và sai lệch kích thước hình học (chiều dài Dầm, Cột) theo dung sai tùy biến (mặc định ±2 mm).
  - Phân loại đánh giá trạng thái chi tiết: `Pass` (Khớp), `Warning` (Cảnh báo sai lệch), `Fail` (Lỗi).
  - Tính toán tỷ lệ đạt chuẩn tổng thể `PassRate` (%) của toàn dự án.
- **Bộ Máy Xuất Báo Cáo Nghiệm Thu (Report Generator - `ReportGenerator.cs`)**:
  - Thiết kế Decoupled (thuần C# POCO) cho phép sinh báo cáo hoàn chỉnh độc lập hoặc tích hợp trực tiếp với Revit Document.
  - **Báo cáo HTML Dashboard**: Thiết kế Dark-Theme hiện đại, lưới thẻ KPI trực quan, bảng thống kê số lượng cấu kiện và bảng chi tiết sai lệch dung sai kèm huy hiệu màu sắc.
  - **Báo cáo Bảng Tính CSV**: Chuẩn hóa mã hóa **UTF-8 with BOM** giúp hiển thị hoàn hảo tiếng Việt trên Microsoft Excel, xuất chi tiết nguồn, mã ID gốc, phân loại, tên tiết diện, tầng và Revit Element ID tương ứng.
- **Tích Hợp Giao Diện**:
  - Thêm nút bấm **"📄 Xuất Báo Cáo QA"** (`GenerateReportCommand`) trên thanh điều khiển chính của `EtabsConverterWindow.xaml`.
  - Tự động kích hoạt mở báo cáo HTML trên trình duyệt mặc định ngay sau khi hoàn tất.
- **Tối Ưu Kiến Trúc Decoupled**:
  - Chuyển `ConversionOptions` và `ConversionReportResult` sang `Core/Models/ConversionResult.cs` giúp tầng Core POCO hoàn toàn độc lập với Revit API runtime.
- **Kiểm Thử & Đóng Gói**:
  - Bổ sung Unit Tests kiểm thử `ValidationSummary` và sinh báo cáo `ReportGenerator` (HTML + UTF-8 BOM CSV): Đạt **10/10 tests PASS 100%**.
  - Biên dịch thành công cả 4 phiên bản: Revit 2024, 2025, 2026, 2027 với **0 lỗi**.
  - Cập nhật checklist kiểm thử bước 8 trong `docs/test-checklist-etabs-converter.md`.

### Added - JNN ETABS Converter (Phase 2: Sàn, Vách, Trình đọc IFC Mode B & Property Viewer)
- **Tấm Sàn & Lỗ Mở (`FloorBuilder.cs`)**:
  - Dựng native Revit `Floor` (`Floor.Create`) từ `ETABSSlab`.
  - Tự động nhận diện `CurveLoop` đường bao ngoài và lồng các đường bao lỗ mở (`Openings`).
- **Tường Vách Kết Cấu (`WallBuilder.cs`)**:
  - Dựng native Revit `Wall` (`Wall.Create`) từ `ETABSWall`.
  - Tự động nhận diện đường chân vách, gán Base Level, chiều cao $H$, bề dày $b$ và tham số JNN.
- **Trình Đọc Tệp Tin IFC Mode B (`Tools/IFC Etabs/IFC/`)**:
  - `IfcReader.cs`: Trình phân tích cú pháp STEP/SPF định dạng `.ifc` (IFC2x3 & IFC4) độc lập không phụ thuộc thư viện ngoài.
  - Bóc tách đầy đủ các thực thể: `IfcBuildingStorey`, `IfcBeam`, `IfcColumn`, `IfcWall`, `IfcSlab`, `IfcMaterial`.
  - Bảo toàn 100% các bộ thuộc tính `Pset_*` (`Pset_BeamCommon`, `Pset_WallCommon`...) vào từ điển `IFCProperties`.
- **Bảng Tra Cứu Nguồn Gốc Cấu Kiện (Property Viewer)**:
  - `PropertyViewerCommand.cs` & `PropertyViewerWindow.xaml`: Tra cứu nhanh cấu kiện đang chọn trên Revit hoặc chọn cấu kiện mới trực tiếp trên view.
  - Hiển thị nguồn (`ETABS` / `IFC`), ID gốc, GUID, Section, Story, Material, Hash và danh sách toàn bộ tham số chi tiết.
- **Tích Hợp Giao Diện & Ribbon**:
  - Mở khóa nút chọn tệp IFC trên `EtabsConverterWindow.xaml`.
  - Thêm nút bấm **Property Viewer** trên Ribbon tab `JNNTool` (Panel `CSI x Revit`).
- **Kiểm Thử**:
  - Bổ sung Unit Tests cho `IfcReader` và hình học Sàn/Vách trong `EtabsConverterTests`: Đạt **8/8 tests PASS 100%**.
  - Biên dịch thành công cả 4 phiên bản: Revit 2024, 2025, 2026, 2027 với **0 lỗi**.

### Added - JNN ETABS Converter (Phase 1: Foundation & First MVP)
- **Module Chuyển Đổi Trực Tiếp ETABS → Revit Native Model (`Tools/IFC Etabs/`)**:
  - **Core POCO Models**: `Point3D`, `Vector3D`, `LineSegment3D`, `CoordinateTransform`, `UnitConverter`, `StructuralModel`, `ETABSLevel`, `ETABSGrid`, `ETABSBeam`, `ETABSColumn`, `ETABSWall`, `ETABSSlab`, `ETABSSection`, `ETABSMaterial`. Hoàn toàn độc lập với Revit API, đơn vị mm.
  - **Mã Băm Nhận Diện Biến Động (Hash Engine)**: `ElementHashService` sinh mã hash SHA256 từ thuộc tính hình học và kỹ thuật để nhận diện trạng thái `New`, `Updated`, `Unchanged`, `Deleted`.
  - **ETABS API Adapter (Mode A)**: `EtabsApiService` giao tiếp trực tiếp với tiến trình ETABS đang mở thông qua COM Late-binding (`CSI.ETABS.API.ETABSObject`), hoạt động mượt mà trên mọi phiên bản ETABS (v18–v22+) mà không phụ thuộc DLL tĩnh.
  - **Revit Native Builders**:
    - `LevelBuilder`: Tìm hoặc tạo `Level` tự động theo cao độ mm với dung sai tùy chỉnh.
    - `GridBuilder`: Tạo và tránh trùng lặp lưới trục kết cấu `Grid`.
    - `TypeAutoCreator`: Tự động tìm kiếm hoặc nhân bản (Duplicate) `FamilySymbol` dầm/cột và gán kích thước $b \times h$ theo tiết diện ETABS.
    - `ColumnBuilder`: Tạo `FamilyInstance` Structural Column native, gán Base/Top Level, Offsets, góc xoay và tham số JNN.
    - `BeamBuilder`: Tạo `FamilyInstance` Structural Framing native giữa 2 điểm, Z-offset và tham số JNN.
  - **Hệ Thống Tham Số Chia Sẻ JNN Traceability**:
    - Tự động nạp và gán: `JNN_Source`, `JNN_ETABS_ID`, `JNN_IFC_GUID`, `JNN_ETABS_Story`, `JNN_ETABS_Section`, `JNN_ETABS_Material`, `JNN_Source_Hash`, `JNN_Conversion_Status`, `JNN_Conversion_Date`.
  - **Tracking & Update Engine**:
    - `ElementTracker` & `UpdateEngine` quét đối tượng Revit đã convert, đối soát phiên bản và cập nhật thay vì tạo trùng lặp.
  - **Giao Diện Điều Khiển (WPF MVVM)**:
    - `EtabsConverterWindow` & `EtabsConverterViewModel` dạng Modeless window, kết nối qua `ActionEventHandler`.
    - Đăng ký nút bấm **ETABS Converter** trên Ribbon tab `JNNTool` (Panel `CSI x Revit`).
  - **Kiểm Thử & Đóng Gói**:
    - Unit Tests `EtabsConverterTests`: 6/6 tests PASS.
    - Build thành công toàn bộ 4 cấu hình: `Release.R24`, `Release.R25`, `Release.R26`, `Release.R27` với **0 lỗi**.
    - Tài liệu checklist kiểm thử: `docs/test-checklist-etabs-converter.md`.

### Added - Nâng cấp .NET 10 & Hỗ trợ Đa Phiên Bản Toàn Diện (Revit 2022 – 2027)
- **Hỗ trợ .NET 10 SDK & Revit 2027 API**:
  - Cài đặt Microsoft .NET SDK 10.0 (`10.0.401`).
  - Cập nhật đa target framework trong `JNNTool.csproj`: `net48` (Revit 2022–2024), `net8.0-windows` (Revit 2025–2026), và `net10.0-windows` (Revit 2027).
  - Tích hợp lớp tương thích `RebarCompat`: Tự động nhận diện cấu trúc `BarTerminationsData` mới trên Revit 2027 và tương thích ngược với `RebarHookOrientation` trên Revit 2022–2026.
  - Tích hợp lớp tương thích `CurveCompat`: Đóng gói hàm giao điểm `Intersect` giữa `CurveIntersectResult` (Revit 2027+) và `IntersectionResultArray` (Revit 2022–2026).
  - Cập nhật tự động phân phối bundle và installer nhận diện cả Revit 2027.
- **Tổng Unit Tests**: **47/47 tests PASS 100%**.
- **Trạng thái Build**: Build đồng thời cả 3 target framework thành công với **0 lỗi**.

### Added - Giai đoạn 6: Tiện Ích Rebar (Utilities Suite)
- **Menu Rebar Utils trên Ribbon**:
  - `RebarVolumeCommand` & `RebarVolumeCalculator`: Thuật toán thuần C# mm thống kê tổng khối lượng, tổng chiều dài và số lượng thanh theo từng loại đường kính (D6–D32), tính tổng trọng lượng Tấn và xuất báo cáo CSV chi tiết ra Desktop.
  - `RebarNumberCommand` & `RebarNumberingEngine`: Thuật toán thuần C# gom nhóm các thanh có cùng đường kính, chiều dài và hình dạng, tự động gán tham số `Mark` tăng dần từ 1, 2, 3...
  - `RebarUnobscuredCommand`: Bật chế độ hiển thị nhìn xuyên bê tông (`SetUnobscuredInView`) hàng loạt cho toàn bộ cốt thép trong view.
  - `IsolateRebarCommand`: Bật / tắt cô lập tạm thời (`IsolateElementsTemporary`) toàn bộ cốt thép trong view làm việc.
  - `RebarTypeManagerCommand`: Tự động kiểm tra và khởi tạo chuẩn hóa hệ thống `RebarBarType` từ D6 đến D32 theo tiêu chuẩn TCVN 5574:2018.
- **Tổng Unit Tests**: Đạt **47/47 tests PASS 100%**.
- **Tài liệu**: `docs/test-checklist-utilities.md`.


### Added - Giai đoạn 5: Thép Thang (Stair Rebar)
- **Module Cầu Thang (Stair)**:
  - `StairModel`, `StairFlightModel`: Biểu diễn bản thang, chiều rộng B, chiều dày bản $h_s$, số bậc, chiều cao và bề rộng mặt bậc, điểm chân dốc và đỉnh dốc.
  - `StairRebarSettings`: Cấu hình thép lớp dưới chịu nhịp, thép mũ gối trên neo dầm chiếu nghỉ (L/4), thép phân bố ngang vuông góc.
  - `StairLayoutEngine`: Thuật toán thuần C# mm tính toán đường cong cốt thép theo độ dốc, bẻ góc neo vào dầm chiếu nghỉ và dầm chân thang.
  - `StairRebarBuilder`: Trích xuất dữ liệu từ `OST_Stairs` / `StairsRun` và sinh cốt thép trong Revit Document.
  - `StairRebarViewModel`, `StairRebarWindow`: Modeless WPF UI, Ribbon button `Rebar Stair` trong panel `JNN Rebar`.
  - `StairLayoutEngineTests`: Kiểm thử tính toán vế thang tiêu chuẩn và trường hợp tắt thép mũ gối.
- **Tổng Unit Tests**: Đạt **45/45 tests PASS 100%**.
- **Tài liệu**: `docs/test-checklist-stair.md`.


### Added - Giai đoạn 4: Thép Móng & Cọc (Foundation & Pile Rebar)
- **Module Móng & Đài Cọc (Foundation)**:
  - `FoundationModel`: Biểu diễn móng đơn, móng băng, đài cọc (kích thước L×W×H, cao độ đáy/đỉnh, vị trí và kích thước cổ cột).
  - `FoundationRebarSettings`: Cấu hình lưới thép đáy X/Y uốn móc đứng 90°, lưới thép trên cấu tạo đài cọc, thép chờ cổ cột uốn chân vịt 90° và đai định vị.
  - `FoundationLayoutEngine`: Thuật toán thuần C# mm tính toán phân bổ lưới thép đáy, lưới thép trên và thép chờ chân vịt.
  - `FoundationRebarBuilder`: Trích xuất `OST_StructuralFoundation` và sinh cốt thép trong Revit Document.
  - `FoundationRebarViewModel`, `FoundationRebarWindow`: Modeless WPF UI, Ribbon button `Rebar Footing`.
  - `FoundationLayoutEngineTests`: Kiểm thử lưới đáy, móc uốn đứng, lưới trên và thép chờ cổ móng chân vịt.
- **Module Cọc (Pile)**:
  - `PileModel`: Biểu diễn cọc khoan nhồi tròn và cọc vuông đúc sẵn.
  - `PileRebarSettings`: Số thanh dọc, đường kính, neo ngàm đài, cốt đai phân vùng dày đầu cọc/thưa thân cọc, vành đai gia cường stiffener rings chống bẹp lồng.
  - `PileLayoutEngine`: Tính toán phân bố tròn đều thanh dọc, cốt đai tròn/xoắn và vành gia cường.
  - `PileRebarBuilder`: Sinh lồng cốt thép cọc trong Revit Document.
  - `PileRebarViewModel`, `PileRebarWindow`: Modeless WPF UI, Ribbon button `Rebar Pile`, tiện ích xuất tọa độ cọc ra file CSV mở trên Excel.
  - `PileLayoutEngineTests`: Kiểm thử lồng thép cọc khoan nhồi và cọc vuông.
- **Tổng Unit Tests**: Đạt **43/43 tests PASS 100%**.
- **Tài liệu**: `docs/test-checklist-foundation-pile.md`.


### Added - Giai đoạn 3: Thép Cột & Vách (Column & Wall Rebar)
- **Module Cột (Column)**:
  - `ColumnStoryModel`, `ColumnStackModel`: Biểu diễn chuỗi cột qua các tầng, độ lệch tiết diện $\Delta B, \Delta H$, chiều sâu dầm giao tại đỉnh tầng.
  - `ColumnRebarSettings`: Bố trí số thanh cạnh B/H, đai vùng dày $\max(H_{clear}/6, B, H, 500)$, đai thưa giữa thân cột, bẻ cổ chai cranked lap 1:6 khi thu hẹp tiết diện, đai AB/CN/C-Tie.
  - `ColumnLayoutEngine`: Thuật toán thuần C# mm bố trí thép dọc và hệ cốt đai phân vùng.
  - `ColumnRebarBuilder`, `ColumnSectionBuilder`: Sinh cốt thép trong Revit Document và tạo Mặt Cắt Dọc (MCD) tự động.
  - `ColumnRebarViewModel`, `ColumnRebarWindow`: Modeless WPF UI hiện đại, Ribbon button `Rebar Column`.
  - `ColumnLayoutEngineTests`: Kiểm thử chuỗi tầng, phát hiện thu hẹp tiết diện và bẻ cổ chai.
- **Module Vách (Wall)**:
  - `WallModel`: Biểu diễn vách bê tông cốt thép (chiều dài, chiều cao, chiều dày, tọa độ đường tâm, vector hướng $V_L$, vector vuông góc $V_T$).
  - `WallRebarSettings`: Cấu hình 2 lớp thép đứng, 2 lớp thép ngang, móc bẻ neo biên 250mm, đai C ôm 2 đầu vách, tùy chọn chống thấm tăng lớp bảo vệ.
  - `WallLayoutEngine`: Tính toán bố trí 2 lớp thép đứng đối xứng, 2 lớp thép ngang móc U, và móc C ghim biên vách.
  - `WallRebarBuilder`, `WallSectionBuilder`: Sinh RebarSet và tạo Mặt Cắt Dọc vách (MCD_Wall).
  - `WallRebarViewModel`, `WallRebarWindow`: Modeless WPF UI, Ribbon button `Rebar Wall`.
  - `WallLayoutEngineTests`: 4 unit tests mới kiểm thử tầng trên cùng, lớp bảo vệ chống thấm, và đai C biên.
- **Tổng Unit Tests**: Đạt **39/39 tests PASS 100%**.
- **Tài liệu**: `docs/test-checklist-column-wall.md`.


### Added - Giai đoạn 2: Thép Dầm Liên Tục (BeamRebarCommand)
- **Models**:
  - `BeamRebarSettings`: Cấu hình toàn diện thép dầm (thép chủ trên/dưới, thép gia cường gối L/4, thép gia cường nhịp, đai dày L/4 gối và đai thưa giữa nhịp, thép giá $H \ge 600$ mm, neo 35d, tự tạo MCD/MCN).
  - `BeamSpanModel`, `ContinuousBeamModel`: Domain model chuỗi dầm liên tục (nhịp, b×h, gối đỡ trái/phải, chiều dài thông thủy).
  - `BeamLayoutResult`, `BeamRebarSetLayoutInfo`: Kết quả phân bổ cốt thép thuần C# mm.
- **Engine**:
  - `BeamLayoutEngine`: Thuật toán thuần C# phân chia vùng đai dày 2 đầu nhịp (L/4 làm tròn 50mm) và đai thưa giữa nhịp, neo cốt chủ, vươn thép gia cường gối, và bố trí thép giá.
- **Builders**:
  - `BeamRebarBuilder`: Trích xuất dầm `OST_StructuralFraming` và tạo các đối tượng Rebar chạy suốt / RebarSet vào Revit Document.
  - `BeamSectionBuilder`: Tự động tạo ViewSection Mặt Cắt Dọc (MCD) cho chuỗi dầm liên tục.
- **MVVM & UI**:
  - `BeamRebarViewModel`: Quản lý binding và điều phối TransactionGroup qua `ActionEventHandler`.
  - `BeamRebarWindow`: Giao diện WPF Modeless hiện đại, trực quan, hỗ trợ tiếng Việt.
  - `BeamRebarCommand`: Tích hợp Ribbon button "Rebar Beam" trong panel "JNN Rebar".
- **Tests & Docs**:
  - `BeamLayoutEngineTests`: Kiểm thử đai L/4, thép gia cường gối giữa, và thép giá thành dầm, đạt 29/29 tests PASS.
  - `docs/test-checklist-beam.md`: Checklist hướng dẫn kiểm thử thủ công dầm trên Revit.

### Added - Giai đoạn 1: Thép Sàn (SlabRebarCommand)
- **Models**:
  - `SlabRebarSettings`: Cấu hình toàn diện thép sàn (lớp dưới X/Y, lớp trên X/Y, thép mũ gối L/4, thép phân bố mũ D6 a300, con kê D10, lớp bảo vệ).
  - `SlabModel`, `Point2D`, `Point3D`, `BeamSupportInfo`: Domain model thuần POCO (đơn vị mm).
  - `SlabLayoutResult`, `RebarSetLayoutInfo`: Lưu trữ kết quả rải thép độc lập Revit API.
- **Engine**:
  - `SlabLayoutEngine`: Thuật toán thuần C# rải thép lớp dưới, lớp trên và thép mũ L/4 làm tròn 50mm, tự động bỏ qua nhịp < 800mm.
- **Builders**:
  - `SlabRebarBuilder`: Trích xuất dữ liệu hình học Floor và tạo các đối tượng `RebarSet` trong Transaction Revit.
- **MVVM & UI**:
  - `SlabRebarViewModel`: Quản lý binding dữ liệu và điều phối lệnh tạo thép qua `ActionEventHandler`.
  - `SlabRebarWindow`: Giao diện WPF Modeless hiện đại, trực quan, hỗ trợ tiếng Việt.
  - `SlabRebarCommand`: Đăng ký lệnh mở Modeless UI vào Revit Ribbon panel "JNN Rebar".
- **Tests & Docs**:
  - `SlabLayoutEngineTests`: Kiểm thử tính toán rải thép sàn và thép mũ, đạt 26/26 tests PASS.
  - `docs/test-checklist-slab.md`: Checklist hướng dẫn kiểm thử tay trên Revit.

### Added - Giai đoạn 0: Core Foundation
- **ExternalEvents**: Triển khai `ActionEventHandler` hỗ trợ hàng đợi `Action<UIApplication>` an toàn giữa Modeless UI và Revit Thread.
- **Logging**: Triển khai `Logger` ghi log hệ thống tại `%AppData%\JNNTool\Logs`.
- **Settings**: Triển khai `SettingsService<T>` hỗ trợ serialization/deserialization JSON cho cấu hình POCO tại `%AppData%\JNNTool\Settings`.
- **Rebar Common**: Bộ công cụ tính toán thuần C# theo tiêu chuẩn TCVN 5574:2018:
  - `BarCatalog`: Quản lý danh mục thép D6–D32, khối lượng riêng, diện tích, phân tích chuỗi ký hiệu.
  - `AnchorCalculator`: Tính chiều dài neo cơ sở và neo tính toán theo cấp độ bền bê tông (B15–B40), nhóm thép (CB240-T, CB300-V, CB400-V, CB500-V) hoặc hệ số kinh nghiệm.
  - `LapCalculator`: Tính chiều dài nối chồng theo tỷ lệ diện tích nối tại mặt cắt (25%, 50%, 100%) hoặc hệ số kinh nghiệm.
  - `HookHelper`: Chuẩn uốn móc 90°, 135°, 180°, đường kính uốn tối thiểu và đoạn kéo thẳng sau móc.
  - `CoverHelper`: Tra cứu lớp bảo vệ theo cấu kiện (Sàn, Dầm, Cột, Móng) và môi trường.
  - `GeometryHelper`: Làm tròn bội số (50mm), thuật toán tính số thanh rải và bước rải thực tế, chuyển đổi đơn vị mm <-> feet.
  - `RebarTypeResolver`: Khớp nối Revit Element types (`RebarBarType`, `RebarHookType`, `RebarCoverType`).
- **Unit Tests**: Thiết lập project xUnit `Tests/JNNTool.Tests` với 23 test cases kiểm thử tự động, pass 100%.
- **Test Checklist**: Tạo tài liệu hướng dẫn kiểm thử thủ công tại `docs/test-checklist-core.md`.
