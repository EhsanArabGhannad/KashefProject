# Craftisma PWA — phase 2

Implemented and checked locally on October 4, 2026. Production deployment and physical-device installation acceptance remain outstanding.

## Implemented

- A stable app identity, root scope, standalone display, and launch into the shop. Manifest shortcuts open the shop or account. Orientation is unrestricted.
- Craftisma icons at 192 and 512 pixels, a separate maskable icon with its mark inside the safe area, and a 180-pixel Apple home-screen icon. Rebuild these code-drawn assets on Windows with `pwsh -File Tools/generate-app-icons.ps1`.
- `/app/` explains Android Chrome and iPhone/iPad Safari installation. A footer link opens it. Browsers that expose `beforeinstallprompt` can show a user-initiated install button on this page; other pages retain the browser's own installation prompt. Standalone mode hides the redundant footer link.
- A root service worker registers only in a secure context, with `updateViaCache: none`. The worker and manifest responses use `Cache-Control: no-cache`; the manifest has `application/manifest+json` content type.
- A local reconnect screen works without CDN fonts or remote assets. It explains that products and orders need a connection, provides a retry button, and directs customers to check their order status after an interrupted payment. Device connectivity is a hint, never proof that the shop is reachable.

## Offline and update policy

The only Cache Storage entries are `/offline.html`, `/css/offline.css`, `/js/offline.js`, and `/icons/icon-192.png`, in the versioned `craftisma-offline-v1` cache. These files contain no personal, catalog, price, or order data.

Every same-origin GET navigation goes to the network with `cache: no-store`. A rejected network request falls back to the generic reconnect screen at the original URL; server HTTP errors pass through unchanged. Razor HTML is never placed in Cache Storage, including public pages whose headers contain account and bag state. Existing MVC `no-store` headers remain intact.

POSTs, non-navigation APIs, uploads, ordinary site assets, query variants of offline assets, and other origins (including Stripe) are outside worker handling. Failed form submissions are not queued or replayed. Buying, signing in, and paying require the server. First-time visitors must load the shop online before offline support is available; browser storage can also be evicted.

Updates do not call `skipWaiting`, reload tabs, or interrupt an active purchase. A new worker activates after clients using the previous worker close. Activation deletes only older caches in Craftisma's own namespace. **Bump the worker cache version whenever an offline asset changes.**

## Verification

- .NET build succeeded: zero warnings and zero errors.
- `node --test Tests/pwa.test.cjs`: 11 tests passed. Covers icon dimensions, manifest identity, cache boundaries, authenticated/public navigation, disconnected navigation, HTTP error handling, POST/payment exclusions, cache cleanup, secure registration, installation user gesture, cancellation, completion, and standalone UI.
- Commerce regression suite: all 56 checks passed on a separate local test database with outbound email and Stripe disabled.
- HTTP checks: manifest, worker, reconnect screen, its CSS/JS, all four PNG icons, installation guide, and checkout were served successfully. Manifest/worker revalidation headers and HTML `no-store` headers were confirmed.
- Installation guide and reconnect screen: no horizontal overflow at 320, 390, 768, and 1280 pixels; icons loaded.
- Real browser worker behavior: loaded the guide online, reloaded, stopped the isolated local server, and opened the shop. The cached reconnect screen and its assets displayed. Opening the account also displayed only that generic screen. Restarting the server and clicking Try again resumed the account's normal sign-in redirect.
- Guide browser console: no warnings or errors during the online check.

Evidence is in the ignored `artifacts/pwa-review/` directory: `pwa-test-results.txt`, `commerce-smoke-results.txt`, `layout-results.json`, `install-mobile.png`, and `offline-mobile.png`. Browser disconnection was simulated by stopping the local server, not by a physical phone losing its network.

The static design export removes PWA links and registration script blocks. App installation targets the server-backed MVC store; the GitHub Pages design preview has no functioning accounts or checkout. No production files or deployment were changed by the verification run.

## Production acceptance

1. Deploy the full MVC store at the root of an HTTPS origin. The manifest URLs and service-worker scope assume root hosting. Confirm `/manifest.webmanifest`, `/service-worker.js`, `/offline.html`, their assets and revalidation headers through the actual reverse proxy.
2. On a physical Android phone, install from Chrome; confirm the icon, standalone launch, shop landing page, account sign-in, bag persistence, external links and back navigation. Installation prompt availability depends on browser eligibility and user settings.
3. On a physical iPhone, add from Safari with Open as Web App enabled when shown. Confirm the home-screen icon, standalone launch, account/session behavior, keyboard and navigation. Do not assume the browser and installed app share an existing login session.
4. On both devices, load online, disconnect, navigate and reopen the app, then reconnect and retry. Confirm that no cached customer/order or product-price screen is shown.
5. Deploy a worker version update while an order form is open. Confirm no forced reload; close old clients and relaunch to verify activation.
6. With configured Stripe test credentials and HTTPS callbacks/webhooks, complete successful, failed and cancelled payments, including authentication challenges and interrupted returns. Confirm the server-owned order status before live-payment acceptance. This remains part of phase 1's outstanding launch checks.

## Platform references

- [Chrome manifest installability guidance](https://developer.chrome.com/docs/lighthouse/pwa/installable-manifest) informed the manifest fields and icon sizes; Lighthouse's old PWA score is not used as an acceptance test.
- [Apple's Safari home-screen guide](https://support.apple.com/en-hk/guide/iphone/iphea86e5236/ios) informed the iPhone installation steps.
- [MDN updateViaCache](https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerRegistration/updateViaCache) describes worker-script cache revalidation.
