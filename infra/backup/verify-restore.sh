#!/bin/sh
# Local/CI drill ONLY. Never run against a shop database.
set -eu
set -o pipefail
umask 077
case "${PGDATABASE:-}" in teapremium_test*) ;; *) echo 'Requires isolated test source' >&2; exit 1 ;; esac
source_db=$PGDATABASE
task_dir=$(mktemp -d)
identity="$task_dir/identity.txt"
archive="$task_dir/database.dump.age"
checksum="$task_dir/database.dump.age.sha256"
trap 'rm -f "$identity" "$archive" "$checksum"; rmdir "$task_dir"' EXIT HUP INT TERM
age-keygen --output "$identity" 2>/dev/null
recipient=$(age-keygen -y "$identity")
pg_dump --format=custom --no-owner --no-privileges | age --recipient "$recipient" --output "$archive"
sha256sum "$archive" | awk '{print $1}' > "$checksum"
source_counts=$(psql -X -v ON_ERROR_STOP=1 -Atc "SELECT count(*) FROM customers UNION ALL SELECT count(*) FROM sales_orders UNION ALL SELECT count(*) FROM products UNION ALL SELECT count(*) FROM audit_logs;")
export AGE_IDENTITY_FILE=$identity
export PGDATABASE="teapremium_restore_$(date -u +%Y%m%d%H%M%S)"
createdb "$PGDATABASE"
/bin/sh /opt/backup/restore-drill.sh "$archive" "$checksum"
restored_counts=$(psql -X -v ON_ERROR_STOP=1 -Atc "SELECT count(*) FROM customers UNION ALL SELECT count(*) FROM sales_orders UNION ALL SELECT count(*) FROM products UNION ALL SELECT count(*) FROM audit_logs;")
test "$source_counts" = "$restored_counts"
if /bin/sh /opt/backup/restore-drill.sh "$archive" "$checksum"; then
    echo 'ERROR: restore unexpectedly accepted nonempty target' >&2; exit 1
fi
export PGDATABASE=$source_db
if /bin/sh /opt/backup/restore-drill.sh "$archive" "$checksum"; then
    echo 'ERROR: restore unexpectedly accepted non-restore target' >&2; exit 1
fi
echo 'PASS: encrypted dump restored, counts match, nonempty and non-restore targets rejected.'
