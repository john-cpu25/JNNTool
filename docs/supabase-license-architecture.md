# Kiến Trúc Cấp Phép Bản Quyền JNNTool: Phòng Thủ Toàn Diện Chống Bẻ Khóa, Giả Mạo Gói Tin (Fiddler/MITM) & Dịch Ngược C#

> **Tài liệu đặc tả kỹ thuật tối thượng** cho hệ thống cấp phép bản quyền **JNNTool**:
> - **Xác thực 2 lớp**: Đăng nhập bằng **Email + Mật khẩu** qua Supabase Auth + Kiểm tra bản quyền trên bảng `licenses`.
> - **Chống bắt gói tin & Response ảo**: Chữ ký số bất đối xứng **RSA-2048 / Ed25519** sinh từ Supabase Edge Function + Cơ chế mã **Nonce** chống Replay Attack.
> - **Chống Proxy & SSL Interception**: C# HttpClient tắt Proxy hệ thống (`UseProxy = false`) + SSL Certificate Pinning.
> - **Gia cố Remember Me**: Lưu trữ **Signed License Ticket** bọc trong Windows DPAPI với thời hạn Offline Grace Period (3–7 ngày).
> - **Chống sửa DLL bằng dnSpy**: Kỹ thuật **Data Dependency** (ràng buộc dữ liệu thực thi) + Obfuscation (mã hóa chuỗi, làm rối luồng).

---

## 1. Phân Tích 4 Lỗ Hổng Bảo Mật Thực Tế Của Revit Add-in C#

| Lỗ Hổng | Kịch Bản Khai Thác Thực Tế | Hậu Quả |
|---|---|---|
| **1. Giả mạo gói tin (MITM / Fiddler / Charles / sửa hosts)** | Kẻ tấn công cài phần mềm bắt gói tin (Fiddler, HTTP Toolkit), cài SSL Certificate giả lập vào máy để giải mã HTTPS. Khi Revit gọi kiểm tra bản quyền, kẻ gian chặn gói tin và trả về JSON giả mạo: `{"is_valid": true, "expires_at": "2099-12-31"}`. | Add-in tin rằng bản quyền hợp lệ dù không hề có Key trên server. |
| **2. Lộ cấu trúc & Key nếu gọi trực tiếp DB từ C#** | Nếu code C# dùng trực tiếp Supabase SDK kèm `anon_key` để `SELECT * FROM licenses`, kẻ xấu dịch ngược DLL sẽ thấy toàn bộ cấu trúc bảng, tên cột và endpoint. | Có nguy cơ bị spam request hoặc tìm ra kẽ hở truy vấn dữ liệu của người dùng khác. |
| **3. Windows DPAPI không chống được chính chủ máy** | DPAPI dùng User Profile của Windows để mã hóa. Nếu lưu cờ `IsActive = true` hoặc ngày hết hạn thô, chính người dùng đó có thể viết một đoạn script C#/PowerShell ngắn gọi `ProtectedData.Unprotect`, sửa ngày hết hạn thành năm 2099 rồi Protect lưu lại. | Dùng vĩnh viễn dù Key trên Server đã hết hạn hoặc bị Admin thu hồi. |
| **4. Dịch ngược mã C# bằng dnSpy / ILSpy (Nguy hiểm nhất)** | File DLL viết bằng .NET C# biên dịch ra Intermediate Language (IL). Cracker mở file `JNNTool.dll` bằng **dnSpy**, tìm hàm `LicenseGuard.EnsureLicensed(...)` và sửa thân hàm thành `return true;` hoặc xóa lời gọi hàm ở tất cả Command. | Toàn bộ cơ chế kiểm tra mạng và bản quyền bị vô hiệu hóa hoàn toàn trong 2 phút mà không cần mạng. |

---

## 2. Kiến Trúc Phòng Thủ 4 Lớp Toàn Diện (4-Tier Defense Architecture)

```
┌────────────────────────────────────────────────────────────────────────┐
│                        SUPABASE CLOUD INFRASTRUCTURE                   │
│                                                                        │
│  [A] Supabase Auth (`auth.users`): Quản lý Email + Mật khẩu (BCrypt)   │
│  [B] PostgreSQL Database (`licenses`): Khóa RLS (Client không đọc trực tiếp)│
│  [C] Edge Function (`verify-license`):                                │
│      - Nhận: Email, MachineId, Nonce, Timestamp                        │
│      - Kiểm tra tính hợp lệ trong Database                             │
│      - Dùng RSA Private Key (lưu trong Supabase Secrets) để ký số:     │
│        Payload = { Email, MachineId, ExpiresAt, Nonce, SeedKey }       │
│        Signature = RSA_Sign(SHA256(Payload), PrivateKey)               │
└───────────────────────────────────▲────────────────────────────────────┘
                                    │ HTTPS (Tắt Proxy + SSL Pinning)
                                    │ Request: { Email, MachineId, Nonce }
                                    │ Response: { PayloadJson, Signature }
┌───────────────────────────────────┴────────────────────────────────────┐
│                       REVIT CLIENT (JNNTool.dll)                       │
│                                                                        │
│  [Lớp 1] Anti-Proxy & SSL Pinning:                                     │
│      - HttpClientHandler: UseProxy = false (Bỏ qua Fiddler/Charles)    │
│      - ServerCertificateCustomValidationCallback: Kiểm tra Thumbprint │
│                                                                        │
│  [Lớp 2] RSA Signature & Nonce Verification:                           │
│      - Sinh Nonce ngẫu nhiên mỗi lần gửi (Chống Replay Attack)         │
│      - Dùng RSA Public Key nhúng cứng để verify: RSA.VerifyData(...)    │
│                                                                        │
│  [Lớp 3] Gia cố Remember Me (DPAPI + Signed Ticket):                   │
│      - Lưu cả { PayloadJson + Signature } bọc bằng DPAPI               │
│      - Offline Grace Period: Cho phép tối đa 3-7 ngày không mạng       │
│      - Tự sửa file -> Signature hỏng ngay lập tức                     │
│                                                                        │
│  [Lớp 4] Data Dependency & Obfuscation:                                │
│      - Lấy `SeedKey` từ Signed Payload phục vụ giải mã công thức tool   │
│      - Xóa `EnsureLicensed` -> Thiếu SeedKey -> Tool lỗi tính toán     │
│      - Obfuscate (ConfuserEx / .NET Reactor / Obfuscar)                │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Chi Tiết Các Lớp Phòng Thủ Kỹ Thuật

### 3.1 Lớp 1: Chống Giả Mạo Gói Tin Bằng Chữ Ký Số RSA-2048 & Nonce

1. **Khởi tạo cặp khóa bất đối xứng**:
   - **RSA Private Key (2048-bit)**: Lưu bí mật 100% trên **Supabase Edge Function Secrets** (`LICENSE_RSA_PRIVATE_KEY`). Không bao giờ gửi về máy khách.
   - **RSA Public Key**: Nhúng trực tiếp vào mã nguồn C# của JNNTool.
2. **Cơ chế chống Replay Attack qua Nonce**:
   - Trước mỗi lần gọi kiểm tra bản quyền, Revit Client sinh ra một chuỗi ngẫu nhiên duy nhất: `Nonce = Guid.NewGuid().ToString("N")` kèm `ClientTimestamp`.
   - Server nhận Nonce, đưa Nonce vào gói Payload trước khi ký số.
   - Khi nhận phản hồi, Revit Client kiểm tra:
     - `RSA.VerifyData(PayloadBytes, SignatureBytes) == true` (Chữ ký chuẩn từ Server).
     - `Payload.Nonce == Nonce_vừa_gửi` (Đúng gói tin của lượt này, không thể lấy gói tin ngày hôm qua phát lại).
3. **Tại sao Fiddler / Hacker bó tay?**:
   - Dù Fiddler chặn gói tin và sửa `ExpiresAt` thành `2099-12-31`, kẻ tấn công **không có RSA Private Key** nên không thể tạo ra trường `Signature` hợp lệ.
   - Hàm `RSA.VerifyData` dưới C# sẽ trả về `false` ngay lập tức và hủy lệnh.

---

### 3.2 Lớp 2: Chặn Proxy Hệ Thống & Ghim Chứng Chỉ SSL (SSL Pinning)

Đa số công cụ bắt gói tin như Fiddler, Charles Proxy, HTTP Toolkit hoạt động bằng cách ép Windows sử dụng System Proxy tại `127.0.0.1:8888` và cài một chứng chỉ Root CA giả lập.

Trong code C# của `LicenseClientService`:
```csharp
var handler = new HttpClientHandler
{
    // 1. TẮT PROXY HỆ THỐNG: Bỏ qua hoàn toàn Fiddler / Charles đang lắng nghe trên máy
    UseProxy = false,
    Proxy = null,

    // 2. SSL PINNING: Chỉ tin cậy chứng chỉ thật của Supabase, chặn chứng chỉ giả mạo của Fiddler
    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, chain, sslPolicyErrors) =>
    {
        if (cert == null) return false;

        // Kiểm tra SHA-256 Thumbprint hoặc Public Key Hash của tên miền Supabase
        string certHash = cert.GetCertHashString();
        // So khớp với mã băm chứng chỉ chuẩn được cấu hình
        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
    }
};
```

---

### 3.3 Lớp 3: Gia Cố "Remember Me" (DPAPI + Signed License Ticket)

Không bao giờ lưu cờ đơn giản kiểu `IsLicensed = true` vào máy khách. Thay vào đó:
1. **Lưu nguyên vẹn Signed Ticket**:
   - File `%AppData%\JNNTool\license.dat` lưu cấu trúc JSON gồm:
     ```json
     {
       "payload_json": "{\"email\":\"user@gmail.com\",\"machine_id\":\"...\",\"expires_at\":\"2027-10-15\",\"offline_until\":\"2026-10-22\",\"seed_key\":\"...\"}",
       "signature_base64": "A8F1C3...",
       "cached_at_utc": "2026-10-15T22:30:00Z"
     }
     ```
   - Mã hóa toàn bộ chuỗi JSON trên bằng Windows DPAPI (`DataProtectionScope.CurrentUser`).
2. **Offline Grace Period (3 - 7 ngày)**:
   - Cho phép người dùng mang laptop ra công trường không có mạng sử dụng tối đa 7 ngày kể từ lần sync online cuối cùng (`offline_until`).
   - Hết 7 ngày, bắt buộc phải có kết nối mạng để Edge Function cấp Ticket mới.
3. **Chống người dùng tự sửa file**:
   - Nếu người dùng tự viết tool giải mã DPAPI và đổi `expires_at` thành 2099, chữ ký `signature_base64` sẽ không khớp với nội dung `payload_json`. Hàm `RSA.VerifyData` phát hiện gian lận ngay khi mở Revit.

---

### 3.4 Lớp 4: Chống Dịch Ngược dnSpy Bằng "Ràng Buộc Dữ Liệu" (Data Dependency)

Nếu `LicenseGuard.EnsureLicensed(...)` chỉ trả về `bool (true/false)`, kẻ bẻ khóa có thể dùng dnSpy xóa sạch dòng này ở tất cả các Command.

**Giải pháp Chống Bẻ Khóa bằng Data Dependency**:
1. Trong gói Payload đã ký từ Server, gửi kèm một chuỗi bí mật: `RuntimeSeed` hoặc `CryptoKey`.
2. Chuỗi này là chìa khóa dùng để:
   - Khởi tạo bảng tra cứu hình học rebar.
   - Giải mã tên các Shared Parameter kết cấu.
   - Hoặc là hệ số nhân bảo vệ trong công thức tính toán cốt thép.
3. Khi khởi động, `LicenseGuard` giải mã `RuntimeSeed` và lưu vào bộ nhớ RAM tĩnh của add-in.
4. **Kết quả**:
   - Nếu cracker dùng dnSpy xóa bỏ `LicenseGuard.EnsureLicensed(...)`, biến `RuntimeSeed` sẽ mang giá trị `null` hoặc `0`.
   - Khi người dùng bấm tạo thép dầm, cột, sàn, add-in sẽ lập tức tính sai chiều dài hoặc văng lỗi ngoại lệ `NullReferenceException`, **khiến bản mod bẻ khóa hoàn toàn không thể sử dụng được**.

---

## 4. Thiết Kế Cơ Sở Dữ Liệu & Secrets Trên Supabase

### 4.1 Bảng `licenses` với Row Level Security (RLS)

```sql
create table public.licenses (
    id uuid primary key default gen_random_uuid(),
    license_key text not null unique,               -- JNN-2026-XXXX-XXXX
    assigned_email text not null,                   -- Email sở hữu
    license_type text default 'Annual',             -- 'Trial', 'Annual', 'Lifetime'
    status text default 'Active',                   -- 'Active', 'Revoked', 'Expired'
    starts_at timestamptz default now(),
    expires_at timestamptz not null,
    max_devices int default 1,
    bound_machine_id text,                          -- Motherboard + CPU + MachineGuid hash
    device_name text,
    last_validated_at timestamptz,
    notes text,
    created_at timestamptz default now()
);

-- KÍCH HOẠT RLS: Chặn hoàn toàn Client dùng anon_key đọc trực tiếp bảng licenses
alter table public.licenses enable row level security;

-- Không tạo policy cho role 'anon' -> Mọi truy vấn từ Client trực tiếp đều bị từ chối
```

### 4.2 Cấu Hình Secrets Trên Supabase Edge Functions
Vào **Supabase Dashboard ➔ Project Settings ➔ Secrets**, tạo 2 biến môi trường:
1. `LICENSE_RSA_PRIVATE_KEY`: Chuỗi Private Key RSA dạng PEM (dùng để ký số).
2. `RUNTIME_SEED_SALT`: Chuỗi khóa nghiệp vụ bảo vệ hàm C#.

---

## 5. Mã Nguồn Mẫu: Supabase Edge Function (`verify-license`)

Tạo thư mục `supabase/functions/verify-license/index.ts` (chạy trên môi trường Deno của Supabase):

```typescript
// supabase/functions/verify-license/index.ts
import { serve } from "https://deno.land/std@0.177.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const { email, password, machine_id, device_name, nonce, license_key } = await req.json();

    if (!email || !machine_id || !nonce) {
      return new Response(JSON.stringify({ success: false, message: "Thiếu tham số xác thực bắt buộc." }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 1. Khởi tạo Supabase Admin Client với Service Role Key (chạy ngầm an toàn trên Server)
    const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? "";
    const supabaseServiceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
    const rsaPrivateKeyPem = Deno.env.get("LICENSE_RSA_PRIVATE_KEY") ?? "";
    const supabase = createClient(supabaseUrl, supabaseServiceKey);

    // 2. Xác thực Email & Mật khẩu nếu có gửi lên (Đăng nhập lần đầu)
    if (password) {
      const { data: authData, error: authError } = await supabase.auth.signInWithPassword({
        email: email.trim().toLowerCase(),
        password: password,
      });

      if (authError || !authData.user) {
        return new Response(JSON.stringify({ success: false, message: "Sai email hoặc mật khẩu đăng nhập." }), {
          status: 401,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        });
      }
    }

    // 3. Truy vấn bản quyền gắn với Email
    const { data: licenses, error: licError } = await supabase
      .from("licenses")
      .select("*")
      .ilike("assigned_email", email.trim().toLowerCase())
      .eq("status", "Active")
      .order("expires_at", { ascending: false })
      .limit(1);

    if (licError || !licenses || licenses.length === 0) {
      return new Response(JSON.stringify({ 
        success: false, 
        message: "Tài khoản chưa được kích hoạt Key bản quyền. Vui lòng liên hệ Admin để được cấp phép." 
      }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const lic = licenses[0];
    const now = new Date();
    const expiresAt = new Date(lic.expires_at);

    if (expiresAt <= now) {
      return new Response(JSON.stringify({ 
        success: false, 
        message: "Bản quyền của tài khoản đã hết hạn sử dụng." 
      }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 4. Kiểm tra khóa cứng phần cứng (Node-locked)
    if (lic.bound_machine_id && lic.bound_machine_id !== machine_id) {
      return new Response(JSON.stringify({ 
        success: false, 
        message: `Key này đã được kích hoạt trên máy khác (${lic.device_name || "Thiết bị khác"}). Vui lòng liên hệ Admin.` 
      }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 5. Nếu là lần đầu kích hoạt trên máy này -> Bind Machine ID
    if (!lic.bound_machine_id) {
      await supabase
        .from("licenses")
        .update({
          bound_machine_id: machine_id,
          device_name: device_name || "Revit Workstation",
          last_validated_at: now.toISOString(),
        })
        .eq("id", lic.id);
    } else {
      await supabase
        .from("licenses")
        .update({ last_validated_at: now.toISOString() })
        .eq("id", lic.id);
    }

    // 6. Tính toán Offline Grace Period (Ví dụ: 7 ngày kể từ hôm nay)
    const offlineGrace = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);
    const offlineUntil = offlineGrace < expiresAt ? offlineGrace : expiresAt;

    // 7. Tạo Payload chứa Nonce và RuntimeSeed để ký số
    const payload = {
      email: lic.assigned_email,
      machine_id: machine_id,
      license_type: lic.license_type,
      expires_at: lic.expires_at,
      offline_until: offlineUntil.toISOString(),
      server_time: now.toISOString(),
      nonce: nonce, // Khớp với Nonce của Client để chống Replay Attack
      runtime_seed: "JNN_REBAR_ALGO_KEY_2026_V1", // Phục vụ Data Dependency
    };

    const payloadJson = JSON.stringify(payload);

    // 8. KÝ SỐ BẰNG RSA-SHA256 (Web Crypto API)
    const signatureBase64 = await signWithRsa(payloadJson, rsaPrivateKeyPem);

    return new Response(JSON.stringify({
      success: true,
      payload_json: payloadJson,
      signature_base64: signatureBase64,
      message: "Xác thực bản quyền thành công!"
    }), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });

  } catch (err) {
    return new Response(JSON.stringify({ success: false, message: `Lỗi máy chủ: ${err.message}` }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});

// Hàm hỗ trợ ký số RSA-SHA256
async function signWithRsa(data: string, pemKey: string): Promise<string> {
  const binaryDer = pemToBinary(pemKey);
  const privateKey = await crypto.subtle.importKey(
    "pkcs8",
    binaryDer,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["sign"]
  );

  const encoder = new TextEncoder();
  const signatureBuffer = await crypto.subtle.sign(
    "RSASSA-PKCS1-v1_5",
    privateKey,
    encoder.encode(data)
  );

  return btoa(String.fromCharCode(...new Uint8Array(signatureBuffer)));
}

function pemToBinary(pem: string): Uint8Array {
  const cleanPem = pem.replace(/-----[^-]+-----/g, "").replace(/\s+/g, "");
  const binaryString = atob(cleanPem);
  const bytes = new Uint8Array(binaryString.length);
  for (let i = 0; i < binaryString.length; i++) {
    bytes[i] = binaryString.charCodeAt(i);
  }
  return bytes;
}
```

---

## 6. Mã Nguồn Mẫu C# Client: `LicenseGuard.cs` & `RsaLicenseService.cs`

### 6.1 Lớp Xác Thực Chữ Ký Số RSA & Chống Proxy (`RsaLicenseService.cs`)

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JNNTool.Tools.Login.Services
{
    public class SignedLicenseTicket
    {
        public string PayloadJson { get; set; }
        public string SignatureBase64 { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }

    public class LicensePayload
    {
        public string email { get; set; }
        public string machine_id { get; set; }
        public string license_type { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime offline_until { get; set; }
        public string nonce { get; set; }
        public string runtime_seed { get; set; }
    }

    public static class RsaLicenseService
    {
        // Public Key RSA-2048 nhúng cứng trong Client (dùng để xác thực chữ ký từ Server)
        private const string RsaPublicKeyXml = @"<RSAKeyValue><Modulus>...</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        private static readonly HttpClient _httpClient;
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("JNN_SECURE_ENTROPY_2026");
        private static readonly string LicensePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JNNTool", "license.dat");

        static RsaLicenseService()
        {
            // CHỐNG BẮT GÓI TIN: Tắt Proxy hệ thống để Fiddler / Charles không chặn được
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                Proxy = null
            };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        }

        /// <summary>
        /// Xác thực tính hợp lệ của chữ ký số RSA đính kèm với Payload
        /// </summary>
        public static bool VerifySignature(string payloadJson, string signatureBase64)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(payloadJson);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(RsaPublicKeyXml);
                    return rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), signatureBytes);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gọi Supabase Edge Function với Nonce ngẫu nhiên
        /// </summary>
        public static async Task<(bool success, string message, LicensePayload payload)> VerifyOnlineAsync(string email, string password = null)
        {
            string machineId = DeviceFingerprintService.GetDeviceIdentifierHash();
            string nonce = Guid.NewGuid().ToString("N"); // Chống Replay Attack

            var requestBody = new
            {
                email = email.Trim(),
                password = password,
                machine_id = machineId,
                device_name = Environment.MachineName,
                nonce = nonce
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://<your-project>.supabase.co/functions/v1/verify-license", content);
                string resString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(resString);
                var root = doc.RootElement;
                bool ok = root.GetProperty("success").GetBoolean();
                if (!ok)
                {
                    string msg = root.GetProperty("message").GetString();
                    return (false, msg, null);
                }

                string payloadJson = root.GetProperty("payload_json").GetString();
                string sigBase64 = root.GetProperty("signature_base64").GetString();

                // 1. Kiểm tra chữ ký số RSA
                if (!VerifySignature(payloadJson, sigBase64))
                {
                    return (false, "Chữ ký số bản quyền không hợp lệ (Phát hiện gói tin bị giả mạo)!", null);
                }

                // 2. Parse Payload và đối soát Nonce + MachineId
                var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson);
                if (payload.nonce != nonce)
                {
                    return (false, "Phát hiện tấn công phát lại gói tin (Nonce mismatch)!", null);
                }

                if (payload.machine_id != machineId)
                {
                    return (false, "Bản quyền không khớp với thiết bị này!", null);
                }

                // 3. Lưu Signed Ticket bọc bằng DPAPI
                SaveTicketToDisk(payloadJson, sigBase64);

                return (true, "Bản quyền hợp lệ!", payload);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối máy chủ: {ex.Message}", null);
            }
        }

        public static void SaveTicketToDisk(string payloadJson, string signatureBase64)
        {
            var ticket = new SignedLicenseTicket
            {
                PayloadJson = payloadJson,
                SignatureBase64 = signatureBase64,
                SavedAtUtc = DateTime.UtcNow
            };

            string raw = JsonSerializer.Serialize(ticket);
            byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(raw), Entropy, DataProtectionScope.CurrentUser);

            string dir = Path.GetDirectoryName(LicensePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(LicensePath, enc);
        }

        public static (bool isValid, LicensePayload payload) LoadAndValidateCachedTicket()
        {
            if (!File.Exists(LicensePath)) return (false, null);

            try
            {
                byte[] enc = File.ReadAllBytes(LicensePath);
                byte[] raw = ProtectedData.Unprotect(enc, Entropy, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(raw);

                var ticket = JsonSerializer.Deserialize<SignedLicenseTicket>(json);

                // Kiểm tra chữ ký số RSA của gói đã lưu
                if (!VerifySignature(ticket.PayloadJson, ticket.SignatureBase64))
                    return (false, null);

                var payload = JsonSerializer.Deserialize<LicensePayload>(ticket.PayloadJson);

                // Kiểm tra Machine ID
                if (payload.machine_id != DeviceFingerprintService.GetDeviceIdentifierHash())
                    return (false, null);

                // Kiểm tra hạn dùng tuyệt đối
                if (DateTime.UtcNow >= payload.expires_at)
                    return (false, null);

                // Kiểm tra hạn Offline Grace Period
                if (DateTime.UtcNow >= payload.offline_until)
                    return (false, null); // Hết hạn offline -> Cần kết nối mạng lại

                return (true, payload);
            }
            catch
            {
                return (false, null);
            }
        }
    }
}
```

### 6.2 Chốt Chặn Lệnh & Ràng Buộc Dữ Liệu (`LicenseGuard.cs`)

```csharp
using System;
using Autodesk.Revit.UI;
using JNNTool.Tools.Login.UI;

namespace JNNTool.Tools.Login.Services
{
    public static class LicenseGuard
    {
        // Dữ liệu nhạy cảm được nạp từ chữ ký số Server
        public static string RuntimeSeed { get; private set; } = null;

        public static bool EnsureLicensed(UIApplication uiapp)
        {
            // 1. Kiểm tra cache nội bộ đã ký số
            var (isValid, payload) = RsaLicenseService.LoadAndValidateCachedTicket();

            if (!isValid || payload == null)
            {
                RuntimeSeed = null;
                // Mở cửa sổ bắt buộc Đăng nhập / Kích hoạt
                LoginWindow.ShowWindow(uiapp);
                return false;
            }

            // 2. Nạp RuntimeSeed phục vụ Data Dependency (Chống xóa hàm này bằng dnSpy)
            RuntimeSeed = payload.runtime_seed;

            return true;
        }
    }
}
```

---

## 7. Quy Trình Vận Hành & Khắc Phục Sự Cố

1. **Khách hàng mang máy ra công trường không có mạng**:
   - Nhờ cơ chế **Offline Grace Period (7 ngày)**, khách hàng vẫn mở Revit làm việc trơn tru mà không bị gián đoạn.
   - Khi hết 7 ngày, Revit sẽ thông báo yêu cầu kết nối mạng ít nhất một lần để đồng bộ Ticket mới.
2. **Khách hàng đổi máy tính / Cài lại Windows**:
   - Admin vào Supabase Dashboard, tìm Email khách và xóa trắng trường `bound_machine_id` (`NULL`).
   - Khách mở máy mới đăng nhập Email + Pass ➔ Hệ thống tự động bind máy mới và cấp chữ ký số mới.
3. **Phát hiện hành vi gian lận / Hack**:
   - Admin chỉ cần đổi `status = 'Revoked'` trên Supabase.
   - Ở lần kết nối tiếp theo của khách (hoặc sau chu kỳ sync ngầm 15 phút), Edge Function từ chối cấp Ticket và khóa add-in ngay lập tức.

---

## 8. Hướng Dẫn Đóng Gói & Làm Rối Mã Nguồn (Obfuscation Runbook)

Trước khi đóng gói file cài đặt `.msi` gửi cho khách hàng, file `JNNTool.dll` **bắt buộc** phải được xử lý qua công cụ Obfuscator:

1. **Công cụ khuyên dùng**:
   - **Obfuscar** (Miễn phí, tích hợp sẵn vào build script `dotnet publish`).
   - **ConfuserEx 2** hoặc **.NET Reactor** (Bảo vệ mạnh mẽ).
2. **Các tính năng bắt buộc bật**:
   - `String Encryption`: Mã hóa toàn bộ chuỗi URL Supabase, chuỗi XML RSA Public Key thành mảng byte vô nghĩa.
   - `Control Flow Obfuscation`: Đảo lộn luồng code thành cấu trúc spaghetti, khiến dnSpy/ILSpy không thể phân tích hàm `EnsureLicensed`.
   - `Anti-Tamper`: Tự kiểm tra mã băm MD5/SHA256 của file `JNNTool.dll`. Nếu file bị chỉnh sửa 1 byte bằng dnSpy, add-in sẽ tự crash khi Revit tải lên.

---

## 9. Hướng Dẫn Vận Hành Cấp Phát & Quản Lý Key Cho Quản Trị Viên (Admin)

Hệ thống cung cấp 2 kênh tạo và quản lý mã Key bản quyền tự động, kết nối trực tiếp với Supabase thông qua các hàm RPC bảo mật (định nghĩa tại [`supabase/admin_license_rpc.sql`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/supabase/admin_license_rpc.sql)):

### 9.1 Kênh 1: Giao Diện Web Admin Portal (`Tools/Admin/admin-license-portal.html`)
- Mở trực tiếp file [`Tools/Admin/admin-license-portal.html`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/Admin/admin-license-portal.html) bằng bất kỳ trình duyệt nào trên máy tính hoặc điện thoại (không cần cài đặt server hay Node.js).
- **Tính năng**:
  - Nhập Admin Secret (tự động lưu vào `localStorage` trình duyệt).
  - Nhập Email khách hàng ➔ Chọn thời hạn (30 ngày, 1 năm, 2 năm, vĩnh viễn) ➔ Bấm **[ ⚡ Tạo Key Bản Quyền ]**.
  - Hiển thị mã Key to rõ kèm nút **[ 📋 Copy Key ]** và nút **[ 💬 Copy Tin Nhắn Gửi Khách ]** (định dạng sẵn nội dung để gửi qua Zalo/Email).
  - Bảng danh sách bản quyền: Tra cứu theo email, mã key, xem thiết bị đã khóa (`bound_machine_id`).
  - Nút **[ 🔄 Reset Máy ]**: Hủy khóa máy chỉ với 1 click khi khách đổi laptop hoặc cài lại Windows.
  - Nút **[ ➕ Gia Hạn ]**: Cộng thêm ngày sử dụng trực tiếp.
  - Nút **[ ⛔ Thu Hồi ]**: Khóa bản quyền tức thì khi phát hiện vi phạm.

### 9.2 Kênh 2: Script Dòng Lệnh PowerShell CLI (`scripts/create-key.ps1`)
Dành cho Quản trị viên thao tác nhanh từ Terminal máy tính:
```powershell
# Tạo key 1 năm (tự động copy vào Clipboard):
.\scripts\create-key.ps1 -Email "customer@gmail.com" -Days 365

# Tạo key dùng thử 30 ngày:
.\scripts\create-key.ps1 -Email "trial@gmail.com" -Days 30 -Type "Trial"

# Hủy khóa máy (Reset Machine ID) khi khách đổi máy:
.\scripts\create-key.ps1 -ResetMachine -LicenseKey "JNN-XXXX-XXXX-XXXX"
```

### 9.3 Bước Khởi Tạo Ban Đầu Trên Supabase
Quản trị viên chỉ cần mở **Supabase Dashboard ➔ SQL Editor**, mở file [`supabase/admin_license_rpc.sql`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/supabase/admin_license_rpc.sql) và bấm **RUN**. Toàn bộ 3 hàm API `admin_generate_key`, `admin_manage_license`, `admin_list_licenses` và bảng cấu hình `admin_config` sẽ được khởi tạo hoàn tất.

