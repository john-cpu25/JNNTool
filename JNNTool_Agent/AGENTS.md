# AGENTS.md — JNNTool (Revit Add-in, Rebar Suite)

> File hướng dẫn cho AI coding agent (Antigravity, Cursor, Claude Code, Copilot...).
> Đặt file này ở **thư mục gốc của JNNTool**. Agent phải đọc toàn bộ file trước khi sửa code.

---

## 0. Việc đầu tiên agent phải làm

1. **Đọc cấu trúc JNNTool hiện có**: `.csproj`, lớp `IExternalApplication` (Ribbon), namespace, các tool đang có.
2. **Giữ nguyên quy ước đang có** của JNNTool (namespace, cách đặt tên, cách tạo Ribbon). Chỉ áp dụng phần kiến trúc ở mục 3 cho code **mới**. Không refactor code cũ nếu chưa được yêu cầu.
3. Kiểm tra cột "Trạng thái" trong mục 6 (Roadmap) để biết đang ở giai đoạn nào.
4. Nếu có điểm mơ hồ về nghiệp vụ kết cấu, **hỏi người dùng**, không tự đoán.

---

## 1. Tổng quan dự án

| Mục | Giá trị |
|---|---|
| Tên | **JNNTool**, add-in Revit cá nhân của chủ dự án |
| Mục tiêu | Bộ tool **Rebar đầy đủ**: Sàn, Dầm, Cột/Vách, Móng/Cọc, Thang, Tiện ích |
| Revit | **2024, 2025, 2026, 2027** |
| Ngôn ngữ | C# (LangVersion latest), WPF |
| Tiêu chuẩn | **TCVN 5574:2018** (mặc định, cho phép người dùng chỉnh) |
| Ngôn ngữ UI | Tiếng Việt (có thể bổ sung English sau) |

---

## 2. ⚠️ Quy tắc Clean-Room (BẮT BUỘC)

JNNTool lấy **ý tưởng tính năng** từ add-in thương mại *HT-Structural*. Để code hoàn toàn thuộc về chủ dự án:

- ✅ Được: dùng **danh sách tính năng, thông số cài đặt, luồng thao tác** trong mục 7 làm đặc tả.
- ❌ Cấm: dịch ngược (ILSpy, dnSpy...) hoặc đọc mã IL của `HtStructural.*.dll`.
- ❌ Cấm: copy family `.rfa`, icon, template `.rte`, file DB, file JSON từ HT-Structural vào JNNTool.
- ❌ Cấm: đặt tên class, family hay type trùng hệ đặt tên của HT (`HT_*`, `BS_*`, `@HT-*`). Dùng tiền tố **`JNN_`**.
- ❌ Cấm: viết bất kỳ code nào liên quan đến license, kích hoạt hay bỏ qua bản quyền của phần mềm khác.

---

## 3. Kiến trúc

### 3.1 Luồng xử lý chuẩn cho mọi tool rebar

```
Ribbon Button
  → IExternalCommand          (mỏng: chỉ lấy UIDocument, mở Window)
  → Selection / Collector     (chọn hoặc lọc cấu kiện)
  → Domain Model              (POCO: SlabModel, BeamSpanModel, ColumnStackModel...)
  → Layout Engine             (THUẦN C#, KHÔNG gọi Revit API → unit test được)
  → ViewModel + WPF Window    (modeless, có preview)
  → ExternalEvent             (đẩy action về Revit thread)
  → Rebar Builder             (tạo Rebar / RebarSet trong Transaction)
  → Annotation Builder        (Tag, Dimension, Detail 2D)
```

**Nguyên tắc:**
- **Layout Engine** (tính số thanh, chiều dài, vùng đai, đoạn nối...) **không được tham chiếu RevitAPI**. Đầu vào và đầu ra là POCO, đơn vị **mm**.
- Chỉ **Rebar Builder** mới đổi mm sang feet (`UnitUtils.ConvertToInternalUnits(v, UnitTypeId.Millimeters)`).
- Cửa sổ **modeless**: mọi thao tác ghi vào model phải đi qua `ExternalEvent`, **không** mở Transaction trực tiếp từ UI thread.
- Một `TransactionGroup` cho mỗi lần "Tạo thép" để người dùng Undo một lần.

### 3.2 Cấu trúc thư mục (cho code mới)

```
JNNTool/
├─ Core/
│  ├─ App.cs                    ← IExternalApplication, tạo Ribbon tab "JNN Rebar"
│  ├─ ExternalEvents/           ← ActionEventHandler (hàng đợi Action<UIApplication>)
│  ├─ Settings/                 ← SettingsService<T> (JSON, %AppData%\JNNTool\Settings)
│  ├─ Logging/                  ← Logger (%AppData%\JNNTool\Logs)
│  └─ Compat/                   ← Khác biệt API giữa các bản Revit (#if)
├─ Rebar/
│  ├─ Common/                   ← CoverHelper, HookHelper, LapCalculator, AnchorCalculator,
│  │                               BarCatalog (D6..D32), RebarTypeResolver, GeometryHelper
│  ├─ Slab/                     ← Thép sàn
│  ├─ Beam/                     ← Thép dầm
│  ├─ Column/                   ← Thép cột
│  ├─ Wall/                     ← Thép vách
│  ├─ Foundation/               ← Móng đơn, móng băng, đài cọc
│  ├─ Pile/                     ← Cọc khoan nhồi, cọc vuông
│  ├─ Stair/                    ← Thang
│  └─ Utilities/                ← Tag, Dim, Isolate, Numbering, Schedule
├─ UI/
│  ├─ Styles/                   ← ResourceDictionary chung (màu, font, button)
│  ├─ Controls/                 ← Control dùng lại (BarPicker, SpacingInput, Preview2D)
│  └─ Converters/
├─ Resources/Icons/             ← Icon 16/32 px tự vẽ, tên = <CommandName>_16.png / _32.png
├─ Families/                    ← JNN_*.rfa tự tạo
└─ Tests/JNNTool.Tests/         ← xUnit, chỉ test Layout Engine (net8.0)
```

Mỗi module (ví dụ `Rebar/Slab/`) có:
```
Slab/
├─ SlabRebarCommand.cs          ← IExternalCommand
├─ Models/                      ← SlabModel, SlabRebarSettings, SlabLayoutResult
├─ Engine/SlabLayoutEngine.cs   ← thuần C#
├─ Builders/SlabRebarBuilder.cs ← gọi Revit API
├─ Builders/SlabAnnotationBuilder.cs
├─ ViewModels/SlabRebarViewModel.cs
└─ Views/SlabRebarWindow.xaml(.cs)
```

---

## 4. Build đa phiên bản Revit

Dùng **một csproj** với Configuration theo bản Revit. Tham chiếu Revit API qua NuGet **Nice3point.Revit.Api** nên build được cả khi máy chưa cài đủ Revit.

```xml
<PropertyGroup>
  <UseWPF>true</UseWPF>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <PlatformTarget>x64</PlatformTarget>
  <Configurations>Debug.R24;Debug.R25;Debug.R26;Debug.R27;Release.R24;Release.R25;Release.R26;Release.R27</Configurations>
</PropertyGroup>

<PropertyGroup Condition="$(Configuration.Contains('R24'))">
  <RevitVersion>2024</RevitVersion>
  <TargetFramework>net48</TargetFramework>
  <DefineConstants>$(DefineConstants);REVIT2024</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="$(Configuration.Contains('R25'))">
  <RevitVersion>2025</RevitVersion>
  <TargetFramework>net8.0-windows</TargetFramework>
  <DefineConstants>$(DefineConstants);REVIT2025;REVIT2025_OR_GREATER</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="$(Configuration.Contains('R26'))">
  <RevitVersion>2026</RevitVersion>
  <TargetFramework>net8.0-windows</TargetFramework>
  <DefineConstants>$(DefineConstants);REVIT2026;REVIT2025_OR_GREATER</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="$(Configuration.Contains('R27'))">
  <RevitVersion>2027</RevitVersion>
  <!-- TODO: XÁC MINH target framework chính thức của Revit 2027 trước khi build -->
  <TargetFramework>net8.0-windows</TargetFramework>
  <DefineConstants>$(DefineConstants);REVIT2027;REVIT2025_OR_GREATER</DefineConstants>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Nice3point.Revit.Api.RevitAPI"   Version="$(RevitVersion).*" PrivateAssets="All" />
  <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$(RevitVersion).*" PrivateAssets="All" />
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
  <PackageReference Include="Newtonsoft.Json" Version="13.*" />
</ItemGroup>
```

**Lệnh build:**
```powershell
dotnet build -c Release.R24
dotnet build -c Release.R25
dotnet build -c Release.R26
dotnet build -c Release.R27
dotnet test Tests/JNNTool.Tests
```

**Lưu ý tương thích API:**
- Dùng `ElementId.Value` (long) cho mọi bản. Có từ 2024; `IntegerValue` đã bị xóa ở 2026.
- Dùng `ForgeTypeId` / `UnitTypeId` / `SpecTypeId`, **không** dùng `DisplayUnitType`, `ParameterType` (đã bị xóa).
- Mọi khác biệt API khác gom vào `Core/Compat/`, không rải `#if` khắp code.
- Revit 2025+ chạy .NET 8: tránh API chỉ có trên .NET Framework (`BinaryFormatter`, `AppDomain` tricks...).
- File `.addin` riêng cho từng bản, deploy vào `%AppData%\Autodesk\Revit\Addins\<năm>\`.

---

## 5. Quy ước code

- **MVVM**: dùng `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`). Không viết logic trong code-behind ngoài `InitializeComponent`.
- **Settings**: mỗi tool một class `XxxSettings` (POCO), lưu `%AppData%\JNNTool\Settings\Xxx.json`. Có `Reset về mặc định`.
- **Đơn vị**: UI và Engine dùng **mm**. Tên biến có hậu tố khi cần rõ: `coverMm`, `spacingMm`.
- **Đường kính thép**: dùng `BarCatalog` (D6, D8, D10, D12, D14, D16, D18, D20, D22, D25, D28, D32). Map sang `RebarBarType` theo tên, cho phép người dùng chọn type trong dự án.
- **Family**: tool **không phụ thuộc cứng** vào tên family. Tên mặc định `JNN_*`, nhưng luôn có dropdown cho người dùng chọn lại.
- **Lỗi**: bắt exception ở Command, ghi log, hiện `TaskDialog` thân thiện. Không để Revit crash.
- **Transaction**: luôn `using`, rollback khi lỗi. Đặt tên transaction tiếng Việt có dấu, ví dụ `"JNN - Tạo thép sàn"`.
- **Comment**: tiếng Việt hoặc English đều được, ngắn gọn. Giải thích **vì sao**, không giải thích **làm gì**.

---

## 6. Roadmap & Trạng thái

> Agent cập nhật cột **Trạng thái** sau mỗi giai đoạn: `⬜ Chưa làm` → `🟨 Đang làm` → `✅ Xong`.

| GĐ | Module | Phạm vi | Trạng thái |
|---|---|---|---|
| 0 | **Core** | Multi-target 2024–2027, Ribbon "JNN Rebar", ExternalEvent, Settings, Logger, Common helpers, project Tests | ✅ |
| 1 | **Sàn** | Mục 7.1 | ✅ |
| 2 | **Dầm** | Mục 7.2 | ✅ |
| 3 | **Cột / Vách** | Mục 7.3, 7.4 | ⬜ |
| 4 | **Móng / Cọc** | Mục 7.5, 7.6 | ⬜ |
| 5 | **Thang** | Mục 7.7 | ⬜ |
| 6 | **Tiện ích** | Mục 7.8 | ⬜ |

**Definition of Done cho mỗi giai đoạn:**
1. Build thành công cả 4 configuration Release.R24…R27.
2. Unit test Layout Engine pass.
3. Có file `docs/test-checklist-<module>.md`: mô hình mẫu, các bước, kết quả mong đợi, để người dùng test tay trên Revit.
4. Cập nhật bảng Trạng thái ở trên và `CHANGELOG.md`.

---

## 7. Đặc tả tính năng (Functional Spec)

> Các thông số dưới đây là **giá trị mặc định**, người dùng chỉnh được trong UI.
> Giá trị theo tiêu chuẩn (neo, nối, lớp bảo vệ) phải **cấu hình được** và cần được người dùng xác nhận lại theo TCVN 5574:2018.

### 7.1 Thép sàn — `SlabRebarCommand`

**Đầu vào:** chọn một hoặc nhiều Floor (sàn kết cấu).

**Chức năng:**
- Thép **lớp dưới** theo phương X, Y: đường kính, khoảng cách, móc 2 đầu (None / 90° / 180°), chiều dài móc.
- Thép **lớp trên** theo phương X, Y (tùy chọn).
- Thép **mũ** (thép tăng cường gối) trên dầm:
  - Chiều dài mũ = hệ số × nhịp (mặc định **L/4**), làm tròn lên bội số **50 mm**.
  - Bỏ qua nhịp nhỏ hơn **800 mm**.
  - Đoạn bẻ xuống: A = 150, B = 100 mm.
  - Thép phân bố cho mũ: D6 a300.
  - Tùy chọn kéo dài mũ tới mép sàn console.
- **Dò dầm**: tự tìm dầm đỡ sàn, sai số **500 mm**, bề rộng mặc định dầm biên / dầm giữa **300 mm**.
- **Lỗ mở**: cắt thép quanh lỗ, tùy chọn bỏ qua lỗ nhỏ hơn **1000 mm**.
- **Con kê / chân chó** (spacer): D10, lưới 1000×1000, móc 200 (tùy chọn).
- **Lớp bảo vệ**: trên 15–20, dưới 15–20, đầu neo 15–25 mm (cấu hình được).
- Hỗ trợ sàn nghiêng và sàn cong (Varying Rebar Set) ở bản sau.

**Thể hiện 2D trên mặt bằng:**
- Detail component `JNN_ThepSan_LopDuoi`, `JNN_ThepSan_LopTren`, `JNN_ThepSan_2Lop`, `JNN_ThepSan_RaiThep`.
- Màu theo lớp (mặc định): dưới `#00AEEF`, trên `#FF5A5F`, mũ `#FF9800`. Line pattern cấu hình được.
- Tự tag (dropdown chọn Tag family/type), tự dim vùng L/4, offset dim 300 mm.
- Tô màu theo đường kính (tùy chọn).
- Gán Mark của host và Partition cho rebar (tùy chọn).

### 7.2 Thép dầm — `BeamRebarCommand`

**Đầu vào:** chọn chuỗi dầm liên tục (nhiều nhịp), tự sắp xếp theo trục, tự nhận gối (cột / vách / dầm khác).

**Chức năng:**
- Thép chủ **trên / dưới** chạy suốt, nối tại vùng cho phép.
- Thép **gia cường gối** (trên) và **gia cường nhịp** (dưới), chiều dài theo hệ số nhịp.
- **Đai**: vùng dày gần gối (L/4 hoặc theo chiều cao dầm), vùng thưa giữa nhịp; kiểu đai kín, đai móc 135°.
- Thép **giá** (cấu tạo) khi dầm cao.
- Dầm **phụ** gác lên dầm chính: thép neo, đai gia cường tại vị trí giao.
- Dầm **cong**, dầm **nghiêng** (giai đoạn sau).
- Tự tạo **mặt cắt dọc (MCD)** và **mặt cắt ngang (MCN)**, đặt view, dim và tag thép.
- Đặt tên view theo mark dầm.

### 7.3 Thép cột — `ColumnRebarCommand`

**Luồng thao tác:**
1. Chọn cột, tự gom thành **chuỗi tầng** theo cao độ.
2. Chọn dầm giao tại mỗi tầng; nếu không chọn thì **tự quét** dầm giao để lấy chiều cao dầm.
3. Dựng `ColumnStackModel`: mỗi tầng gồm Level, chiều cao H, tiết diện B×H, chiều sâu dầm.
4. Mở cửa sổ modeless, preview **3D** chuỗi cột và thép.
5. Tạo thép qua ExternalEvent.

**Chức năng:**
- Thép dọc: số thanh theo cạnh B / H, đường kính, **nối chồng** hoặc **bẻ cổ chai** khi đổi tiết diện.
- Đai: kiểu **AB**, **CN** (chữ nhật), **LAP** (đai nối chồng), đai móc (C-tie); vùng dày / thưa.
- Cột **tròn** (đai tròn / xoắn).
- Lưu cấu hình theo **Group cột** (gán tên Group) để áp dụng lại cho cột cùng loại.
- Kiểm tra cấu tạo đai (khoảng cách tối đa, đường kính tối thiểu).
- Tự tạo MCD / MCN cột, dim chiều cao (bao gồm móng).

### 7.4 Thép vách — `WallRebarCommand`

- Thép đứng, thép ngang 2 lớp; đai / thép C biên vách.
- Vách tầng hầm, vách bể nước (tùy chọn chống thấm: lớp bảo vệ lớn hơn).
- Nối chồng theo tầng.
- MCD / MCN vách, auto dim.

### 7.5 Thép móng — `FoundationRebarCommand`

- **Móng đơn**: đúng tâm, lệch tâm biên, lệch tâm góc. Thép lưới đáy 2 phương, thép chờ cột.
- **Móng băng**: thép dọc, thép ngang, sườn (dầm móng), vát góc.
- **Đài cọc**: 1–5 cọc, đài chữ nhật, đài tam giác 3 cọc. Thép đáy, thép đầu cọc, thép cấu tạo mặt trên.
- **Dầm móng / giằng móng**: dùng lại Engine của dầm (7.2).
- Bản vẽ: mặt bằng móng, chi tiết móng, dim móng, dim lưới trục.

### 7.6 Thép cọc — `PileRebarCommand`

- **Cọc khoan nhồi**: thép dọc, đai xoắn hoặc đai tròn, đai gia cường, ống siêu âm (tùy chọn).
- **Cọc vuông**: thép dọc, đai vùng dày đầu / mũi cọc.
- Đánh số cọc, xuất tọa độ cọc (Excel / CSV).

### 7.7 Thép thang — `StairRebarCommand`

- Thang **1, 2, 3 vế**, vế vuông góc, chiếu nghỉ.
- Thép bản thang lớp dưới / trên, thép mũ tại gối, thép bậc.
- Mặt cắt thang, tag thép.

### 7.8 Tiện ích Rebar — nhóm `Utilities`

| Tool | Chức năng |
|---|---|
| `TagMultiRebarCommand` | Tag hàng loạt thép trong view, căn thẳng hàng |
| `AutoDimRebarCommand` | Dim tự động thép trong mặt cắt (cột, vách, dầm) |
| `IsolateRebarCommand` | Cô lập thép theo host / theo đường kính |
| `RebarUnobscuredCommand` | Bật / tắt "View Unobscured" hàng loạt |
| `SelectRebarByCategoryCommand` | Chọn thép theo host category / type |
| `RebarNumberCommand` | Đánh số hiệu thép (Mark) |
| `RebarVolumeCommand` | Thống kê khối lượng thép theo đường kính / cấu kiện / tầng |
| `QuickRebarScheduleCommand` | Tạo nhanh bảng thống kê thép |
| `SlabRebarVisibilityCommand` | Bật / tắt hiển thị thép sàn theo lớp |
| `RebarTypeManagerCommand` | Tạo và chuẩn hóa RebarBarType D6–D32, HookType, CoverType |

---

## 8. Ghi chú Revit Rebar API

- Tạo thép từ curve: `Rebar.CreateFromCurves(...)`. Từ shape: `Rebar.CreateFromRebarShape(...)`.
- Rải thép: `rebar.GetShapeDrivenAccessor().SetLayoutAsMaximumSpacing(...)` / `SetLayoutAsNumberWithSpacing(...)` / `SetLayoutAsFixedNumber(...)`.
- Thép biến thiên (sàn cong, sàn nghiêng): `Rebar.CreateFreeForm(...)` hoặc Varying Rebar Set.
- Hiển thị: `rebar.SetUnobscuredInView(view, true)`, `SetSolidInView(view3D, true)`.
- Lớp bảo vệ: `RebarHostData.GetRebarHostData(host)` và `RebarCoverType`.
- Kiểm tra host hợp lệ: `RebarHostData.IsValidHost(element)`.
- Hook: `RebarHookType` (lọc theo góc 90/135/180). Nếu dự án chưa có thì tạo bằng `RebarHookType.Create(...)`.
- Không gọi `doc.Regenerate()` trong vòng lặp lớn, gom lại cuối batch.
- Hiệu năng: với vài nghìn thanh, tạo trong một Transaction, tắt preview UI trong lúc tạo.

---

## 9. Kiểm thử

- **Unit test (xUnit)**: chỉ cho Layout Engine. Ví dụ:
  - Số thanh trên chiều dài L với khoảng cách a và lớp bảo vệ c.
  - Chiều dài mũ L/4 có làm tròn 50.
  - Vùng đai dày / thưa của dầm, cột.
  - Chuỗi tầng cột: phát hiện đổi tiết diện.
- **Không** unit test code gọi Revit API. Thay bằng checklist test tay (`docs/test-checklist-*.md`).
- Agent **không thể** chạy Revit. Phải báo rõ phần nào cần người dùng test thủ công.

---

## 10. Giao tiếp với người dùng

- Trả lời bằng **tiếng Việt**, ngắn gọn.
- Trước khi bắt đầu một giai đoạn: đưa **kế hoạch ngắn** (file cần tạo / sửa) và chờ người dùng duyệt.
- Sau mỗi giai đoạn: tóm tắt thay đổi, kết quả build, và việc người dùng cần test.
