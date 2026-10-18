# Supplied Craftisma logo — 2026-10-19

Published on 2026-10-19 at `https://craftisma.net/`. The logo, versioned favicons and app icons are live.

The user's supplied gold dimensional mark replaces the former circular C in the public header/footer and Admin header. The Craftisma text wordmark is retained. The original transparent 1254 × 1254 PNG is preserved byte-for-byte at `Assets/Brand/craftisma-logo-source.png` (SHA-256 `350E13A2B5B138034270B686BC0767B651A148CA5F5CC5684B02E32CF0B2A257`). This is deterministic image preparation, not generated or redrawn artwork.

`Tools/generate-app-icons.ps1` rebuilds derivatives on Windows/PowerShell using System.Drawing. It trims visible bounds (ignoring near-invisible alpha export noise), preserves the supplied artwork and aspect ratio, and exports:

- Transparent `wwwroot/images/brand/craftisma-logo.png` (256 px), displayed at 40 px in the headers and 48 px in the footer.
- Transparent favicon PNGs (16 / 32 px) and a multi-resolution ICO containing 16, 24, 32, 48, 64, 128 and 256 px PNG entries.
- Updated app icons (192 / 512 px), an opaque dark maskable icon with the artwork within the central safe circle, and a dark Apple touch icon (180 px).

Shared `_Favicons.cshtml` links versioned favicons in the storefront, Admin and invoice layouts. Logo URLs also use ASP.NET file-content versioning. The offline page reuses the precached 192 px icon for its favicon; no extra offline/private assets are cached. The service worker cache version is bumped to v2, with existing non-forced activation behavior retained so an update cannot interrupt checkout. Existing manifest identity, start URL, icon paths and installation behavior are unchanged.

Verification before publication: clean `BrandPreview` configuration build (zero warnings/errors), 37 asset assertions (dimensions, transparency, artwork presence, all ICO entries decoded and cache version), 23 HTTP brand checks on home/shop/calendar/account login/Admin login (including exact SHA-256 comparisons for all eight logo/icon files), 31 local PWA HTTP checks and 11 worker/manifest/installation tests passed. The generated 256 px logo and 32 px favicon were visually inspected as images. Full browser/mobile visual QA and installed-device icon refresh are separate checks. Existing installed apps may retain their OS icon until the browser/device refreshes it; service-worker activation remains non-forced.

## Production publication

- Active release: `/opt/craftisma/releases/20261018215440` (server-generated release identifier).
- Prior release retained: `/opt/craftisma/releases/20261015191133`.
- Backup: `/opt/craftisma/backups/update-uTOUBion` (persistent store snapshot and service/proxy configuration).
- Local archive: `artifacts/craftisma-brand-20261019.tar.gz` (71,919,223 bytes).
- SHA-256 matched after transfer: `C6B06FDFFE760A9BFB6C9FAA7A688E08925DACACFF3BD4C2B706F2E11725A6F8`.
- Source: working tree based on `919d64c`, retaining the previously published calendar management changes plus branding updates; no commit/push performed.

Used the existing pinned SSH host key and deployment workflow. The first execution connection timed out before running the update; a read-only recheck confirmed the old release remained healthy and the uploaded archive matched, then the update succeeded. Payment/email configuration, customer/order/calendar data and uploads were retained; no new database migration or content import was introduced for branding. After publication, 23 brand HTTP checks, 31 HTTPS PWA checks and 11 calendar-management route/asset checks passed. The eight live logo/icon files match local SHA-256 hashes. Services and backup timer are active; backup and prior release existence confirmed. No admin sign-in, production event mutations, uploads, payments or email sends were performed for verification.

```powershell
pwsh -File Tools/generate-app-icons.ps1
pwsh -File Tests/brand-assets.ps1
node --test Tests/pwa.test.cjs
pwsh -File Tests/brand-http-smoke.ps1 -BaseUrl http://127.0.0.1:5197
```
