# SEO redirects và chính sách xuất bản

## Phạm vi

Migration mới `AddSeoRedirectsAndSiteSettings` tạo `seo_redirects` với `id`, `source_path` unique, `destination_path`, `redirect_type=301`, `created_at_utc`, `updated_at_utc`. Chỉ nhận đường dẫn local đã chuẩn hóa cho `/san-pham/<slug>`, `/danh-muc/<slug>` và `/kien-thuc/<slug>`; không lưu URL ngoài site, query hoặc fragment. Không sửa migration cũ.

Khi Admin đổi slug của Product, Category hoặc Article **Published**, backend kiểm tra slug, quyền theo endpoint hiện có, xung đột entity/redirect và canonical override. Entity và redirect được ghi trong cùng transaction PostgreSQL. Những redirect cũ đang trỏ tới URL trước đó được cập nhật thẳng sang URL mới, tránh chuỗi 301. Đường dẫn đã là nguồn redirect không thể dùng lại để tạo/sửa nội dung; đổi về slug cũ bị từ chối. Nếu canonical override còn trỏ đúng URL cũ, API yêu cầu xóa override trước khi đổi slug.

`GET /api/v1/public/seo-redirects?sourcePath=/san-pham/<slug>` chỉ trả `{ destinationPath, redirectType: 301 }` khi đích vẫn là nội dung công khai. Next.js `proxy.ts` gọi API này bằng `no-store` rồi trả HTTP 301 cùng origin; `permanentRedirect()` của Next.js trả 308 nên không được dùng ở đây. API không nhận đích tùy ý từ browser. Không có màn tạo redirect thủ công trong Prompt 8.

## SEO và HTTP

| Trạng thái | Trang detail | Metadata | Sitemap |
|---|---|---|---|
| Draft / InReview / Archived | 404 | Không index | Không có |
| Published, `robotsIndex=true` | 200 | Index nếu `SITE_INDEXABLE=true` và cấu hình site hợp lệ | Có nếu canonical trỏ chính URL 200 |
| Published, `robotsIndex=false` | 200 | `noindex` | Không có |

Category là điều kiện hiển thị của Product: Product Published chỉ public khi danh mục chính Published. Nếu đích của redirect bị Archive hoặc mất điều kiện public, endpoint resolve trả 404 để tránh 301 tới trang 404. Canonical và JSON-LD của trang đích dùng slug mới; sitemap lấy detail public hiện tại, loại URL cũ và canonical ngoài URL. Search, Admin và Preview vẫn noindex. Development mặc định `SITE_INDEXABLE=false`; Publish không đồng nghĩa Google sẽ index ngay.

Next.js mặc định chuẩn hóa trailing slash theo cấu hình App Router. Kiểm tra `/slug` là dạng canonical; yêu cầu `/<slug>/` có thể nhận redirect chuẩn hóa của Next trước 301 lịch sử. Khi triển khai, kiểm tra cả dạng URL này trên domain thật nếu mục tiêu là một lần chuyển hướng duy nhất.

## Migration an toàn

Không chạy migration tự động lúc API Production startup. Tạo SQL để review, backup DB rồi mới áp dụng bằng bước deploy riêng:

```powershell
dotnet tool run dotnet-ef migrations script AddLeadFoundation AddSeoRedirectsAndSiteSettings --idempotent --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext --output prompt8-migration.sql
```

Đặt `ConnectionStrings__Postgres` cho môi trường đích bằng secret manager; không commit file SQL có thông tin nhạy cảm. Local có thể dùng service `migrate` trong Compose theo README. Integration tests chỉ dùng database tên bắt đầu `teapremium_test` qua `TEAPREMIUM_TEST_CONNECTION_STRING`.

## Kiểm tra

- Đổi slug Published, kiểm tra URL cũ 301, đích 200, canonical/sitemap chỉ có URL mới.
- Đổi lần hai, xác nhận cả hai alias cũ trỏ thẳng đích cuối. Thử quay lại alias, tạo entity trùng alias và canonical override cũ: API từ chối.
- Archive đích: detail và resolve redirect đều 404. Published noindex vẫn 200 và không vào sitemap.
- Kiểm tra URL ngoài site, `//host`, query, chữ hoa và trailing slash không thể được lưu làm redirect record.
