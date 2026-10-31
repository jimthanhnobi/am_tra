# CMS Content và Lead Foundation

## Phạm vi đã triển khai

- CMS có `articles`, `article_categories`, `article_category_links`; bài viết liên kết tác giả Identity và ảnh bìa trong `media_assets`.
- Admin API: `GET/POST /api/v1/admin/articles`, `GET/PATCH /{id}`, `GET /{id}/preview`, `POST /{id}/review`, `/publish`, `/archive`; danh mục bài viết dùng `/api/v1/admin/article-categories` (GET/POST/PATCH).
- Public API: `GET /api/v1/public/articles` và `/{slug}` chỉ trả bài `Published`. Bài Published có `robotsIndex=false` vẫn trả 200 với metadata noindex trên HTML, nhưng không vào sitemap. Draft, InReview và Archived trả 404 ở detail. Slug bài Published không đổi cho tới khi có redirect 301.
- Admin UI: `/admin/articles`, `/admin/articles/new`, `/admin/articles/[id]/edit`, `/admin/article-categories`; editor Tiptap lưu JSON. Backend chỉ tạo HTML từ JSON bằng whitelist node/mark và sanitizer. Không nhận HTML từ client làm nguồn nội dung.
- Storefront `/kien-thuc` và `/kien-thuc/[slug]` render trên server, metadata/canonical/Article JSON-LD/breadcrumb/sitemap lấy từ Public API. JSON-LD không tự thêm tác giả vì Identity chỉ có email nội bộ, không có tên tác giả công khai đã xác nhận.
- Lead: `leads` và `contact_events` là hai bảng riêng. Chỉ form liên hệ có consent mới tạo lead. Nhấn Zalo/Facebook/hotline, nếu khách đồng ý ghi nhận, tạo contact event; không tạo lead từ lượt nhấn.
- Admin `/admin/leads` xem và đổi trạng thái với `Leads.Read` / `Leads.Manage`. Editor không có quyền lead. API vẫn kiểm tra quyền, cookie và CSRF.

## Migration và chạy local

Hai migration theo thứ tự `AddArticles` rồi `AddLeadFoundation`. Chạy bằng lệnh deploy có review; Production cần backup và kế hoạch rollback/forward fix. Không chạy migration tự động khi API khởi động.

```powershell
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet tool run dotnet-ef database update --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext
dotnet run --project backend/src/TeaPremium.Api --launch-profile http
corepack pnpm --dir apps/web dev
```

Backend cần `ConnectionStrings__Postgres` hoặc user secrets như [README](../README.md). Frontend cần `API_BASE_URL` cho SSR/Admin và `NEXT_PUBLIC_API_BASE_URL` cho form/contact event từ trình duyệt. Mặc định Development là `http://localhost:5080/api/v1`; Production phải đặt URL Public API thật, có HTTPS. Backend chỉ cho CORS `http://localhost:3000` và `http://127.0.0.1:3000` ở Development; đặt `PUBLIC_WEB_ORIGIN=https://domain-da-xac-nhan` ở Production khi web và API khác origin. Nếu truy cập Next ở port khác trong Development, đặt thêm `PUBLIC_WEB_ORIGIN` đúng origin đó. CORS chỉ kiểm soát trình duyệt, rate limiter vẫn áp dụng cho request trực tiếp.

Seed bài demo là lệnh **chủ động**, chỉ chạy khi `ASPNETCORE_ENVIRONMENT=Development`, sau migration và `bootstrap-admin`:

```powershell
dotnet run --project backend/src/TeaPremium.Api -- seed-demo-article
```

Bài demo chỉ mô tả cách thử CMS, không nêu xuất xứ, chứng nhận hay dữ liệu hàng hóa. Lệnh không chèn dữ liệu vào migration/Production và chạy lặp không tạo bản sao. Không chuyển bài demo sang Production.

## Quy tắc dữ liệu và bảo mật

- JSON của Tiptap là nguồn; HTML được render/sanitize tại Application trước khi lưu. Text được encode; chỉ link HTTP(S); node lạ không sinh HTML. Giới hạn kích thước, độ sâu và số node. Preview Admin `no-store`, `X-Robots-Tag: noindex, nofollow`.
- Bài viết Published cần nội dung, SEO title và meta description. Public API dựa vào trạng thái Published; sitemap lọc thêm robotsIndex, canonical và site gate. Site vẫn mặc định noindex nếu chưa có domain và hồ sơ shop thật.
- Lead đòi consent checkbox thật; `consent_at` do server ghi. Honeypot có giá trị nhận 202 nhưng không lưu. Cần tên, nội dung, ít nhất điện thoại hoặc email; UTM chỉ nhận `source/medium/campaign/term/content` có giới hạn độ dài. API không log body/PII; cần chính sách lưu giữ và xóa dữ liệu trước Production.
- Contact event chỉ lưu khi `consentState=true`; `clientEventId` unique giúp retry không đếm trùng. Đây là lượt nhấn, không xác nhận khách đã chat/gọi thành công. Dữ liệu event không chứa PII trực tiếp.
- ASP.NET rate limit theo IP: lead 5/phút, contact event 30/phút. Với nhiều API instance, cần rate limit chung ở ingress để giữ ngưỡng toàn cụm. Browser gọi Public API trực tiếp để backend thấy IP khách, không đi qua Next BFF.
- `NEXT_PUBLIC_GA_MEASUREMENT_ID` để trống mặc định. Nếu điền ID GA4 thật dạng `G-...`, script chỉ tải sau khi khách đồng ý; lượt xem trang lưu ở GA4, không ghi page view vào PostgreSQL. Cần bật/kiểm tra Enhanced Measurement cho điều hướng SPA trong GA4 và đối chiếu Realtime/DebugView; chưa có kết nối GSC hay số liệu GA4 giả.

## Kiểm tra

```powershell
dotnet test backend/TeaPremium.sln --configuration Release --no-build
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web build
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web test:storefront
```

Integration tests dùng PostgreSQL riêng qua `TEAPREMIUM_TEST_CONNECTION_STRING` và cần migration được test fixture chạy trong database đó. Kiểm tra thủ công: Editor tạo Draft, preview nội dung, Publisher publish, xem HTML `/kien-thuc/[slug]`; Archive trả 404; gửi form có/không consent, honeypot và thử >5 request/phút; kiểm tra Network CORS từ origin web thật. Chạy Lighthouse/SEO trên HTTPS với nội dung và ảnh thật. Không coi Publish là được Google index ngay.

Playwright chạy Chromium trên API và web trỏ **database kiểm thử riêng**, không trỏ database Development đang chứa dữ liệu. Chỉ sau khi đã xác nhận DB thử, đặt `E2E_ISOLATED_DATABASE=true`, `E2E_WEB_BASE_URL`, `E2E_ADMIN_EMAIL` và `E2E_ADMIN_PASSWORD` của Admin thử rồi chạy `corepack pnpm --dir apps/web test:e2e`. Trên máy đã cài Chrome, có thể đặt `E2E_CHROME_CHANNEL=chrome`; nếu không, cài Chromium bằng `corepack pnpm --dir apps/web exec playwright install chromium`. Test Product còn cần `E2E_MEDIA_ASSET_ID` trỏ tới MediaAsset fixture có tên `product-fixture.webp` trong DB thử; nếu thiếu, riêng test đó được skip. Fixture metadata ảnh **không** kiểm tra R2 upload thật. Các bài test tạo bài viết, sản phẩm và lead thử; chỉ dọn dữ liệu thuộc database kiểm thử do lượt này tạo. Nếu thiếu tài khoản test hoặc marker DB, lệnh thất bại rõ ràng. Bộ test kiểm tra Draft, preview bài viết, Publish, HTML/JSON-LD public, Archive 404, gửi lead có consent và đổi trạng thái trong Admin. Quyền Editor bị từ chối Publish/đọc lead đã có integration test; chưa có browser test riêng cho role này.

Local development hiện dùng SDK pin `10.0.302` và Compose đầy đủ PostgreSQL/API/Next.js. Xem [README](../README.md) để khởi động, chạy migration thủ công và bootstrap Admin Development. Kết quả lệnh đã chạy, smoke Docker và giới hạn kiểm chứng nằm ở [verification.md](verification.md). Không dùng Compose local làm cấu hình deploy Production.

## Giới hạn còn lại

Không có workflow redirect 301 để đổi slug Published. Chưa có màn gỡ lead/xuất dữ liệu hay chính sách retention tự động. Chưa có xác nhận gửi tin nhắn, tích hợp chat hai chiều, GA4/GSC account thực, hoặc điểm Lighthouse thực tế. Ảnh bài viết cần Media/R2 đã cấu hình thật; không có ảnh giả.
