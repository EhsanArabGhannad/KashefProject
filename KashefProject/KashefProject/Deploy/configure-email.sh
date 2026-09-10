#!/usr/bin/env bash
set -Eeuo pipefail

# Read secrets and addresses from stdin so they never appear in process arguments.
IFS= read -r email_api_key
IFS= read -r from_address
IFS= read -r from_name
IFS= read -r admin_address

case "$email_api_key" in
  re_*) ;;
  *) echo "Invalid Resend API key." >&2; exit 1 ;;
esac
case "$from_address" in
  *@*.*) ;;
  *) echo "Invalid sender email address." >&2; exit 1 ;;
esac
case "$admin_address" in
  *@*.*) ;;
  *) echo "Invalid admin email address." >&2; exit 1 ;;
esac
case "$from_name" in
  *$'\n'*|*$'\r'*|*'"'*|'') echo "Invalid sender name." >&2; exit 1 ;;
esac

install -d -m 0700 -o root -g root /etc/craftisma
temporary="$(mktemp /etc/craftisma/email.env.XXXXXXXX)"
cleanup() { rm -f "$temporary"; }
trap cleanup EXIT
chmod 0600 "$temporary"
printf 'Email__ApiKey=%s\nEmail__FromAddress=%s\nEmail__FromName="%s"\nEmail__AdminAddress=%s\n' \
  "$email_api_key" "$from_address" "$from_name" "$admin_address" > "$temporary"
mv -f "$temporary" /etc/craftisma/email.env
trap - EXIT

systemctl restart craftisma
for attempt in {1..20}; do
  if curl --fail --silent --show-error -H 'Host: craftisma.net' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5000/ >/dev/null; then
    echo "Email configuration installed; Craftisma is healthy."
    exit 0
  fi
  sleep 1
done
journalctl -u craftisma --no-pager -n 80 >&2
exit 1
