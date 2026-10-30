---
title: "THIẾT KẾ KỸ THUẬT WEBSITE TRÀ CỤ CAO CẤP"
subtitle: "SEO-First · Product Catalog · Custom CMS · Lead Generation"
author: "Technical Design / Architecture Blueprint"
date: "30/10/2026 · Phiên bản 1.0"
lang: vi-VN
---

# MỤC LỤC

| Phần 1–9 | Phần 10–18 |
|---|---|
| 1. Mục tiêu & phạm vi | 10. Bảo mật & phân quyền |
| 2. Công nghệ & kiến trúc | 11. Deployment & vận hành |
| 3. Giao diện & URL | 12. Yêu cầu phi chức năng |
| 4. Database & ERD | 13. Backlog theo sprint |
| 5. API Contract | 14. Mở rộng thương mại điện tử |
| 6. SEO Architecture | 15. Rủi ro & kiểm soát |
| 7. CMS & Media | 16. Go-live checklist |
| 8. Lead & Chat | 17. Phụ lục: luồng nghiệp vụ |
| 9. Analytics & báo cáo | 18. Nguồn tham khảo |

## Thông tin tài liệu


**Loại tài liệu:** Software Technical Design (STD) / MVP Blueprint  
**Ngày:** 30/10/2026  
**Trạng thái:** Đề xuất triển khai; cần xác nhận nghiệp vụ và pháp lý trước go-live  
**Đối tượng:** Chủ dự án, lập trình viên full-stack, SEO/content, vận hành cửa hàng

> **Tuyên bố phạm vi:** Giai đoạn đầu xây dựng website giới thiệu ấm Tử Sa, ấm gang, bộ ấm chén và trà cụ cao cấp, tập trung SEO và tạo khách hàng tư vấn. **Không có giá công khai, giỏ hàng, đặt hàng, thanh toán hoặc xác nhận giao dịch trực tuyến.** Hệ thống được chuẩn bị để thêm thương mại điện tử trong tương lai nhưng không bật ở MVP.

# 1. Executive summary & mục tiêu

## 1.1. Kết quả mong muốn

- Website công khai nhanh, đẹp, tin cậy, ưu tiên mobile và dễ được Google thu thập nội dung.
- Khách có thể tìm sản phẩm theo nhóm, chất liệu, xuất xứ, kiểu dáng; xem hình ảnh chất lượng cao, chi tiết và hướng dẫn sử dụng.
- CTA rõ ràng để liên hệ Zalo, Facebook/Messenger, hotline hoặc gửi form yêu cầu tư vấn; có thể hỗ trợ live chat trên web.
- Admin có thể quản lý sản phẩm, danh mục, bài viết, landing page, metadata SEO, redirect, media, thông tin liên hệ, người dùng và lịch sử thao tác.
- Dashboard xem traffic, hiệu quả tìm kiếm, sản phẩm/bài viết được quan tâm và các yêu cầu tư vấn thực tế.
- Bảo trì và mở rộng được thành hệ thống bán hàng sau khi các điều kiện vận hành/pháp lý phù hợp.

## 1.2. Giả định thiết kế

| Mục | Giả định | Tác động |
|---|---|---|
| Ngôn ngữ | Tiếng Việt trước, thiết kế sẵn khả năng i18n | Không tạo bản dịch giả trong MVP |
| Quy mô | 50–3.000 sản phẩm, 100–2.000 bài viết | PostgreSQL và tìm kiếm full-text đủ ban đầu |
| Nhân sự | 1–5 tài khoản Admin/Editor | RBAC vừa đủ, audit log bắt buộc |
| Thương hiệu | Một shop, một tên miền chính | Một storefront, không multi-tenant |
| Nội dung | Chủ shop biên soạn ảnh và bài viết | CMS workflow Draft → Review → Published |
| Hạ tầng | VPS Linux, Docker; CDN và object storage | Không triển khai Kubernetes hoặc microservices |

Các con số trên là giả định để lựa chọn kiến trúc, không phải dự báo tăng trưởng hoặc giới hạn cố định.

## 1.3. Phạm vi và ưu tiên

| Mức | Hạng mục | Ghi chú |
|---|---|---|
| P0 | Danh mục, chi tiết sản phẩm, blog, SEO nền tảng, CMS, liên hệ, phân quyền, backup | Điều kiện vận hành MVP |
| P0 | Đo click liên hệ, GA4/GSC được cài và xác minh | Có số liệu từ ngày đầu |
| P1 | Dashboard hợp nhất GA4 + GSC API, SEO audit nâng cao, media batch | Sau khi site ổn định |
| P1 | Workflow duyệt bài nhiều cấp, phân công lead cho nhân viên | Khi tăng nhân sự |
| P2 | Public price, variants bán hàng, giỏ hàng, checkout, thanh toán, kho | Không xây UI ở MVP |

# 2. Quyết định công nghệ và kiến trúc

## 2.1. Tech stack được chọn

| Layer | Công nghệ | Lý do |
|---|---|---|
| Public Web | Next.js App Router, React, TypeScript | Server rendering, metadata, ISR, component reuse |
| Admin | Next.js chung codebase; Tailwind CSS + shadcn/ui | Dễ tái sử dụng auth/layout/component |
| API | ASP.NET Core 10 Web API, Controllers | Hợp với .NET, rõ contract, mở rộng nghiệp vụ |
| Application | C# use cases, FluentValidation, mapper thủ công/Mapster nếu cần | Tách logic khỏi HTTP |
| Database | PostgreSQL + EF Core 10, migrations | Quan hệ chặt, JSONB cho thuộc tính linh hoạt |
| Identity | ASP.NET Core Identity, role & permission | Tránh tự viết bảo mật đăng nhập |
| Rich content | Tiptap editor, JSON document + HTML đã sanitize | Trình soạn thảo chủ động và an toàn |
| Files | Cloudflare R2, signed upload, CDN | Tách ảnh khỏi server ứng dụng |
| Observability | Serilog, OpenTelemetry (mức cơ bản), health checks | Theo dõi lỗi và truy vết |
| Analytics | GA4, Search Console, Microsoft Clarity (tùy chọn) | Đo traffic, tìm kiếm, trải nghiệm |
| Infra | Docker Compose, Nginx/Caddy, Cloudflare, CI/CD | Mức vận hành phù hợp dự án nhỏ |

**Lưu ý phiên bản:** .NET 10 là LTS đến tháng 11/2028; pin chính xác phiên bản Next.js và các package tương thích khi khởi tạo repository, dùng lockfile và cập nhật bản vá bảo mật định kỳ.

## 2.2. Sơ đồ hệ thống

![Kiến trúc tổng thể](architecture.png)

- Browser chỉ truy cập tên miền công khai qua HTTPS; Admin cũng đi qua Next.js và một lớp BFF (Backend for Frontend).
- Next.js lấy dữ liệu công khai từ public API; các thao tác Admin chạy qua session server-side/proxy được bảo vệ.
- ASP.NET Core kiểm tra RBAC, validate dữ liệu, lưu PostgreSQL và phát sự kiện nội bộ.
- Ảnh lưu trên object storage; browser lấy ảnh tối ưu qua CDN thay vì tải từ API.
- Backend không được expose trực tiếp PostgreSQL hoặc credentials cho trình duyệt.

## 2.3. Quy tắc module

| Module | Chịu trách nhiệm | Không chịu trách nhiệm |
|---|---|---|
| Catalog | Product, category, brand, attributes, gallery, publish | Thanh toán |
| Content | Article, article category, editorial workflow | Giá bán |
| SEO | Metadata validation, sitemap sources, redirects, schema policy | Ghi nhận GA4 |
| Leads | Contact form, tư vấn, trạng thái xử lý | Giỏ hàng |
| Analytics | Tracking schema, internal click aggregation, external reports | Thay thế Google Analytics |
| Identity | User, role, permission, login/session, audit | Dữ liệu catalog |
| Settings | Shop profile, social links, logo, thông báo, feature flags | UI-specific state |

**Quy tắc dependency:** `Domain` không tham chiếu Infrastructure; `Application` định nghĩa use case và interface; `Infrastructure` triển khai EF Core/storage/integration; `Api` chỉ orchestration HTTP. Cho phép module trong một deploy trao đổi qua application interface/sự kiện nội bộ, không gọi HTTP lẫn nhau.

## 2.4. Cấu trúc repository

```text
tea-premium/
├── apps/
│   └── web/
│       ├── src/app/(storefront)/
│       ├── src/app/admin/
│       ├── src/app/api/            # BFF, revalidate webhook
│       ├── src/features/{catalog,content,seo,leads,admin}/
│       ├── src/components/{ui,shared}/
│       └── src/lib/{api,auth,seo,analytics}/
├── backend/
│   ├── TeaPremium.Api/
│   ├── TeaPremium.Application/
│   ├── TeaPremium.Domain/
│   ├── TeaPremium.Infrastructure/
│   └── TeaPremium.sln
├── tests/
│   ├── TeaPremium.UnitTests/
│   ├── TeaPremium.IntegrationTests/
│   └── e2e/                        # Playwright
├── infra/{docker,reverse-proxy,scripts}/
├── docs/{adr,api,seo,runbooks}/
└── .github/workflows/
```

Giai đoạn đầu **một Next.js deployment**, Admin nằm trong `/admin`; có thể tách `admin.example.vn` sau này nếu nhu cầu thay đổi. Để route public SEO có ổn định lâu dài, không gắn URL sản phẩm với ID hoặc danh mục có thể đổi.

# 3. Thiết kế giao diện và tuyến URL

## 3.1. Sitemap thông tin (information architecture)

| Route | Nội dung | Index | Render |
|---|---|---|---|
| `/` | Trang chủ, brand story, danh mục, sản phẩm chọn lọc | Có | Prerender + ISR |
| `/danh-muc/[slug]` | Danh mục ấm Tử Sa, ấm gang, bộ trà… | Có khi chất lượng đủ | ISR |
| `/san-pham/[slug]` | Chi tiết sản phẩm, gallery, thông số, CTA | Có nếu Published | ISR |
| `/kien-thuc` | Trang hub kiến thức, chuyên mục | Có | ISR |
| `/kien-thuc/[slug]` | Bài viết SEO chuyên sâu | Có nếu Published | ISR |
| `/gioi-thieu` | Cửa hàng, nguồn gốc, cam kết | Có | Prerender |
| `/lien-he` | Thông tin shop, form, bản đồ nếu có | Có | SSR/ISR |
| `/tim-kiem?q=...` | Kết quả tìm kiếm nội bộ | Noindex | SSR |
| `/admin/*` | CMS, dashboard, users, leads | Không; auth required | Dynamic |
| `/preview/*` | Preview bản nháp với token thời hạn | Noindex + auth/token | Dynamic |

Ví dụ: `/danh-muc/am-tu-sa`, `/san-pham/am-tu-sa-thach-bieu`, `/kien-thuc/cach-khai-am-tu-sa`. URL do slug chuẩn hóa Unicode/tiếng Việt không dấu; mọi thay đổi slug Published tạo redirect 301.

## 3.2. Public storefront

- **Trang chủ:** hero thương hiệu, định vị cao cấp, 3–5 nhóm trà cụ chủ lực, sản phẩm nổi bật, quy trình lựa chọn/kiểm định, bài viết chuyên sâu, khu vực tư vấn.
- **Danh mục:** tiêu đề/H1 và đoạn giới thiệu độc nhất, bộ lọc theo chất liệu, dung tích, kiểu dáng, màu, xuất xứ (chỉ khi dữ liệu xác thực), phân trang và SEO text tự nhiên.
- **Chi tiết:** ảnh chính lớn, thumbnail, tên, SKU (nếu phù hợp), mô tả, chất liệu, dung tích, kích thước, nguồn gốc, hướng dẫn sử dụng/bảo quản, FAQ, liên kết bài liên quan và CTA “Liên hệ tư vấn”. **Không hiển thị giá và không có nút đặt mua**.
- **Blog:** hub → cluster → bài chuyên sâu → liên kết danh mục/sản phẩm; tác giả và ngày cập nhật khi có thực, ảnh gốc, chú thích rõ ràng.
- **Liên hệ:** Zalo OA/profile do shop cung cấp, Facebook Page/Messenger, hotline, giờ làm việc, địa chỉ công khai phù hợp, form opt-in.

## 3.3. Admin screens

| Nhóm | Màn hình cần có |
|---|---|
| Dashboard | Tổng quan impressions, clicks, sessions, product views, contacts, leads, bài được truy cập |
| Catalog | Danh sách, thêm/sửa, gallery, thuộc tính, danh mục, publish/archive, kiểm tra thiếu dữ liệu |
| Content | Danh sách, Tiptap editor, chuyên mục, preview, lịch đăng, liên kết nội bộ |
| SEO | Metadata từng entity, robots, redirects, sitemap status, lỗi URL, preview snippet |
| Leads | Danh sách form tư vấn, nguồn, trạng thái, ghi chú, lọc thời gian/sản phẩm |
| Media | Upload ảnh, kích thước, ALT, crop, sắp thứ tự, tìm media chưa sử dụng |
| Settings | Shop, social, domain, logo, scripts đo lường, flags |
| Security | Users, roles, quyền, đăng nhập, audit logs |

**Ngôn ngữ thiết kế:** giao diện public phong cách trà đạo tinh gọn, tông trung tính; typography rõ ràng, whitespace rộng, ảnh sản phẩm chân thực. Tránh hero video/slider nặng gây giảm LCP; mobile-first, sticky CTA nhưng không che nội dung.

# 4. Thiết kế dữ liệu (PostgreSQL)

## 4.1. ERD sơ bộ

![ERD dữ liệu lõi](erd.png)

Sơ đồ thể hiện quan hệ chính, không bao gồm toàn bộ cột quản trị (`created_at`, `updated_at`, `created_by`, `row_version`) hoặc các bảng Identity chuẩn. Sơ đồ được dùng để phân ranh giới trước khi viết EF Core migrations.

## 4.2. Quy ước dữ liệu chung

- PK UUID (`Guid`), timestamp `timestamptz` ở UTC, trình bày múi giờ `Asia/Ho_Chi_Minh` trong UI.
- `created_at_utc`, `updated_at_utc`, `created_by`, `updated_by` cho các bảng quản trị; `deleted_at_utc` chỉ dùng khi có lý do giữ lịch sử.
- `status` được giới hạn bởi enum/check constraint hoặc schema mapping được kiểm soát.
- Dùng `jsonb` cho tập thuộc tính thay đổi theo loại ấm, nhưng các trường cần filter/unique/index vẫn có cột riêng.
- Migration do EF Core quản lý, luôn review generated SQL trước khi chạy Production; không tự động migrate trên startup production.
- Soft delete không tự động áp dụng cho mọi bảng: sản phẩm/bài viết nên dùng `Archived` để giữ đường dẫn và lịch sử SEO.

## 4.3. Bảng lõi và cột quan trọng

| Bảng | Trường chính (rút gọn) | Index / Ràng buộc |
|---|---|---|
| `products` | `id`, `sku`, `slug`, `name`, `summary`, `description_jsonb`, `description_html`, `status`, `primary_category_id`, `brand_id?`, `specs_jsonb`, `published_at`, `seo_*` | `UQ(sku)`, `UQ(slug)`, `(status,published_at)` |
| `categories` | `id`, `parent_id?`, `slug`, `name`, `description_html`, `sort_order`, `status`, `seo_*` | `UQ(slug)`, `(parent_id,sort_order)` |
| `product_categories` | `product_id`, `category_id` | PK kép, FK cascade tại bảng nối |
| `product_media` | `id`, `product_id`, `asset_id`, `alt_text`, `sort_order`, `is_cover` | `(product_id,sort_order)`; chỉ một cover trên mỗi product |
| `media_assets` | `id`, `storage_key`, `mime_type`, `byte_size`, `width`, `height`, `checksum`, `created_at` | `UQ(storage_key)`, MIME allowlist |
| `brands` | `id`, `name`, `slug`, `origin?` | `UQ(slug)` |
| `articles` | `id`, `slug`, `title`, `excerpt`, `content_jsonb`, `content_html`, `status`, `author_id`, `cover_asset_id`, `published_at`, `seo_*` | `UQ(slug)`, `(status,published_at DESC)` |
| `article_categories` | `id`, `slug`, `name`, `seo_*` | `UQ(slug)` |
| `article_category_links` | `article_id`, `category_id` | PK kép |
| `leads` | `id`, `name?`, `phone?`, `email?`, `message`, `product_id?`, `source_page`, `channel`, `status`, `utm_jsonb`, `consent_at?`, `created_at` | `(created_at DESC)`, `(status,created_at)` |
| `contact_events` | `id`, `event_type`, `channel`, `product_id?`, `page_path`, `occurred_at`, `consent_state` | `(occurred_at,channel)`, `UQ(id)` |
| `seo_redirects` | `id`, `old_path`, `new_path`, `http_code`, `is_active` | `UQ(old_path)`; validate không loop |
| `outbox_messages` | `id`, `event_type`, `aggregate_id`, `payload_jsonb`, `created_at`, `processed_at?`, `attempts` | `(processed_at,created_at)` |
| `audit_logs` | `id`, `actor_user_id`, `action`, `entity`, `entity_id`, `created_at`, `changes_jsonb` | `(entity,entity_id,created_at)` |
| `site_settings` | `key`, `value_jsonb`, `updated_at` | `PK(key)`, chỉ Admin quyền cao sửa |

`seo_*` ở các entity Published là một **SEO value object** được EF Core map thành cột: `seo_title`, `meta_description`, `canonical_override?`, `robots_index`, `og_title`, `og_description`, `og_image_asset_id?`. Cách này giữ FK và tính toàn vẹn của entity, tránh `seo_metadata(owner_type, owner_id)` không có foreign key chuẩn.

## 4.4. Model sản phẩm và tính linh hoạt

Trường sản phẩm khuyến nghị: `SKU`, `slug`, `name`, `summary`, `primary_category_id`, nhiều category bổ trợ, `brand_id?`, `product_type`, `origin_text?`, `material_text?`, `capacity_ml?`, `dimensions_text?`, `specs_jsonb`, mô tả rich text, gallery và trạng thái xuất bản. **Chỉ lưu nguồn gốc/xác thực khi có bằng chứng; không tự suy đoán “chính hãng”, “Nhật Bản” hoặc “thủ công”.**

Mẫu thuộc tính linh hoạt:

```json
{
  "material": "Gang",
  "capacity_ml": 900,
  "shape": "Tròn",
  "finish": "Men đen",
  "care_note": "Không chà mạnh lớp men bên trong"
}
```

Không cần bảng `product_variants`, `prices`, `stocks` trong MVP nếu chưa phát sinh nghiệp vụ. Tuy nhiên service Catalog và DTO tách rõ với module Commerce để khi có giao dịch bổ sung **ProductVariant / Price / Inventory**, dùng FK đến `products` mà không sửa tên URL hoặc SEO entity.

## 4.5. Tình trạng xuất bản và điều kiện hiển thị

`Draft → InReview → Published → Archived`; role `Editor` được sửa draft, `Publisher` mới có quyền publish. Một bản ghi Published cần tối thiểu slug hợp lệ, tên, mô tả, ảnh cover + ALT và SEO title/meta; yêu cầu này có thể cấu hình mức độ nghiêm ngặt.

- Draft/InReview: chỉ Admin hoặc preview token có thời hạn truy cập; không đưa vào sitemap.
- Published: Public API trả dữ liệu; được xem xét index theo SEO flags.
- Archived: public không trả trang như sản phẩm đang bán; điều hướng 301 đến lựa chọn thay thế nếu phù hợp, nếu không trả 410/404 theo quyết định SEO; không 301 về trang chủ hàng loạt.
- Điều chỉnh slug Published phải tạo redirect 301 từ URL cũ; bảo đảm không tạo vòng redirect hoặc chain dài.

# 5. API Contract (ASP.NET Core)

## 5.1. Nguyên tắc

Base path `/api/v1`. Trả `application/json` nhất quán, lỗi theo `application/problem+json` (RFC 9457), phân trang có upper bound, validation 400/422 theo chuẩn nội bộ nhất quán, lỗi không tiết lộ stack trace. API công khai tuyệt đối không trả dữ liệu nhạy cảm, giá ẩn hoặc draft dù FE không render.

## 5.2. Public API

| HTTP | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/v1/public/products` | Danh sách Published, filter/search/pagination |
| GET | `/api/v1/public/products/{slug}` | Chi tiết và SEO payload |
| GET | `/api/v1/public/categories` | Cây danh mục Published |
| GET | `/api/v1/public/categories/{slug}` | Nội dung + products theo danh mục |
| GET | `/api/v1/public/articles` | Danh sách bài Published |
| GET | `/api/v1/public/articles/{slug}` | Chi tiết bài viết |
| GET | `/api/v1/public/site-profile` | Logo, hotline, social, thông tin shop |
| POST | `/api/v1/public/leads` | Gửi yêu cầu tư vấn (rate limit / honeypot) |
| POST | `/api/v1/public/contact-events` | Ghi click/contact event (validate/limit/dedupe) |

Ví dụ `GET /api/v1/public/products?category=am-tu-sa&page=1&pageSize=12&sort=latest`:

```json
{
  "items": [
    {
      "id": "2d17e240-2469-4e19-bef4-60efc814c44e",
      "slug": "am-tu-sa-thach-bieu",
      "name": "Ấm Tử Sa dáng Thạch Biều",
      "summary": "Mô tả ngắn được kiểm duyệt",
      "coverImage": { "url": "https://cdn.example.vn/p/abc.webp", "alt": "Ấm Tử Sa dáng Thạch Biều" },
      "primaryCategory": "Ấm Tử Sa"
    }
  ],
  "page": 1,
  "pageSize": 12,
  "totalItems": 1,
  "totalPages": 1
}
```

**Không có** `price`, `salePrice`, `cartUrl`, `checkoutUrl` trong public DTO v1.

## 5.3. Admin API

| HTTP | Endpoint | Quyền |
|---|---|---|
| POST / PATCH | `/api/v1/admin/products`, `/api/v1/admin/products/{id}` | `Catalog.Write` |
| POST | `/api/v1/admin/products/{id}/publish` | `Catalog.Publish` |
| POST | `/api/v1/admin/products/{id}/archive` | `Catalog.Publish` |
| CRUD | `/api/v1/admin/categories` | `Catalog.Write` |
| CRUD | `/api/v1/admin/articles` | `Content.Write` |
| POST | `/api/v1/admin/articles/{id}/publish` | `Content.Publish` |
| POST | `/api/v1/admin/media/upload-intent` | `Media.Write` |
| POST | `/api/v1/admin/media/{id}/complete` | `Media.Write` |
| CRUD | `/api/v1/admin/seo/redirects` | `SEO.Manage` |
| GET / PATCH | `/api/v1/admin/leads`, `/api/v1/admin/leads/{id}` | `Leads.Read`, `Leads.Manage` |
| GET | `/api/v1/admin/analytics/overview` | `Analytics.Read` |
| GET / PATCH | `/api/v1/admin/settings` | `Settings.Manage` |
| GET / POST / PATCH | `/api/v1/admin/users`, `/api/v1/admin/roles` | `Users.Manage` |

Có thể đóng gói các action dùng chung như `PublishProductCommand`, `ChangeProductSlugCommand`, `CreateLeadCommand`; controller mỏng, use-case test được. API contract dùng OpenAPI, sinh TypeScript client cho frontend để giảm sai DTO.

## 5.4. Search & pagination

Bắt đầu bằng PostgreSQL (`pg_trgm`, full-text search phù hợp tiếng Việt hoặc normalized search cho tên/SKU), filter có index và `LIMIT/OFFSET` với pageSize tối đa 48. Duy trì đúng `totalItems`; với tập dữ liệu lớn, có thể chuyển keyset pagination cho danh sách theo ngày. Không cần Elasticsearch tại MVP.

# 6. SEO architecture — phần ưu tiên cao nhất

## 6.1. Chính sách crawl và index

| Loại trang | Điều kiện | Quy tắc SEO |
|---|---|---|
| Sản phẩm | Published + nội dung đủ chất lượng | `index,follow`, self-canonical, Product markup phù hợp |
| Bài viết | Published + unique content | `index,follow`, Article/BlogPosting |
| Danh mục | Nội dung riêng, hữu ích | `index,follow`, CollectionPage/Breadcrumb |
| Search nội bộ | Có tham số `q` | `noindex,follow`, không đưa sitemap |
| Filter/sort params | Không phải landing page SEO chủ đích | canonical về URL gốc khi nội dung tương đương, hoặc noindex theo chính sách |
| Admin/Preview | Private | Auth; noindex không thay cho authentication |
| Draft/Archived | Không công khai | Không xuất hiện sitemap; xử lý 404/410/redirect hợp lý |

Tuyệt đối không để staging public có thể được Google index: dùng Basic Auth/IP restriction khi phù hợp; robots/noindex là lớp phụ. Tất cả URL trong sitemap phải canonical, trả HTTP 200, không redirect và thuộc phạm vi indexable.

## 6.2. On-page SEO fields và validation

- `seo_title` (fallback từ `name/title`, preview độ dài; không cắt cứng theo số ký tự vì SERP dùng pixel).
- `meta_description` (tóm tắt độc nhất, không nhồi từ khóa).
- `slug`, `canonical` (override chỉ người có quyền SEO; default self-canonical).
- `robots_index`, `og_title`, `og_description`, `og_image`.
- H1 duy nhất về mặt ý nghĩa, heading H2/H3 có cấu trúc; breadcrumbs và anchor text tự nhiên.
- ALT mô tả ảnh đúng thực tế, không spam keyword; gallery có ảnh đủ độ phân giải và lazy load ảnh dưới fold.
- `updated_at` chỉ thay đổi `lastmod` có ý nghĩa khi nội dung hiển thị công khai thực sự đổi.

## 6.3. Structured data

Dùng JSON-LD do Next.js sinh phía server:

| Trang | Schema đề xuất | Quy tắc |
|---|---|---|
| Home | `Organization`, `WebSite` | Thông tin shop thực tế |
| Category | `BreadcrumbList`, có thể `CollectionPage` | Đúng cấu trúc trang |
| Product | `Product`, `BreadcrumbList` | Tên/ảnh/mô tả đúng dữ liệu; chưa có Offer giá |
| Article | `Article`/`BlogPosting`, `BreadcrumbList` | Tác giả và ngày xuất bản khi có dữ liệu |
| FAQ thực tế | `FAQPage` nếu phù hợp | Không mặc định kỳ vọng Google hiển thị FAQ rich result |

Google phân biệt Product snippets (có thể áp dụng trang không mua trực tiếp) và Merchant listings (trang mua trực tiếp). **Không tạo `offers.price = 0`, rating/review giả hoặc availability không có bằng chứng.** Việc thêm schema không đảm bảo có rich result.

## 6.4. SEO publishing workflow (bắt buộc triển khai)

1. Editor lưu sản phẩm/bài viết ở Draft; backend validate dữ liệu và lưu audit.
2. Publisher chọn Publish; service kiểm tra cover/ALT, slug unique, nội dung, canonical/robots.
3. Trong transaction: cập nhật trạng thái + `published_at`, thêm `OutboxMessage(ContentPublished, entityId, affectedPaths)`.
4. Background worker đọc outbox, gọi **webhook revalidation có chữ ký HMAC** tại Next.js; retry và idempotency nếu lỗi.
5. Next.js xác thực HMAC + timestamp, gọi `revalidatePath` cho trang chi tiết, danh mục/hub liên quan và sitemap (hoặc tags phù hợp phiên bản được pin).
6. Trang tiếp theo được tạo lại theo cơ chế revalidation/caching; sitemap chỉ trả nội dung Published và indexable.
7. Log kết quả webhook, nếu thất bại thì cảnh báo và có endpoint retry cho Admin.

**Đặc biệt:** revalidation không đồng nghĩa Google index ngay. Search Console chỉ cho biết trạng thái crawl/index sau khi Google xử lý. Với đổi slug, tạo 301 và invalidate cả old/new URL.

## 6.5. Nội dung SEO cho lĩnh vực trà cụ

Chiến lược pillar/cluster (chỉ là kế hoạch biên tập, cần nghiên cứu keyword volume sau):

| Pillar | Cluster | Điểm chuyển đổi |
|---|---|---|
| Ấm Tử Sa | Đất Tử Sa; cách khai ấm; dáng Thạch Biều; dưỡng ấm | Trang danh mục & sản phẩm Tử Sa |
| Ấm gang | Ấm gang Nambu; cách vệ sinh; phân biệt ấm đun và ấm pha | Trang ấm gang |
| Bộ trà cao cấp | Chất liệu; chọn bộ theo nhu cầu; quà biếu tặng | Trang bộ trà / tư vấn |
| Kiến thức thưởng trà | Nhiệt độ, dụng cụ, cách pha theo loại trà | Bài đọc tiếp + danh mục phù hợp |

**Chất lượng > số lượng bài:** mô tả chính xác nguồn gốc, hình ảnh thật, trải nghiệm sử dụng có căn cứ; không dựng hàng loạt landing page mỏng chỉ khác vài từ khóa. Checklist biên tập gồm: search intent, keyword chính/phụ, unique H1, thực chứng ảnh, external/internal links hợp lý, CTA và cập nhật định kỳ.

## 6.6. Hiệu năng SEO (mục tiêu thiết kế, không phải cam kết trước đo)

- Core Web Vitals trên mobile tại percentile 75: **LCP ≤ 2,5 giây; INP ≤ 200ms; CLS ≤ 0,1** là ngưỡng hướng tới.
- Dùng `next/image`, ảnh hero có dimensions và priority phù hợp, thumbnails WebP/AVIF, tránh tải ảnh gốc nhiều MB.
- Không load widget chat, video hoặc script quảng cáo đồng loạt trong critical path; lazy-load theo hành vi và consent.
- Có ngân sách JS/CSS theo trang; test Lighthouse + WebPageTest + dữ liệu thực tế Search Console/CrUX.
- Những trang SEO thường xuyên truy cập ưu tiên prerender/ISR; Admin luôn dynamic và không index.

# 7. CMS & Media workflow

## 7.1. Tiptap và nội dung có cấu trúc

Lưu bản gốc editor dạng JSON (`content_jsonb`) và tạo HTML sau sanitize server-side (`content_html`) để render. Hỗ trợ heading, đoạn văn, danh sách, bảng nhỏ, ảnh có ALT, block quote, internal link, anchor; hạn chế nhúng HTML/script tùy ý. Có `content_revision` cho các mốc publish; tối thiểu giữ 5–10 bản gần nhất theo policy cấu hình.

## 7.2. Media pipeline

1. Admin yêu cầu upload-intent, backend kiểm tra permission, MIME allowlist, max size và loại media.
2. Backend tạo presigned URL ngắn hạn tới bucket private; trình duyệt upload trực tiếp R2.
3. Client gọi complete; backend xác nhận object tồn tại, checksum, kích thước; kiểm tra header/định dạng thực tế.
4. Worker tạo biến thể thumbnail/card/detail, metadata width/height, URL CDN; media nào chưa publish không public nếu cần bảo mật nội dung.
5. Editor sắp xếp gallery và nhập ALT có ý nghĩa; `is_cover` duy nhất trên một sản phẩm.

**Chính sách ảnh dự kiến:** ảnh chính tối thiểu khoảng 1200px cạnh dài nếu nguồn đủ chất lượng; thumbnail 400–600px, ảnh detail tối đa theo layout. Ưu tiên ảnh thực, màu sắc chính xác vì khách hàng cao cấp dựa nhiều vào chất liệu và lớp men.

# 8. Lead generation & chat

## 8.1. Hành trình chuyển đổi

Khách Google → bài viết hoặc danh mục → trang sản phẩm → click Zalo/Messenger hoặc form → người bán tư vấn. Hiển thị nút liên hệ ở header/footer/product, sticky CTA trên mobile có kiểm soát vị trí. Với link ngoài, tạo event trước khi mở link nhưng không chặn điều hướng nếu tracking lỗi.

## 8.2. Giải pháp chat MVP

- **Bắt buộc:** link Zalo của cửa hàng, Facebook Page/Messenger và form tư vấn gửi về backend.
- **Tùy chọn:** tawk.to/Crisp/Zalo OA widget nếu yêu cầu hộp chat trực tiếp trong site; kiểm tra điều khoản, tác động Core Web Vitals và quyền riêng tư của provider trước khi tích hợp.
- **Không tự viết realtime chat server ở MVP:** sẽ phát sinh agent console, định danh, WebSocket/SignalR, unread, offline queue, retention, spam filtering. Nâng cấp khi có volume cần thiết.
- **Phân định chỉ số:** nhấn vào Zalo = `contact_click`, không phải cuộc trò chuyện hoàn tất; form gửi thành công = `lead_created`; chỉ khi tích hợp provider/webhook có bằng chứng mới đo được số cuộc chat thật.

## 8.3. Lead pipeline

`New → Contacted → Qualified → ClosedWon/ClosedLost` (ClosedWon là kết quả tư vấn ghi nhận thủ công, không đồng nghĩa đơn hàng online). Lead lưu nguồn `utm_source`, `utm_medium`, `utm_campaign`, `referrer` khi hợp pháp và có căn cứ; không gửi PII như số điện thoại, tên hoặc nội dung chat lên GA4.

Form có ô đồng ý xử lý dữ liệu cần thiết, thông báo quyền riêng tư, server-side validation, chống spam honeypot/rate limit, thời gian lưu trữ và cơ chế xóa theo chính sách. Thông báo email cho admin có thể là tính năng P1.

# 9. Analytics & báo cáo

## 9.1. Hệ thống dữ liệu và ý nghĩa chỉ số

| Nguồn | Chỉ số | Giới hạn |
|---|---|---|
| Search Console | Impressions, clicks, CTR, average position, query/page | Có độ trễ, query ẩn, không phải số phiên |
| GA4 | Users, sessions, traffic channel, page views, engagement | Bị ảnh hưởng consent/ad blocker/attribution |
| Event nội bộ | Click Zalo/Messenger, submit form, sản phẩm liên hệ | Click không phải lead thực tế |
| Chat provider | Chats received/replied nếu hỗ trợ API | Tùy provider và quyền tích hợp |

**Tuyệt đối không cộng Search Console clicks và GA4 sessions thành “traffic”**; chúng là hệ quy chiếu khác nhau. Dashboard ghi rõ nguồn dữ liệu và múi giờ hiển thị.

## 9.2. Event specification

| Event | Trigger | Tham số cho phép |
|---|---|---|
| `view_item` | Xem trang chi tiết Published | `item_id`, `item_name`, `item_category` |
| `view_item_list` | Xem danh mục | `item_list_id`, `item_list_name` |
| `search` | Gửi tìm kiếm nội bộ | `search_term` đã lọc PII |
| `contact_zalo_click` | Click CTA Zalo | `item_id?`, `page_type`, `placement` |
| `contact_facebook_click` | Click CTA Facebook | `item_id?`, `page_type`, `placement` |
| `contact_phone_click` | Click tel: | `page_type`, `placement` |
| `generate_lead` | Backend xác nhận lưu form thành công | `lead_channel`, `page_type` (không có PII) |
| `chat_started` | Provider xác nhận cuộc trò chuyện bắt đầu | `provider` (nếu tích hợp được) |

Chống event trùng do hydration / Next.js route transitions bằng `event_id`, logic emit một lần mỗi hành động; event click nội bộ có rate limit và chống bot thô. Không ghi raw IP, user-agent đầy đủ hoặc khóa nhận dạng nhạy cảm vô thời hạn.

## 9.3. Data flow Dashboard

- **MVP:** cài GA4 + Search Console và xác thực, dùng dashboard gốc; Admin có widget tóm tắt nội bộ (lead và contact events) với nguồn rõ ràng.
- **P1:** Scheduled Job truy vấn GA4 Data API và Search Console Search Analytics API hàng ngày, ghi snapshot vào `analytics_daily` theo `date`, `source`, `page_path`, `metric_name`, `value` hoặc các bảng aggregate rõ cấu trúc.
- API Google có quotas; đồng bộ theo lịch (ví dụ 1 lần/ngày) và cache kết quả, không gọi API khi mỗi Admin mở Dashboard.
- GSC trả số liệu tìm kiếm trễ và có giới hạn hàng dữ liệu; dashboard hiển thị `data_until` và trạng thái lần đồng bộ gần nhất.
- Báo cáo sản phẩm “được quan tâm” nên có cả `views` và `contact_clicks`, đồng thời ưu tiên `leads_created` khi form được ghi nhận.

## 9.4. Privacy & consent

Cần trang Chính sách quyền riêng tư, xác định căn cứ xử lý và công cụ consent cho analytics/cookie phù hợp phạm vi người dùng và pháp luật áp dụng. Có thể dùng Google Consent Mode nhưng không coi đây là giải pháp pháp lý tự động. Nếu người dùng không đồng ý analytics không thiết yếu, website vẫn phải dùng được. Không đưa thông tin nhận diện người dùng vào URL, event hay logs ngoài nhu cầu cần thiết.

# 10. Authentication, authorization & security

## 10.1. Phương án bảo vệ Admin

- ASP.NET Core Identity quản lý user/password hash, lockout, reset, role/claims; đề xuất MFA cho Owner/Admin ngay khi có thể.
- Browser gọi Next.js Admin BFF, BFF giữ phiên bằng **Secure + HttpOnly + SameSite** cookie; API credential không nằm trong `localStorage` và không bị expose client.
- BFF proxy API bằng credential server-side, áp dụng CSRF protection cho request thay đổi dữ liệu, session expiry, rotation và kiểm tra Origin/Host.
- API Admin luôn kiểm tra role/permission; **không phụ thuộc việc ẩn nút trên giao diện**.

| Vai trò | Catalog | Content | Leads | Settings/Users |
|---|---|---|---|---|
| Owner | Tất cả | Tất cả | Tất cả | Tất cả |
| Admin | Write/Publish | Write/Publish | Read/Manage | Hạn chế Users |
| Editor | Draft/Write | Draft/Write | Không | Không |
| SEO Specialist | SeoManage | SEO content / suggestion | Không | Không |
| Sales/Consultant | Read | Read | Read/Manage | Không |

Permission nên lưu ở claims/policy (`Catalog.Read`, `Catalog.Write`, `Catalog.Publish`, `Content.Write`, `SEO.Manage`, `Leads.Manage`, `Analytics.Read`, `Users.Manage`), tách role khỏi hardcode nghiệp vụ.

## 10.2. Kiểm soát an toàn

- TLS, HSTS, CSP/headers hợp lý, input validation, output encoding, HTML sanitization trước khi render rich text.
- Rate limit cho login, lead form, tracking endpoint và upload-intent; CSRF cho mutations; kiểm soát CORS theo origin cụ thể.
- Upload allowlist MIME+magic bytes, giới hạn file, virus/malware scanning khi phù hợp; private bucket và short-lived presigned URLs.
- Secrets qua Docker secrets/env hoặc secret manager, không commit `.env`, không log token/phone/email đầy đủ.
- Database role ít quyền, dùng backup encrypt và kiểm thử restore; migration dùng credential riêng; Admin audit logs không thể sửa trực tiếp bằng API thông thường.
- Dependency audit, image scanner, SAST tối thiểu trong CI; cập nhật security patches định kỳ.

# 11. Deployment, scaling & vận hành

## 11.1. Môi trường

| Môi trường | Mục đích | Chính sách |
|---|---|---|
| Local | Dev Next/.NET/Postgres qua Compose | Seed data giả; `.env.local` |
| Staging | QA trước khi phát hành | Auth/IP restriction; noindex; không dữ liệu cá nhân thật |
| Production | Public site + Admin | HTTPS, backup, WAF, giám sát, deploy rollback |

**Topology MVP:** Cloudflare (DNS, CDN, WAF) → reverse proxy → `web` (Next Node) và `api` (.NET container); PostgreSQL chỉ internal network, R2 external storage. Có thể dùng managed PostgreSQL nếu ngân sách cho phép để giảm rủi ro vận hành VPS.

## 11.2. CI/CD

`Push → lint/typecheck → unit tests → API integration tests → Next build → Docker image build → vulnerability scan → deploy staging → e2e SEO/auth smoke → manual approve → production deploy → health checks`.

Migrations production chạy như bước kiểm soát trước rollout với backup, backward-compatible schema khi có thể. Có health endpoints `/health/live`, `/health/ready`, logging theo correlation ID, alerts lỗi 5xx, dung lượng đĩa, DB backup và tình trạng đồng bộ analytics.

## 11.3. Backup & recovery

- PostgreSQL: backup tự động ít nhất hằng ngày, retention ví dụ 14–30 ngày tùy ngân sách; có off-site copy, encryption và kiểm thử khôi phục định kỳ.
- R2 media: versioning hoặc retention theo nhu cầu; lưu manifest/metadata cho khôi phục consistency.
- Xác định RPO/RTO chính thức sau khi có SLA/budget; kiểm thử restore quan trọng hơn chỉ có file backup.
- Rollback phiên bản web/api; với DB migration có phương án forward-fix an toàn hơn rollback cứng khi dữ liệu đã thay đổi.

# 12. Non-functional requirements và acceptance criteria

## 12.1. Mục tiêu đo lường ban đầu

| Nhóm | Tiêu chí chấp nhận trước go-live |
|---|---|
| SEO HTML | View Source/SSR response chứa H1, content chính, title/meta/canonical/JSON-LD tương ứng |
| Index | sitemap chỉ chứa URL Published/indexable; robots và noindex nhất quán |
| Publishing | Publish mới, sửa slug, archive tạo phản hồi/cache/redirect đúng; lỗi webhook có retry |
| Performance | Phân tích CWV mobile; không có ảnh kích thước lớn không tối ưu, không layout shift đáng kể |
| Security | Không truy cập được Admin API nếu chưa đăng nhập; role thấp bị 403; chống CSRF/XSS cơ bản |
| Catalog | Filter/pagination ổn định; slug không trùng; gallery/ALT đúng; public DTO không có giá |
| Leads | Form tạo lead thành công đúng một lần; spam bị giới hạn; có consent và nguồn lead |
| Analytics | GA4 DebugView/GTM Preview thấy event thử; Search Console verified; không bắn PII |
| Deployment | Health checks OK, HTTPS, backup thành công, đã thử restore, staging private |

Các ngưỡng latency/availability cụ thể cần đo với traffic và hạ tầng thật; không cam kết con số trước benchmark. Mức độ ưu tiên cao là bảo đảm indexability, dữ liệu đúng và conversion tracking chính xác.

## 12.2. Test matrix

| Test | Kịch bản |
|---|---|
| Unit | Slug normalization, publish guard, RBAC policy, lead validation, redirects anti-loop |
| Integration | EF Core/Postgres constraints, product publish transaction + outbox, media complete |
| API contract | 200/201/204/400/401/403/404/409, pagination, error ProblemDetails |
| E2E | Admin login → tạo sản phẩm → ảnh → SEO → Publish → public URL → contact |
| SEO smoke | title, description, canonical, schema, sitemap, robots, 301, 404/410, preview noindex |
| Performance | Mobile Lighthouse, LCP element, responsive image sizes, script waterfall |
| Operations | DB restore, environment secrets, webhook retry, deployment rollback |

# 13. Backlog triển khai theo sprint/gate

| Gate | Phạm vi build | Definition of Done |
|---|---|---|
| S0 - Foundation | Repo, .NET API, Next, Postgres, migrations, CI, Docker, Identity/RBAC | Local + staging chạy; test pipeline xanh |
| S1 - Catalog Domain | Products, categories, media upload, admin CRUD, publish | Demo end-to-end admin → public API |
| S2 - Storefront | Home, category, product detail, search/filter, responsive UI | HTML server-render, mobile dùng tốt |
| S3 - SEO Engine | Meta, JSON-LD, sitemap, robots, redirects, publish revalidation | SEO smoke tests và staging crawl pass |
| S4 - CMS Content | Tiptap, Article, category, preview/revisions, nội dung SEO | Bài publish xuất hiện site/sitemap |
| S5 - Lead & Contact | Zalo/Facebook/phone, form, spam, lead Admin | Lead lưu an toàn, track đúng sự kiện |
| S6 - Analytics | GA4, GSC, Clarity tùy chọn, Admin số liệu nội bộ | Các nguồn số liệu phân biệt rõ |
| S7 - Hardening/Launch | Security, audit, backup/restore, monitoring, CWV, privacy/legal QA | Go-live checklist được ký duyệt |

Không nên tối ưu một dashboard tổng hợp phức tạp trước khi đã có nội dung thật và tracking ổn định. S0–S3 là chuỗi kỹ thuật quan trọng nhất; nội dung SEO có thể chuẩn bị song song với việc phát triển CMS.

## 13.1. Điểm quyết định cần xác nhận (ADR)

| ADR | Đề xuất mặc định | Khi nào đổi |
|---|---|---|
| ADR-01 | 1 Next.js app cho Storefront + Admin | Đội vận hành lớn, nhịp deploy khác biệt |
| ADR-02 | ASP.NET Core Modular Monolith | Có giới hạn tổ chức/scale thật sự cần tách |
| ADR-03 | PostgreSQL + JSONB specs | Bộ lọc phức tạp cần chuẩn hóa thuộc tính |
| ADR-04 | Outsourced live-chat provider | Cần CRM, chat routing, SLA, lưu toàn bộ message |
| ADR-05 | GA4/GSC là nguồn chuẩn, click nội bộ là bổ sung | Có data team/warehouse và mục tiêu sâu hơn |
| ADR-06 | No public price/cart/checkout | Shop được phép và sẵn sàng vận hành thương mại điện tử |

# 14. Mở rộng thương mại điện tử tương lai

Khi cần kích hoạt mua hàng trực tuyến, mở rộng bằng module Commerce riêng:

- `product_variants` (SKU biến thể, thuộc tính được chọn), `prices` (giá và hiệu lực), `inventory_items` (tồn kho).
- `carts`, `cart_items`, `orders`, `order_items`, `payments`, `payment_transactions`.
- `checkout`, shipping, promotion, order lifecycle, email, đối soát, báo cáo doanh thu.
- Public DTO v2 hoặc resource `offer` chỉ được xuất ra khi chính sách bán hàng được bật hợp lệ; SEO schema bổ sung `Offer` có giá thật và điều kiện khác khi phù hợp.
- Không lấy tiền từ client làm nguồn sự thật; server tính giá và chốt order snapshot.
- Không bật chức năng đặt hàng công khai trước khi hoàn tất rà soát yêu cầu pháp lý/công bố thông tin tương ứng.

**Điểm tránh rework:** stable IDs/slugs, category/product models, media service, publish lifecycle, auth roles, SEO canonical và notification/outbox đều tái sử dụng được.

# 15. Rủi ro và kiểm soát

| Rủi ro | Mức ưu tiên | Biện pháp |
|---|---|---|
| Tối ưu kỹ thuật nhưng nội dung mỏng/không có ảnh thật | Rất cao | Content plan + quy trình biên tập; SEO review mỗi bài |
| Xuất bản sai metadata hoặc duplicate URL | Rất cao | Publish validation, redirect, canonical policy, automated tests |
| Thêm script/chat làm chậm mobile | Cao | Lazy load, script budget, CWV monitoring |
| Dashboard thể hiện sai “traffic” và “khách mua” | Cao | Data dictionary, label nguồn, phân biệt click/lead/chat |
| XSS từ editor/ảnh upload | Cao | Sanitize server, CSP, MIME validation, security tests |
| Mất database/media | Cao | Backup, off-site, thử restore, runbook |
| Legal/consent không đáp ứng | Cao | Rà soát với đơn vị chuyên môn trước go-live; ghi nhận consent |
| Overengineering MVP | Trung bình | S0–S7 theo gate; không dựng microservices/Elasticsearch sớm |

# 16. Go-live checklist

- [ ] DNS + HTTPS + domain chuẩn và redirects http→https, www policy, canonical chính xác.
- [ ] Search Console property verified; sitemap gửi thành công; robots.txt và staging access kiểm tra.
- [ ] Mỗi danh mục chiến lược có SEO copy riêng; mỗi sản phẩm công khai có cover, ALT, summary/description hữu ích.
- [ ] Trang liên hệ, thông tin shop, quyền riêng tư và consent được rà soát.
- [ ] Không có giá, giỏ hàng, checkout, đặt hàng, offers giả trên bất kỳ route/API/schema nào.
- [ ] Login/MFA (nếu áp dụng), RBAC, CSRF, rate limit, file upload security đã test.
- [ ] GA4/GSC tracking được nghiệm thu và phân biệt đúng nguồn đo.
- [ ] Backup + restore rehearsal, CI/CD rollback, health monitoring hoạt động.
- [ ] Legal/compliance review hoàn tất trước khi công khai website.

# 17. Phụ lục A — Luồng xử lý mẫu

## A.1. Khi Admin xuất bản sản phẩm

```text
Admin → POST /admin/products/{id}/publish
      → Policy Catalog.Publish
      → Validate product + SEO + cover media
      → DB transaction (Published + Outbox)
      → Worker → Signed webhook /api/revalidate
      → Next.js invalidate product / category / sitemap
      → Public GET /san-pham/{slug} có HTML & metadata mới
```

## A.2. Khi khách nhấn Zalo từ trang sản phẩm

```text
Khách → /san-pham/{slug}
      → click CTA "Tư vấn qua Zalo"
      → gửi contact_zalo_click (nếu được phép, không chặn link)
      → mở Zalo link cấu hình trong Shop Settings
      → Admin analytics ghi click, KHÔNG tự nhận là lead hoàn tất
```

## A.3. Khi khách gửi yêu cầu tư vấn

```text
Khách → POST /api/v1/public/leads (form + consent)
      → validate / spam guard / rate limit
      → lưu Leads (New) + audit/notification intent
      → trả 201 + leadId
      → emit generate_lead (không PII)
      → Sales xem lead trong Admin, cập nhật trạng thái
```

# 18. Phụ lục B — Nguồn tham khảo kỹ thuật & pháp lý

Các liên kết dưới đây là nguồn để đối chiếu trước khi triển khai; thông số và luật lệ cần kiểm tra lại tại thời điểm go-live.

1. Microsoft .NET support policy — https://learn.microsoft.com/dotnet/core/releases-and-support
2. Next.js `generateMetadata` — https://nextjs.org/docs/app/api-reference/functions/generate-metadata
3. Next.js sitemap — https://nextjs.org/docs/app/api-reference/file-conventions/metadata/sitemap
4. Next.js ISR — https://nextjs.org/docs/app/guides/incremental-static-regeneration
5. Google Product structured data — https://developers.google.com/search/docs/appearance/structured-data/product
6. Google Product snippets — https://developers.google.com/search/docs/appearance/structured-data/product-snippet
7. Google SEO Starter Guide — https://developers.google.com/search/docs/fundamentals/seo-starter-guide
8. Search Console Search Analytics API — https://developers.google.com/webmaster-tools/v1/searchanalytics/query
9. Google Analytics Data API quotas — https://developers.google.com/analytics/devguides/reporting/data/v1/quotas
10. Google Consent Mode — https://developers.google.com/tag-platform/security/guides/consent
11. Luật Thương mại điện tử 122/2025/QH15 — https://vanban.chinhphu.vn/?docid=216503&pageid=27160
12. Nghị định 248/2026/NĐ-CP — https://vanban.chinhphu.vn/?docid=218747&pageid=27160

**Ghi chú pháp lý:** Điều 23 Nghị định 248/2026/NĐ-CP quy định về nền tảng TMĐT kinh doanh trực tiếp có chức năng đặt hàng trực tuyến và việc xác nhận thông báo trước vận hành. Mô hình catalog-only cần được rà soát đúng theo chức năng thật và quy trình tư vấn/giao kết của shop. Không được suy luận “không có giá/giỏ hàng = đương nhiên miễn mọi nghĩa vụ pháp lý”. Tài liệu này không thay thế ý kiến tư vấn pháp lý.

---

**KẾT LUẬN:** Bắt đầu từ Catalog + SEO publishing flow + CMS, triển khai Analytics đúng nghĩa và Lead Tracking từ ngày đầu; không đầu tư sớm vào thanh toán, microservices hoặc công cụ tìm kiếm nặng. Đây là bộ khung tối thiểu có khả năng mở rộng cho thương hiệu trà cụ cao cấp.
