-- ==============================================================================
-- JNNTool License Management System - Supabase Backend RPC
-- File: supabase/admin_license_rpc.sql
-- Mục đích: Cung cấp API bảo mật cho Quản trị viên (Admin) tạo Key, quản lý thiết bị,
--          gia hạn và thu hồi bản quyền từ xa qua Web Portal & CLI PowerShell.
-- ==============================================================================

-- 1. BẢNG CẤU HÌNH QUẢN TRỊ (ADMIN CONFIG)
-- Lưu trữ Admin Secret và các thiết lập nhạy cảm của hệ thống.
-- Bật RLS và không cấp quyền cho anon/authenticated để bảo vệ tuyệt đối.
CREATE TABLE IF NOT EXISTS public.admin_config (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    description TEXT,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

ALTER TABLE public.admin_config ENABLE ROW LEVEL SECURITY;

-- Khởi tạo Admin Secret mặc định (Khuyến nghị Admin đổi mật khẩu này sau khi chạy)
INSERT INTO public.admin_config (key, value, description)
VALUES ('admin_secret', 'JNN_Admin_Secret_2026_SecureKey', 'Mật khẩu quản trị viên để tạo và quản lý key')
ON CONFLICT (key) DO NOTHING;


-- ==============================================================================
-- 2. HÀM KIỂM TRA MẬT KHẨU ADMIN (HELPER FUNCTION)
-- ==============================================================================
CREATE OR REPLACE FUNCTION public.check_admin_secret(p_secret TEXT)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_correct_secret TEXT;
BEGIN
    IF p_secret IS NULL OR TRIM(p_secret) = '' THEN
        RETURN FALSE;
    END IF;

    SELECT value INTO v_correct_secret
    FROM public.admin_config
    WHERE key = 'admin_secret';

    IF v_correct_secret IS NULL THEN
        -- Fallback mật khẩu mặc định nếu bảng config chưa có dòng
        v_correct_secret := 'JNN_Admin_Secret_2026_SecureKey';
    END IF;

    RETURN (TRIM(p_secret) = TRIM(v_correct_secret));
END;
$$;


-- ==============================================================================
-- 3. HÀM TẠO KEY BẢN QUYỀN MỚI (RPC: admin_generate_key)
-- Sinh mã Key chuẩn Base32 Crockford (loại bỏ ký tự dễ nhầm lẫn: 0, O, 1, I, L)
-- Định dạng chuẩn: JNN-XXXX-XXXX-XXXX
-- ==============================================================================
CREATE OR REPLACE FUNCTION public.admin_generate_key(
    p_email TEXT,
    p_days INT DEFAULT 365,
    p_type TEXT DEFAULT 'Annual',
    p_admin_secret TEXT DEFAULT ''
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_is_admin BOOLEAN;
    v_charset TEXT := '23456789ABCDEFGHJKMNPQRSTVWXYZ';
    v_char_len INT := 31;
    v_key TEXT;
    v_part1 TEXT := '';
    v_part2 TEXT := '';
    v_part3 TEXT := '';
    v_i INT;
    v_expires_at TIMESTAMPTZ;
    v_clean_email TEXT;
BEGIN
    -- 1. Xác thực quyền Admin
    v_is_admin := public.check_admin_secret(p_admin_secret);
    IF NOT v_is_admin THEN
        RETURN json_build_object(
            'success', false,
            'message', 'Mật khẩu quản trị viên (Admin Secret) không chính xác!'
        );
    END IF;

    -- 2. Chuẩn hóa Email
    v_clean_email := LOWER(TRIM(p_email));
    IF v_clean_email IS NULL OR v_clean_email = '' OR POSITION('@' IN v_clean_email) = 0 THEN
        RETURN json_build_object(
            'success', false,
            'message', 'Địa chỉ Email khách hàng không hợp lệ!'
        );
    END IF;

    -- 3. Sinh mã Key ngẫu nhiên không trùng lặp (JNN-XXXX-XXXX-XXXX)
    LOOP
        v_part1 := '';
        v_part2 := '';
        v_part3 := '';

        FOR v_i IN 1..4 LOOP
            v_part1 := v_part1 || SUBSTRING(v_charset FROM (FLOOR(RANDOM() * v_char_len)::INT + 1) FOR 1);
            v_part2 := v_part2 || SUBSTRING(v_charset FROM (FLOOR(RANDOM() * v_char_len)::INT + 1) FOR 1);
            v_part3 := v_part3 || SUBSTRING(v_charset FROM (FLOOR(RANDOM() * v_char_len)::INT + 1) FOR 1);
        END LOOP;

        v_key := 'JNN-' || v_part1 || '-' || v_part2 || '-' || v_part3;

        -- Kiểm tra trùng lặp trong bảng licenses
        IF NOT EXISTS (SELECT 1 FROM public.licenses WHERE license_key = v_key) THEN
            EXIT;
        END IF;
    END LOOP;

    -- 4. Tính toán thời hạn hết hạn
    IF p_days <= 0 THEN
        p_days := 365;
    END IF;
    v_expires_at := NOW() + (p_days || ' days')::INTERVAL;

    -- 5. Ghi vào bảng licenses
    INSERT INTO public.licenses (
        license_key,
        assigned_email,
        license_type,
        status,
        starts_at,
        expires_at,
        max_devices,
        bound_machine_id,
        device_name,
        created_at
    ) VALUES (
        v_key,
        v_clean_email,
        COALESCE(p_type, 'Annual'),
        'Active',
        NOW(),
        v_expires_at,
        1,
        NULL, -- Chưa khóa máy (sẽ tự khóa vào máy đầu tiên khi người dùng bấm kích hoạt)
        NULL,
        NOW()
    );

    RETURN json_build_object(
        'success', true,
        'license_key', v_key,
        'assigned_email', v_clean_email,
        'license_type', COALESCE(p_type, 'Annual'),
        'days', p_days,
        'starts_at', NOW(),
        'expires_at', v_expires_at,
        'message', 'Đã tạo mã Key bản quyền thành công!'
    );
EXCEPTION WHEN OTHERS THEN
    RETURN json_build_object(
        'success', false,
        'message', 'Lỗi khi tạo key: ' || SQLERRM
    );
END;
$$;


-- ==============================================================================
-- 4. HÀM QUẢN LÝ BẢN QUYỀN (RPC: admin_manage_license)
-- Hỗ trợ:
--   - 'reset_machine': Hủy khóa máy để khách kích hoạt sang máy tính khác
--   - 'renew': Gia hạn thêm số ngày sử dụng
--   - 'revoke': Khóa bản quyền ngay lập tức
-- ==============================================================================
CREATE OR REPLACE FUNCTION public.admin_manage_license(
    p_license_key TEXT,
    p_action TEXT,
    p_days INT DEFAULT 365,
    p_admin_secret TEXT DEFAULT ''
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_is_admin BOOLEAN;
    v_lic RECORD;
    v_clean_key TEXT;
    v_new_expires_at TIMESTAMPTZ;
BEGIN
    -- 1. Xác thực quyền Admin
    v_is_admin := public.check_admin_secret(p_admin_secret);
    IF NOT v_is_admin THEN
        RETURN json_build_object(
            'success', false,
            'message', 'Mật khẩu quản trị viên (Admin Secret) không chính xác!'
        );
    END IF;

    v_clean_key := UPPER(TRIM(p_license_key));

    -- 2. Tìm bản quyền theo Key
    SELECT * INTO v_lic
    FROM public.licenses
    WHERE license_key = v_clean_key;

    IF v_lic.id IS NULL THEN
        RETURN json_build_object(
            'success', false,
            'message', 'Không tìm thấy mã Key: ' || v_clean_key
        );
    END IF;

    -- 3. Thực thi hành động tương ứng
    IF p_action = 'reset_machine' THEN
        UPDATE public.licenses
        SET bound_machine_id = NULL,
            device_name = NULL,
            last_validated_at = NOW()
        WHERE id = v_lic.id;

        RETURN json_build_object(
            'success', true,
            'action', 'reset_machine',
            'license_key', v_clean_key,
            'message', 'Đã hủy khóa máy thành công! Khách hàng có thể kích hoạt trên máy mới.'
        );

    ELSIF p_action = 'renew' THEN
        IF p_days <= 0 THEN
            p_days := 365;
        END IF;

        -- Nếu key đã hết hạn thì cộng từ thời điểm hiện tại, nếu còn hạn thì cộng dồn tiếp
        IF v_lic.expires_at < NOW() THEN
            v_new_expires_at := NOW() + (p_days || ' days')::INTERVAL;
        ELSE
            v_new_expires_at := v_lic.expires_at + (p_days || ' days')::INTERVAL;
        END IF;

        UPDATE public.licenses
        SET expires_at = v_new_expires_at,
            status = 'Active',
            last_validated_at = NOW()
        WHERE id = v_lic.id;

        RETURN json_build_object(
            'success', true,
            'action', 'renew',
            'license_key', v_clean_key,
            'new_expires_at', v_new_expires_at,
            'message', 'Đã gia hạn thêm ' || p_days || ' ngày thành công! (Hạn mới: ' || TO_CHAR(v_new_expires_at, 'YYYY-MM-DD') || ')'
        );

    ELSIF p_action = 'revoke' THEN
        UPDATE public.licenses
        SET status = 'Revoked',
            last_validated_at = NOW()
        WHERE id = v_lic.id;

        RETURN json_build_object(
            'success', true,
            'action', 'revoke',
            'license_key', v_clean_key,
            'message', 'Đã thu hồi (khóa) bản quyền thành công!'
        );

    ELSE
        RETURN json_build_object(
            'success', false,
            'message', 'Hành động không hợp lệ: ' || COALESCE(p_action, 'null')
        );
    END IF;
EXCEPTION WHEN OTHERS THEN
    RETURN json_build_object(
        'success', false,
        'message', 'Lỗi khi xử lý: ' || SQLERRM
    );
END;
$$;


-- ==============================================================================
-- 5. HÀM LIỆT KÊ DANH SÁCH BẢN QUYỀN (RPC: admin_list_licenses)
-- Phục vụ hiển thị danh sách trên Web Admin Portal có tìm kiếm và phân trang
-- ==============================================================================
CREATE OR REPLACE FUNCTION public.admin_list_licenses(
    p_admin_secret TEXT,
    p_search TEXT DEFAULT '',
    p_limit INT DEFAULT 100
)
RETURNS JSON
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_is_admin BOOLEAN;
    v_result JSON;
    v_search_pattern TEXT;
BEGIN
    -- 1. Xác thực quyền Admin
    v_is_admin := public.check_admin_secret(p_admin_secret);
    IF NOT v_is_admin THEN
        RETURN json_build_object(
            'success', false,
            'message', 'Mật khẩu quản trị viên (Admin Secret) không chính xác!'
        );
    END IF;

    -- 2. Tìm kiếm danh sách
    v_search_pattern := '%' || LOWER(TRIM(COALESCE(p_search, ''))) || '%';

    SELECT json_agg(t) INTO v_result
    FROM (
        SELECT 
            id,
            license_key,
            assigned_email,
            license_type,
            status,
            starts_at,
            expires_at,
            bound_machine_id,
            device_name,
            last_validated_at,
            created_at,
            CASE 
                WHEN expires_at <= NOW() THEN 0
                ELSE CEIL(EXTRACT(EPOCH FROM (expires_at - NOW())) / 86400)::INT
            END AS days_remaining,
            CASE 
                WHEN bound_machine_id IS NOT NULL AND bound_machine_id != '' THEN true
                ELSE false
            END AS is_bound
        FROM public.licenses
        WHERE 
            (v_search_pattern = '%%')
            OR (LOWER(assigned_email) LIKE v_search_pattern)
            OR (LOWER(license_key) LIKE v_search_pattern)
            OR (LOWER(COALESCE(device_name, '')) LIKE v_search_pattern)
        ORDER BY created_at DESC
        LIMIT GREATEST(1, LEAST(COALESCE(p_limit, 100), 500))
    ) t;

    RETURN json_build_object(
        'success', true,
        'licenses', COALESCE(v_result, '[]'::JSON)
    );
EXCEPTION WHEN OTHERS THEN
    RETURN json_build_object(
        'success', false,
        'message', 'Lỗi truy vấn: ' || SQLERRM
    );
END;
$$;

-- CẤP QUYỀN THỰC THI CHO VAI TRÒ ANON ĐỂ WEB PORTAL VÀ CLI CÓ THỂ GỌI QUA RPC
GRANT EXECUTE ON FUNCTION public.check_admin_secret(TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.admin_generate_key(TEXT, INT, TEXT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.admin_manage_license(TEXT, TEXT, INT, TEXT) TO anon, authenticated;
GRANT EXECUTE ON FUNCTION public.admin_list_licenses(TEXT, TEXT, INT) TO anon, authenticated;
