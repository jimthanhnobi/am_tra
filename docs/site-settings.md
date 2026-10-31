# Site Settings

`/admin/settings` cho người có `Settings.Manage` chỉnh thông tin công khai của một website: General, Brand Identity, Contact Information, SEO Defaults và Social Links. API Admin: `GET/PUT /api/v1/admin/settings`; API công khai: `GET /api/v1/public/settings`. Admin API yêu cầu Identity cookie, permission và CSRF theo BFF same-origin hiện có. Bảng `site_settings` chỉ có một dòng `id=1`, lưu `updated_by_user_id` và `updated_at_utc` để truy vết lần cập nhật gần nhất; nền tảng hiện chưa có audit event history riêng.

Trường hỗ trợ: Site Name, Site Description, Brand Tagline, logo/favicon asset, hotline, Zalo/Facebook URL, email, địa chỉ, giờ mở cửa, SEO title/meta description/OG image mặc định và Public Website URL. Asset ID phải tồn tại trong `media_assets`; Public API chỉ trả URL công khai của ảnh, không trả ID nội bộ, tác giả cập nhật hay cấu hình R2. Không đặt secret, credential hoặc dữ liệu shop chưa xác minh trong màn này.

Backend kiểm tra độ dài, email, số điện thoại, URL Zalo/Facebook thuộc host cho phép và Website URL là HTTPS origin. Frontend kiểm tra trước khi gửi; backend là lớp quyết định. `Public Website URL` nên khớp domain HTTPS triển khai. Thay site settings được đọc bằng `no-store` ở server render tiếp theo, không cần publish webhook. Ảnh logo/favicon/OG cần R2 public URL và `next.config.ts` remotePatterns đã cấu hình đúng.

Nếu chưa lưu settings, storefront dùng env/profile cũ như fallback, với placeholder được ghi rõ. Sau khi lưu settings, tên, mô tả, liên hệ và ảnh lấy từ Public API; URL canonical dùng Public Website URL nếu đã nhập, nếu để trống thì fallback về URL env. `SITE_ABOUT` vẫn là nội dung giới thiệu từ env vì không nằm trong schema Prompt 8. `SITE_INDEXABLE` là cổng deployment độc lập, mặc định false; lưu settings **không** tự bật index. Muốn index cần có Site Name, Public Website URL HTTPS thực và chủ động đặt `SITE_INDEXABLE=true` sau khi kiểm tra nội dung. Giữ `API_BASE_URL`, R2 credentials, DB connection, cookie/CSRF key và các biến hạ tầng ở môi trường triển khai.

## Kiểm tra local

1. Áp dụng migration bằng service `migrate` trong Compose theo README; không chạy tự động trên API Production.
2. Login Admin có `Settings.Manage`, vào `/admin/settings`, nhập thông tin đã xác minh và lưu. Editor không có quyền phải nhận 403 từ API.
3. Mở `GET /api/v1/public/settings`: kiểm tra chỉ có trường công khai. Tải lại Home, Contact, Product, `robots.txt` và `sitemap.xml`; với `SITE_INDEXABLE=false` vẫn noindex và sitemap rỗng.
4. Nếu chọn ảnh, cần R2 development bucket, public URL và Next remotePatterns cùng host; xem [media-storage.md](media-storage.md).

Không có kết nối GA4/GSC hay xác minh quyền sở hữu domain trong Site Settings.
