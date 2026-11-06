#!/bin/sh
set -eu
set -o pipefail
umask 077

: "${PGHOST:?Set the isolated restore host}"
: "${PGDATABASE:?Set an empty teapremium_restore_* database}"
: "${PGUSER:?Set PGUSER}"
: "${PGPASSWORD:?Set PGPASSWORD}"
: "${AGE_IDENTITY_FILE:?Mount the age private identity read-only}"
archive=${1:?Pass the downloaded encrypted archive path}
checksum=${2:?Pass the downloaded SHA256 file path}
case "$PGDATABASE" in teapremium_restore_*) ;; *) echo 'Refusing non-restore database' >&2; exit 1 ;; esac
test -r "$archive" && test -r "$checksum" && test -r "$AGE_IDENTITY_FILE"
expected=$(tr -d '\r\n' < "$checksum")
actual=$(sha256sum "$archive" | awk '{print $1}')
test "$actual" = "$expected" || { echo 'Checksum mismatch' >&2; exit 1; }
tables=$(psql -X -v ON_ERROR_STOP=1 -Atc "SELECT count(*) FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog','information_schema');")
test "$tables" = 0 || { echo 'Target database is not empty; refusing restore' >&2; exit 1; }

# No --clean, no DROP, no automatic database creation.
age --decrypt --identity "$AGE_IDENTITY_FILE" "$archive" | pg_restore --exit-on-error --no-owner --no-privileges --dbname "$PGDATABASE"
psql -X -v ON_ERROR_STOP=1 -c "SELECT 'customers' AS entity, count(*) FROM customers UNION ALL SELECT 'sales_orders', count(*) FROM sales_orders UNION ALL SELECT 'products', count(*) FROM products UNION ALL SELECT 'audit_logs', count(*) FROM audit_logs;"
echo 'Restore completed into isolated database. Compare counts and run application smoke tests before accepting the drill.'
