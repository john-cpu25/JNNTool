# JNNTool — Danh sách công việc còn lại

> Cập nhật: 2026-09-13

---

## ✅ Đã hoàn thành

- [x] Đổi tên toàn bộ codebase `NhanStr` → `JNNTool`
- [x] Build plugin C# (net48 + net8, Revit 2022–2026)
- [x] Xóa file rác cũ (NhanStr.dll, NhanStr.zip, NhanStrSetup.msi...)
- [x] Tạo MSI installer bằng WiX v4 → `JNNToolSetup_v1.0.0.msi`
- [x] Tạo `JNNToolInstaller.exe` (WPF app quản lý update)
- [x] Gắn icon JNN logo vào exe và shortcut Start Menu
- [x] Thêm nút Minimize (−) và Close (✕) vào title bar
- [x] Xử lý lỗi 404 thân thiện khi GitHub chưa có release
- [x] Auto-update flow: fetch `version.json` → download MSI → chạy msiexec

---

## ⏳ Còn lại — cần làm

### 🔴 Ưu tiên cao

- [ ] **Push code lên GitHub**
  ```powershell
  cd "c:\Users\nhann\OneDrive\AI\JNNTool"
  git init
  git remote add origin https://github.com/john-cpu25/JNNTool.git
  git add .
  git commit -m "Release v1.0.0"
  git push -u origin main
  ```

- [ ] **Tạo GitHub Release v1.0.0**
  - Tạo tag: `git tag v1.0.0 && git push origin v1.0.0`
  - Vào: https://github.com/john-cpu25/JNNTool/releases/new
  - Upload: `Installer\JNNToolSetup_v1.0.0.msi`

- [ ] **Cập nhật `downloadUrl` trong `version.json`** sau khi upload xong
  - File: `Installer\JNNToolInstaller\version.json`
  - Giá trị: `https://github.com/john-cpu25/JNNTool/releases/download/v1.0.0/JNNToolSetup_v1.0.0.msi`
  - Push lại lên GitHub

- [ ] **Rebuild MSI cuối cùng** và cài lại trên máy
  ```powershell
  cd Installer
  .\BuildInstaller.ps1
  ```

---

### 🟡 Ưu tiên trung bình

- [ ] **Kiểm tra plugin hiện trong Revit**
  - Mở Revit → xem tab **JNNTool** trên Ribbon
  - Test từng tool: FloorByRoom, CreateFilter, SpeedOverrider, ImportE2K...

- [ ] **Test JNNToolInstaller.exe sau khi có GitHub Release**
  - Mở app từ Start Menu → JNNTool → JNNTool Updater
  - Xác nhận: version installed, version latest, changelog hiển thị đúng
  - Bấm "Cài lại" → xác nhận tải MSI và cài đặt thành công

- [ ] **Xác nhận icon JNN hiện trong Start Menu** sau khi cài MSI mới

---

### 🟢 Ưu tiên thấp / tương lai

- [ ] **Quy trình release lần sau** (v1.x.x):
  1. Sửa code
  2. Chạy `.\BuildInstaller.ps1 -Version "1.1.0"`
  3. Upload `JNNToolSetup_v1.1.0.msi` lên GitHub Releases
  4. Cập nhật `version.json` → `downloadUrl` trỏ đúng file mới
  5. Push lên GitHub → app tự thông báo update cho người dùng

- [ ] **Nâng cấp `System.Text.Json`** từ 8.0.0 (có lỗ hổng bảo mật) lên 8.0.5+
  - File: `Installer\JNNToolInstaller\JNNToolInstaller.csproj`
  - Đổi: `Version="8.0.5"`

- [ ] **(Tùy chọn)** Thêm nút "Xem Release Notes" mở GitHub Release trong browser

---

## 📁 File quan trọng

| File | Mục đích |
|---|---|
| `Installer\JNNToolSetup_v1.0.0.msi` | File cài đặt cho người dùng |
| `Installer\Package.wxs` | Source WiX để build MSI |
| `Installer\BuildInstaller.ps1` | Script build tự động |
| `Installer\JNNToolInstaller\version.json` | Manifest version + downloadUrl |
| `JNNTool.bundle\version.json` | Manifest cho Revit plugin check update |
| `ICON JNN.jpg` | Logo gốc |
| `Installer\JNNToolInstaller\icon.ico` | Icon đã convert (256/48/32/16px) |
