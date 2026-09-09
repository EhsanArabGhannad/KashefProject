#!/usr/bin/env bash
set -Eeuo pipefail

# Read secrets from stdin so they never appear in shell history or process arguments.
IFS= read -r stripe_secret
IFS= read -r webhook_secret

case "$stripe_secret" in
  sk_test_*|rk_test_*|sk_live_*|rk_live_*) ;;
  *) echo "Invalid Stripe API key." >&2; exit 1 ;;
esac
case "$webhook_secret" in
  whsec_*) ;;
  *) echo "Invalid Stripe webhook secret." >&2; exit 1 ;;
esac

install -d -m 0700 -o root -g root /etc/craftisma
temporary="$(mktemp /etc/craftisma/payment.env.XXXXXXXX)"
cleanup() { rm -f "$temporary"; }
trap cleanup EXIT
chmod 0600 "$temporary"
printf 'Stripe__SecretKey=%s\nStripe__WebhookSecret=%s\n' "$stripe_secret" "$webhook_secret" > "$temporary"
mv -f "$temporary" /etc/craftisma/payment.env
trap - EXIT

systemctl restart craftisma
for attempt in {1..20}; do
  if curl --fail --silent --show-error -H 'Host: craftisma.net' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5000/ >/dev/null; then
    echo "Stripe configuration installed; Craftisma is healthy."
    exit 0
  fi
  sleep 1
done
journalctl -u craftisma --no-pager -n 80 >&2
exit 1
