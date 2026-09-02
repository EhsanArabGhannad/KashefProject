#!/usr/bin/env bash
set -euo pipefail

archive_path="${1:-/tmp/kashef-vps-linux-x64.tar.gz}"
app_root="/opt/craftisma"
release_id="$(date -u +%Y%m%d%H%M%S)"
release_dir="${app_root}/releases/${release_id}"

if [[ ! -f "${archive_path}" ]]; then
  echo "Deployment archive not found: ${archive_path}" >&2
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y nginx curl

if ! id -u craftisma >/dev/null 2>&1; then
  useradd --system --home-dir "${app_root}" --shell /usr/sbin/nologin craftisma
fi

install -d -m 0755 "${app_root}/releases" "${release_dir}"
tar -xzf "${archive_path}" -C "${release_dir}"
chmod 0755 "${release_dir}/KashefProject"
chown -R root:root "${release_dir}"
ln -sfn "${release_dir}" "${app_root}/current"

cat > /etc/systemd/system/craftisma.service <<'UNIT'
[Unit]
Description=Craftisma ASP.NET Core website
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=craftisma
Group=craftisma
WorkingDirectory=/opt/craftisma/current
ExecStart=/opt/craftisma/current/KashefProject
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=craftisma
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_NOLOGO=true
NoNewPrivileges=true
PrivateTmp=true
ProtectHome=true
ProtectSystem=full

[Install]
WantedBy=multi-user.target
UNIT

cat > /etc/nginx/sites-available/craftisma <<'NGINX'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name craftisma.net www.craftisma.net _;

    client_max_body_size 10m;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
}
NGINX

systemctl daemon-reload
systemctl enable --now craftisma

for attempt in {1..20}; do
  if curl --fail --silent --show-error http://127.0.0.1:5000/ >/dev/null; then
    break
  fi
  if [[ "${attempt}" == "20" ]]; then
    journalctl -u craftisma --no-pager -n 80 >&2
    exit 1
  fi
  sleep 1
done

if [[ -e /etc/nginx/sites-enabled/default && ! -e /root/nginx-default-before-craftisma ]]; then
  cp -a /etc/nginx/sites-enabled/default /root/nginx-default-before-craftisma
fi
ln -sfn /etc/nginx/sites-available/craftisma /etc/nginx/sites-enabled/default

nginx -t
systemctl enable --now nginx
systemctl reload nginx

if systemctl is-active --quiet ufw; then
  ufw allow OpenSSH
  ufw allow 'Nginx Full'
fi

curl --fail --silent --show-error -H 'Host: craftisma.net' http://127.0.0.1/ >/dev/null
rm -f "${archive_path}"

echo "release=${release_id}"
echo "craftisma=$(systemctl is-active craftisma)"
echo "nginx=$(systemctl is-active nginx)"
