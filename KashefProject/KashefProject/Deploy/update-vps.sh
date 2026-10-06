#!/usr/bin/env bash
set -Eeuo pipefail

archive_path="${1:?Deployment archive is required}"
installer_path="${2:?Installer path is required}"
previous_release="$(readlink -f /opt/craftisma/current)"
case "$previous_release" in
  /opt/craftisma/releases/*) ;;
  *) echo "The existing release could not be verified." >&2; exit 1 ;;
esac
test -f "$archive_path"
test -f "$installer_path"
test ! -e /run/craftisma-bootstrap/admin.env
install -d -m 0700 /opt/craftisma/backups
backup_dir="$(mktemp -d /opt/craftisma/backups/update-XXXXXXXX)"
cp -a /etc/systemd/system/craftisma.service "$backup_dir/craftisma.service"
cp -a /etc/nginx/sites-available/craftisma "$backup_dir/nginx.conf"
printf '%s\n' "$previous_release" > "$backup_dir/previous-release"

rollback() {
  trap - ERR
  cp -a "$backup_dir/craftisma.service" /etc/systemd/system/craftisma.service
  cp -a "$backup_dir/nginx.conf" /etc/nginx/sites-available/craftisma
  ln -sfn "$previous_release" /opt/craftisma/current
  systemctl daemon-reload
  systemctl restart craftisma
  nginx -t && systemctl reload nginx
  # Keep the persistent database intact: this release only adds tables and
  # restoring an older database automatically could discard newer store data.
  echo "Deployment failed; previous application restored. Backup: $backup_dir" >&2
  exit 1
}
trap rollback ERR
if [[ -d /var/lib/craftisma ]]; then
  systemctl stop craftisma
  tar -czf "$backup_dir/store-data.tar.gz" -C /var/lib craftisma
  systemctl start craftisma
fi
bash "$installer_path" "$archive_path" > "$backup_dir/deploy.log" 2>&1
healthy=false
for attempt in {1..30}; do
  cart_code="$(curl --silent --output /dev/null --write-out '%{http_code}' -H 'Host: craftisma.net' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5000/cart/ || true)"
  admin_code="$(curl --silent --output /dev/null --write-out '%{http_code}' -H 'Host: craftisma.net' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5000/admin/login || true)"
  if [[ "$cart_code" == "200" && "$admin_code" == "200" ]]; then healthy=true; break; fi
  sleep 1
done
test "$healthy" == true
# Validate the app through the HTTPS proxy as well as the local backend.
# Keep rollback armed until the installable app and offline shell are reachable.
for path in /app/ /manifest.webmanifest /service-worker.js /offline.html /css/offline.css /js/offline.js /icons/icon-192.png /icons/icon-512.png /icons/icon-maskable-512.png /icons/apple-touch-icon.png; do
  curl --fail --silent --show-error --max-time 20 --resolve craftisma.net:443:127.0.0.1 "https://craftisma.net${path}" >/dev/null
done
worker_headers="$(curl --fail --silent --show-error --max-time 20 --head --resolve craftisma.net:443:127.0.0.1 https://craftisma.net/service-worker.js)"
manifest_headers="$(curl --fail --silent --show-error --max-time 20 --head --resolve craftisma.net:443:127.0.0.1 https://craftisma.net/manifest.webmanifest)"
grep -Eiq '^cache-control:.*no-cache' <<< "$worker_headers"
grep -Eiq '^cache-control:.*no-cache' <<< "$manifest_headers"
grep -Eiq '^content-type: application/manifest\+json' <<< "$manifest_headers"
trap - ERR
echo "Store, admin and HTTPS PWA assets are healthy. Previous release retained. Backup: $backup_dir"
systemctl is-active craftisma nginx
