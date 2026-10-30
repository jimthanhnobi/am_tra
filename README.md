# TeaPremium

Foundation cho website catalog trà cụ cao cấp ưu tiên SEO, custom CMS và lead generation. MVP không có giá công khai, giỏ hàng, đặt hàng hoặc thanh toán trực tuyến.

## Toolchain đã pin

- .NET SDK `10.0.204`, target framework `net10.0`
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
infra/docker/                           PostgreSQL local-only
docs/                                   Thiết kế và quyết định kiến trúc
```

## Chuẩn bị môi trường

Yêu cầu: .NET SDK 10, Node.js 22 LTS, Corepack và Docker Desktop/Engine có Compose.

Windows PowerShell:

```powershell
Copy-Item .env.example .env
Copy-Item apps/web/.env.example apps/web/.env.local
corepack prepare pnpm@12.16.1 --activate
```

Nếu `corepack enable` báo thiếu quyền ghi vào `Program Files`, không cần chạy lại với quyền Admin; các lệnh `corepack pnpm` bên dưới vẫn hoạt động.

Linux/macOS:

```bash
cp .env.example .env
cp apps/web/.env.example apps/web/.env.local
corepack enable
corepack prepare pnpm@12.16.1 --activate
```

Các file `.env` và `.env.local` bị Git bỏ qua. Giá trị trong `.env.example` chỉ là placeholder local, không phải credential dùng chung hoặc Production.

## Chạy từng thành phần độc lập

### PostgreSQL local

Thay `POSTGRES_PASSWORD` trong `.env`, sau đó chạy:

```powershell
docker compose --env-file .env -f infra/docker/compose.yml up -d
docker compose --env-file .env -f infra/docker/compose.yml ps
```

Compose này chỉ dành cho local và bind PostgreSQL vào `127.0.0.1`. Production không dùng file này và không expose cổng database.

Dừng database mà vẫn giữ named volume:

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

Trước Production phải sinh/review SQL, backup và chạy migration như deploy step riêng.

Trên Linux/macOS có thể dùng `curl` với cùng URL.

### Frontend

```powershell
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web dev
```

Mở `http://localhost:3000` và `http://localhost:3000/admin`. Trang Admin hiện chỉ là placeholder noindex, chưa có auth và không gọi Admin API.

## Build và test

```powershell
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet build backend/TeaPremium.sln --configuration Release --no-restore
dotnet test backend/TeaPremium.sln --configuration Release --no-build

corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web build
```

CI chạy cùng các quality gates trong `.github/workflows/ci.yml`.

Integration tests yêu cầu PostgreSQL thật và biến `TEAPREMIUM_TEST_CONNECTION_STRING`; CI cấp database service cô lập. Test không fallback sang EF InMemory.

## Chưa triển khai

- Catalog application use cases/API/Admin UI; Content/CMS/lead modules
- ASP.NET Core Identity, Admin authentication/authorization
- Tiptap, Cloudflare R2, GA4, Search Console hoặc live chat
- Giá, cart, order, checkout, payment và mọi bảng commerce
