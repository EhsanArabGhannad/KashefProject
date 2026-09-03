#!/usr/bin/env bash
set -Eeuo pipefail

# Run on the existing VPS. Bootstrap credentials arrive as JSON on stdin,
# never as command arguments or as part of the deployment archive.
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
backup_dir="$(mktemp -d /opt/craftisma/backups/admin-XXXXXXXX)"
cp -a /etc/systemd/system/craftisma.service "$backup_dir/craftisma.service"
cp -a /etc/nginx/sites-available/craftisma "$backup_dir/nginx.conf"
printf '%s\n' "$previous_release" > "$backup_dir/previous-release"

rollback() {
  trap - ERR
  rm -f /run/craftisma-bootstrap/admin.env
  cp -a "$backup_dir/craftisma.service" /etc/systemd/system/craftisma.service
  cp -a "$backup_dir/nginx.conf" /etc/nginx/sites-available/craftisma
  ln -sfn "$previous_release" /opt/craftisma/current
  systemctl daemon-reload
  systemctl restart craftisma
  nginx -t && systemctl reload nginx
  echo "Deployment failed; the previous release was restored. Backup: $backup_dir" >&2
  exit 1
}
trap rollback ERR

if [[ -d /var/lib/craftisma ]]; then
  systemctl stop craftisma
  tar -czf "$backup_dir/store-data.tar.gz" -C /var/lib craftisma
fi

install -d -m 0700 /run/craftisma-bootstrap
python3 -c '
import json, os, re, sys
data = json.load(sys.stdin)
email, password = data["email"].strip(), data["password"]
if not re.fullmatch(r"[^\s@]+@[^\s@]+\.[^\s@]+", email):
    raise SystemExit("Invalid administrator email")
if len(password) < 12 or any(not re.search(pattern, password) for pattern in (r"[A-Z]", r"[a-z]", r"[0-9]", r"[^a-zA-Z0-9]")):
    raise SystemExit("Administrator password does not meet the requirements")
if any(char in email + password for char in "\r\n\0"):
    raise SystemExit("Bootstrap values must be single-line")
def quote(value):
    return chr(34) + value.replace(chr(92), chr(92) * 2).replace(chr(34), chr(92) + chr(34)) + chr(34)
fd = os.open("/run/craftisma-bootstrap/admin.env", os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, "w") as output:
    output.write("Admin__Email=" + quote(email) + "\nAdmin__Password=" + quote(password) + "\n")
'

bash "$installer_path" "$archive_path" > "$backup_dir/deploy.log" 2>&1

# Remove the bootstrap secret and restart so it is no longer in the process
# environment. The administrator now exists in the persistent database.
rm -f /run/craftisma-bootstrap/admin.env
systemctl restart craftisma
healthy=false
for attempt in {1..30}; do
  code="$(curl --silent --output /dev/null --write-out '%{http_code}' -H 'Host: craftisma.net' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5000/admin/login || true)"
  if [[ "$code" == "200" ]]; then
    healthy=true
    break
  fi
  sleep 1
done
test "$healthy" == true
trap - ERR
echo "Administrator panel is healthy. Previous release retained. Backup: $backup_dir"
systemctl is-active craftisma nginx
