# Quyết định kiến trúc TeaPremium

## Nguồn và trạng thái

Tài liệu nguồn `docs/technical-design-v1.0.md` có tồn tại và đã được đối chiếu. Yêu cầu trong prompt hiện tại là ràng buộc phạm vi; tài liệu thiết kế cung cấp chi tiết triển khai. Khi hai nguồn mâu thuẫn, cần dừng và xác nhận thay vì tự đổi phạm vi hoặc stack.

Trạng thái dùng trong tài liệu:

- **Chốt:** có thể dùng làm ràng buộc triển khai.
- **Cần kiểm chứng:** chưa đủ dữ liệu để khóa phương án hoặc giá trị cụ thể.
- **Để sau:** không triển khai trong giai đoạn nền tảng hiện tại.

## Quyết định đã chốt

| ID | Quyết định | Hệ quả chính |
|---|---|---|
| AD-01 | MVP là SEO-first product catalog + custom CMS + contact/lead generation + Admin. | Tối ưu publish, nội dung và chuyển đổi liên hệ; không tối ưu luồng mua hàng online. |
| AD-02 | Không public price; không cart, order, checkout, payment hay nút đặt hàng online; không tạo bảng commerce trong MVP. | Public DTO và JSON-LD không có giá/`Offer`; commerce chỉ là hướng mở rộng sau này. |
| AD-03 | Một shop, một storefront, một ngôn ngữ `vi-VN`. | Chuẩn bị boundary i18n nhưng không thêm locale route hoặc bản dịch giả trong MVP. |
| AD-04 | Backend là .NET 10 Web API Controllers + EF Core 10; dữ liệu trong một PostgreSQL. | Controller mỏng, contract rõ; migration do EF Core quản lý nhưng không chạy tự động khi Production startup. |
| AD-05 | Hệ thống là Modular Monolith với các lớp `Api/Application/Domain/Infrastructure`. | `Domain` không phụ thuộc hạ tầng; `Application` định nghĩa use case/port; `Infrastructure` triển khai; `Api` làm HTTP orchestration. Module không gọi HTTP lẫn nhau. |
| AD-06 | Frontend dùng Next.js App Router, TypeScript, Tailwind và shadcn/ui; storefront và Admin ở cùng codebase/deployment ban đầu. | Public route ưu tiên Server Components/SSR/prerender; Admin ở `/admin` và luôn dynamic/noindex. |
| AD-07 | API có prefix `/api/v1`, tách `/public` và `/admin`. | Public API chỉ trả Published/indexable; Admin API luôn xác thực và kiểm tra permission phía server; lỗi theo Problem Details. |
| AD-08 | ASP.NET Core Identity quản lý tài khoản; authorization dùng role/claim/policy. | Không tự viết password/auth; không lưu token ở `localStorage`; UI không thay thế kiểm tra quyền tại API. |
| AD-09 | Public routes ổn định gồm `/`, `/danh-muc/[slug]`, `/san-pham/[slug]`, `/kien-thuc`, `/kien-thuc/[slug]`, `/gioi-thieu`, `/lien-he`, `/tim-kiem`. | Slug là public identifier; đổi slug Published phải tạo 301; tìm kiếm nội bộ là noindex. |
| AD-10 | Mọi public page Published phải có nội dung chính và SEO metadata trong HTML server-rendered/prerendered. | Sitemap chỉ chứa canonical URL trả 200 và indexable; Draft/Archived/Preview/Admin không được index hoặc rò qua public API. |
| AD-11 | Structured data chỉ phản ánh dữ liệu đã kiểm chứng. | Không tạo giá `0`, `Offer`, availability, rating/review, địa chỉ hoặc nguồn gốc giả. Product schema không chứa offer trong MVP. |
| AD-12 | Lead đi qua Zalo/Facebook/hotline/form; form lưu backend và có validation, consent, anti-spam/rate limit. | Click liên hệ và lead được phân biệt; không gửi PII lên analytics; live chat riêng không thuộc MVP hiện tại. |
| AD-13 | Ưu tiên giải pháp đơn giản, có nhu cầu thật. | Chưa dùng microservices, CQRS framework, generic repository, Redis, Elasticsearch hoặc Kafka. Tìm kiếm bắt đầu bằng PostgreSQL. |
| AD-14 | Rich content phải lưu dữ liệu có cấu trúc và HTML đã sanitize phía server. | Không render HTML tùy ý; Tiptap là editor dự kiến nhưng tích hợp ở giai đoạn sau. |
| AD-15 | Media object storage, GA4 và Search Console là tích hợp sau. | Cloudflare R2, pipeline media hoàn chỉnh và analytics không được giả lập như đã có trong giai đoạn nền tảng. |

## Dependency và boundary bắt buộc

```text
Api ───────────────► Application ───────────────► Domain
                         ▲
Infrastructure ──────────┘
```

- `Domain`: entity, value object, invariant, domain event; không biết EF Core, HTTP, file storage hoặc analytics.
- `Application`: use case, DTO/contract nội bộ, validation orchestration và interface cho persistence/integration.
- `Infrastructure`: EF Core/PostgreSQL, Identity store, storage và external integration.
- `Api`: controller, authentication/authorization, serialization, Problem Details, rate limiting và DI.
- Next.js chỉ dùng contract API/BFF; không truy cập PostgreSQL trực tiếp.

Các module nghiệp vụ dự kiến: Catalog, Content, SEO, Leads, Identity và Settings. Analytics chỉ thêm khi bắt đầu tích hợp đo lường. Không tạo module Commerce ở MVP.

## Quyết định để sau

| Hạng mục | Giới hạn hiện tại |
|---|---|
| Tiptap | Chỉ tích hợp khi bắt đầu CMS rich editor; trước đó chưa thêm dependency. |
| Cloudflare R2/CDN | Chỉ triển khai sau khi xác nhận account, domain, bucket policy, giới hạn file và quy trình biến thể ảnh. |
| GA4 + Search Console | Chỉ tích hợp sau khi có domain/property/consent policy; không dùng dữ liệu mẫu như dữ liệu thật. |
| Live chat | Chưa chọn provider và không tự xây realtime chat server. |
| Commerce | Chỉ mở ADR mới sau khi nghiệp vụ, vận hành và pháp lý cho phép; không dựng schema sẵn trong MVP. |

## Giả định cần kiểm chứng

| ID | Cần xác nhận trước khi triển khai liên quan | Tiêu chí khóa quyết định |
|---|---|---|
| AV-01 | Tên repository/sản phẩm chính thức và namespace (`TeaPremium` mới là tên làm việc). | Chủ dự án xác nhận tên hiển thị và namespace kỹ thuật. |
| AV-02 | Phiên bản patch .NET SDK/EF Core, Next.js, Node.js và package manager. | Pin phiên bản được hỗ trợ, có lockfile và CI chạy restore/build thành công. |
| AV-03 | Cách tổ chức solution/repo chính xác theo đề xuất `backend/`, `apps/web/`, `tests/`. | Xác nhận trước khi scaffold; không suy ra từ repo hiện tại chỉ có tài liệu. |
| AV-04 | Domain chính, quy tắc www, canonical base URL và môi trường staging. | DNS/hosting được cấp; staging có auth hoặc IP restriction và noindex bổ sung. |
| AV-05 | Thông tin shop, hotline, Zalo/Facebook, địa chỉ, giờ làm việc, logo và nội dung giới thiệu. | Chỉ publish dữ liệu do shop cung cấp và phê duyệt. |
| AV-06 | Taxonomy danh mục, SKU, thuộc tính sản phẩm và dữ liệu xuất xứ/chứng nhận. | Có data dictionary và mẫu dữ liệu thật; trường không có bằng chứng phải để trống. |
| AV-07 | Workflow nội dung và ma trận quyền thực tế (Owner/Admin/Editor/SEO/Sales). | Có danh sách tác vụ/quyền được duyệt; API policy có test 401/403. |
| AV-08 | Điều kiện publish tối thiểu, chính sách archive/404/410/301 và lịch sử revision. | SEO/content owner duyệt checklist; test publish và đổi slug có kết quả xác định. |
| AV-09 | Session/BFF flow, CSRF, MFA, password/lockout và thời hạn phiên Admin. | Security review chấp thuận threat model và cookie policy. |
| AV-10 | Quy mô catalog/bài viết và chất lượng tìm kiếm tiếng Việt bằng PostgreSQL. | Test với dữ liệu đại diện; chỉ cân nhắc công cụ khác nếu đo được hạn chế. |
| AV-11 | Trường form, consent text, retention/xóa lead, quyền riêng tư và nghĩa vụ pháp lý. | Chủ dữ liệu/pháp lý phê duyệt trước go-live; không suy luận miễn trừ từ việc không có checkout. |
| AV-12 | Kênh thông báo lead, SLA xử lý, anti-spam và rate-limit thực tế. | Chạy thử có giám sát, không mất lead và không gửi PII vào log/analytics. |
| AV-13 | Hạ tầng deploy, backup, RPO/RTO, email provider và ngân sách dịch vụ ngoài. | Có owner vận hành, restore rehearsal và runbook được duyệt. |
| AV-14 | Chính sách analytics/cookie và quyền truy cập GA4/GSC. | Consent được duyệt, property được xác minh và test không phát PII. |

## Nguyên tắc thay đổi quyết định

Thay đổi phạm vi, stack, public URL, boundary dependency, auth/session hoặc chính sách index phải được ghi bằng ADR mới: nêu vấn đề, lựa chọn, quyết định, hệ quả và kế hoạch migration. Không mở rộng sang commerce chỉ để “chuẩn bị trước”; giữ stable ID, slug, publish lifecycle và contract rõ là đủ để mở rộng sau.

