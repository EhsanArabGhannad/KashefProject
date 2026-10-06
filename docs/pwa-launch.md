# Craftisma PWA publication

Published on October 4, 2026, after explicit user approval to transfer the application archive to the existing Craftisma VPS.

## Deployment record

- Public installation guide: https://craftisma.net/app/
- Application source: commit `93542c4` (mobile fixes and PWA).
- Linux x64, self-contained Release build completed successfully.
- Archive SHA-256: `1D8D3789C1EA9C3F12C4E2E9B67F0E8EF76C2EE7F6710A2FC7C7A80EAC005942`.
- Published release: `/opt/craftisma/releases/20261004132402`.
- Previous release retained: `/opt/craftisma/releases/20260929121616`.
- Update backup: `/opt/craftisma/backups/update-hKyFmD50`, containing the previous service/proxy configuration, release pointer, persistent store snapshot and deployment log.
- Store and nginx services are active; the existing daily backup timer remains active.

The archive was checked for credentials, `App_Data`, database files and private runtime configuration before transfer. It contains the application and public site assets. Existing server-side payment/email configuration and persistent data directories are preserved. SSH used the existing pinned OpenSSH host key, and HTTPS certificate validation remained enabled.

The update script now keeps automatic rollback armed until both the backend and the HTTPS PWA assets are reachable and the manifest/worker headers are correct. Its new HTTPS checks use bounded request timeouts. The rollback branch was not deliberately triggered on the live store. Rollback restores the prior application and configuration while keeping the persistent database, as the existing script specifies.

## Confirmed checks

- Release archive checksum matched on the VPS before installation.
- Installation and update scripts passed shell syntax checks on the server.
- `Tests/pwa-http-smoke.ps1`: **31 checks passed locally and all 31 passed on the public HTTPS origin**. These cover the installation guide, registration script, manifest, actual PNG sizes, worker body matching reviewed source, reconnect assets, revalidation headers and fresh HTML for shop/account/bag/checkout.
- `Tests/pwa.test.cjs`: all 11 worker and installation behavior tests passed again before publication.
- Public guide displayed correctly in the browser at 320 and 390 pixels without horizontal overflow; its icon loaded and no browser warnings/errors were recorded.
- Stripe configuration remains **test mode**, with a webhook secret configured. A read-only call to the Checkout sessions API succeeded. An unsigned webhook was rejected with HTTP 400. No payment or order was created by these checks.

An initial balance-resource probe returned HTTP 403; balance access is not needed by this store. The subsequent probe used the Checkout resource required by the application and succeeded. This establishes access to that resource, not complete payment-flow acceptance or every possible API permission.

Evidence is in ignored `artifacts/pwa-review/`: `deploy-results.txt`, `http-smoke-local.txt`, `http-smoke-production.txt`, `payment-check-results.txt`, `production-layout-results.json` and `production-mobile.png`. Temporary local password files were deleted after each SSH operation. Credentials are not in these reports.

To repeat the read-only publication check with PowerShell 7:

```powershell
pwsh -NoProfile -File Tests/pwa-http-smoke.ps1 -BaseUrl https://craftisma.net
```

## Acceptance still to complete

| Check | Android Chrome | iPhone Safari |
| --- | --- | --- |
| Install from `/app/`, then launch from the home screen | Pending | Pending |
| Correct icon and standalone shop launch | Pending | Pending |
| Sign in, retain the bag, navigate back, reopen the app | Pending | Pending |
| Disconnect after an online visit, see the generic reconnect screen, reconnect and retry | Pending | Pending |
| Stripe test success/failure/cancellation, authentication challenge and return to the same owned order | Pending | Pending |
| Interrupted return and delayed webhook; check status before trying payment again | Pending | Pending |

Browser viewport checks do not establish actual-device installation or session behavior. This environment does not expose either physical phone, so those checks require device access. See phase 2's [acceptance procedure](pwa-readiness.md#production-acceptance).

The complete Stripe payment/notification path and a worker-version update with an open purchase form still need acceptance. Switching to live Stripe keys is a later launch step; no live payment settings were changed during this publication.
