# TeaPremium

Foundation cho website catalog trà cụ cao cấp ưu tiên SEO, custom CMS và lead generation. MVP không có giá công khai, giỏ hàng, đặt hàng hoặc thanh toán trực tuyến.

## Toolchain đã pin

- .NET SDK `10.0.302`, target framework `net10.0`
- Node.js `22.22.2` LTS
- pnpm `12.16.1`
- Next.js `16.4.3`, React `19.3.0`, TypeScript `5.9.3`, Tailwind CSS `4.3.3`, ESLint `9.39.5`
- PostgreSQL `17.11` Alpine cho local Docker Compose

`global.json`, `.nvmrc`, `apps/web/package.json` và `pnpm-lock.yaml` giữ các phiên bản tương thích. Trên máy không cho Corepack tạo shim toàn cục, dùng `corepack pnpm` thay cho lệnh `pnpm` trực tiếp.

## Cấu trúc

```text
apps/web/                              Next.js storefront + Admin
backend/src/TeaPremium.Api/            HTTP, health, OpenAPI, middleware
backend/src/TeaPremium.Application/    Use cases và ports
backend/src/TeaPremium.Domain/         Domain thuần
backend/src/TeaPremium.Infrastructure/ Adapter hạ tầng
backend/tests/                          Unit + integration tests
infra/docker/                           Compose local: PostgreSQL, API, web, migration thủ công
infra/nginx/                            Cấu hình reverse proxy HTTPS mẫu, chưa deploy
docs/                                   Thiết kế và quyết định kiến trúc
```

## Chuẩn bị môi trường

Yêu cầu: .NET SDK 10, Node.js 22 LTS, Corepack và Docker Desktop/Engine có Compose.

Windows PowerShell:

```powershell
if (!(Test-Path .env)) { Copy-Item .env.example .env }
if (!(Test-Path apps/web/.env.local)) { Copy-Item apps/web/.env.example apps/web/.env.local }
corepack prepare pnpm@12.16.1 --activate
```

Nếu `corepack enable` báo thiếu quyền ghi vào `Program Files`, không cần chạy lại với quyền Admin; các lệnh `corepack pnpm` bên dưới vẫn hoạt động.

Linux/macOS:

```bash
[ -f .env ] || cp .env.example .env
[ -f apps/web/.env.local ] || cp apps/web/.env.example apps/web/.env.local
corepack enable
corepack prepare pnpm@12.16.1 --activate
```

Các file `.env` và `.env.local` bị Git bỏ qua. Điền `POSTGRES_PASSWORD` bằng mật khẩu riêng cho máy này; để trống thì Compose từ chối khởi động. Tên site trong ví dụ chỉ là tên làm việc, `SITE_INDEXABLE=false`. Không đưa dữ liệu shop hoặc secret thật vào `.env.example`.

## Chạy toàn bộ bằng Docker Compose

Compose bind PostgreSQL, API và web vào loopback của máy; các container gọi nhau qua tên service. Chạy migration **thủ công** trước khi mở API/web. PowerShell:

```powershell
if (!(Test-Path .env)) { Copy-Item .env.example .env }
# Sửa .env: điền POSTGRES_PASSWORD riêng, giữ SITE_INDEXABLE=false khi chưa có domain/nội dung thật.
docker compose --env-file .env -f infra/docker/compose.yml up -d postgres
docker compose --env-file .env -f infra/docker/compose.yml build migrate
docker compose --env-file .env -f infra/docker/compose.yml run --rm migrate
docker compose --env-file .env -f infra/docker/compose.yml up -d --build api web
docker compose --env-file .env -f infra/docker/compose.yml ps
```

Bash:

```bash
[ -f .env ] || cp .env.example .env
# Sửa .env: điền POSTGRES_PASSWORD riêng, giữ SITE_INDEXABLE=false khi chưa có domain/nội dung thật.
docker compose --env-file .env -f infra/docker/compose.yml up -d postgres
docker compose --env-file .env -f infra/docker/compose.yml build migrate
docker compose --env-file .env -f infra/docker/compose.yml run --rm migrate
docker compose --env-file .env -f infra/docker/compose.yml up -d --build api web
docker compose --env-file .env -f infra/docker/compose.yml ps
```

| Dịch vụ | Mặc định trên máy | Biến đổi cổng trong `.env` |
|---|---|---|
| PostgreSQL | `127.0.0.1:5432` | `POSTGRES_PORT` |
| API | `http://localhost:5080` | `API_PORT` |
| Web/Admin | `http://localhost:3000` | `WEB_PORT`; cập nhật `NEXT_PUBLIC_SITE_URL` cùng cổng |

`http://localhost:5080/health/live` kiểm tra tiến trình; `/health/ready` còn kiểm tra kết nối PostgreSQL và migration còn thiếu. Admin tại `http://localhost:3000/admin/login`; storefront tại `/`. Web SSR gọi API qua `http://api:5080`, còn form public từ trình duyệt gọi `http://localhost:5080`. `NEXT_PUBLIC_SITE_URL` phải khớp chính xác origin trình duyệt đang dùng, gồm giao thức và cổng, để Admin CSRF hoạt động. Các biến `NEXT_PUBLIC_*` và `R2_PUBLIC_BASE_URL` dùng khi build image web: sau khi đổi URL, cổng, CDN hoặc GA4 ID, chạy lại `up -d --build web`. Chỉ điền cả bộ biến R2 khi có bucket/CORS/CDN thật; để trống thì upload báo unavailable, không giả thành công.

Nếu chưa có Admin Development, đặt `AuthBootstrap__Email` và `AuthBootstrap__Password` trong môi trường shell hiện tại rồi chạy lệnh một lần:

```powershell
docker compose --env-file .env -f infra/docker/compose.yml exec -e AuthBootstrap__Email -e AuthBootstrap__Password api dotnet TeaPremium.Api.dll bootstrap-admin
```

```bash
docker compose --env-file .env -f infra/docker/compose.yml exec -e AuthBootstrap__Email -e AuthBootstrap__Password api dotnet TeaPremium.Api.dll bootstrap-admin
```

Lệnh bootstrap chỉ được phép trong `Development`; không ghi mật khẩu vào repository. Dừng stack nhưng giữ dữ liệu bằng `docker compose --env-file .env -f infra/docker/compose.yml down`. Chỉ dùng `down --volumes` với database local mà bạn chủ động muốn xóa.

## Chạy từng thành phần độc lập

### PostgreSQL local

Thay `POSTGRES_PASSWORD` trong `.env`, sau đó chạy:

```powershell
docker compose --env-file .env -f infra/docker/compose.yml up -d postgres
docker compose --env-file .env -f infra/docker/compose.yml ps
```

Compose này chỉ dành cho local. Cả ba cổng host đều bind `127.0.0.1`; Production không dùng file này và không expose cổng database.

Dừng các service Compose mà vẫn giữ named volume:

```powershell
docker compose --env-file .env -f infra/docker/compose.yml down
```

### Backend API

API đọc PostgreSQL connection string từ ASP.NET Core user secrets hoặc biến môi trường chuẩn. Không commit connection string thật vào appsettings.

Thiết lập local bằng user secrets (dùng đúng password trong `.env` local):

```powershell
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=127.0.0.1;Port=5432;Database=teapremium;Username=teapremium;Password=<local-password>" --project backend/src/TeaPremium.Api
```

Hoặc đặt biến môi trường `ConnectionStrings__Postgres` trong môi trường chạy API. Sau đó:

```powershell
dotnet restore backend/TeaPremium.sln
dotnet run --project backend/src/TeaPremium.Api --launch-profile http
```

Kiểm tra:

```powershell
Invoke-RestMethod http://localhost:5080/health/live
Invoke-RestMethod http://localhost:5080/health/ready
Invoke-RestMethod http://localhost:5080/openapi/v1.json
```

### Migration development

Migration không tự chạy khi API startup. Chỉ với database development/test đã xác nhận đúng target:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext
```

Trước Production, sinh SQL **idempotent**, review từng thay đổi, backup database đích và chuẩn bị rollback/forward fix trước khi apply. Lệnh PowerShell (sau khi đã cấu hình connection string cho design-time API):

```powershell
New-Item -ItemType Directory -Force backend/artifacts | Out-Null
dotnet tool restore
dotnet tool run dotnet-ef migrations script --idempotent --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext --output backend/artifacts/migrations.sql
```

Bash:

```bash
mkdir -p backend/artifacts
dotnet tool restore
dotnet tool run dotnet-ef migrations script --idempotent --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext --output backend/artifacts/migrations.sql
```

Không tự chạy EF migration ở API startup; không dùng `docker compose run migrate` trên Production. File SQL trong `artifacts/` bị Git bỏ qua. Mẫu reverse proxy HTTPS ở [infra/nginx/https.example.conf](infra/nginx/https.example.conf); cần thay domain/cert/upstream, cấu hình `TRUSTED_PROXY_IP` là IP proxy thực sự thấy từ API, đặt `PUBLIC_WEB_ORIGIN`/`NEXT_PUBLIC_SITE_URL` theo origin HTTPS thật và build `NEXT_PUBLIC_API_BASE_URL` theo HTTPS public API. Mẫu này chưa được deploy.

Trên Linux/macOS có thể dùng `curl` với cùng URL.

### Frontend

```powershell
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web dev
```

Mở `http://localhost:3000/admin/login`. Admin Catalog dùng Identity cookie, BFF và Catalog API thật. Xem [hướng dẫn Admin](docs/admin-catalog-ui.md) và [cấu hình R2](docs/media-storage.md). Khi có R2, đặt `R2_PUBLIC_BASE_URL` giống nhau cho API và Next.js để hiển thị ảnh qua CDN.

Storefront tại `http://localhost:3000` dùng Public API thật và SSR. Các trang danh mục, sản phẩm, tìm kiếm, giới thiệu và liên hệ đã có; không seed dữ liệu shop. Mặc định toàn site `noindex` cho tới khi cấu hình domain/thương hiệu thật và bật `SITE_INDEXABLE`. Xem [Storefront SEO](docs/storefront-seo.md) để cấu hình kênh liên hệ, cache và kiểm tra SEO/Lighthouse.

CMS bài viết `/kien-thuc` và form liên hệ/lead đã có trong Prompt 6. Sau khi chạy migration, xem [hướng dẫn CMS và Lead](docs/content-leads.md) để thử Tiptap, Publish, consent, CORS và cấu hình GA4 thật nếu dùng.

## Build và test

```powershell
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet build backend/TeaPremium.sln --configuration Release --no-restore
dotnet test backend/TeaPremium.sln --configuration Release --no-build

corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web test:storefront
corepack pnpm --dir apps/web build
```

CI chạy các quality gates và kiểm tra cú pháp Compose trong `.github/workflows/ci.yml`. Kết quả đã chạy trên máy này nằm ở [verification.md](docs/verification.md).

`test:storefront` tự build và chạy Next production cùng Public API fixture trên các port local ngẫu nhiên, rồi đóng chúng. Đây là HTTP integration, không phải browser E2E. Không chạy cùng `next dev/start/build`; chạy lại `build` với env thật sau test vì `.next` chứa cấu hình fixture.

Integration tests yêu cầu PostgreSQL thật và biến `TEAPREMIUM_TEST_CONNECTION_STRING` trỏ database riêng có tên bắt đầu bằng `teapremium_test`; fixture từ chối tên database Development mặc định. CI cấp database service cô lập. Test không fallback sang EF InMemory.

## Khắc phục lỗi local thường gặp

- Docker daemon chưa chạy: mở Docker Desktop/Engine rồi chạy `docker compose ... ps` lại.
- Compose báo thiếu `POSTGRES_PASSWORD`: điền mật khẩu riêng vào `.env`; không sửa `.env.example` để chứa secret.
- Cổng `5432`, `5080` hoặc `3000` đang dùng: đổi `POSTGRES_PORT`, `API_PORT`, `WEB_PORT` trong `.env`; cập nhật `NEXT_PUBLIC_SITE_URL` rồi build lại web.
- `/health/live` trả 200 nhưng `/health/ready` trả 503: kiểm tra PostgreSQL/connection string và `docker compose ... logs api postgres`; migration/schema chưa có thì chạy service `migrate` thủ công.
- Web không gọi được API: kiểm tra `API_BASE_URL=http://api:5080/api/v1` trong container và `NEXT_PUBLIC_API_BASE_URL` đã được build với cổng host đúng. CORS chỉ cho origin web đã cấu hình.
- Login Admin thất bại: chạy bootstrap một lần cho Development, kiểm tra cùng origin/port và cookie/CSRF; không lưu token trong localStorage.
- Upload ảnh báo 503: chưa có bộ cấu hình R2 thật hoặc bucket/CORS/CDN chưa đúng. Các phần catalog/CMS/lead vẫn thử được khi chưa có R2.
- Windows chặn DLL khi chạy .NET trực tiếp: dùng Compose Linux cho local hoặc chạy test trên Linux/CI; không tắt Application Control để xử lý.

## Giới hạn hiện tại

- Đổi slug Published của Product, Category và Article tạo redirect 301; Site Settings công khai được quản lý tại `/admin/settings`. Xem [SEO redirects](docs/seo-redirects.md) và [Site Settings](docs/site-settings.md). Cần áp dụng migration `AddSeoRedirectsAndSiteSettings` bằng bước migrate có review trước khi chạy API bản mới.
- Chưa xác nhận R2, GA4/GSC account thật hoặc live chat hai chiều. GA4 chỉ tải sau consent và khi đã cấu hình Measurement ID thật.
- Chưa có chính sách tự động lưu giữ/xóa lead; phải chốt trước Production.
- Giá công khai, cart, order, checkout, payment và bảng commerce không thuộc MVP này.

Contract và quy tắc Catalog backend được mô tả tại `docs/catalog-api.md`; luồng Identity/cookie/BFF tại `docs/auth.md`.
