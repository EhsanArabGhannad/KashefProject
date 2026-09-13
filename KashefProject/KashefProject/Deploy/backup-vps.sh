#!/usr/bin/env bash
set -Eeuo pipefail

backup_root="/var/backups/craftisma"
data_root="/var/lib/craftisma"
retention_days="${CRAFTISMA_BACKUP_RETENTION_DAYS:-14}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
archive_path="${backup_root}/craftisma-${timestamp}.tar.gz"

install -d -m 0700 -o root -g root "${backup_root}"
test -f "${data_root}/craftisma.db"

staging_dir="$(mktemp -d "${backup_root}/.staging-XXXXXXXX")"
cleanup() {
  rm -rf -- "${staging_dir}"
}
trap cleanup EXIT

install -d -m 0700 "${staging_dir}/data"

# SQLite's online backup API creates a consistent database snapshot while the
# storefront remains available and orders may still be written.
python3 - "${data_root}/craftisma.db" "${staging_dir}/data/craftisma.db" <<'PY'
import sqlite3
import sys

source_path, destination_path = sys.argv[1], sys.argv[2]
with sqlite3.connect(f"file:{source_path}?mode=ro", uri=True) as source:
    with sqlite3.connect(destination_path) as destination:
        source.backup(destination)
        result = destination.execute("PRAGMA integrity_check").fetchone()
        if not result or result[0] != "ok":
            raise RuntimeError("SQLite integrity check failed")
PY

if [[ -d "${data_root}/uploads" ]]; then
  cp -a "${data_root}/uploads" "${staging_dir}/data/uploads"
fi
if [[ -d "${data_root}/keys" ]]; then
  cp -a "${data_root}/keys" "${staging_dir}/data/keys"
fi

printf 'created_utc=%s\nsource=%s\n' "$(date -u --iso-8601=seconds)" "${data_root}" > "${staging_dir}/manifest.txt"
tar -czf "${archive_path}" -C "${staging_dir}" manifest.txt data
chmod 0600 "${archive_path}"
tar -tzf "${archive_path}" >/dev/null

find "${backup_root}" -maxdepth 1 -type f -name 'craftisma-*.tar.gz' -mtime "+${retention_days}" -delete
printf 'Backup created: %s\n' "${archive_path}"
