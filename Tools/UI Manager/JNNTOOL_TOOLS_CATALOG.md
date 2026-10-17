# JNNTool — Danh Mục Tổng Hợp Công Cụ (HIỆN & ẨN)

> **Cập nhật ngày:** 2026-10-17  
> **Vị trí file:** `c:\Users\nhann\OneDrive\AI\JNNTool\Tools\UI Manager\JNNTOOL_TOOLS_CATALOG.md`  
> **Nguyên tắc cốt lõi:** Đồng bộ 1:1 giữa **Ribbon JNNTool** và **Command Palette (Dockable Pane)**.

---

## 📊 Bảng Thống Kê Tổng Quan

| Trạng thái | Số lượng | Vị trí kiểm soát mã nguồn | Ghi chú |
|---|:---:|---|---|
| 🟢 **ĐANG HIỆN** | **21** | [`Core/App.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/App.cs) & [`Core/Palette/CommandRegistry.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/Palette/CommandRegistry.cs) | Đang hiển thị trực tiếp trên thanh Ribbon và Command Palette |
| 🔴 **ĐANG ẨN** | **22** | Được gom gọn an toàn trong khối `#if false` | Đã code sẵn, sẵn sàng mở lại bất cứ lúc nào |
| ⚙️ **ADMIN CLI/WEB**| **2** | `Tools/Admin/` & `scripts/` | Cổng web offline và script PowerShell cấp key độc lập |
| 🚀 **TỔNG CỘNG** | **45** | Toàn bộ dự án JNNTool | |

---

## 🟢 PHẦN 1: DANH SÁCH CÔNG CỤ ĐANG HIỆN (21 TOOLS)

Toàn bộ 21 công cụ dưới đây hiển thị trực tiếp trên thanh Ribbon của Revit và đồng bộ xuất hiện trong Command Palette:

### 1. Panel `General` (3 công cụ)
| STT | Tên Ribbon / Palette | Command ID | Icon Enum | Class thực thi | Chức năng chính |
|:---:|---|---|:---:|---|---|
| 1 | **Command Palette** | `cmdPalette` | `CommandPalette` | `JNNTool.Core.Palette.TogglePaletteCmd` | Bảng điều khiển tra cứu (Ctrl+F) tìm kiếm nhanh toàn bộ công cụ JNNTool, hỗ trợ gõ tiếng Việt không dấu, ghim lệnh yêu thích ★. |
| 2 | **Account & License** | `cmdLicense` | `Key` | `JNNTool.Tools.Login.Commands.LoginCmd` | Quản lý bản quyền, đăng nhập tài khoản, nhập mã key kích hoạt máy tính qua đám mây Supabase. |
| 3 | **Ribbon Theme** | `cmdUIManager` | `Palette` | `JNNTool.Tools.UIManager.Commands.UIManagerCmd` | Quản lý giao diện, đổi 12 bảng màu theme và chuyển đổi 4 phong cách biểu tượng (Badge, Glyph, Circle, Outline). |

### 2. Panel `Modeling` (6 công cụ - Bố cục 2 Cột x 3 Nút nhỏ Stacked)
> **Bố cục Ribbon:** Gom gọn thành **2 cột nút nhỏ xếp tầng** (Stacked Items 16×16 px), tiết kiệm 50% diện tích chiều ngang thanh Ribbon:
> - **Cột 1:** `Split Beam`, `Create Section`, `Disallow Join Beam`
> - **Cột 2:** `Create Filter`, `Speed Overrider`, `Pile Location`

| STT | Tên Ribbon / Palette | Command ID | Icon Enum | Class thực thi | Chức năng chính |
|:---:|---|---|:---:|---|---|
| 4 | **Split Beam** | `cmdSplitBeamByColumn` | `Split` | `JNNTool.SplitBeamByColumnCmd` | Tự động ngắt dầm thành từng đoạn ngắn tại các vị trí giao nhau với cột hoặc dầm ngang. |
| 5 | **Create Section** | `cmdCreateSection` | `Section` | `JNNTool.CreateSectionCmd` | Duplicate hàng loạt Type dầm/cột với kích thước tiết diện $b \times h$ mới siêu tốc. |
| 6 | **Disallow Join Beam** | `cmdDisallowJoinBeam` | `DisallowJoin` | `JNNTool.Tools.DisallowJoinBeam.DisallowJoinBeamCmd` | Quét chọn nhiều dầm và thực hiện Disallow Join ở cả 2 đầu dầm chống lỗi tự động nhảy liên kết. |
| 7 | **Create Filter** | `cmdCreateFilter` | `Filter` | `JNNTool.Tools.CreateFilter.Command` | Tạo bộ lọc View Filter tự động từ danh sách phần tử và gán màu sắc trực quan. |
| 8 | **Speed Overrider** | `cmdSpeedOverriderElement` | `Flash` | `JNNTool.Tools.SpeedOverriderElement.Command` | Ghi đè màu sắc đồ họa và độ trong suốt của phần tử nhanh trong view hiện hành. |
| 9 | **Pile Location** | `cmdPileLocation` | `Target` | `JNNTool.Tools.PileLocation.Command` | Chọn 1 cọc gốc $\rightarrow$ tool tự tìm tất cả cọc trong view và ghi tọa độ thực tế ($X, Y$) mm vào tham số. |

### 3. Panel `CSI x Revit` (5 công cụ - 1 Nút lớn + 1 Cột 3 Nút nhỏ Stacked)
> **Bố cục Ribbon:**
> - **Nút lớn độc lập (32×32 px):** `Import E2K` (lệnh trung tâm nổi bật)
> - **1 Cột 3 nút nhỏ xếp tầng (16×16 px):** `Fit Scope Box`, `Boundary Align`, và `Auto Join` (SplitButton: Active View / Entire Project)

| STT | Tên Ribbon / Palette | Command ID | Icon Enum | Class thực thi | Chức năng chính |
|:---:|---|---|:---:|---|---|
| 10 | **Import E2K** | `cmdImportE2K` | `Cad` | `JNNTool.Tools.CSIxRevit.Commands.ImportE2KCommand` | Chuyển đổi mô hình ETABS/SAP2000 (file `.e2k`, `.$et`) sang cấu kiện native Revit chuẩn xác. |
| 11 | **Fit Scope Box** | `cmdFitScopeBox` | `ScopeBox` | `JNNTool.Tools.CSIxRevit.Commands.FitScopeBoxCommand` | Cắt khớp và khóa toàn bộ đầu Lưới trục (Grid) và Cao độ (Level) theo Scope Box `JNN_SB`. |
| 12 | **Boundary Align** | `cmdBoundaryAlign` | `Align` | `JNNTool.Tools.CSIxRevit.Commands.BoundaryAlignCommand` | Căn dầm/cột biên vào trong ranh đất / lưới trục ($B/2$), tự động co/kéo các dầm ngang kết nối vào. |
| 13 | **Auto Join (View)** | `cmdAutoJoinActiveView` | `Join` | `JNNTool.Tools.AutoJoin.AutoJoinActiveViewCommand` | Tự động Join và đảo thứ tự ưu tiên ($Cột > Dầm > Sàn$) trong View hiện hành *(SplitButton)*. |
| 14 | **Auto Join (Project)** | `cmdAutoJoinProject` | `Join` | `JNNTool.Tools.AutoJoin.AutoJoinProjectCommand` | Tự động Join và đảo thứ tự ưu tiên ($Cột > Dầm > Sàn$) cho toàn bộ mô hình dự án *(SplitButton)*. |

### 4. Panel `Layout` (4 công cụ)
| STT | Tên Ribbon / Palette | Command ID | Icon Enum | Class thực thi | Chức năng chính |
|:---:|---|---|:---:|---|---|
| 15 | **Auto View & Sheet** | `cmdAutoViewSheet` | `ViewSheet` | `RincoNhan.Tools.AutoViewSheet.AutoViewSheetCommand` | Tự động sinh View mặt bằng kết cấu / Loading Plan và tự dàn vào Sheet theo tầng và Zone. |
| 16 | **Dim Grid** | `cmdAutoDimGrid` | `Dim` | `JNNTool.Tools.AutoDimGrid.AutoDimGridCommand` | Tự động Dim lưới trục 4 cạnh (2 hoặc 3 lớp: Tường / Trục / Tổng), hoặc Dim thủ công theo điểm chọn. |
| 17 | **Dim Col/Wall** | `cmdAutoDimColumn` | `DimColumn` | `JNNTool.Tools.AutoDimColumn.Commands.AutoDimColumnCommand` | Tự động ghi kích thước tiết diện Cột, Vách ($b \times h / L$) và khoảng cách định vị từ Lưới trục. |
| 18 | **Element Mark** | `cmdElementMark` | `Mark` | `JNNTool.Tools.ElementMark.ElementMarkCommand` | Đánh số hiệu Mark tự động cho Dầm, Cột, Sàn theo quy tắc dự án (Trái $\rightarrow$ Phải, Trên $\rightarrow$ Dưới). |

### 5. Panel `Stair` (2 công cụ)
| STT | Tên Ribbon / Palette | Command ID | Icon Enum | Class thực thi | Chức năng chính |
|:---:|---|---|:---:|---|---|
| 19 | **Stair Model** | `cmdStairModel` | `Stair` | `JNNTool.Tools.StairModel.Commands.StairModelCommand` | Tự động dựng cầu thang BTCT (Thẳng, Chữ U, Chữ L) gồm bản thang nghiêng, chiếu nghỉ và dầm đỡ. |
| 20 | **Stair Designer** | `cmdStairDesigner` | `StairDesigner` | `JNN.Stair.Commands.StairDesignerCommand` | Thiết kế tham số cầu thang 3D Solid (kèm bậc rẽ quạt Winder) và tự động xuất nạp Family (`.rfa`). |

### 6. Panel `Quantity` (1 công cụ)
| STT | Tên Ribbon / Palette | Command ID | Icon Enum | Class thực thi | Chức năng chính |
|:---:|---|---|:---:|---|---|
| 21 | **Quantity Takeoff** | `cmdQuantityTakeoff` | `Quantity` | `JNNTool.Tools.QuantityTakeoff.Commands.QuantityTakeoffCommand` | Bóc tách khối lượng toàn diện (Bê tông, Ván khuôn, Thép), tạo lập tức 3D Section Box và xuất file Excel. |

---

## 🔴 PHẦN 2: DANH SÁCH CÔNG CỤ ĐANG ẨN (22 TOOLS)

Tất cả công cụ này được giữ nguyên trong source code (bọc trong khối `#if false`) để phục vụ mở lại khi cần hoặc khi hoàn thiện tính năng:

### 1. Nhóm Modeling & Dầm tạm ẩn (7 công cụ)
| STT | Tên công cụ | Command ID | Class thực thi | Lý do & Hướng mở lại |
|:---:|---|---|---|---|
| 1 | **Floor By Room** | `cmdFloorByRoom` | `JNNTool.FloorByRoomCmd` | Tự vẽ sàn theo Boundary của Room. Tạm ẩn để tinh gọn panel Modeling. |
| 2 | **Rebar Beam (Quick)** | `cmdRebarBeam` | `JNNTool.RebarBeamCmd` | Bố trí thép dầm bản cũ. Đã có phân hệ Rebar Beam Suite chuyên sâu thay thế. |
| 3 | **Steel Connection** | `cmdConnection` | `JNNTool.Tools.Connection.Command` | Tạo liên kết thép cho dầm cột. Tạm ẩn do ít dự án kết cấu thép sử dụng. |
| 4 | **Clash Detection** | `cmdClashDetection` | `JNNTool.Tools.ClashDetection.Command` | Kiểm tra va chạm mô hình với Revit Link. |
| 5 | **Dim Beam** | `cmdCreateDimBeam` | `JNNTool.Tools.CreateDim.CreateDimBeamCmd` | Tự tạo Dim chiều dài tổng 2 đầu dầm trên mặt bằng. |
| 6 | **Extend Beam** | `cmdExtendBeam` | `JNNTool.Tools.ExtendBeam.ExtendBeamCmd` | Tự nhận diện mặt gối ở 2 đầu dầm và kéo tim dầm tới mặt gối chính xác. |
| 7 | **Rebar Column (Quick)** | `cmdRebarColumn` | `JNNTool.Tools.RebarColumn.Command` | Bố trí thép cột nhanh. Đã chuyển ngầm gom vào phân hệ Rebar Suite. |

### 2. Nhóm CSI x Revit / IFC tạm ẩn (2 công cụ)
| STT | Tên công cụ | Command ID | Class thực thi | Lý do & Hướng mở lại |
|:---:|---|---|---|---|
| 8 | **ETABS Converter** | `cmdEtabsConverter` | `JNNTool.Tools.IFCEtabs.Commands.EtabsConverterCommand` | Chuyển đổi trực tiếp mô hình ETABS sang cấu kiện native kèm tham số IFC (Đang hoàn thiện). |
| 9 | **Property Viewer** | `cmdPropertyViewer` | `JNNTool.Tools.IFCEtabs.Commands.PropertyViewerCommand` | Tra cứu thuộc tính nguồn gốc IFC/ETABS của cấu kiện. |

### 3. Nhóm Stair tạm ẩn (1 công cụ)
| STT | Tên công cụ | Command ID | Class thực thi | Lý do & Hướng mở lại |
|:---:|---|---|---|---|
| 10 | **Stair Rebar** | `cmdStairRebarSuiteNew` | `JNN.Stair.Commands.StairDetailingCommand` | Bố trí thép thang đan chéo chịu kéo góc lõm chống bung nổ bê tông theo TCVN 5574:2018. |

### 4. Phân hệ `JNN Rebar Suite` (12 công cụ - Ẩn nguyên Panel)
| STT | Tên công cụ | Command ID | Class thực thi | Nội dung tính năng |
|:---:|---|---|---|---|
| 11 | **Rebar Slab** | `cmdSlabRebar` | `JNNTool.RebarSuite.Slab.SlabRebarCommand` | Bố trí thép sàn tự động: Lớp dưới, lớp trên và thép mũ gối $L/4$ theo TCVN 5574:2018. |
| 12 | **Rebar Beam Suite** | `cmdBeamRebarSuite` | `JNNTool.RebarSuite.Beam.BeamRebarCommand` | Bố trí thép dầm liên tục nhiều nhịp: Thép chủ, gia cường gối, đai dày/thưa, tự tạo MCD/MCN. |
| 13 | **Rebar Column Suite**| `cmdColumnRebarSuite` | `JNNTool.RebarSuite.Column.ColumnRebarCommand` | Bố trí thép chuỗi cột theo tầng: Bẻ cổ chai 1:6 khi đổi tiết diện, đai AB/CN, tự tạo MCD. |
| 14 | **Rebar Wall** | `cmdWallRebarSuite` | `JNNTool.RebarSuite.Wall.WallRebarCommand` | Bố trí thép vách 2 lớp: Thép đứng, ngang, đai C ghim biên, chống thấm bể nước. |
| 15 | **Rebar Footing** | `cmdFoundationRebarSuite` | `JNNTool.RebarSuite.Foundation.FoundationRebarCommand` | Bố trí thép móng đơn, móng băng, đài cọc: Lưới đáy móc uốn 90°, thép chờ cột chân vịt. |
| 16 | **Rebar Pile** | `cmdPileRebarSuite` | `JNNTool.RebarSuite.Pile.PileRebarCommand` | Bố trí thép cọc khoan nhồi / cọc vuông: Lồng thép, đai phân vùng và xuất tọa độ CSV. |
| 17 | **Rebar Stair Suite** | `cmdStairRebarSuite` | `JNNTool.RebarSuite.Stair.StairRebarCommand` | Bố trí thép bản thang chịu nhịp và thép neo dầm chiếu nghỉ. |
| 18 | **Rebar Volume** | `cmdRebarVolume` | `JNNTool.RebarSuite.Utilities.Commands.RebarVolumeCommand` | Tiện ích: Thống kê khối lượng thép (CSV) theo đường kính, cấu kiện và tầng. |
| 19 | **Rebar Number** | `cmdRebarNumber` | `JNNTool.RebarSuite.Utilities.Commands.RebarNumberCommand` | Tiện ích: Tự động đánh số hiệu thép (Auto Mark). |
| 20 | **View Unobscured** | `cmdRebarUnobscured` | `JNNTool.RebarSuite.Utilities.Commands.RebarUnobscuredCommand` | Tiện ích: Bật/tắt chế độ nhìn xuyên cốt thép trong 3D View (Unobscured). |
| 21 | **Isolate Rebar** | `cmdRebarIsolate` | `JNNTool.RebarSuite.Utilities.Commands.IsolateRebarCommand` | Tiện ích: Cô lập cốt thép theo cấu kiện trong view đang mở. |
| 22 | **Rebar Type Manager**| `cmdRebarTypeManager`| `JNNTool.RebarSuite.Utilities.Commands.RebarTypeManagerCommand` | Tiện ích: Tạo và chuẩn hóa RebarBarType D6–D32, HookType, CoverType theo TCVN. |

---

## ⚙️ PHẦN 3: CÔNG CỤ QUẢN TRỊ ĐỘC LẬP (NGOÀI RIBBON)

| STT | Công cụ | Đường dẫn file | Mục đích sử dụng |
|:---:|---|---|---|
| 1 | **Admin License Portal** | [`Tools/Admin/admin-license-portal.html`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/Admin/admin-license-portal.html) | Trang web HTML tĩnh chạy offline local bằng Chrome/Edge: cấp Key mới cho khách hàng, xem danh sách user, reset khóa máy khi khách đổi thiết bị, gia hạn key. |
| 2 | **Create Key CLI Script** | [`scripts/create-key.ps1`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/scripts/create-key.ps1) | Script PowerShell dòng lệnh: Tạo nhanh mã key bản quyền `JNN-XXXX-XXXX-XXXX` tự động copy vào Clipboard chỉ với 1 dòng lệnh. |

---

## 🛠️ Hướng Dẫn Kích Hoạt / Ẩn Một Công Cụ

Để bật hoặc tắt bất kỳ công cụ nào giữa trạng thái **HIỆN** và **ẨN**, bạn chỉ cần thao tác tại 2 file duy nhất:

1. **File Ribbon ([`Core/App.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/App.cs)):**
   - Đưa đoạn tạo `PushButtonData` ra ngoài hoặc vào trong khối `#if false ... #endif`.
2. **File Palette ([`Core/Palette/CommandRegistry.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/Palette/CommandRegistry.cs)):**
   - Di chuyển khối `new ToolCommandInfo(...)` tương ứng ra ngoài hoặc vào trong `#if false ... #endif`.
3. **Build lại dự án:**
   ```powershell
   dotnet build -c Release.R26
   ```
   Command Palette và Ribbon sẽ tự động cập nhật số lượng khớp nhau ngay lập tức!
