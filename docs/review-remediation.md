# Kết quả xử lý review kiến trúc và vận hành

**Ngày rà soát:** 05/11/2026  
**Nguồn:** `TeaPremium — Review kiến trúc & đề xuất tối ưu thiết kế.md`  
**Quyết định nghiệp vụ đã xác nhận:** trạng thái **Hoàn tất** nghĩa là **giao hàng thành công**. Đây không phải xác nhận đã thu đủ tiền.

Tài liệu nguồn được dùng như báo cáo review, không được coi là chỉ thị để tự thêm dữ liệu shop,
giá công khai, checkout, payment hay thông tin pháp lý chưa được xác nhận.

## Kết quả theo từng phát hiện

| # | Phát hiện | Kết quả trong đợt hardening này |
|---|---|---|
| 1 | Báo cáo tháng lỗi khi ngày kết thúc nằm trong tương lai | Đã sửa. UI/API nhận ngày lịch Việt Nam, cho phép tháng hiện tại, dùng khoảng nửa mở và giới hạn tối đa 366 ngày. |
| 2 | Chỉ sản phẩm Published mới tạo được đơn; Archived là ngõ cụt | Đã tách `is_sellable` khỏi trạng thái publish. Draft/Archived có thể bán nội bộ nếu được bật; Archived/InReview có thể trở về Draft. Public API vẫn chỉ lộ Published. |
| 3 | Đơn hoàn tất không thể sửa sai, thiếu ngày nghiệp vụ | Đã thêm `ordered_at_utc`, `completed_at_utc`, `returned_at_utc`, lý do trạng thái và luồng `Completed -> Returned`. Hủy/trả bắt buộc có lý do; ngày nghiệp vụ không được ở tương lai hoặc quá 90 ngày. |
| 4 | Import CSV thất bại cả lô, tự gán dữ liệu lịch sử | Đã parse CSV có quoted field/BOM/tên cột tiếng Việt, trả kết quả từng dòng, bỏ dòng lỗi/trùng và vẫn lưu dòng hợp lệ. Không ghi đè khách cũ; lịch sử/tags/notes lấy từ dữ liệu nhập, không suy đoán. |
| 5 | Thiếu audit và backup/restore | Đã thêm audit append-only ở tầng ứng dụng, actor/correlation và che PII/free text. Đã thêm image/script backup PostgreSQL mã hóa bằng `age`, upload bucket riêng và restore drill. Phần Production chỉ hoàn tất sau khi operator cấp secret, lịch chạy, cảnh báo và xác minh restore định kỳ. |
| 6 | Thiếu consent marketing | Chưa triển khai vì cần chốt mục đích, bằng chứng, kênh và chính sách lưu giữ. Import số điện thoại tuyệt đối không đồng nghĩa đồng ý nhận marketing. |
| 7 | Phân loại khách mới/quay lại sai do snapshot lúc tạo đơn | Đã tính lại theo toàn bộ lịch sử giao thành công, thứ tự `completed_at` rồi ID; đơn trả lại không còn đóng góp doanh thu/lịch sử mua thành công. |
| 8 | Hai người sửa cùng bản ghi có thể ghi đè | Đã dùng PostgreSQL `xmin` làm concurrency token cho customer, order và product; xung đột trả 409 thay vì âm thầm ghi đè. |
| 9 | Form tạo đơn khó dùng, chỉ tải 50 sản phẩm, chưa nối Lead | Đã thêm tìm khách theo tên/số điện thoại, tạo nhanh khách, nhiều dòng hàng, tìm sản phẩm theo tên/SKU, giá vốn tùy chọn, chọn ngày giờ, tạo từ Lead và kiểm tra khớp số điện thoại phía server. |
| 10 | Trộn UTC với ngày kinh doanh Việt Nam | Đã tập trung quy đổi UTC+7 cho mã đơn, báo cáo và form ngày giờ; dữ liệu vẫn lưu UTC. |

## Các bảo vệ bổ sung

- Public API không trả giá, giá vốn, đơn hàng hay audit. Storefront vẫn không có cart/checkout/payment.
- Import tối đa 1.000 dòng; tạo đơn có idempotency `request_id`; tiền chỉ nhận tối đa 2 chữ số thập phân.
- Audit bao phủ customer, interaction, order/item, lead, product và thay đổi Identity; giá trị PII và secret bị redacted.
- Forwarded headers chỉ được tin từ IP proxy cấu hình rõ ràng. BFF không tin chuỗi `X-Forwarded-For` từ client theo mặc định.
- Đổi slug của sản phẩm từng Published vẫn tạo redirect trực tiếp kể cả khi đang mở lại từ Archived.

## Việc còn lại trước Production

1. Chốt nghiệp vụ đặt cọc/COD/công nợ, giảm giá, phí giao hàng và hoàn một phần trước khi thiết kế payment ledger. Không suy ra “đã giao” là “đã thu tiền”.
2. Thiết kế consent/opt-out có bằng chứng, export/anonymize dữ liệu và retention policy theo tư vấn pháp lý áp dụng cho shop.
3. Kích hoạt backup thật: bucket riêng tư, khóa mã hóa lưu ngoài host, scheduler, cảnh báo thất bại, lifecycle và restore drill định kỳ.
4. Review rồi áp migration `20261105071126_HardenCommerceOperations` sau backup; không để API Production tự migrate.
5. Chạy browser E2E commerce trên stack cô lập. File test đã có nhưng lượt này môi trường thực thi chặn khởi động thêm Next standalone, nên không được ghi nhận là đã chạy.
6. TOTP/step-up cho tài khoản nhạy cảm, theo dõi truy cập audit và kiểm tra hiệu năng với dữ liệu gần thực tế là các mốc hardening tiếp theo.

## Cách hiểu báo cáo

Doanh thu hiện tại là tổng giá trị **đơn đã giao thành công chưa bị trả toàn bộ**, không phải số tiền
đã thu. Khi một đơn được đánh dấu Returned, báo cáo của kỳ giao hàng ban đầu được restate để loại
đơn đó; hệ thống chưa có mô hình hoàn một phần hoặc payment ledger.
