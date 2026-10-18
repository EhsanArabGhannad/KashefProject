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

## Redesign publication — 2026-10-15

- Published source commit: `919d64c`.
- Active release: `/opt/craftisma/releases/20261015070929`.
- Previous release retained: `/opt/craftisma/releases/20261006163026`.
- Pre-update backup: `/opt/craftisma/backups/update-mOP7FgLL`; persistent store snapshot existence confirmed.
- Local archive: `artifacts/craftisma-calendar-redesign-919d64c-20261015.tar.gz` (70,894,561 bytes).
- SHA-256, matched after upload before installation: `37A8AA0164BCCAD299BBBFA38CC1ED54F5ECB1A9BA1DCF0AC16DC2D5502C3DCB`.

Published the dark calendar grid, lime selection, hover/light effects, removal of public sources and removal of empty-day placeholder stories. The underlying source catalogue remains intact. Existing update/install scripts were used with pinned SSH host-key verification. No database migration was added; persistent data and payment/email configuration were retained.

Verification: 8,353 model assertions before publication; 30 calendar HTTP checks and 31 HTTPS PWA checks against production; live calendar CSS and JavaScript exactly match the reviewed local files after newline normalization. Store/admin health and `craftisma`, `nginx`, `craftisma-backup.timer` are active. Visual/interactive browser verification of the redesign remains pending; this deployment did not retry the previously stopped browser session or perform payments/email sends.

## Calendar management publication — 2026-10-15

- Active release: `/opt/craftisma/releases/20261015191133`.
- Previous application retained: `/opt/craftisma/releases/20261015070929`.
- Pre-update backup: `/opt/craftisma/backups/update-kOqXvsDu` (persistent store snapshot and service/proxy configuration).
- Local package: `artifacts/craftisma-calendar-management-20261015.tar.gz`, 70,940,528 bytes.
- SHA-256, matched after transfer before installation: `EFFA47FDF3E9822C30DF817C5913E524AB529F7F14B65BCC388A239912AA489F`.
- Source: working tree based on `919d64c`, including the uncommitted calendar-management implementation; no commit/push was performed.

Used existing pinned-host-key SSH and update/install workflow. Migration `20261015181921_AddManagedCalendarEvents` created two calendar-only tables. The transactional one-time import produced 38 published bilingual entries and a durable marker; read-only database audit found zero incomplete entries. Customer/order schema was not changed. Existing payment/email configuration and persistent uploads were retained; application-account upload write permission confirmed.

Before publication: 8,353 recurrence assertions and 30 management assertions passed. After publication: 30 public calendar, 11 anonymous management route/asset, and 31 HTTPS PWA checks passed. All services and the backup timer are active. No admin login, production event mutations/uploads, payment, email or visual browser test was performed for deployment. Preserve newer store data if recovery is required; do not restore the whole backup automatically.
