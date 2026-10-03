// Bump this version when any offline asset changes. Updates activate after old tabs close.
const CACHE_PREFIX = "craftisma-offline-";
const CACHE_NAME = `${CACHE_PREFIX}v1`;
const OFFLINE_PAGE = "/offline.html";
const OFFLINE_ASSETS = [OFFLINE_PAGE, "/css/offline.css", "/js/offline.js", "/icons/icon-192.png"];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.addAll(
    OFFLINE_ASSETS.map((url) => new Request(url, { cache: "reload" }))
  )));
  // No skipWaiting: a new release must not interrupt an active purchase.
});

self.addEventListener("activate", (event) => {
  event.waitUntil((async () => {
    const keys = await caches.keys();
    await Promise.all(keys.filter((key) => key.startsWith(CACHE_PREFIX) && key !== CACHE_NAME)
      .map((key) => caches.delete(key)));
    await self.clients.claim();
  })());
});

self.addEventListener("fetch", (event) => {
  const request = event.request;
  const url = new URL(request.url);
  // Never handle form submissions, payment providers, uploads, or APIs as offline content.
  if (request.method !== "GET" || url.origin !== self.location.origin) return;
  if (request.mode === "navigate") {
    event.respondWith((async () => {
      try {
        // Razor HTML contains account and bag state, even on public pages.
        return await fetch(new Request(request, { cache: "no-store" }));
      } catch {
        const cache = await caches.open(CACHE_NAME);
        return await cache.match(OFFLINE_PAGE) || Response.error();
      }
    })());
    return;
  }
  // Only this fixed, non-personal offline shell is cached. Never cache products or prices.
  if (!url.search && OFFLINE_ASSETS.includes(url.pathname)) {
    event.respondWith(caches.open(CACHE_NAME).then(async (cache) =>
      await cache.match(url.pathname) || fetch(request)));
  }
});
