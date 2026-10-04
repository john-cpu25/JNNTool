# Changelog — JNNTool

All notable changes to this project will be documented in this file.

## [Unreleased]

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
