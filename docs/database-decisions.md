# Database decisions — Catalog + Media

## Phạm vi

`InitialCatalog` chỉ được phép tạo: `products`, `categories`, `product_categories`, `brands`, `media_assets`, `product_media`. Bảng lịch sử `__EFMigrationsHistory` do EF Core quản lý. Không tạo ProductVariant, Price, Inventory, Cart, Order, Payment hoặc module khác.

## Quyết định

| ID | Quyết định | Lý do / hệ quả |
|---|---|---|
| DB-01 | Dùng PostgreSQL `uuid`, `timestamptz`, snake_case và FK có tên tường minh. | Schema dễ audit; thời gian lưu UTC, chuyển múi giờ ở presentation. |
| DB-02 | `PublicationStatus` lưu dạng text với check constraint gồm Draft/InReview/Published/Archived. | Dễ đọc và thay đổi hơn PostgreSQL enum ở giai đoạn đầu; không có cột `is_published` song song. Publish rules thuộc use case sau. |
| DB-03 | SEO là owned value object map vào bảng product/category. | Giữ FK thật cho `og_image_asset_id`, không tạo bảng polymorphic không có referential integrity. SEO text nullable để Draft được lưu chưa hoàn chỉnh; publish use case sẽ kiểm tra mức bắt buộc. |
| DB-04 | `description_jsonb` và `specs_jsonb` dùng `jsonb`, được validate ở domain và check `jsonb_typeof(...) = 'object'` ở database. | Chấp nhận cấu trúc linh hoạt nhưng loại array/scalar; các trường filter/unique vẫn là cột riêng. |
| DB-05 | Primary category dùng FK nullable trên `products`; category đó đồng thời phải có row trong `product_categories`. | Invariant chéo được enforce trong Application service và cùng transaction khi create/update/publish. Không tạo vòng/composite FK ở migration này vì sẽ làm write flow và migration phức tạp không cần thiết. |
| DB-06 | Xóa category/brand/media đang được product hoặc SEO tham chiếu dùng `RESTRICT`. | Không làm mất liên kết của Published content. Vận hành ưu tiên Archive. |
| DB-07 | Chỉ dependent/link rows được cascade: `product_categories` từ hai parent và `product_media` từ product. | Dọn dữ liệu liên kết khi parent được xóa hợp lệ; xóa media asset không cascade vào gallery/product. |
| DB-08 | SKU nullable có partial unique index `WHERE sku IS NOT NULL`. | Cho nhiều Draft chưa có SKU nhưng ngăn SKU thật trùng nhau. |
| DB-09 | Cover dùng partial unique index `UNIQUE(product_id) WHERE is_cover = TRUE`. | Database bảo đảm tối đa một cover ngay cả khi có concurrent writes. Việc yêu cầu Published phải có cover thuộc publish use case sau. |
| DB-10 | Độ dài text được giới hạn theo loại dữ liệu; rich HTML và JSON dùng `text/jsonb`. | Ngăn key/slug/metadata vô hạn mà không cắt nội dung rich text. Các giới hạn cần được đối chiếu UI khi xây CMS. |
| DB-11 | Brand delete dùng `RESTRICT`, không `SET NULL`. | Tránh mất attribution âm thầm; Admin phải bỏ/chuyển brand có chủ ý trước khi xóa. |
| DB-12 | Migration không chạy trong Production startup. | Local/test có thể dùng `database update`; Production phải review generated SQL, backup và chạy như deploy step kiểm soát. |

## Điểm cần kiểm chứng ở use case sau

- Quy tắc chuyển trạng thái và trường bắt buộc khi Published.
- Application transaction bảo đảm `primary_category_id` luôn có link tương ứng trong `product_categories`.
- Chính sách đổi slug/redirect và Archive thay vì hard delete.
- MIME allowlist, checksum format, kích thước media tối đa và storage provider khi triển khai upload/R2.
- Độ dài SEO/slug/ALT cuối cùng theo form CMS và nội dung thật.
