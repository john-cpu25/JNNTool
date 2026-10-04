# Checklist Kiểm Thử - Giai Đoạn 1: Thép Sàn (SlabRebarCommand)

## 1. Mô hình mẫu chuẩn bị trong Revit
1. Tạo 1 sàn kết cấu mẫu (`Floor: Structural`):
   - Kích thước ví dụ: 4000 × 5000 mm hoặc nhịp bất kỳ.
   - Chiều dày: 120 mm hoặc 150 mm.
   - Bố trí các dầm đỡ xung quanh (`Structural Framing: Concrete Beam` 300×500 mm).

## 2. Các bước thực hiện
1. Chọn sàn kết cấu vừa tạo trên mặt bằng hoặc view 3D.
2. Trên Ribbon **JNNTool**, tìm nhóm **JNN Rebar** → Bấm nút **Rebar Slab**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Sàn (TCVN 5574:2018)** sẽ mở ra:
   - **Tab Thép Lớp Dưới**:
     - Phương X: D10 a150.
     - Phương Y: D10 a150.
   - **Tab Thép Mũ Gối (L/4)**:
     - Tích chọn "Bố trí thép mũ tăng cường tại các gối dầm".
     - Đường kính mũ: D10, khoảng cách: 150 mm.
     - Hệ số nhịp: `0.25` (L/4).
     - Bẻ móc mép ngoài: 150 mm, mép trong: 100 mm.
     - Thép phân bố: D6 a300.
   - **Tab Lớp Bảo Vệ & Quản Lý**:
     - Lớp bảo vệ dưới: 15 mm, trên: 15 mm.
     - Partition: `JNN_Slab`.
4. Bấm nút **✓ Tạo Thép Sàn**.

## 3. Kết quả mong đợi
- Revit tạo thành công các bộ thép (RebarSet) trong sàn:
  - Thép lớp dưới phương X rải đều dọc theo sàn, cao độ Z cách đáy sàn 15 mm + D/2.
  - Thép lớp dưới phương Y nằm ngay trên lớp X.
  - Thép mũ gối L/4 bẻ mỏ cắm xuống gối dầm, chiều dài vươn = L/4 làm tròn lên bội số 50 mm.
  - Thép phân bố D6 a300 rải vuông góc để giữ thép mũ.
- Thép tự động bật chế độ hiển thị rõ (Unobscured) trong View hiện tại.
- Toàn bộ thao tác gom vào một `TransactionGroup` `"JNN - Tạo thép sàn"`, cho phép người dùng `Ctrl+Z` (Undo) một lần là khôi phục toàn bộ trạng thái trước khi tạo.
