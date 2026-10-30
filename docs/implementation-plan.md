# Kế hoạch triển khai TeaPremium

Kế hoạch này chia thành 7 milestone. Mỗi milestone chỉ được coi là hoàn tất khi các tiêu chí Definition of Done (DoD) có bằng chứng chạy thật. Tiptap, Cloudflare R2, GA4/Search Console và live chat không được thêm sớm hơn milestone cần chúng và bước xác nhận tương ứng.

## M1 — Foundation và contract

**Phạm vi:** scaffold solution .NET và Next.js theo kiến trúc đã chốt; cấu hình PostgreSQL local, quality gates, OpenAPI/Problem Details, CI cơ bản và ranh giới module. Đây là milestone đầu tiên có code; công việc tài liệu hiện tại chưa scaffold.

**DoD:**

- Phiên bản .NET/EF Core/Node/Next.js và package manager được pin; lockfile được commit.
- `Domain` không tham chiếu project khác; `Application` chỉ tham chiếu `Domain`; dependency tests hoặc build rule phát hiện tham chiếu sai.
- API health endpoint và web shell chạy local bằng hướng dẫn đã ghi; không có migration tự chạy ở Production startup.
- `dotnet build`, `dotnet test`, frontend lint/typecheck/build và CI chạy xanh bằng đúng lệnh được lưu trong README/AGENTS.
- Secret chỉ lấy từ cấu hình môi trường/user secrets; repository scan không thấy credential thật.

## M2 — Identity, Admin foundation và audit

**Phạm vi:** ASP.NET Core Identity, session Admin an toàn, policy-based authorization, Admin shell và audit cho mutation quan trọng.

**DoD:**

- Có luồng login/logout/expiry hoạt động; credential không xuất hiện trong `localStorage` hoặc client bundle.
- API Admin chưa đăng nhập trả 401; user thiếu quyền trả 403; policy Catalog/Content/SEO/Leads/Users có integration test đại diện.
- Mutation qua browser được bảo vệ CSRF/Origin theo thiết kế đã duyệt; cookie có `Secure`, `HttpOnly`, `SameSite` phù hợp ở môi trường triển khai.
- Login có validation, rate limit và lockout; log không chứa password, token hoặc PII đầy đủ.
- Ít nhất một mutation được ghi audit với actor, action, entity và thời gian UTC; audit không sửa qua API thông thường.

## M3 — Catalog và Content core

**Phạm vi:** Product, Category, Article, trạng thái Draft/Published/Archived, slug, SEO fields, admin CRUD và migration đầu tiên; chưa tạo bảng commerce.

**DoD:**

- Migration tạo đúng schema Catalog/Content/Identity cần thiết và không có bảng price/cart/order/payment/checkout.
- Slug unique được bảo vệ ở database và application; invariant publish có unit test.
- Admin tạo/sửa/publish/archive được Product, Category và Article theo permission; conflict trả lỗi contract xác định.
- Public API chỉ trả entity Published; test chứng minh Draft và Archived không thể lấy qua list, detail hoặc filter tùy biến.
- Public DTO/OpenAPI không có price, sale price, cart URL, checkout URL hoặc trường quản trị.
- Rich content khi được bật chỉ render HTML đã sanitize phía server; Tiptap chỉ thêm ở bước editor đã được phê duyệt.

## M4 — Storefront SEO-first và tìm kiếm

**Phạm vi:** triển khai các route public đã chốt, giao diện responsive, PostgreSQL search/filter/pagination và CTA liên hệ dùng cấu hình shop đã được duyệt.

**DoD:**

- Tất cả route `/`, `/danh-muc/[slug]`, `/san-pham/[slug]`, `/kien-thuc`, `/kien-thuc/[slug]`, `/gioi-thieu`, `/lien-he`, `/tim-kiem` có trạng thái loading/error/404 xác định.
- View Source/HTML response của trang Published chứa H1, nội dung chính, title, meta description và canonical mà không cần client-side fetch.
- `/tim-kiem` có `noindex,follow`; filter/sort không tạo URL indexable ngoài chính sách; pagination có upper bound và thứ tự ổn định.
- Không có giá, `Offer`, cart, checkout hoặc nút đặt hàng online trên HTML, API response và JSON-LD.
- Responsive smoke test qua các viewport đã chọn; ảnh có width/height, ALT đúng dữ liệu và không tải ảnh gốc không giới hạn.
- Search/filter được integration test trên PostgreSQL với bộ dữ liệu tiếng Việt đại diện đã được phê duyệt.

## M5 — Publishing và SEO engine

**Phạm vi:** publish validation, sitemap/robots, JSON-LD, redirects và cache revalidation an toàn; cơ chế outbox/worker chỉ triển khai ở mức cần cho tính tin cậy đã thống nhất.

**DoD:**

- Sitemap chỉ chứa URL Published + indexable, canonical, trả 200; không chứa Draft/Archived/Search/Admin/Preview.
- Product/Article/Category/Home sinh đúng schema từ dữ liệu thật; automated test khẳng định không có giá giả, review giả hoặc thông tin shop chưa cấu hình.
- Đổi slug Published tạo redirect 301 không loop và làm mới cả URL cũ/mới; archive trả 404/410/redirect theo policy đã duyệt.
- Revalidation endpoint xác thực chữ ký và thời gian, chống replay theo thiết kế; retry/idempotency có test khi dùng worker/outbox.
- SEO smoke suite kiểm tra title, description, canonical, robots, JSON-LD, sitemap, redirect và custom 404 trên staging.
- Staging được chặn crawl bằng authentication/IP restriction khi khả dụng, với noindex là lớp bổ sung.

## M6 — Lead và contact operations

**Phạm vi:** link Zalo/Facebook/hotline từ settings, form tư vấn, lead Admin, consent, anti-spam và contact event tối thiểu; không tự xây live chat.

**DoD:**

- Thông tin liên hệ chỉ hiển thị sau khi dữ liệu shop được nhập và duyệt; thiếu dữ liệu không sinh link/địa chỉ giả.
- Form hợp lệ tạo đúng một lead và trả response xác định; submission lặp/idempotency behavior được test.
- Server-side validation, honeypot/rate limit và consent theo policy đã duyệt hoạt động; test abusive request không tạo lead hàng loạt.
- Admin được lọc/xem/cập nhật trạng thái lead theo permission; user không có quyền không đọc được PII.
- Click liên hệ và `lead_created` là hai chỉ số riêng; tracking lỗi không chặn mở kênh liên hệ.
- Log và analytics payload không chứa tên, số điện thoại, email hoặc nội dung tin nhắn đầy đủ.

## M7 — Hardening, tích hợp sau và go-live

**Phạm vi:** kiểm thử bảo mật/hiệu năng/accessibility, backup/restore, observability, privacy/legal review; chỉ sau đó tích hợp R2, GA4/GSC hoặc live-chat provider đã được duyệt.

**DoD:**

- Unit/integration/contract/E2E/SEO smoke chạy xanh; E2E bao phủ Admin publish → public HTML → contact form.
- Security review xác nhận auth/RBAC/CSRF/XSS/upload/rate-limit/CORS; dependency và image scan không còn lỗi nghiêm trọng chưa có disposition.
- Production migration được review và chạy như deploy step riêng; backup được tạo và restore rehearsal thành công, có bằng chứng thời gian/kết quả.
- Health checks, structured logs/correlation ID và cảnh báo 5xx/DB/backup hoạt động; có runbook rollback/forward-fix.
- Kiểm tra mobile performance/accessibility trên trang đại diện có báo cáo thật; mọi ngoại lệ ngưỡng được ghi owner và kế hoạch xử lý.
- Privacy, consent, nội dung shop và nghĩa vụ pháp lý được owner chuyên trách phê duyệt trước public launch.
- Nếu bật R2/GA4/GSC: bucket/property/domain/consent đã xác minh và test end-to-end; nếu chưa đủ điều kiện, hạng mục được ghi rõ là chưa triển khai chứ không dùng dữ liệu giả.
- Checklist cuối xác nhận không có public price, cart, order, checkout, payment, commerce tables hoặc online-order CTA.

## Ngoài kế hoạch MVP

- Commerce (price, variant bán hàng, inventory, cart, order, checkout, payment).
- Multi-shop, multi-tenant hoặc đa ngôn ngữ thật.
- Realtime chat tự xây, data warehouse/dashboard analytics phức tạp.
- Microservices và các hạ tầng phân tán khi chưa có số liệu chứng minh nhu cầu.
