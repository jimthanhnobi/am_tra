# TeaPremium — hướng dẫn làm việc

## Mục tiêu và phạm vi

- Xây dựng catalog trà cụ cao cấp ưu tiên SEO, custom CMS, quản trị và thu lead.
- MVP phục vụ một shop, chỉ `vi-VN`; cấu trúc phải sẵn sàng cho i18n nhưng không tạo bản dịch giả.
- Không hiển thị giá công khai; không cart, checkout, payment hay nút đặt hàng online. Ngoại lệ giai đoạn CRM: có bảng `sales_orders` **chỉ trong Admin** để ghi nhận đơn tư vấn/chốt ngoài website; tuyệt đối không lộ giá, đơn nội bộ trên Public API.
- Không tự suy đoán thông tin shop, địa chỉ, liên hệ, xuất xứ, chứng nhận hoặc dữ liệu sản phẩm.

## Kiến trúc và dependency

- Modular Monolith, một PostgreSQL; backend gồm `Api`, `Application`, `Domain`, `Infrastructure`; frontend là một Next.js App Router cho storefront và Admin.
- Dependency: `Domain` độc lập; `Application` chỉ phụ thuộc `Domain`; `Infrastructure` triển khai interface của `Application`; `Api` chỉ ghép HTTP/auth/DI. Module trao đổi qua application interface hoặc internal event, không gọi HTTP nội bộ.
- Controller mỏng, nghiệp vụ nằm trong use case/domain. Không dùng microservices, CQRS framework, generic repository, Redis, Elasticsearch hoặc Kafka nếu chưa có nhu cầu đã đo.
- API dùng `/api/v1/public/*` và `/api/v1/admin/*`; public DTO không được rò rỉ dữ liệu Draft/Archived, giá hoặc trường quản trị.

## Naming và coding conventions

- C#: PascalCase cho type/member, camelCase cho local/parameter, hậu tố `Async`, bật nullable, truyền `CancellationToken`; thời gian lưu UTC.
- TypeScript: `strict`, component/type PascalCase, function/variable camelCase; ưu tiên Server Components, chỉ dùng Client Components khi cần tương tác trình duyệt.
- URL/slug dùng chữ thường không dấu và kebab-case; database dùng snake_case; tên route/API ổn định, không đưa ID vào public URL.
- Không trả EF entity trực tiếp. Validate ở biên hệ thống, trả lỗi `application/problem+json`, không lộ stack trace. Tránh abstraction sớm và code dùng chung không có consumer thật.

## Security

- Dùng ASP.NET Core Identity và authorization policy ở API; không coi việc ẩn UI là phân quyền.
- Không hardcode/commit secret, không lưu token trong `localStorage`; session Admin dùng cookie `Secure`, `HttpOnly`, `SameSite` phù hợp và bảo vệ CSRF cho mutation.
- Sanitize rich HTML phía server; encode output; rate-limit login, lead và upload; kiểm tra MIME/magic bytes/kích thước file; không log token hoặc PII đầy đủ.
- Không tự chạy migration khi Production startup. Migration Production là bước deploy có review, backup và rollback/forward-fix plan.

## SEO và publishing

- Các trang public Published phải có HTML nội dung chính và metadata được render/prerender phía server; không phụ thuộc client fetch để index.
- API public chỉ trả entity `Published`; `robotsIndex=false` vẫn có thể xem công khai với HTTP 200 và metadata noindex. Sitemap chỉ đưa URL Published, indexable, canonical 200. Draft, Archived, Admin, Preview và tìm kiếm nội bộ không được index.
- Mỗi trang indexable có title, description, canonical, H1 hợp lý và metadata social; slug Published đổi phải có redirect 301.
- JSON-LD phải phản ánh dữ liệu thật. Không tạo `Offer`, giá `0`, availability, rating/review hoặc thông tin shop giả.
- Giữ `/tim-kiem` noindex; filter/sort không tự sinh landing page indexable. Ảnh cần dimensions và ALT đúng thực tế.

## Kiểm tra và lệnh chuẩn

Chạy từ repository root; cập nhật file này nếu cấu trúc hoặc package manager được chốt khác đi.

```powershell
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet build backend/TeaPremium.sln --configuration Release --no-restore
dotnet test backend/TeaPremium.sln --configuration Release --no-build
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web test:commerce
corepack pnpm --dir apps/web test:storefront
corepack pnpm --dir apps/web build
```

- Mọi thay đổi phải có kiểm tra phù hợp và báo đúng lệnh đã chạy; không nhận pass nếu lệnh chưa chạy/không tồn tại.
- Test tối thiểu theo rủi ro: unit cho rule nghiệp vụ, integration cho EF/PostgreSQL và auth, contract cho API, E2E cho publish → public HTML → contact, SEO smoke cho metadata/canonical/sitemap/redirect.

