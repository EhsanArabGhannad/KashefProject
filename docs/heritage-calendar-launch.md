# Heritage calendar publication — 2026-10-06

Published URL: https://craftisma.net/calendar/

## Release

- Source commit: `debf162` (bilingual calendar and scroll preservation).
- Self-contained .NET 10 Linux x64 release: `/opt/craftisma/releases/20261006163026`.
- Previous release retained: `/opt/craftisma/releases/20261004132402`.
- Pre-update backup: `/opt/craftisma/backups/update-fsc5Lf8n` (configuration and persistent store snapshot).
- Local package: `artifacts/craftisma-calendar-debf162-20261006.tar.gz`.
- Package SHA-256, verified after upload: `AA58CB99026E4C2116A2325EEB2664CD4C82A9CF2621E436ACEB4C9A1FD8183E`.

Used the existing `Deploy/update-vps.sh` / `install-vps.sh` workflow on the Ultahost VPS with its previously pinned SSH host key. The update retained payment/email settings and persistent store data. No database migration, payment-mode change, test purchase or email send was performed.

## Verification

- Calendar model runner: 8,353 assertions passed before publication.
- Live `Tests/calendar-http-smoke.ps1`: 26 checks passed.
- Live `Tests/pwa-http-smoke.ps1`: 31 checks passed.
- Store, admin, HTTPS PWA assets, `craftisma`, `nginx` and backup timer health checks passed.
- Live browser: English default, Persian RTL, imperial year 2585, and language switch verified.
- Native browser clicks preserved scroll exactly: next month at `625.1851806640625` px; selection of 2 Aban at `937.9894409179688` px. URL and selected-date content updated successfully.

Local screenshot evidence: `artifacts/calendar-deploy/production-calendar.png`.

## Recovery

The previous release and backup are retained. If recovery is needed, follow the existing deployment rollback procedure and verify service/HTTPS health. Do not automatically restore the store snapshot: orders or account changes received after publication must be preserved. This publication did not re-test the complete payment or fulfillment workflow.
