# Mobile purchase readiness — phase 1

Reviewed on October 1, 2026. Local mobile preparation is implemented and verified. Live payment and physical-device acceptance remain outstanding.

## Implemented

- Product columns can shrink to the viewport. Mobile titles wrap, images use a proportional frame, and thumbnails scroll horizontally.
- Mobile navigation uses larger touch targets. Closed links are excluded from keyboard focus; Escape, outside clicks, and focus leaving the header close the menu.
- Checkout presents the order summary before contact details on mobile, including subtotal, shipping, and total before tax. Desktop keeps its two-column layout.
- Mobile account and checkout fields use 16px text, and account forms no longer open the keyboard automatically. Quantity fields request a numeric keyboard.
- Product, bag, login, registration, checkout, and payment buttons show a pending state and prevent repeated submission of the same form. Back/forward restoration resets the buttons. Existing server checkout idempotency remains in effect.
- Customer validation now loads jQuery before its validators. Boolean consent ranges are mapped to required checkboxes so accepting the policies passes client validation. Server policy validation remains enforced.
- A known, owned Stripe return routes to the saved order with an explanatory message if verification is unavailable. A protected payment-status action lets the owner request another Stripe check. Unknown sessions cannot reveal orders, and the client never marks an order paid.

## Verification

- Application build: succeeded with zero warnings and zero errors.
- Commerce smoke suite: 56 checks passed against an isolated local database, with Stripe and outbound email disabled. Includes guest-bag transfer, price review, concurrent submission, order ownership, unpaid status, refunds, shipping, and payment-status access checks.
- Product, bag, and checkout: browser viewport checks at 320, 390, 768, and 1280 pixels found no content clipping. Intentional horizontal thumbnail scrolling is excluded from this check.
- Login and registration: no content overflow at 320, 390, and 768 pixels. Invalid submissions leave the button usable; a checked policy box passes client validation while other invalid fields still block submission.
- Browser interactions: menu open/close and Escape focus restoration, gallery view selection, login returning to checkout with its bag, and required checkout fields were exercised.
- New registration/login browser session: no console errors.

Local evidence is in `artifacts/mobile-review/`: `layout-results.json`, `commerce-smoke-results.txt`, `product-mobile.png`, and `checkout-mobile.png`. The $85 product shown in the checkout screenshot is an isolated test fixture, not a production price.

## Remaining acceptance work

The local environment has no Stripe credentials or outbound-email configuration. Stripe-hosted checkout, 3-D Secure, successful/failed/cancelled payments, webhook timing, and the new verification-error branch have not been exercised against Stripe. Verify these with test credentials and an HTTPS return/webhook endpoint, then perform the authorized live launch checks when payment is ready.

Browser viewport checks do not replace testing on actual Android Chrome and iPhone Safari. Verify keyboard behavior, autofill, browser back navigation, external Stripe return, and the whole purchase path on those devices before declaring phase 1 accepted for launch.

PWA implementation is now covered by [phase 2](pwa-readiness.md). Its offline cache contains only the generic reconnect screen and its static assets; product, account, cart, checkout, and payment data stay online.
