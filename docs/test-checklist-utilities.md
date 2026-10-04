# Checklist Kiểm Thử Thủ Công — Tiện Ích Rebar (Rebar Utilities)

Tài liệu hướng dẫn kiểm tra thủ công menu **Rebar Utils** trong bộ công cụ **JNNTool Rebar Suite** trên Revit.

---

## 1. Kiểm Thử Thống Kê Khối Lượng Thép (`Rebar Volume`)
1. Mở mô hình Revit đã có cốt thép (Sàn, Dầm, Cột, Vách, Móng, Cọc hoặc Thang).
2. Trên Ribbon tab **JNN Rebar**, nhấn menu **Rebar Utils** -> chọn **Thống Kê Khối Lượng Thép (CSV)**.
3. Kết quả mong đợi:
   - [ ] Hộp thoại hiển thị tổng hợp số lượng thanh, tổng chiều dài (m), khối lượng (kg) phân loại theo từng đường kính (D6, D8, D10, D12...).
   - [ ] Hiển thị tổng trọng lượng toàn dự án (Tấn).
   - [ ] File CSV chi tiết `JNN_ThongKeThep_*.csv` được tạo tự động trên Desktop và mở tốt bằng Microsoft Excel.

---

## 2. Kiểm Thử Đánh Số Hiệu Thép (`Auto Mark`)
1. Mở mô hình và chọn một vùng hoặc toàn bộ dự án.
2. Trên Ribbon tab **JNN Rebar**, nhấn menu **Rebar Utils** -> chọn **Đánh Số Hiệu Thép (Auto Mark)**.
3. Kết quả mong đợi:
   - [ ] Thuật toán tự động gom nhóm các thanh có cùng đường kính và chiều dài.
   - [ ] Gán số hiệu tăng dần (1, 2, 3...) vào tham số `Mark` của đối tượng Rebar.
   - [ ] Các thanh giống nhau nhận cùng một số hiệu `Mark`.
   - [ ] Thông báo số lượng cốt thép đã được cập nhật thành công.

---

## 3. Kiểm Thử Bật Nhìn Xuyên (`View Unobscured`)
1. Chuyển sang một View 3D hoặc Mặt Cắt (Section View).
2. Trên Ribbon tab **JNN Rebar**, nhấn menu **Rebar Utils** -> chọn **Bật Nhìn Xuyên (View Unobscured)**.
3. Kết quả mong đợi:
   - [ ] Toàn bộ cốt thép nằm bên trong bê tông lập tức hiển thị rõ nét (nhìn xuyên qua bê tông mà không cần chỉnh chế độ Wireframe).

---

## 4. Kiểm Thử Cô Lập Cốt Thép (`Isolate Rebar`)
1. Trong Active View đang chứa nhiều cấu kiện (Cột, Dầm, Sàn, Tường...).
2. Nhấn menu **Rebar Utils** -> chọn **Cô Lập Cốt Thép (Isolate Rebar)**.
3. Kết quả mong đợi:
   - [ ] View lập tức kích hoạt chế độ Temporary Isolate, chỉ giữ lại duy nhất cốt thép, ẩn toàn bộ cấu kiện bê tông xung quanh.
   - [ ] Nhấn lại lần 2 để tắt chế độ cô lập, khôi phục lại view ban đầu.

---

## 5. Kiểm Thử Chuẩn Hóa Loại Thép (`Rebar Type Manager`)
1. Mở một dự án mới hoặc dự án thiếu các loại đường kính thép tiêu chuẩn.
2. Nhấn menu **Rebar Utils** -> chọn **Chuẩn Hóa RebarBarType (D6-D32)**.
3. Kết quả mong đợi:
   - [ ] Tự động kiểm tra và khởi tạo đầy đủ hệ thống các loại đường kính D6, D8, D10, D12, D14, D16, D18, D20, D22, D25, D28, D32 theo đúng kích thước hình học tiêu chuẩn TCVN 5574:2018.
