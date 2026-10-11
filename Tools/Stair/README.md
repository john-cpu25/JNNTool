# BỘ CÔNG CỤ THIẾT KẾ & BỐ TRÍ THÉP CẦU THANG (JNN STAIR SUITE)
> Thư mục làm việc độc lập: `Tool/Stair`

---

## 1. Mục Tiêu Dự Án
Xây dựng lại **100% độc lập (Clean-Room)** bộ công cụ thiết kế và bố trí cốt thép cầu thang chuyên nghiệp cho Autodesk Revit (hỗ trợ Revit 2024, 2025, 2026, 2027), thay thế toàn diện các add-in thương mại như HT-Structural với các ưu điểm:
1. **Chạy hoàn toàn Offline**: Không phụ thuộc internet, không kiểm tra bản quyền (license), mở lệnh tức thì (< 0.1s).
2. **Decoupled Preview (Không Lag)**: Mô phỏng hình học 3D và cốt thép 2D trực tiếp trên WPF Canvas/Viewport bằng C# thuần trên RAM, không tạo Transaction tạm làm chậm Revit.
3. **Mã nguồn sạch, hiện đại**: Tương thích .NET 8 (Revit 2025+) và .NET Framework 4.8 (Revit 2024), sử dụng hệ `ForgeTypeId` chuẩn mới.
4. **Chuẩn kết cấu TCVN 5574:2018**: Áp dụng đúng nguyên tắc neo, nối, chiều dài ngàm và **kỹ thuật đan chéo thép chịu kéo tại góc lõm** chống bạt vỡ bê tông bảo vệ.

---

## 2. Cấu Trúc Hệ Thống (Module Architecture)

```
Tool/Stair/
├── README.md                          <-- Tài liệu kiến trúc và hướng dẫn vận hành
├── StairSuite.csproj                  <-- Project cấu hình đa phiên bản Revit 2024-2027
│
├── Models/                            <-- DTO & Data Transfer Objects (POCO thuần túy)
│   ├── StairEnums.cs                  <-- StairPreset, StairInputMode, RebarCornerMode...
│   ├── StairLayout.cs                 <-- Dữ liệu hình học tổng thể của cầu thang
│   ├── StairLandingBeam.cs            <-- Thông số dầm chiếu nghỉ tích hợp
│   ├── StairWinderStep.cs             <-- Hình học từng bậc rẻ quạt (winder)
│   ├── StairRebarData.cs              <-- Dữ liệu đường sinh các thanh thép (curves, hooks)
│   └── StairSectionData.cs            <-- Dữ liệu mặt cắt trích xuất từ Revit view
│
├── Core/                              <-- Core Engines (Thuần C#, không dính Revit API)
│   ├── StairGeometryEngine.cs         <-- Tính toán tọa độ không gian 3D bản thang & bậc
│   ├── StairWinderCalculator.cs       <-- Thuật toán chia nan quạt bậc chéo chuẩn an toàn
│   └── StairRebarCurveBuilder.cs      <-- Thuật toán uốn cốt thép, đan chéo góc lõm
│
├── Services/                          <-- Revit API Services (Tương tác với Revit Document)
│   ├── StairFamilyGeneratorService.cs <-- Mở template Metric Generic Model.rft, dựng Solid, xuất RFA
│   ├── StairRebarService.cs           <-- Đẩy Transaction tạo RebarSet 3D thật vào Revit
│   └── StairSectionScanner.cs         <-- Cắt Solid bản thang qua Plane để lấy biên dạng
│
├── ViewModels/                        <-- Tầng MVVM ViewModels
│   ├── StairDesignerViewModel.cs      <-- ViewModel thiết kế hình học & tạo Family
│   └── StairDetailingViewModel.cs      <-- ViewModel rải thép & điều chỉnh thanh
│
└── Views/                             <-- Giao diện WPF (HandyControl/Segoe UI 12, bo góc 5px)
    ├── StairDesignerWindow.xaml       <-- Cửa sổ thiết kế hình học 3D
    └── StairDetailingWindow.xaml      <-- Cửa sổ rải thép với Canvas tương tác 2D
```

---

## 3. Bản Đồ Tham Số Cầu Thang (Parameters Map)

| Tham số | Ý nghĩa | Giá trị mặc định |
|---|---|---|
| `FloorToFloorMm` | Chiều cao tầng (sàn - sàn) | $3600\text{ mm}$ |
| `StairWidthMm` | Chiều rộng vế thang | $1000\text{ mm}$ |
| `TreadMm` | Chiều rộng mặt bậc | $280\text{ mm}$ |
| `PreferredRiserMm` | Chiều cao bậc mong muốn | $160\text{ mm}$ (tự tính ra số bậc chẵn) |
| `WaistThicknessMm` | Chiều dày bản nghiêng bê tông | $120\text{ mm}$ |
| `LandingDepthMm` | Chiều sâu chiếu nghỉ | $1200\text{ mm}$ |
| `FlightGapMm` | Khoảng hở giữa các vế (giếng thang) | $100\text{ mm}$ |
| `LandingBeamWidthMm` | Bề rộng dầm chiếu nghỉ | $200\text{ mm}$ |
| `LandingBeamHeightMm` | Chiều cao dầm chiếu nghỉ | $350\text{ mm}$ |
| `WinderStepCount` | Số bậc xòe tại góc cua | $3$ bậc (hoặc $4$ bậc) |

---

## 4. Nguyên Tắc Cốt Lõi Về Thép Cầu Thang (Engineering Rules)
1. **Góc lồi (Convex Corner)**:
   - Thép lớp dưới được uốn liên tục ôm theo mép trong của góc lồi.
2. **Góc lõm (Concave Corner)**:
   - ⚠️ **Tuyệt đối KHÔNG uốn cong thanh thép chịu kéo lớp dưới theo góc lõm**.
   - Phải kéo thẳng thanh thép đâm xuyên sang vùng bê tông đối diện tối thiểu đoạn neo $L_{\text{anc}} = 35d$, hình thành **mối nối chéo chống bung nở vỡ góc**.
3. **Thép mũ gối (Top Rebar)**:
   - Chiều dài vươn ra khỏi mép gối tối thiểu $L/4$ của nhịp vế thang.
4. **Thép phân bố (Distribution Rebar)**:
   - Rải đều theo phương vuông góc với vế thang, bước rải $a = 150 \sim 200\text{ mm}$.
