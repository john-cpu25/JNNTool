# Kế Hoạch Triển Khai: Hệ Thống Tạo & Quản Lý Key Bản Quyền JNNTool Qua Web & Dòng Lệnh (CLI)

> **Tài liệu kế hoạch kỹ thuật** — Dành cho Quản trị viên (Admin) tạo Key tự động kết nối trực tiếp với Database Supabase.  
> 📌 **Trạng thái**: *Đã triển khai hoàn tất (Mã nguồn SQL, Web Portal & CLI PowerShell).*

---

## 1. Mục Tiêu

1. **Tạo Key nhanh chóng qua Web (Web Admin Portal)**:
   - Một giao diện Web Admin nhẹ, trực quan, mở được trên máy tính hoặc điện thoại di động: `Tools/Admin/admin-license-portal.html`.
   - Nhập Email khách hàng ➔ Chọn thời hạn (30 ngày, 1 năm, Vĩnh viễn) ➔ Bấm **[ Tạo Key ]**.
   - Tự động sinh mã Key chuẩn `JNN-XXXX-XXXX-XXXX` (Base32 Crockford chống nhầm lẫn ký tự), ghi vào Supabase và hiển thị nút **[ Copy Key ]** và **[ Copy Tin Nhắn Gửi Khách ]**.
   - Quản lý danh sách Key: Xem máy đã kích hoạt, nút **[ Reset máy ]** (khi khách đổi máy), nút **[ Gia hạn ]**, nút **[ Thu hồi ]**.
2. **Tạo Key bằng 1 dòng lệnh (CLI / Script)**:
   - Dành cho những lúc đang làm việc trên máy tính: mở Terminal/PowerShell gõ 1 dòng lệnh ngắn qua `scripts/create-key.ps1` là sinh ra mã Key ngay lập tức và tự động copy vào Clipboard.
3. **Bảo mật tuyệt đối**:
   - Sử dụng **Admin Secret Key** (mật khẩu quản trị bí mật). Người ngoài không có Secret Key này thì không thể gọi API để tạo Key trái phép.

---

## 2. Sơ Đồ Hoạt Động (Architecture Flow)

```
┌─────────────────────────────────────────────────────────────┐
│                 CÁC KÊNH TẠO KEY CỦA ADMIN                  │
│                                                             │
│  [Kênh 1] Web Portal: `Tools/Admin/admin-license-portal.html`│
│  [Kênh 2] Dòng lệnh CLI: `scripts/create-key.ps1`           │
│  [Kênh 3] SQL Query: Trên Supabase SQL Editor               │
└──────────────────────────────┬──────────────────────────────┘
                               │ Gửi: Email, Hạn dùng + ADMIN_SECRET
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                   SUPABASE BACKEND RPC                      │
│             `public.admin_generate_key(...)`                │
│             `public.admin_manage_license(...)`              │
│             `public.admin_list_licenses(...)`               │
│                                                             │
│  1. Kiểm tra ADMIN_SECRET (bảng `admin_config` có RLS):     │
│     ├── ❌ Sai mật khẩu ➔ Từ chối ngay                       │
│     └── ✅ Đúng mật khẩu ➔ Tiếp tục                          │
│  2. Tự sinh mã Base32 ngẫu nhiên: JNN-XXXX-XXXX-XXXX        │
│  3. Ghi vào bảng `public.licenses` (`bound_machine_id = NULL`)│
│  4. Trả về kết quả JSON cho Admin                           │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│  Hiển thị mã Key trên màn hình + Tự động copy vào Clipboard  │
│  ➔ Admin gửi mã Key cho khách hàng để kích hoạt trên Revit   │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Danh Sách Các File & Thành Phần Đã Triển Khai

| STT | File / Thành phần | Loại | Trạng thái | Mục đích |
|:---:|---|---|:---:|---|
| **1** | `supabase/admin_license_rpc.sql` | **SQL Script trên Supabase** | <mark>**✅ ĐÃ XONG**</mark> | Chứa 3 hàm RPC: `admin_generate_key`, `admin_manage_license`, `admin_list_licenses` và bảng cấu hình `admin_config`. |
| **2** | `Tools/Admin/admin-license-portal.html` | **Giao diện Web Admin** | <mark>**✅ ĐÃ XONG**</mark> | Giao diện Web tĩnh độc lập (HTML/CSS/JS thuần), chuẩn Clean UI JNNTool. Chạy trực tiếp từ trình duyệt trên máy tính/điện thoại hoặc deploy web tĩnh. |
| **3** | `scripts/create-key.ps1` | **Script PowerShell (CLI)** | <mark>**✅ ĐÃ XONG**</mark> | Script chạy dòng lệnh: `.\scripts\create-key.ps1 -Email "..." -Days 365`. Tự động copy Key vào Windows Clipboard. |
| **4** | `docs/supabase-license-architecture.md` | **Tài liệu dự án** | <mark>**✅ ĐÃ XONG**</mark> | Tài liệu bảo mật 4 lớp chống Fiddler, Replay Attack và dịch ngược IL. |

---

## 4. Chi Tiết Kỹ Thuật

### 4.1 Hàm Backend Supabase (`admin_generate_key`)
- **Đầu vào**:
  - `p_email`: Email khách hàng.
  - `p_days`: Số ngày hiệu lực (mặc định 365 ngày).
  - `p_type`: Loại bản quyền (`Trial`, `Annual`, `Lifetime`).
  - `p_admin_secret`: Mật khẩu bảo vệ của Admin.
- **Xử lý**:
  - Đối soát `p_admin_secret` với bảng `admin_config` bí mật (RLS enabled).
  - Sinh mã định dạng: `JNN-` + 4 ký tự + `-` + 4 ký tự + `-` + 4 ký tự (bảng Base32 Crockford `23456789ABCDEFGHJKMNPQRSTVWXYZ` loại trừ 0, O, 1, I, L).
  - `INSERT INTO public.licenses (...)`.
- **Đầu ra**: Trả về chuỗi JSON gồm: `{ success: true, license_key: "JNN-...", expires_at: "..." }`.

### 4.2 Giao Diện Web Admin Portal (`admin-license-portal.html`)
- **Thiết kế**: Clean Light Theme đồng bộ JNNTool (nền `#F5F7F9`, bo góc 5px, chữ thanh thoát `font-weight: normal`).
- **Tính năng trên Web**:
  - **Khung cấu hình**: Nhập `Admin Secret Key`, `Supabase URL`, `Anon Key` (lưu tự động vào `localStorage` trình duyệt).
  - **Khung tạo Key**:
    - Ô nhập Email khách.
    - Dropdown chọn gói: `Annual (1 Năm)`, `Trial (30 Ngày)`, `2 Năm (730 Ngày)`, `Lifetime (10 Năm)` hoặc số ngày tùy chỉnh.
    - Nút bấm **[ ⚡ Tạo Key Bản Quyền ]**.
    - Hộp kết quả: Hiển thị mã Key to rõ kèm nút **[ 📋 Copy Key ]** và **[ 💬 Copy Tin Nhắn Gửi Khách ]**.
  - **Khung danh sách khách hàng**:
    - Hiển thị bảng danh sách các Key đã tạo: Email, Mã Key, Hạn dùng, Trạng thái máy (`Chưa khóa máy` hoặc `🔒 Đã khóa máy: [Tên máy]`).
    - Nút thao tác nhanh trên từng dòng:
      - **[ 🔄 Reset Máy ]**: Xóa khóa máy khi khách đổi máy tính hoặc cài lại Windows.
      - **[ ➕ Gia Hạn ]**: Cộng thêm 365 ngày trực tiếp.
      - **[ ⛔ Thu Hồi ]**: Khóa bản quyền ngay lập tức.

### 4.3 Script Dòng Lệnh (`create-key.ps1`)
- Cú pháp thực thi:
  ```powershell
  # Tạo key 1 năm:
  .\scripts\create-key.ps1 -Email "khachhang@gmail.com" -Days 365

  # Tạo key dùng thử 30 ngày:
  .\scripts\create-key.ps1 -Email "trial@gmail.com" -Days 30 -Type "Trial"

  # Reset máy khi khách đổi máy tính:
  .\scripts\create-key.ps1 -ResetMachine -LicenseKey "JNN-XXXX-XXXX-XXXX"
  ```
- Kết quả xuất ra màn hình Terminal:
  ```text
  ======================================================
    [JNNTool] TẠO KEY BẢN QUYỀN THÀNH CÔNG!
  ======================================================
   Email       : khachhang@gmail.com
   License Key : JNN-8F3D-4A2B-9C1E
   Loại Key    : Annual
   Hạn Dùng    : 365 Ngày (Đến: 16/10/2027 21:00)
   Khóa Máy    : Chưa khóa (Sẽ tự khóa vào máy khách khi nhập)
  ======================================================
  >> [OK] Đã tự động sao chép mã Key vào Clipboard!
  ```

---

## 5. Các Bước Triển Khai

- [x] **Giai đoạn 1**: Tạo file SQL `supabase/admin_license_rpc.sql` gồm các hàm `admin_generate_key`, `admin_manage_license`, `admin_list_licenses` và bảng cấu hình `admin_config`.
- [x] **Giai đoạn 2**: Tạo file giao diện Web `Tools/Admin/admin-license-portal.html` hỗ trợ tạo key, tra cứu và quản lý khách hàng từ trình duyệt.
- [x] **Giai đoạn 3**: Tạo script PowerShell `scripts/create-key.ps1` hỗ trợ tạo key nhanh từ máy tính và tự động copy vào Clipboard.
- [x] **Giai đoạn 4**: Cập nhật tài liệu hướng dẫn vận hành vào `docs/supabase-license-architecture.md` và `docs/admin-key-generator-plan.md`.
