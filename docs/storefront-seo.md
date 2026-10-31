# Storefront SEO

Storefront dùng Public API hiện có; không seed sản phẩm, gọi Admin API, gửi session cookie tới Public API hoặc truy cập database từ Next.js. Dữ liệu fixture chỉ nằm trong `apps/web/tests`.

## Routes và dữ liệu

| Route | Nội dung / chính sách |
|---|---|
| `/` | Danh mục và 6 sản phẩm mới nhất từ API; ảnh hero lấy từ cover thật; empty state khi catalog trống. API chưa có cờ featured nên không gắn nhãn “nổi bật” giả. |
| `/danh-muc/[slug]` | Tên, mô tả HTML đã sanitize, 12 sản phẩm/trang; `?page=2` có title và canonical riêng; page không hợp lệ/vượt giới hạn trả 404. |
| `/san-pham/[slug]` | Gallery, tên, mô tả, thông số scalar, thương hiệu nếu có, breadcrumbs và các kênh liên hệ được cấu hình. |
| `/gioi-thieu` | Văn bản thật từ `SITE_ABOUT`; thiếu thì hiển thị hồ sơ chưa công bố, noindex và không vào sitemap. |
| `/lien-he` | Hotline/Zalo/Facebook/địa chỉ từ env; thiếu thì ghi rõ chưa công bố. Không có kênh liên hệ thì noindex, không vào sitemap. |
| `/tim-kiem?q=...` | Form GET gọi `GET /public/products?search=...`; phân trang giữ từ khóa. Luôn noindex, canonical `/tim-kiem`; không chặn crawl bằng robots.txt để crawler đọc được noindex. |

API lọc trạng thái Published trước khi tính tổng/phân trang. Product cần primary category Published. `robotsIndex=false` chỉ điều khiển metadata noindex và sitemap, không ẩn entity Published: Product, Category và Article vẫn trả HTML **200** và Public API vẫn trả dữ liệu. Draft, Archived và slug không tồn tại trả HTTP **404**. Lỗi mạng/API 5xx hiển thị lỗi chung; không biến lỗi dịch vụ thành catalog trống hoặc 404.

## Cấu hình thật

Copy `apps/web/.env.example` sang `.env.local` cho local; Production đặt env qua hệ thống deploy. Các trường chưa xác minh để trống:

| Biến | Ý nghĩa |
|---|---|
| `NEXT_PUBLIC_SITE_NAME` | Tên thương hiệu thật. `TeaPremium` trong template chỉ là tên làm việc. |
| `NEXT_PUBLIC_SITE_URL` | Origin canonical HTTPS, không có path/query/fragment/credentials; chọn www/non-www trước khi index. |
| `NEXT_PUBLIC_SITE_DESCRIPTION` | Mô tả thương hiệu được duyệt. |
| `API_BASE_URL` | URL server tới API, gồm `/api/v1`; không phải biến NEXT_PUBLIC. |
| `R2_PUBLIC_BASE_URL` | CDN ảnh công khai khớp backend, không có credential. Next Image chỉ cho phép host/path này. |
| `SITE_ABOUT`, `SITE_ADDRESS` | Văn bản giới thiệu và địa chỉ do shop cung cấp; không nhận HTML. |
| `SITE_HOTLINE` | Số hotline thật, hiển thị và liên kết `tel:`. |
| `SITE_ZALO_URL` | HTTPS trên `zalo.me`/`www.zalo.me`. |
| `SITE_FACEBOOK_URL` | HTTPS trên `facebook.com`, `www.facebook.com`, `m.facebook.com` hoặc `fb.com`. |
| `SITE_INDEXABLE` | Mặc định `false`; chỉ đặt `true` khi tên/domain/nội dung thật đã sẵn sàng. |

Index chỉ bật khi có tên cấu hình, origin HTTPS không phải localhost/example và `SITE_INDEXABLE=true`. Đây là chốt cấu hình, không tự xác minh quyền sở hữu thương hiệu/domain. Staging giữ `false`. Chưa cấu hình thì robots.txt disallow toàn site, metadata noindex và sitemap trống. Không tạo Organization/LocalBusiness/address/certification schema từ placeholders.

Các biến `NEXT_PUBLIC_*` được Next inline tại build; phải **build lại** khi đổi chúng. Đổi profile server cần restart deployment; thay CDN cần build lại vì nó nằm trong next.config. Không deploy artifact của bài HTTP test: test cố ý dùng domain/nhãn fixture và ghi vào `.next`.

## Cache và trạng thái HTTP — Next.js 16.4.3

- Storefront layout `dynamic = "force-dynamic"`; mọi fetch Public API dùng `cache: "no-store"`. Sitemap/robots cũng dynamic. Không có ISR TTL, Data Cache, tag revalidation hay webhook publish giả.
- React `cache()` chỉ gộp lần đọc trùng trong một server render (metadata + page), không giữ catalog qua các request.
- Tắt `cacheComponents` và `partialPrefetching` trong next.config. Các khai báo Admin `instant=false` cũ được bỏ vì chỉ hợp lệ khi Cache Components bật; protected Admin vẫn `force-dynamic`.
- `htmlLimitedBots: /.*/` chờ metadata trước khi stream với mọi user agent. Storefront không có loading/Suspense boundary bao quanh nội dung chính. HTTP integration xác nhận notFound là 404 thật, tránh “soft 404” HTTP 200 do streaming quá sớm.
- Mỗi request HTTP mới thấy publish/archive/noindex từ API mà không cần purge. Tắt robotsIndex giữ trang 200 nhưng cập nhật metadata noindex và loại URL khỏi sitemap. Tab đã mở/lịch sử điều hướng phía client có thể giữ UI cũ tới khi tải lại; không có live push. Proxy/CDN không được ép cache public HTML, RSC, sitemap hoặc lỗi API trái policy này.
- Đổi lại, TTFB phụ thuộc API; mỗi request dùng timeout 10 giây, không trả bản Published cũ khi API lỗi. Nếu sau này cần ISR, phải thiết kế invalidation cho publish/archive/category/slug và test visibility trước.
- Image optimization dùng `next/image`, dimensions thật, `sizes`, chỉ preload ảnh đầu; thumbnail/ảnh còn lại lazy. Không thêm carousel client library. Rich HTML đi qua sanitizer backend; CSS giữ ảnh/bảng trong viewport.

## Metadata, sitemap và JSON-LD

Metadata theo entity dùng SEO title/description/OG overrides, canonical đã validate và robots. Trang category tiếp theo có hậu tố trang; query filter/sort phụ trên detail/category bị noindex. BreadcrumbList dùng các đường dẫn thật. Product JSON-LD chỉ có tên, tóm tắt, URL, SKU/chất liệu/brand/ảnh khi tồn tại. Không có Offer, giá, availability, rating hoặc review; không hứa hẹn đủ điều kiện Google Product rich result.

OG image dùng `ogImageAssetId` nếu ID nằm trong gallery và có publicUrl, nếu không dùng cover. Public API hiện không có endpoint resolve ảnh SEO độc lập; category không tự bịa ảnh OG. Card social vẫn có title/description/canonical.

Sitemap đọc toàn bộ cây danh mục và **mọi** trang sản phẩm, rồi lấy detail để kiểm tra robotsIndex/canonical và khả năng render. Nó bỏ detail 404, noindex, canonical sang URL khác, trang query, Admin, Preview và tìm kiếm. Home dùng cùng chuẩn canonical không trailing slash như Next. `/gioi-thieu` và `/lien-he` chỉ được thêm khi có nội dung/kênh thật tương ứng. Không dùng ngày build làm lastmod vì Public DTO chưa cung cấp thời điểm cập nhật nội dung.

Detail lookups chạy tối đa 4 đồng thời; đây là chi phí N+1 có chủ đích do list DTO thiếu SEO flags. API lỗi làm sitemap trả lỗi, không xuất sitemap rỗng 200 giả. Trên 49.990 entity thì fail rõ để bổ sung sitemap index/split sitemap trước khi vượt giới hạn. Snapshot có thể thay đổi giữa lúc sinh sitemap và lúc crawler ghé; không thể hứa URL tồn tại vĩnh viễn.

### Slug và redirect

Backend hiện **chưa có redirect record/table/API**, và chặn đổi slug của entity Published bằng 409. Vì vậy không có record để resolve 301; slug lạ/cũ chưa biết trả 404. Không suy luận redirect bằng tên hay tìm gần đúng, không thêm backend workflow ngoài phạm vi tích hợp.

Khi bổ sung redirect workflow, cần contract rõ cho source path → canonical destination, chặn loop/chain/open redirect, bảo đảm đích Published/indexable, và test HTTP **301** trước khi cho đổi slug Published. `permanentRedirect()` mặc định của Next là 308 nên không tự dùng nó để tuyên bố đạt yêu cầu 301.

## Tests tự động

```powershell
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web test:storefront
# Build lại với env triển khai thật sau bài test HTTP.
corepack pnpm --dir apps/web build
```

- `test:seo`: metadata/canonical/social/robots, JSON-LD và escape script injection, pagination, sitemap qua nhiều trang, loại noindex/missing/canonical khác, API failure.
- `test:storefront`: build production Next, chạy API fixture và Next trên loopback port ngẫu nhiên, kiểm tra HTML trước hydration, Product/Category/Article Published noindex 200 với robots noindex, Draft/Archived 404, search, pagination, sitemap URL 200/canonical, empty state, archive không stale cache và API 5xx. Sentinel price/admin không xuất hiện trong toàn bộ response HTML (gồm JSON-LD/RSC); kiểm tra không gửi cookie/token hoặc gọi Admin API. Tự đóng server khi kết thúc. Không chạy đồng thời với Next dev/start/build trên cùng checkout.
- Đây là HTTP integration, chưa phải Playwright browser E2E. Fixture thể hiện contract; không chứng minh API triển khai thật đã có dữ liệu.
- Backend `CatalogApiTests` với PostgreSQL thật kiểm tra publish guard, Published 200/Draft 404, noindex product/category và lọc thông số riêng tư cả nested JSON. Chạy các lệnh restore/build/test ở README với `TEAPREMIUM_TEST_CONNECTION_STRING` trỏ database test cô lập.

DTO projection không thể xác định giá hoặc thông tin riêng được gõ vào mô tả văn bản; nội dung công khai vẫn cần người biên tập kiểm duyệt. Frontend không serialize nguyên DTO/Admin entity vào Client Component.

## Kiểm tra thủ công trước khi index

1. Với API/PostgreSQL/R2 thật của môi trường test, tạo category + product Draft trong Admin. Điền nội dung, SEO, ảnh đúng ALT; xác nhận public URL 404. Chuyển InReview → Publish; mở public URL 200, xem source có H1/mô tả, canonical/OG/robots và JSON-LD. Nhấp Zalo/Facebook/hotline đã cấu hình và kiểm tra đúng đích, không cần gửi tin.
2. Archive product: URL 404 và không còn trong list/search/sitemap. Tắt robotsIndex trên product/category/article Published: URL 200 với robots noindex, vẫn có trong Public API/list phù hợp, nhưng không còn trong sitemap. Tải lại request mới thay vì chỉ kiểm tra tab/router cache cũ. Kiểm tra published slug PATCH bị 409 trong khi chưa có redirect workflow.
3. Mở sitemap, kiểm tra mỗi URL bằng HTTP GET: 200, indexable và canonical tự trỏ; không có Admin/Preview/search/draft/noindex/query. Kiểm tra `/tim-kiem?q=...` noindex và trang 2 category có canonical/title riêng.
4. Chrome DevTools → Lighthouse, chạy **Mobile**, Navigation trên production build ở home/category/product/contact, kiểm tra Performance, Accessibility, Best Practices, SEO. Chạy ít nhất 3 lần, ghi median và điều kiện mạng/API/ảnh; chưa có số đo thì không ghi điểm giả. Theo dõi LCP/CLS/INP, ảnh preload đúng một ảnh chính, không tải ảnh gốc khổng lồ.
5. Kiểm tra viewport 375/768/1440px, không tràn ngang (trừ vùng gallery/table chủ động), tab bàn phím, skip link, label tìm kiếm, focus, ALT, gallery anchors và pagination khi tắt JavaScript. Xem source/network để tìm trường Admin/price/sentinel/token; kiểm tra link preview/admin không nằm trong sitemap.
6. Dùng Schema.org validator cho Product/BreadcrumbList; Search Console URL Inspection trên domain đã xác minh khi có quyền. Publish chỉ làm URL có thể crawl; Google tự quyết định thời điểm crawl/index và rich result.

## Kết quả chạy trong phiên triển khai

Các số test và hành vi noindex dưới đây là **lịch sử ngày 30/10/2026**, trước đợt sửa semantics 7.1. Kết quả và hành vi hiện tại nằm trong [verification.md](verification.md).

Ngày 2026-10-30, Windows, Node 24.18.1 trên máy (repository vẫn pin 22.22.2):

| Lệnh đã chạy từ repo root | Kết quả |
|---|---|
| `corepack pnpm --dir apps/web install --frozen-lockfile` | Pass, không đổi lockfile/dependency. |
| `corepack pnpm --dir apps/web lint` | Pass. |
| `corepack pnpm --dir apps/web typecheck` | Pass. |
| `corepack pnpm --dir apps/web test:seo` | 5/5 pass; Node cảnh báo module type khi đọc TS trực tiếp, không có test fail. |
| `corepack pnpm --dir apps/web test:storefront` | 7/7 pass (1 suite test + 6 subtests), production Next + API fixture. |
| `corepack pnpm --dir apps/web build` | Pass sau test HTTP, artifact fixture đã được build lại bằng env workspace. |
| `git diff --check` | Pass; Git thông báo chuẩn hóa LF/CRLF trên vài file. |

Máy chỉ có SDK 10.0.302, repository pin 10.0.204; giữ nguyên pin. Các lệnh backend thực tế:

```powershell
dotnet exec 'C:\Program Files\dotnet\sdk\10.0.302\dotnet.dll' restore backend/TeaPremium.sln --locked-mode
dotnet exec 'C:\Program Files\dotnet\sdk\10.0.302\dotnet.dll' build backend/TeaPremium.sln --configuration Release --no-restore
dotnet exec 'C:\Program Files\dotnet\sdk\10.0.302\dotnet.dll' test backend/TeaPremium.sln --configuration Release --no-build
```

Restore và build trên Windows pass (0 warnings/errors). Lần test Windows: 21 unit tests pass; integration runner bị Windows Application Control chặn nạp `TeaPremium.Api.dll` với mã `0x800711C7`, nên lần chạy Windows không pass. Không thay đổi policy của hệ điều hành.

Đã kiểm chứng lại trong **container Linux với SDK đúng pin 10.0.204**, PostgreSQL `17.11-alpine3.24` cô lập và source gồm `.editorconfig`, `global.json`, `backend/` (không dùng bin/obj từ Windows). Image nền là `mcr.microsoft.com/dotnet/sdk:10.0.302`; SDK 10.0.204 được tải riêng từ Microsoft release metadata, đối chiếu SHA-512 và giải nén vào `/opt/sdk204`. Không đổi `global.json`. Các lệnh thực tế bên trong container, tại `/work`:

```sh
/opt/sdk204/dotnet --version
/opt/sdk204/dotnet restore backend/TeaPremium.sln --locked-mode
/opt/sdk204/dotnet build backend/TeaPremium.sln --configuration Release --no-restore
/opt/sdk204/dotnet test backend/TeaPremium.sln --configuration Release --no-build
```

Kết quả Linux: **restore/build pass, 0 warnings/errors; 21 unit tests + 25 integration tests pass, không skip**. Bao gồm Publish → public 200, Draft/Archived/noindex 404, list/search visibility, thông số nested không rò dữ liệu riêng và dữ liệu Admin vẫn được giữ nguyên. Các container/database test và server fixture đã dừng sau kiểm tra. CI chưa được kích hoạt từ phiên local này. Chưa chạy browser E2E hoặc đo Lighthouse thực tế.
