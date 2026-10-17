<#
.SYNOPSIS
    JNNTool - Script dòng lệnh (CLI) tạo và quản lý mã Key bản quyền tự động qua Supabase.

.DESCRIPTION
    Script cho phép Quản trị viên (Admin) tạo nhanh mã Key bản quyền JNNTool chỉ bằng 1 dòng lệnh.
    Tự động ghi vào Database Supabase và tự động sao chép mã Key vào bộ nhớ tạm (Clipboard) của Windows.

.PARAMETER Email
    Địa chỉ Email của khách hàng cần cấp bản quyền (Bắt buộc khi tạo key mới).

.PARAMETER Days
    Số ngày hiệu lực của bản quyền (Mặc định: 365 ngày).

.PARAMETER Type
    Loại bản quyền: 'Annual', 'Trial', 'Lifetime' (Mặc định: 'Annual').

.PARAMETER Secret
    Mật khẩu Quản trị viên (Admin Secret). Nếu không truyền, script tự đọc từ biến môi trường
    $env:JNNTOOL_ADMIN_SECRET hoặc dùng mật khẩu mặc định.

.PARAMETER ResetMachine
    Hủy khóa máy của mã Key đã cấp (Dùng khi khách đổi máy hoặc cài lại Windows). Cần kèm -LicenseKey.

.PARAMETER Renew
    Gia hạn thêm số ngày cho mã Key. Cần kèm -LicenseKey.

.PARAMETER Revoke
    Thu hồi (khóa vĩnh viễn) mã Key. Cần kèm -LicenseKey.

.PARAMETER LicenseKey
    Mã Key bản quyền cần thao tác (Ví dụ: 'JNN-A8F2-9C1E-7D4B').

.EXAMPLE
    # Tạo key 1 năm cho khách:
    .\scripts\create-key.ps1 -Email "customer@gmail.com" -Days 365

.EXAMPLE
    # Tạo key dùng thử 30 ngày:
    .\scripts\create-key.ps1 -Email "trial@gmail.com" -Days 30 -Type "Trial"

.EXAMPLE
    # Hủy khóa máy (Reset Machine ID) khi khách đổi máy tính:
    .\scripts\create-key.ps1 -ResetMachine -LicenseKey "JNN-A8F2-9C1E-7D4B"
#>

[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [string]$Email,

    [Parameter(Position = 1)]
    [int]$Days = 365,

    [Parameter(Position = 2)]
    [ValidateSet("Annual", "Trial", "Lifetime")]
    [string]$Type = "Annual",

    [string]$Secret = $env:JNNTOOL_ADMIN_SECRET,

    [string]$SupabaseUrl = "https://srcijpacycgfkyneoxyu.supabase.co",
    [string]$ApiKey = "sb_publishable_31xEGAXxKSJcJLXGOb5wgw_IXv9E_KH",

    [switch]$ResetMachine,
    [switch]$Renew,
    [switch]$Revoke,
    [string]$LicenseKey
)

# Thiết lập encoding UTF-8 cho console
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# Fallback Secret mặc định nếu chưa cấu hình
if ([string]::IsNullOrWhiteSpace($Secret)) {
    $Secret = "JNN_Admin_Secret_2026_SecureKey"
}

$cleanBaseUrl = $SupabaseUrl.TrimEnd('/')
$headers = @{
    "Content-Type"  = "application/json"
    "apikey"        = $ApiKey
    "Authorization" = "Bearer $ApiKey"
}

# ==============================================================================
# TRƯỜNG HỢP 1: QUẢN LÝ KEY (RESET MÁY / GIA HẠN / THU HỒI)
# ==============================================================================
if ($ResetMachine -or $Renew -or $Revoke) {
    if ([string]::IsNullOrWhiteSpace($LicenseKey)) {
        Write-Host "[!] Lỗi: Bạn phải truyền tham số -LicenseKey khi sử dụng các thao tác quản lý!" -ForegroundColor Red
        Exit 1
    }

    $action = "reset_machine"
    if ($Renew) { $action = "renew" }
    if ($Revoke) { $action = "revoke" }

    $body = @{
        p_license_key   = $LicenseKey.Trim()
        p_action        = $action
        p_days          = $Days
        p_admin_secret  = $Secret
    } | ConvertTo-Json

    try {
        $endpoint = "$cleanBaseUrl/rest/v1/rpc/admin_manage_license"
        $response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body

        if ($response.success) {
            Write-Host ""
            Write-Host "======================================================" -ForegroundColor Green
            Write-Host "  [JNNTool] THAO TÁC QUẢN LÝ THÀNH CÔNG!" -ForegroundColor Green
            Write-Host "======================================================" -ForegroundColor Green
            Write-Host " Mã Key     : $($response.license_key)" -ForegroundColor White
            Write-Host " Hành động  : $action" -ForegroundColor Cyan
            Write-Host " Thông báo  : $($response.message)" -ForegroundColor Yellow
            Write-Host "======================================================" -ForegroundColor Green
            Write-Host ""
        } else {
            Write-Host "[X] Thao tác thất bại: $($response.message)" -ForegroundColor Red
        }
    } catch {
        Write-Host "[X] Lỗi kết nối Supabase: $($_.Exception.Message)" -ForegroundColor Red
    }
    Exit 0
}

# ==============================================================================
# TRƯỜNG HỢP 2: TẠO KEY BẢN QUYỀN MỚI
# ==============================================================================
if ([string]::IsNullOrWhiteSpace($Email)) {
    Write-Host ""
    Write-Host "======================================================" -ForegroundColor Cyan
    Write-Host "           JNNTool Admin License Key Generator        " -ForegroundColor Cyan
    Write-Host "======================================================" -ForegroundColor Cyan
    $Email = Read-Host "Nhập Email khách hàng"
    if ([string]::IsNullOrWhiteSpace($Email)) {
        Write-Host "[!] Email không được để trống." -ForegroundColor Red
        Exit 1
    }
}

$body = @{
    p_email         = $Email.Trim()
    p_days          = $Days
    p_type          = $Type
    p_admin_secret  = $Secret
} | ConvertTo-Json

try {
    Write-Host ">> Đang kết nối Supabase để tạo key cho: $Email..." -ForegroundColor Gray
    $endpoint = "$cleanBaseUrl/rest/v1/rpc/admin_generate_key"
    $response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers $headers -Body $body

    if ($response.success) {
        $key = $response.license_key
        $expires = $response.expires_at

        # Định dạng ngày hết hạn
        $expiresFormatted = $expires
        try {
            $dt = [DateTime]::Parse($expires)
            $expiresFormatted = $dt.ToString("dd/MM/yyyy HH:mm")
        } catch {}

        # Tự động sao chép mã Key vào bộ nhớ tạm (Clipboard)
        try {
            Set-Clipboard -Value $key
            $copiedNotice = ">> [OK] Đã tự động sao chép mã Key vào Clipboard!"
        } catch {
            $copiedNotice = ">> Vui lòng bôi đen và sao chép mã Key ở trên."
        }

        Write-Host ""
        Write-Host "======================================================" -ForegroundColor Green
        Write-Host "  [JNNTool] TẠO KEY BẢN QUYỀN THÀNH CÔNG!" -ForegroundColor Green
        Write-Host "======================================================" -ForegroundColor Green
        Write-Host " Email       : $($response.assigned_email)" -ForegroundColor White
        Write-Host " License Key : $key" -ForegroundColor Yellow
        Write-Host " Loại Key    : $($response.license_type)" -ForegroundColor White
        Write-Host " Hạn Dùng    : $($response.days) Ngày (Đến: $expiresFormatted)" -ForegroundColor Cyan
        Write-Host " Khóa Máy    : Chưa khóa (Sẽ tự khóa vào máy khách khi nhập)" -ForegroundColor Gray
        Write-Host "======================================================" -ForegroundColor Green
        Write-Host $copiedNotice -ForegroundColor Green
        Write-Host ""

        # Mẫu tin nhắn gửi khách
        $msgTemplate = "Chào bạn, JNNTool gửi bạn thông tin kích hoạt bản quyền:`n" +
            "- Email tài khoản : $($response.assigned_email)`n" +
            "- Mã Key kích hoạt: $key`n" +
            "- Hạn sử dụng     : $expiresFormatted ($($response.days) ngày)`n" +
            "Hướng dẫn: Trên Revit, vào tab JNNTool -> bấm Account / License -> nhập Email và dán mã Key trên rồi bấm Activate License."

        Write-Host "Tin nhắn gửi khách (Gợi ý):" -ForegroundColor DarkGray
        Write-Host $msgTemplate -ForegroundColor DarkCyan
        Write-Host ""

    } else {
        Write-Host ""
        Write-Host "[X] Không thể tạo key: $($response.message)" -ForegroundColor Red
        Write-Host ""
    }
} catch {
    Write-Host ""
    Write-Host "[X] Lỗi kết nối Supabase: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}
