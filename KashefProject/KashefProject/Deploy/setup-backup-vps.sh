#!/usr/bin/env bash
set -Eeuo pipefail

source_script="${1:-/tmp/backup-vps.sh}"
test -f "${source_script}"

install -m 0750 -o root -g root "${source_script}" /usr/local/sbin/craftisma-backup
install -d -m 0700 -o root -g root /var/backups/craftisma

cat > /etc/systemd/system/craftisma-backup.service <<'UNIT'
[Unit]
Description=Create a consistent Craftisma data backup
After=local-fs.target

[Service]
Type=oneshot
User=root
Group=root
ExecStart=/usr/local/sbin/craftisma-backup
Nice=10
IOSchedulingClass=best-effort
IOSchedulingPriority=7
PrivateTmp=true
ProtectHome=true
ProtectSystem=strict
ReadOnlyPaths=/var/lib/craftisma
ReadWritePaths=/var/backups/craftisma
NoNewPrivileges=true
UNIT

cat > /etc/systemd/system/craftisma-backup.timer <<'UNIT'
[Unit]
Description=Daily Craftisma backup timer

[Timer]
OnCalendar=*-*-* 03:15:00 UTC
RandomizedDelaySec=15m
Persistent=true
Unit=craftisma-backup.service

[Install]
WantedBy=timers.target
UNIT

systemctl daemon-reload
systemctl enable --now craftisma-backup.timer
systemctl start craftisma-backup.service
systemctl is-active craftisma-backup.timer
systemctl is-failed craftisma-backup.service >/dev/null && exit 1 || true
systemctl list-timers craftisma-backup.timer --no-pager
ls -lh /var/backups/craftisma/craftisma-*.tar.gz | tail -n 1
