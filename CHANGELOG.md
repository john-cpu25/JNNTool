# Changelog — JNNTool

All notable changes to this project will be documented in this file.

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
