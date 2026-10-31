# Admin UI/UX — Prompt 9B

**Ngày kiểm tra:** 31/10/2026
**Phạm vi:** Giao diện Admin của Next.js. Không thay đổi Identity, BFF, API, database hoặc migration.

## Thiết kế và phạm vi trang

Admin dùng nền sáng, panel giấy, forest green cho thao tác chính và clay cho liên kết/focus. Token và pattern nằm ở `apps/web/src/app/admin/admin.css`, chỉ áp dụng trong `.admin-root`; storefront giữ design system riêng của Prompt 9A. Heading dùng Lora, nội dung dùng Be Vietnam Pro. Sidebar desktop nhóm Catalog, Content, Leads, Settings theo quyền; topbar có breadcrumb, menu tài khoản và Logout. Mobile dùng menu có nút mở/đóng, hỗ trợ Escape, đưa focus vào menu khi mở và trả focus về nút mở khi đóng.

Đã rà soát và cập nhật `/admin`, `/admin/products`, `/admin/products/new`, `/admin/products/[id]/edit`, `/admin/categories`, `/admin/brands`, `/admin/media`, `/admin/articles`, `/admin/articles/new`, `/admin/articles/[id]/edit`, `/admin/article-categories`, `/admin/leads`, `/admin/settings` và `/admin/login`. Các component dùng chung cho nút, input, card, badge, dialog và empty state có màu, khoảng cách và focus nhất quán. Không thay đổi contract mutation hoặc kiểm tra quyền ở API.

Protected Admin có skeleton chung khi chuyển trang đang chờ dữ liệu server. Trang lỗi vẫn có nút thử lại; các thao tác ghi tiếp tục dùng toast thành công/thất bại hiện có.

Product list giữ tìm kiếm, trạng thái, danh mục, sort và pagination ở URL; liên kết sửa mang đường quay lại để không mất filter. Trên desktop có bảng, mobile có card. Article list và Lead list giữ server pagination; API hiện chỉ hỗ trợ `page`/`pageSize` nên giao diện không thêm ô tìm kiếm/lọc giả. Trạng thái và hành động hiển thị theo quyền hiện có. Lead PII chỉ render sau khi route kiểm tra `Leads.Read`.

Product editor có điều hướng theo section, thông tin cơ bản, danh mục/thương hiệu, thông số, mô tả, gallery, SEO và xuất bản; lưu và publish tách riêng, có cảnh báo dữ liệu chưa lưu, validation summary và thanh hành động mobile. Article editor có toolbar H2/H3, đậm/nghiêng, danh sách, trích dẫn, liên kết HTTP(S), khu nội dung rộng để đọc, preview HTML đã lưu, SEO thu gọn và xác nhận chuyển trạng thái. Ảnh trong thân bài chưa được backend renderer hỗ trợ; nút “Ảnh bìa” đưa tới trường ảnh bìa và UI nói rõ giới hạn này. Không đưa raw HTML vào editor.

## Visual review

Chạy `node tests/capture-admin-fixture.mjs` từ `apps/web`: chụp 14 màn hình tại **1440×900** và **390×844**, tổng **28 ảnh**, lưu ở `artifacts/prompt9b/` (Git ignore). Script chạy Next production server với Admin API fixture chỉ đọc trên loopback; dữ liệu trong ảnh là fixture có nhãn kiểm thử, không ghi PostgreSQL. Tất cả route trả 200, không có tràn ngang hoặc ảnh lỗi. Script cũng kiểm tra menu mobile mở/đóng bằng Escape và focus, Logout hiển thị trong menu tài khoản, quay về danh sách giữ filter, validation summary của form Product/Article và cấu trúc form Article hợp lệ.

Đã xem ảnh dashboard, product list/editor, article editor, media library và leads ở cả hai kích thước. Sau lượt chụp đầu đã sửa chữ nút chính có tương phản thấp, chiều cao nút upload media trên mobile và nền thanh lưu editor; lượt chụp thứ hai đã xác nhận các điểm đó. Ảnh full-page chụp thanh lưu `sticky` tại vị trí viewport ban đầu; khi cuộn trong trình duyệt thanh vẫn bám đáy viewport.

Review hành vi còn phát hiện form URL liên kết lồng trong form Article; đã đổi thành nhóm điều khiển không có `<form>` lồng nhau để Enter không submit cả bài viết. Liên kết tới section trong Product editor được bỏ qua bởi cảnh báo rời trang khi có thay đổi chưa lưu. Browser fixture kiểm tra hai hành vi này.

Fixture chỉ kiểm tra giao diện và tương tác không ghi dữ liệu. Nó **không** chứng minh đăng nhập/đăng xuất thật, permission của Identity, CSRF, lưu API, publish, lead workflow hoặc R2. Các luồng đó cần Playwright với database thử nghiệm riêng.

## Kiểm tra đã chạy

| Lệnh từ repository root, trừ khi ghi khác | Kết quả | Giới hạn |
|---|---|---|
| `corepack pnpm --dir apps/web lint` | PASS | ESLint, 0 warning |
| `corepack pnpm --dir apps/web typecheck` | PASS | Next typegen và TypeScript |
| `corepack pnpm --dir apps/web build` | PASS | Next 16.4.3 production build sau chỉnh sửa form liên kết cuối cùng |
| `corepack pnpm --dir apps/web test:seo` | PASS, 6/6 | SEO unit/contract |
| `corepack pnpm --dir apps/web test:storefront` | PASS, 12/12 | HTTP contract, gồm BFF same-origin và no-store |
| `node tests/capture-admin-fixture.mjs` từ `apps/web` | PASS, 28/28 | Fixture UI, không phải auth/API E2E |
| `corepack pnpm --dir apps/web exec playwright test --list` | PASS, 3 bài được nhận diện | Chỉ xác nhận test discovery, không chạy test |
| `dotnet build backend/TeaPremium.sln --configuration Release --no-restore` | PASS | 0 warning, 0 error; không sửa backend trong Prompt 9B |
| `dotnet test backend/tests/TeaPremium.UnitTests/TeaPremium.UnitTests.csproj --configuration Release --no-build` | PASS, 36/36 | Unit tests; không thay thế integration test |
| `git diff --check` | PASS | Có cảnh báo LF/CRLF trên Windows, không có whitespace error |
| `corepack pnpm --dir apps/web test:e2e` | BLOCKED, chưa chạy | Bài E2E yêu cầu `E2E_ISOLATED_DATABASE=true` cùng tài khoản Admin trên database riêng; phiên này chưa có cấu hình test độc lập. Không chạy trên database Development |

Máy kiểm tra dùng Node **24.18.1**, pnpm **12.16.1**. Version này là môi trường local thực tế; CI/production pin theo cấu hình repository và không được xem là đã chạy trong lượt này. Backend integration tests chưa chạy vì phiên này không có `TEAPREMIUM_TEST_CONNECTION_STRING` cho database test riêng. Không có kiểm thử thực tế đăng nhập, publish hoặc lead trên database riêng trong lượt này.

## Chạy lại và nghiệm thu với dữ liệu thật

Từ repository root:

```powershell
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web build
cd apps/web
node tests/capture-admin-fixture.mjs
```

```bash
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web build
cd apps/web
node tests/capture-admin-fixture.mjs
```

Script chụp ảnh cần Chrome/Chromium khả dụng cho Playwright. Để nghiệm thu E2E thật, dùng API + Next trỏ tới **PostgreSQL test riêng**, đặt `E2E_ISOLATED_DATABASE=true`, `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD` cho tài khoản Development có quyền phù hợp, rồi chạy `corepack pnpm --dir apps/web test:e2e`. Không dùng database Production hoặc Development đang có dữ liệu cần giữ. Thử login/logout, tạo và publish Product, tạo/preview/publish/archive Article, gửi Lead và cập nhật trạng thái, kiểm tra Editor bị chặn ở API. Sau test chỉ dọn các record do test tạo trong database test.

## Giới hạn còn lại

- Chưa chạy Playwright ghi dữ liệu với Identity, PostgreSQL và R2 thật trong lượt Prompt 9B.
- Chưa đo bằng công cụ accessibility chuyên dụng hoặc thử trên thiết bị mobile thật; visual smoke kiểm tra Chrome ở hai viewport.
- Search/filter cho Articles và Leads cần API hỗ trợ trước khi thêm vào giao diện. Không có analytics giả trên dashboard.
