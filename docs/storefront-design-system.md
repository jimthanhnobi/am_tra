# Storefront design system — Prompt 9A

## Phạm vi và hướng thiết kế

Storefront dùng phong cách editorial tối giản với nền giấy sáng, chữ xanh rừng, điểm nhấn màu đất và vàng trầm. Phần quản trị giữ giao diện riêng. Trang tiếp tục lấy danh mục, sản phẩm, bài viết và site settings từ Public API; không tạo thông tin cửa hàng, ảnh sản phẩm hay đánh giá giả.

Các trang đã áp dụng: `/`, `/danh-muc/[slug]`, `/san-pham/[slug]`, `/kien-thuc`, `/kien-thuc/[slug]`, `/gioi-thieu`, `/lien-he`, `/tim-kiem`, trang lỗi và 404.

## Tokens và thành phần

CSS chung nằm trong `apps/web/src/app/(storefront)/storefront.css`, mọi selector có phạm vi `.sf-site` để không đổi Admin. Container rộng tối đa **80rem / 1280px**; mobile chừa 16px mỗi bên. Section có padding thích ứng theo viewport. Breakpoint bố cục chính: 640px cho card/form, 768px cho hero, 1024px cho điều hướng và chia cột desktop.

| Vai trò | Giá trị | Ghi chú |
|---|---|---|
| Nền ivory | `#F7F4EC` | Nền chính |
| Giấy sáng | `#FFFCF6` | Card, empty state |
| Forest | `#233B32` | Heading, CTA |
| Charcoal | `#202722` | Nội dung chính |
| Clay | `#925B3D` | Kicker, focus, link; đậm hơn màu tham chiếu để tăng tương phản |
| Gold | `#92794F` | Viền/chi tiết; đậm hơn màu tham chiếu để tăng tương phản |
| Muted | `#56635B` | Chữ phụ |

Heading dùng **Lora**, nội dung dùng **Be Vietnam Pro** với subset tiếng Việt qua `next/font`. Hệ thống gồm `sf-heading`, `sf-kicker`, `sf-copy`, `sf-button`, `sf-text-link`, `sf-card`, `sf-empty`, `sf-richtext`; breadcrumb, pagination và product card là component dùng chung. Ảnh card có tỷ lệ 4:5, dùng `next/image` với kích thước và `sizes`; thiếu ảnh hiện thông báo trung thực. Article HTML đã sanitize từ backend được trình bày qua `.sf-richtext` cho heading, danh sách, trích dẫn, bảng, ảnh và liên kết.

Các liên kết/nút/ô nhập có focus ring hiển thị; CTA và phân trang có chiều cao tối thiểu 44–52px; mobile menu dùng `<details>` có thể mở bằng bàn phím. Chuyển động ngắn và tắt gần hết khi `prefers-reduced-motion: reduce`. Form liên hệ có lỗi ngay dưới trường, trạng thái gửi/thành công/thất bại và consent rõ ràng; honeypot và API contract cũ được giữ nguyên.

## Nội dung và SEO

Hero ưu tiên cover sản phẩm công khai; khi chưa có ảnh, hiển thị hình trang trí CSS kèm nhãn rằng ảnh đang được cập nhật. Trang chủ chỉ dùng các danh mục/sản phẩm/bài viết Published trả từ Public API. Nếu thiếu dữ liệu, các section giữ empty state rõ nghĩa; không tự thêm mặt hàng hoặc câu chuyện thương hiệu. CTA liên hệ chỉ xuất hiện theo cấu hình site profile/settings và dẫn tới kênh thật hoặc `/lien-he`.

Nội dung chính vẫn được render trên server. Metadata, canonical, robots, sitemap, JSON-LD, filter Published và hành vi redirect của các bước trước được giữ. `/tim-kiem` vẫn noindex và chỉ hiển thị kết quả API thật.

## Chụp và kiểm tra giao diện

Ảnh được lưu local ở `artifacts/prompt9a/` (thư mục artifact bị Git ignore). Có 15 ảnh từ Development API thật: `home`, `blog`, `article`, `contact`, `about` tại desktop **1440×900**, tablet **768×1024**, mobile **390×844**. Development hiện có một bài demo và chưa có Category/Product Published. Sáu ảnh `fixture-category-*` và `fixture-product-*` dùng Public API fixture của HTTP test trên production Next server; fixture chỉ chạy trong bài test, không ghi vào database. Từng trang được kiểm tra HTTP 200 và không tràn ngang. Ở viewport mobile, kiểm tra thêm mở menu và lỗi form khi submit trống.

Chụp lại local (Chrome cần được cài):

```powershell
cd apps/web
$env:STOREFRONT_CAPTURE_DIR = 'D:\bussiness\am_tra\artifacts\prompt9a'
$env:STOREFRONT_VISUAL_ARTICLE_SLUG = 'bai-viet-mau-cho-moi-truong-phat-trien'
node tests/capture-local-storefront.mjs
```

```bash
cd apps/web
STOREFRONT_CAPTURE_DIR=../../artifacts/prompt9a \
STOREFRONT_VISUAL_ARTICLE_SLUG=bai-viet-mau-cho-moi-truong-phat-trien \
node tests/capture-local-storefront.mjs
```

Để chụp fixture category/product, đặt `STOREFRONT_CAPTURE_DIR` rồi chạy `corepack pnpm --dir apps/web test:storefront` từ root. Biến này chỉ bật bước screenshot bổ sung; test contract thông thường không cần Chrome.

Đã xem ảnh và tinh chỉnh sau lượt chụp đầu: giảm cỡ hero H1 để desktop còn hai dòng; đưa mô tả danh mục ngay dưới tên; bỏ tagline mặc định khi site chưa cấu hình; ẩn Dev Indicator trong artifact chụp, không thay đổi ứng dụng.

## Kết quả và giới hạn

Ngày 31/10/2026: `corepack pnpm --dir apps/web lint`, `typecheck`, `build`, `test:seo` (6/6) và `test:storefront` (12/12 gồm 11 subtests) đều PASS. Browser capture 21 ảnh PASS HTTP 200 và không tràn ngang. Test fixture xác nhận menu mobile mở và lỗi form trống hiển thị. `git diff --check` được kiểm tra riêng khi bàn giao.

Chưa nghiệm thu ảnh catalog thật qua R2 trong Prompt 9A vì Development chưa có Product/Category Published có ảnh. Chưa đo Lighthouse hoặc kiểm tra accessibility bằng công cụ chuyên dụng; các ảnh chụp và smoke test không thay thế đánh giá người dùng trên nhiều thiết bị thật.
