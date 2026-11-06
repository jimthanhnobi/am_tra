# Sao lưu PostgreSQL và kiểm tra khôi phục

Job trong `infra/backup` tạo `pg_dump -Fc`, mã hóa age trước khi ghi file và tải lên bucket R2 **riêng, private**. Không dùng bucket ảnh/CDN. Script không tự xóa backup trên R2. Cấu hình lifecycle giữ tối thiểu 30 ngày trên bucket; xác nhận một bản restore thành công trước khi bật xóa tự động.

## Cấu hình vận hành

1. Tạo cặp khóa bằng `age-keygen` trên máy quản trị. Lưu private identity offline có bản dự phòng; server backup chỉ nhận public recipient `age1...`. Mất private key thì không khôi phục được.
2. Tạo bucket R2 riêng, không public domain/r2.dev; cấp token Object Read & Write chỉ cho bucket backup. Có thể dùng credential read-only riêng cho restore.
3. Copy `infra/backup/backup.env.example` ra ngoài repository, điền thông tin thật, giới hạn quyền đọc file. PostgreSQL backup role cần CONNECT, USAGE schema, SELECT toàn bộ bảng/sequences hiện tại và default privileges cho bảng mới. Không cấp quyền ghi cho role này.
4. Đặt `BACKUP_ENV_FILE` thành đường dẫn tuyệt đối của file đó và `BACKUP_DATABASE_NETWORK` thành tên network của PostgreSQL nguồn. Build `docker compose -f infra/backup/compose.yml build`. Chạy thử `docker compose -f infra/backup/compose.yml run --rm backup`.
5. Scheduler trên host gọi lệnh trên hằng đêm (ví dụ 02:00 giờ Việt Nam), dùng khóa `flock` để tránh chạy chồng. Theo dõi exit code, báo lỗi khi job thất bại hoặc không có bản backup mới trong 26 giờ. Không chỉ kiểm tra container còn chạy.

File tạm chỉ chứa ciphertext, nằm trong tmpfs và được xóa khi job kết thúc. Giới hạn tmpfs hiện là 1 GiB, cần tăng theo kích thước database. AWS credential được cấp vào container qua env; người có quyền Docker có thể đọc env — giới hạn quyền quản trị host. Không bật shell tracing, không đưa presigned URL/password vào log.

## Restore drill hằng tháng

Tải `.dump.age` và `.sha256` tương ứng từ bucket. Tạo PostgreSQL **riêng**, database mới bắt đầu bằng `teapremium_restore_`, không chứa bảng. Mount archive, checksum và private age identity read-only vào container backup. Dùng entrypoint `/bin/sh /opt/backup/restore-drill.sh /restore/database.dump.age /restore/database.dump.age.sha256`, với PGHOST/PGDATABASE/PGUSER/PGPASSWORD của database thử và AGE_IDENTITY_FILE trỏ file identity đã mount.

Script kiểm tra checksum, tên database và tính rỗng trước khi restore; không có `DROP`, `--clean` hay thao tác sửa DB nguồn. Sau restore, đối chiếu số bảng/bản ghi với snapshot nguồn tại thời điểm dump, kiểm tra đăng nhập trên app staging, xem khách/đơn và báo cáo. Lưu ngày, tên object, checksum, kết quả, thời gian khôi phục và người kiểm tra. Chỉ xem backup đạt khi restore drill đạt.

Chưa cấu hình credential/bucket/scheduler thật thì đây là bộ công cụ sẵn để triển khai, **chưa phải hệ thống backup Production đang hoạt động**. RPO mục tiêu của lịch hằng đêm là tối đa 24 giờ; RTO phải đo bằng restore drill trên dữ liệu thật. R2 ảnh và Identity DataProtection keys cần kế hoạch sao lưu riêng.
