# Xác minh local development và CI

**Ngày:** 31/10/2026  
**Phạm vi:** cấu hình chạy local, đóng gói, CI, health, logging/lỗi, bảo mật tích hợp. Không triển khai Production hoặc mở rộng nghiệp vụ.

## Prompt 7.1 — kết quả hiện tại

| Hạng mục | Trạng thái | Bằng chứng / giới hạn |
|---|---|---|
| Migration chain và an toàn DB | PASS | PostgreSQL kiểm thử riêng `teapremium_tests_p71` trên cổng `5547`, dữ liệu nằm trong tmpfs; bảng `__EFMigrationsHistory` có đủ 5 migration: `InitialCatalog`, `AddAdminIdentity`, `AddMediaUploadSessions`, `AddArticles`, `AddLeadFoundation`. Không sửa migration cũ, không dùng database Development hoặc xóa volume hiện có. Fixture từ chối tên database không bắt đầu `teapremium_test`. |
| Toolchain | PASS | Host: .NET SDK `10.0.302`, Node `24.18.1`, pnpm `12.16.1`, Docker `29.6.2`, Compose `5.3.1`. `.nvmrc`, CI và Docker web pin Node `22.22.2`; `global.json`, CI và Docker API pin SDK `10.0.302`. `docker manifest inspect` có image SDK `10.0.302` và Node `22.22.2-slim`; tag SDK `10.0.204` không có manifest. Không đổi pin trong lượt 7.1. |
| Docker/health | PASS | `docker build` API/web pass; API cô lập `/health/live` và `/health/ready` trả 200 trên DB đã migrate. Prompt 7 trước đó đã kiểm tra live 200/ready 503 trên DB chưa migrate. Compose config pass với password giả chỉ dùng để parse cấu hình. Không chạy migration khi API startup. Stack thử dùng tmpfs không mount volume, đã dừng đúng 3 container `p71` và xóa riêng network thử sau khi kiểm tra. |
| Backend restore/build/tests | PASS | Locked restore, Release build 0 warning/error; 28/28 unit và 38/38 integration, 0 skip. Các nhóm test bao phủ Catalog, Identity/Auth/CSRF, Media, CMS, Leads, health, PostgreSQL và noindex visibility. |
| Frontend install/lint/typecheck/SEO/HTTP/build | PASS | Frozen install, lint, typecheck, `test:seo` 6/6, `test:storefront` 10/10 sau khi thêm probe BFF; `pnpm build` PASS sau HTTP fixture để trả `.next` về cấu hình local. |
| SEO semantics | PASS | Product/Category/Article Published có `robotsIndex=false` vẫn trả Public API và HTML 200 với robots noindex/canonical; sitemap loại chúng. Draft/Archived/slug thiếu trả 404. Bài list `/kien-thuc` vẫn vào sitemap nếu có bài Published, kể cả khi mọi detail noindex. Có backend và HTTP regression tests. |
| Security regression | PASS | Admin permission/CSRF/cookie và no-store có integration tests; BFF từ chối cross-origin write 403 và không public-cache response. Public lead không chấp nhận ID sản phẩm Draft/Archived; body 202 không trả PII. Rate limit lead 5/phút và contact events 30/phút có integration tests. Public catalog lọc trường commerce/nội bộ trong specifications. Tracked private-path/signature scan: 0; password Admin thử và email lead thử không xuất hiện trong log API cô lập. Phạm vi scan không bao gồm toàn bộ Git history hoặc bí mật có định dạng khác. |
| Browser E2E trên stack cô lập | PASS | Chromium/Chrome 3/3: Admin login; Product Draft → Publish → public HTML → Archive với MediaAsset fixture; Article Draft/preview → Publish → Archive; lead có consent và Admin status. Fixture ảnh chỉ nằm trong DB kiểm thử, **không** xác nhận upload R2 thật. |
| Development đang chạy | PASS | Sau khi dọn stack thử, `GET http://localhost:5080/health/live`, `/health/ready` và `GET http://localhost:3000/admin/login` vẫn trả 200. Trước đó `GET http://localhost:3000/` cũng trả 200. Không gửi mutation tới API/database Development. |
| Next dev thứ hai cùng checkout | BLOCKED | `next dev --port 3002` báo lock do server trên cổng 3000 đang chạy; không dừng server của người dùng. Browser E2E dùng Next standalone cô lập ở cổng 3002. |
| R2 config/adapter/flow tĩnh | PASS | Compose map đủ 5 biến R2 vào API, chỉ public base URL vào web; S3 endpoint mặc định, presigned PUT ký `Content-Type`, complete xác minh size/MIME/magic bytes và tạo MediaAsset. Next `remotePatterns` lấy từ public base URL khi build. |
| R2 ghi/đọc object thật | PASS | Sau khi người dùng cập nhật quyền, S3 PUT và PUT bằng URL ký giống adapter ứng dụng đều trả 200; HEAD trả 200 đúng kích thước, public r2.dev GET trả 200; DELETE cả hai object thử trả 204. Không tạo MediaAsset trong database. |
| R2 CORS và PUT với browser Origin | PASS | Sau khi người dùng cập nhật bucket CORS, preflight `OPTIONS` cho presigned PUT từ `http://localhost:3000` trả 204 với `Access-Control-Allow-Origin` đúng; PUT có Origin trả 200 với allow-origin đúng, HEAD/public GET và Next image optimization đều 200, cleanup object 204. S3 GetCORS và Cloudflare REST API vẫn trả 403 với token object-scoped, nhưng chính sách CORS thực tế đã được xác minh qua HTTP. |
| Admin UI upload-intent → complete | NOT RUN | Chưa dùng phiên đăng nhập Admin để tạo MediaAsset qua UI/API; probe đã xác minh R2 và CORS nhưng chưa xác nhận toàn bộ luồng ứng dụng. Có thể thử tại `/admin/media` trên web Development đang chạy. |
| GitHub Actions remote | NOT RUN | Workflow có PostgreSQL service, restore/build/test và pnpm install/lint/typecheck/SEO/HTTP/build. Chưa commit/push nên không có remote run để xác nhận. |
| HTTPS Production, GA4/GSC, Lighthouse, R2 Production | NOT RUN | Ngoài phạm vi local/CI, thiếu môi trường/chứng chỉ/account hoặc dữ liệu thật. |

**FAIL còn mở:** không có trong các kiểm tra đã hoàn tất. Các lần chạy lỗi trung gian và nguyên nhân được ghi dưới đây; kết quả PASS chỉ áp dụng cho lần chạy lại cuối.

### Cấu hình R2 Development và dịch vụ đang chạy

Người dùng cung cấp bucket `teapremium-media-dev` và public URL `https://pub-45885a813ed14f6d8fa60b4003a5a2aa.r2.dev`. Đã cấu hình secret trong root `.env` bị Git ignore; web chỉ nhận public URL trong `apps/web/.env.local`. Không ghi token/access key/secret vào repository hoặc báo cáo. `docker compose --env-file .env -f infra/docker/compose.yml config --quiet` PASS. API R2 chạy ở `http://localhost:5081` với readiness 200, web Next dev ở `http://localhost:3000` với `/` và `/admin/login` 200; web trỏ API 5081. Next image proxy chấp nhận hostname r2.dev (URL thử không có object trả 404, không phải lỗi host bị cấm). API cũ ở 5080 và PostgreSQL Development 5432 vẫn chạy; lệnh dừng API cũ bị cơ chế duyệt tự động từ chối, nên không can thiệp.

Probe R2 dùng project C# tạm trong `backend/artifacts/r2-probe` (thư mục bị ignore), AWSSDK.S3 cùng phiên bản backend, đọc credential từ `.env` chỉ trong process. Lệnh chạy: `dotnet run --project backend/artifacts/r2-probe/R2Probe.csproj --configuration Release`. Lần đầu SDK PUT trả 501 vì AWS SDK mặc định thêm cơ chế checksum/streaming R2 không hỗ trợ; đã đặt `DisablePayloadSigning` và `DisableDefaultChecksumValidation` cho probe theo tài liệu Cloudflare. Trước khi người dùng cập nhật quyền, PUT trả 403; sau cập nhật quyền, SDK PUT và PUT bằng URL ký đều OK nhưng preflight CORS còn 403. **Lượt cuối sau khi người dùng cập nhật CORS:** bucket/list OK; preflight 204 và đúng allow-origin; SDK PUT và PUT bằng URL ký có browser Origin đều 200; HEAD đúng kích thước, public GET và Next image optimization 200, DELETE cả hai object thử 204; không còn object thử. File env tạm dùng khởi động container đã được xóa; root `.env` vẫn giữ cấu hình Development.

**Bước xác minh còn lại:** đăng nhập Admin ở `/admin/media`, chọn một ảnh thử, kiểm tra upload-intent → PUT → complete tạo MediaAsset rồi mở public URL. Token object-scoped hiện có không dùng được để gọi Cloudflare REST API quản lý bucket; cần Admin Read & Write nếu sau này tự động hóa việc sửa CORS qua REST API. Xem [Cloudflare token permissions](https://developers.cloudflare.com/r2/api/tokens/) và [CORS policy](https://developers.cloudflare.com/r2/buckets/cors/).

### Lệnh đã chạy trong 7.1

Chạy từ repository root. Password database thử được sinh trong shell và lấy lại từ container thử; không ghi giá trị vào tài liệu. `TEAPREMIUM_TEST_CONNECTION_STRING` trỏ **chỉ** `teapremium_tests_p71`, không phải `teapremium` Development. Riêng lệnh Compose config đặt `POSTGRES_PASSWORD=ci-config-only` trong shell rồi xóa ngay sau lệnh; đây là giá trị thử để parse cấu hình, không dùng cho database.

```text
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet build backend/TeaPremium.sln --configuration Release --no-restore
dotnet test backend/TeaPremium.sln --configuration Release --no-build
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web test:storefront
corepack pnpm --dir apps/web build
corepack pnpm --dir apps/web test:e2e
docker compose --env-file .env.example -f infra/docker/compose.yml config --quiet
docker build --file backend/Dockerfile --target runtime --tag teapremium-p71-api backend
docker build --quiet --file apps/web/Dockerfile --target runtime --tag teapremium-p71-web --build-arg NEXT_PUBLIC_API_BASE_URL=http://localhost:5091/api/v1 --build-arg NEXT_PUBLIC_SITE_URL=http://localhost:3002 --build-arg NEXT_PUBLIC_SITE_NAME=TeaPremium apps/web
dotnet tool restore
dotnet tool run dotnet-ef migrations script --idempotent --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext --output backend/artifacts/migrations-p71.sql
```

HTTP smoke cuối: `GET http://localhost:5091/health/live`, `/health/ready`, `GET http://localhost:3000/`, `/admin/login` và `GET http://localhost:3002/admin/login` đều trả 200. Kiểm tra Git cuối: `git diff --check` exit 0 (chỉ cảnh báo LF/CRLF). SQL script idempotent sinh 33.675 byte trong thư mục ignore; không apply vào Development/Production.

Lệnh `test:e2e` cần `E2E_ISOLATED_DATABASE=true`, `E2E_WEB_BASE_URL`, `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD` và `E2E_MEDIA_ASSET_ID` của MediaAsset fixture trong DB thử. Không đặt marker này khi web/API trỏ database Development. Bài product E2E dùng fixture metadata ảnh, còn R2 live phải được kiểm tra riêng.

### Lỗi phát hiện và cách xử lý

1. Public query và frontend client dùng `robotsIndex` để ẩn entity Published; noindex bị 404. Đã bỏ điều kiện này khỏi visibility, giữ filter noindex/canonical ở metadata và sitemap, thêm regression tests.
2. Lead/contact event chấp nhận `productId` của Draft/Archived vì store kiểm tra mọi sản phẩm. Đã giới hạn tham chiếu tới sản phẩm và category Published; cùng lỗi 422 cho ID không public.
3. Integration fixture trước đó chỉ yêu cầu có connection string, nên có thể trỏ nhầm DB Development. Đã bắt buộc tên database thử bắt đầu `teapremium_test` và thêm safety tests.
4. Article sitemap bỏ `/kien-thuc` nếu chỉ có bài Published noindex, dù listing vẫn indexable. Đã sửa và test.
5. Test HTTP BFF đầu tiên thất bại do assertion cuối còn giả định mọi request fixture đều là Public API; probe Admin mới là request có chủ đích. Đã tách assertion và chạy lại 10/10 PASS. Browser product E2E ban đầu có selector trùng nhãn; đã sửa, chạy lại 3/3 PASS. Lần khởi tạo PostgreSQL thử đầu dùng static `RandomNumberGenerator.GetBytes` không có trong PowerShell/.NET host; đã dùng giá trị ngẫu nhiên sinh bằng GUID và chạy lại PASS.
6. Next dev thứ hai bị khóa bởi server cùng checkout ở cổng 3000. Không can thiệp server đang chạy; dùng standalone cho test cô lập.

### Checklist local development

1. Sao chép `.env.example` thành `.env` bị Git ignore; đặt `POSTGRES_PASSWORD` riêng. Giữ `SITE_INDEXABLE=false` khi chưa có domain/nội dung thật.
2. Chạy PostgreSQL, gọi service `migrate` **thủ công**, rồi khởi động API/web theo [README](../README.md). Kiểm tra `/health/live` và `/health/ready`.
3. Bootstrap Admin chỉ trong Development, giữ credential ngoài repository. Để chạy integration, cấp DB riêng tên `teapremium_test...`; không dùng DB Development có dữ liệu.
4. Để chạy browser E2E, dựng web/API trên DB thử, cấp Admin thử và MediaAsset fixture, đặt `E2E_ISOLATED_DATABASE=true`, rồi chạy `corepack pnpm --dir apps/web test:e2e`. Dọn đúng test stack/fixture sau khi kiểm tra; không xóa volume hoặc DB hiện có.
5. Với R2 dev, cấu hình 5 biến ở root `.env`, public read URL riêng S3 endpoint, CORS đúng origin và `Content-Type`; rebuild API/web rồi thử upload-intent → PUT → complete → public GET. Xem [media-storage.md](media-storage.md). Chưa có cấu hình thật thì ghi BLOCKED.

## Kết quả Prompt 7 trước đợt hardening (lịch sử)

| Kiểm tra | Trạng thái | Bằng chứng và giới hạn |
|---|---|---|
| `docker compose --env-file .env.example -f infra/docker/compose.yml config --quiet` | PASS | Cấp `POSTGRES_PASSWORD=ci-config-only` trong shell; file ví dụ để trống password và Compose từ chối nếu không cấp. |
| `docker compose --env-file .env -f infra/docker/compose.yml build api web migrate` | PASS | Ba image multi-stage build được; API image đã build lại sau sửa proxy cuối. Node runtime dùng standalone, không mang toàn bộ source/build dependencies. |
| Compose local trên project kiểm thử riêng | PASS | PostgreSQL, API và web healthy; host ports kiểm thử `5435`, `5090`, `3001`. Các port mặc định là `5432`, `5080`, `3000`. API chạy UID `1654`, web chạy UID `1000`. |
| Migration Development thủ công | PASS | `docker compose ... run --rm migrate` áp dụng 5 migrations trong database kiểm thử. Khi chạy API trên database mới chưa migrate, `/health/live` trả 200, `/health/ready` trả 503 và không có bảng public: API không tự migrate khi startup. |
| HTTP smoke trên Compose | PASS | Live/ready, Public Articles API, `/`, `/kien-thuc`, `/admin/login`, `/lien-he` trả 200 trên database đã migrate. Admin API và BFF anonymous trả 401 với `Cache-Control: no-store`. |
| `dotnet restore backend/TeaPremium.sln --locked-mode` | PASS | SDK `10.0.302`. |
| `dotnet build backend/TeaPremium.sln --configuration Release --no-restore` | PASS | 0 warning, 0 error, chạy lại sau sửa code cuối. |
| `dotnet test backend/TeaPremium.sln --configuration Release --no-build` | PASS | 28/28 unit và 31/31 integration, 0 skip; integration dùng PostgreSQL riêng. Gồm readiness khi DB thiếu, auth/CSRF, quyền, CMS, leads và public projection. |
| `corepack pnpm --dir apps/web install --frozen-lockfile` | PASS | Lockfile không đổi do cấu hình local/CI này. |
| `corepack pnpm --dir apps/web lint` | PASS | 0 ESLint warnings. |
| `corepack pnpm --dir apps/web typecheck` | PASS | Next typegen và TypeScript. |
| `corepack pnpm --dir apps/web test:seo` | PASS | 6/6 tests. |
| `corepack pnpm --dir apps/web build` | PASS | Production Next build, có standalone output; đã build lại sau HTTP fixture test để trả `.next` về cấu hình local. |
| `corepack pnpm --dir apps/web test:storefront` | PASS | 8/8 HTTP contract tests sau production build. |
| Playwright Chromium với Compose standalone | PASS | 2/2: tạo/preview/publish/archive bài viết và gửi lead có consent rồi quản lý trong Admin. Dùng tài khoản/database Development tạm trong project kiểm thử riêng. Đây không phải audit giao diện toàn diện. |
| SQL migration script idempotent | PASS | `dotnet tool run dotnet-ef migrations script --idempotent --project backend/src/TeaPremium.Infrastructure --startup-project backend/src/TeaPremium.Api --context AppDbContext --output backend/artifacts/migrations.sql` sinh file 33.675 byte trong thư mục bị Git ignore; chưa apply vào Production. |
| Kiểm tra file bí mật đang được Git track | PASS | `git ls-files` không thấy `.env`, `.env.local`, `.pem`, `.p12`, `.pfx`, `.key`; `git grep` không thấy một số mẫu private key/token phổ biến. Đây là kiểm tra tĩnh trên tracked files, không phải quét toàn bộ Git history hoặc bảo đảm không có bí mật dưới dạng khác. |
| Kiểm tra phân quyền/cache/public data | PASS | Test API xác nhận 401/403 và `no-store` cho Admin; code dùng antiforgery ở Admin controller base và BFF xác nhận Origin. Public write endpoints có rate limit; public catalog chỉ truy vấn Published/indexable và lọc khóa commerce/nội bộ trong specifications. HTTP storefront tests kiểm tra nội dung public và hydration theo fixture, không thay thế duyệt nội dung nhân viên nhập. |
| GitHub Actions trên remote | NOT RUN | Workflow đã có service PostgreSQL và các lệnh restore/build/test/install/lint/typecheck/test/build; cần push/PR để xác nhận runner thật. Không tạo commit/push trong lượt này vì working tree đã chứa thay đổi Prompt 4B/5/6 chưa chốt. |
| Nginx HTTPS với domain/chứng chỉ thật | NOT RUN | Chỉ thêm `infra/nginx/https.example.conf` để review, không deploy. |
| R2, GA4, Lighthouse và Production migration | NOT RUN | Thiếu cấu hình/dịch vụ thật hoặc nằm ngoài phạm vi local/CI. |

**FAIL hiện còn:** không có trong các lệnh đã chạy. Lần build Docker API đầu thất bại do analyzer CA1861 trên mã migration EF sinh tự động; đã giới hạn ngoại lệ analyzer vào riêng thư mục migration, build lại PASS. Playwright lần đầu phát hiện BFF so sánh Origin với cổng nội bộ của Next standalone; đã sửa để dùng `NEXT_PUBLIC_SITE_URL`, chạy lại 2/2 PASS.

## Cách tái hiện

Chạy từ repository root. Sao chép `.env.example` thành `.env`, đặt mật khẩu PostgreSQL local riêng và giữ `SITE_INDEXABLE=false` cho đến khi có domain/nội dung thật. Xem [README](../README.md) cho lệnh PowerShell/bash đầy đủ, bootstrap Admin Development và đổi port. Trên môi trường kiểm thử, đặt `TEAPREMIUM_TEST_CONNECTION_STRING` trỏ tới PostgreSQL riêng trước khi chạy integration tests. Không ghi connection string chứa password vào tài liệu, command line chia sẻ hoặc CI log.

```text
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet build backend/TeaPremium.sln --configuration Release --no-restore
dotnet test backend/TeaPremium.sln --configuration Release --no-build
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web build
corepack pnpm --dir apps/web test:storefront
```

Frontend local chạy bằng Node `24.18.1` có sẵn trên máy, còn web Docker image build bằng Node `22.22.2` đã pin. CI cũng pin Node `22.22.2`, nhưng kết quả remote vẫn là **NOT RUN**. Backend local và Docker dùng SDK `10.0.302`.

## Chính sách migration và vận hành

Compose local có service `migrate` profile `tools`, chỉ chạy khi gọi `docker compose ... run --rm migrate`. API startup không gọi `Database.Migrate()`. Production phải sinh SQL script, review nội dung/target, backup database và chuẩn bị rollback hoặc forward fix trước khi apply; không dùng Compose local để deploy. `/health/live` chỉ báo tiến trình, `/health/ready` kiểm tra PostgreSQL và migration còn thiếu. Logging API là JSON có correlation/trace ID, Problem Details trả lỗi HTTP; request logging không ghi body hoặc PII đầy đủ.

Khi chạy sau TLS reverse proxy, cấu hình `TRUSTED_PROXY_IP` bằng đúng IP của proxy mà API nhìn thấy để xử lý forwarded headers; proxy là nơi chuyển HTTP sang HTTPS. Chỉ mẫu cấu hình được thêm, chưa kiểm tra chứng chỉ hay network Production.

## Prompt 8 — SEO redirects và Site Settings (31/10/2026)

Kiểm tra trên Windows với .NET SDK `10.0.302`, Node `24.18.1`, pnpm `12.16.1`, Next `16.4.3`. PostgreSQL test chạy trong container tạm `postgres:17.11-alpine3.24`, database `teapremium_test_p8` trên cổng localhost 5548; `TEAPREMIUM_TEST_CONNECTION_STRING` chỉ trỏ database này. Database Development và volumes cũ không bị xóa hay migrate trong lượt Prompt 8.

| Trạng thái | Lệnh / kiểm tra | Kết quả |
|---|---|---|
| PASS | `dotnet restore backend/TeaPremium.sln --locked-mode` | Locked restore thành công. |
| PASS | `dotnet build backend/TeaPremium.sln --configuration Release --no-restore` | 0 warnings, 0 errors. |
| PASS | `dotnet test backend/TeaPremium.sln --configuration Release --no-build` | 36 unit + 42 integration, 0 fail/skip; integration dùng PostgreSQL test riêng và áp dụng toàn chuỗi migration tại đó. |
| PASS | `corepack pnpm --dir apps/web lint` | ESLint 0 warnings. |
| PASS | `corepack pnpm --dir apps/web typecheck` | Next typegen + TypeScript. |
| PASS | `corepack pnpm --dir apps/web build` | Production build thành công. |
| PASS | `corepack pnpm --dir apps/web test:seo` | 6/6. |
| PASS | `corepack pnpm --dir apps/web test:storefront` | 12/12 HTTP contracts; 301/200, noindex/404, canonical, sitemap, settings. Test tự build Next production với API fixture, không phải browser E2E. |
| PASS | `git diff --check` và quét mã nguồn cho credential được người dùng cung cấp | Không có whitespace error hoặc credential trong tracked source. Git có cảnh báo chuẩn hóa LF/CRLF. |
| NOT RUN | Migration trên database Development/Production | Không được thực hiện để giữ nguyên dữ liệu; chỉ database test chạy migration. |
| NOT RUN | Browser E2E, R2 runtime trong lượt Prompt 8, GitHub Actions remote | Không nằm trong bài tự động đã chạy; không báo PASS. |

Lượt integration đầu có 39/40 PASS: test schema cũ kỳ vọng chính xác danh sách bảng trước Prompt 8. Đã bổ sung `seo_redirects`, `site_settings` vào assertion; lượt cuối 42/42 PASS. Lượt HTTP đầu sai kỳ vọng `Location` tuyệt đối trong khi Next trả `Location` tương đối cùng origin; đã đổi assertion sang URL được resolve. Test Settings ban đầu kỳ vọng canonical gốc có dấu `/`, nhưng helper canonical chủ động chuẩn hóa thành origin; đã sửa assertion theo policy hiện có. Không có failure tồn đọng ở các lệnh tự động cuối.

Giới hạn: chưa chạy browser UI thực tế cho `/admin/settings` và đổi slug trong Admin; chưa áp migration Prompt 8 vào Development, nên API/web đang chạy local từ trước chưa dùng được chức năng mới cho tới khi migrate và restart. CI remote, Production, R2/GA4 thực tế không được suy ra từ các test trên.
