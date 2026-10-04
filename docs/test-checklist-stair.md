# Checklist Kiểm Thử Thủ Công — Cốt Thép Bản Thang (Stair Rebar)

Tài liệu hướng dẫn kiểm tra thủ công tính năng **Rebar Stair** trong bộ công cụ **JNNTool Rebar Suite** trên Revit.

---

## 1. Chuẩn bị mô hình Revit mẫu
1. Mở mô hình Revit (2024, 2025, 2026, 2027).
2. Tạo một cầu thang bê tông bản đúc liền (`Monolithic Stair`) 2 vế chữ U hoặc 1 vế thẳng:
   - Chiều rộng bản thang: 1000 - 1200 mm.
   - Số bậc mỗi vế: 8 - 11 bậc (bề rộng mặt bậc 280 mm, chiều cao bậc 160 mm).
   - Chiều dày bản bê tông thang: 120 mm.

## 2. Các bước thực hiện
1. Chọn cấu kiện cầu thang (`Stairs`) trên mặt bằng hoặc view 3D.
2. Trên Ribbon Revit tab **JNN Rebar**, nhấn nút **Rebar Stair**.
3. Cửa sổ **JNN Rebar — Bố trí Thép Bản Thang** hiển thị:
   - **Tab Thép Chịu Lực (Lớp Dưới & Mũ)**:
     - Thép bản thang lớp dưới: `D10`, khoảng cách a = `150 mm`.
     - Đoạn neo vào gối dầm: `350 mm` (35d).
     - Lớp bảo vệ: `15 mm`.
     - Bật *Bố trí thép mũ chịu mô-men âm tại gối*, đường kính `D10` a = `150 mm`, hệ số vươn nhịp L/4 = `0.25`.
   - **Tab Thép Phân Bố Ngang**:
     - Đường kính thép phân bố: `D6`, khoảng cách a = `200 mm`.
4. Nhấn **Tạo Thép Thang**.

## 3. Kết quả mong đợi
- [ ] Lớp cốt thép dọc dưới chạy nghiêng dọc theo chiều dốc của bản thang, 2 đầu thanh được bẻ góc neo phẳng nằm ngang vào dầm chân thang và dầm chiếu nghỉ (chiều dài neo 350 mm).
- [ ] Thép mũ gối trên được uốn neo vào dầm chiếu nghỉ và vươn xuống dốc thang một đoạn bằng 1/4 chiều dài vế dốc.
- [ ] Hệ cốt thép phân bố ngang D6 a200 được rải đều vuông góc với thép chịu lực chính dọc theo chiều dài vế thang.
- [ ] Toàn bộ cốt thép hiển thị rõ nét trên Active View (Unobscured).
