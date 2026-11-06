#!/bin/sh
set -eu
set -o pipefail
umask 077

: "${PGHOST:?Set PGHOST}"
: "${PGDATABASE:?Set PGDATABASE}"
: "${PGUSER:?Set PGUSER}"
: "${PGPASSWORD:?Set PGPASSWORD}"
: "${AGE_RECIPIENT:?Set the age PUBLIC recipient key}"
: "${BACKUP_BUCKET:?Use a separate private backup bucket}"
: "${R2_ENDPOINT:?Set the R2 S3 HTTPS endpoint}"
case "$R2_ENDPOINT" in https://*.r2.cloudflarestorage.com) ;; *) echo 'Invalid R2 endpoint' >&2; exit 1 ;; esac
case "$BACKUP_BUCKET" in *[!a-z0-9.-]*|'') echo 'Invalid bucket name' >&2; exit 1 ;; esac

task_dir=$(mktemp -d)
archive="$task_dir/database.dump.age"
checksum="$task_dir/database.dump.age.sha256"
trap 'rm -f "$archive" "$checksum"; rmdir "$task_dir"' EXIT HUP INT TERM

# Plaintext never touches disk; pipefail prevents uploading truncated/encryption-failed dumps.
pg_dump --format=custom --no-owner --no-privileges | age --recipient "$AGE_RECIPIENT" --output "$archive"
sha256sum "$archive" | awk '{print $1}' > "$checksum"
key="postgres/$(date -u +%Y/%m/%d)/$(date -u +%Y%m%dT%H%M%SZ)-$(basename "$task_dir").dump.age"
aws --endpoint-url "$R2_ENDPOINT" s3 cp "$archive" "s3://$BACKUP_BUCKET/$key" --only-show-errors
aws --endpoint-url "$R2_ENDPOINT" s3 cp "$checksum" "s3://$BACKUP_BUCKET/$key.sha256" --only-show-errors
echo "Encrypted backup uploaded: $key"
