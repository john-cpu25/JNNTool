# Checklist Kiểm Thử - Giai Đoạn 2: Thép Dầm Liên Tục (BeamRebarCommand)

## 1. Mô hình mẫu chuẩn bị trong Revit
1. Dựng một chuỗi dầm liên tục gồm 2 hoặc 3 nhịp thẳng hàng:
   - Category: `Structural Framing: Concrete - Rectangular Beam`.
   - Tiết diện mẫu: 300 × 500 mm hoặc 300 × 700 mm.
   - Chiều dài nhịp: Nhịp 1 = 4000 mm, Nhịp 2 = 5000 mm.
   - Gối tựa: Cột bê tông 300 × 300 mm hoặc tường.

## 2. Các bước thực hiện
1. Quét chọn các dầm trong chuỗi liên tục trên mặt bằng hoặc view 3D.
2. Trên Ribbon **JNNTool**, tìm nhóm **JNN Rebar** → Bấm nút **Rebar Beam**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Dầm Liên Tục (TCVN 5574:2018)** sẽ mở ra:
   - **Tab Thép Chủ & Gia Cường**:
     - Thép chủ trên: 2D20.
     - Thép chủ dưới: 2D20.
     - Thép gia cường gối: Tích chọn, 2D20, Hệ số nhịp `0.25` (L/4).
     - Thép gia cường nhịp: Tích chọn, 2D20.
   - **Tab Cốt Đai & Thép Giá**:
     - Đường kính đai: D8.
     - Bước đai gần gối: 100 mm (phạm vi L/4).
     - Bước đai giữa nhịp: 200 mm.
     - Thép giá: Tích chọn tự động bố trí khi $H \ge 600$ mm.
   - **Tab Neo & Bản Vẽ Mặt Cắt**:
     - Lớp bảo vệ: 25 mm.
     - Đoạn neo: 35d.
     - Tự động tạo Mặt Cắt Dọc (MCD): Tích chọn, tiền tố `MCD_`.
4. Bấm nút **✓ Tạo Thép Dầm**.

## 3. Kết quả mong đợi
- Revit tự động tạo cốt thép hoàn chỉnh trong dầm:
  - Thép chủ trên và dưới chạy suốt chuỗi dầm, 2 đầu bẻ neo 90 độ vào gối biên.
  - Cốt đai chia rõ 3 vùng: vùng dày 2 đầu nhịp (L/4, a100) và vùng thưa giữa nhịp (a200).
  - Thép gia cường gối (2D20) vươn cân xứng 2 bên gối giữa theo nhịp L/4 làm tròn 50 mm.
  - Thép giá thành dầm tự động sinh nếu dầm cao $H \ge 600$ mm.
- Tạo tự động View Section Mặt Cắt Dọc `MCD_<Tên_dầm>` trong cây thư mục Project Browser.
- Thép tự động Unobscured, gom toàn bộ vào `TransactionGroup` `"JNN - Tạo thép dầm liên tục"`, Undo 1 lần sạch sẽ.
