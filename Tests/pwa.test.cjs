// Run with Node.js 18+: node Tests/pwa.test.cjs
// Exercises the worker policy without a browser or production services.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const root = path.resolve(__dirname, '../KashefProject/KashefProject/wwwroot');
const worker = fs.readFileSync(path.join(root, 'service-worker.js'), 'utf8');

function harness() {
  const listeners = {};
  const storage = new Map();
  const calls = [];
  let failNetwork = false;
  let networkStatus = 200;
  let claimed = false;
  class Request {
    constructor(value, options = {}) {
      Object.assign(this, typeof value === 'string' ? { url: value, method: 'GET' } : value, options);
    }
  }
  const caches = {
    async open(name) {
      if (!storage.has(name)) storage.set(name, new Map());
      const cache = storage.get(name);
      return {
        async addAll(requests) {
          for (const request of requests) {
            assert.equal(request.cache, 'reload');
            assert.ok(fs.existsSync(path.join(root, request.url)));
            cache.set(request.url, new Response(`offline asset: ${request.url}`));
          }
        },
        async match(key) { return cache.get(typeof key === 'string' ? key : key.url)?.clone(); }
      };
    },
    async keys() { return [...storage.keys()]; },
    async delete(name) { return storage.delete(name); }
  };
  const self = {
    location: { origin: 'https://craftisma.example' },
    clients: { async claim() { claimed = true; } },
    addEventListener(name, callback) { listeners[name] = callback; },
    skipWaiting() { throw new Error('An update must not force activation during checkout'); }
  };
  vm.runInNewContext(worker, {
    self, caches, Request, Response, URL,
    fetch: async (request) => {
      calls.push(request);
      if (failNetwork) throw new TypeError('Disconnected');
      return new Response('fresh server response', { status: networkStatus });
    }
  });
  return {
    storage, calls,
    offline(value = true) { failNetwork = value; },
    status(value) { networkStatus = value; },
    get claimed() { return claimed; },
    async lifecycle(name) {
      let work;
      listeners[name]({ waitUntil(value) { work = value; } });
      await work;
    },
    async request(route, options = {}) {
      let response;
      listeners.fetch({
        request: new Request(new URL(route, self.location.origin).href, options),
        respondWith(value) { response = value; }
      });
      return response;
    }
  };
}

test('manifest has stable identity, standalone launch, shortcuts and real PNG sizes', () => {
  const manifest = JSON.parse(fs.readFileSync(path.join(root, 'manifest.webmanifest')));
  assert.equal(manifest.id, '/');
  assert.equal(manifest.scope, '/');
  assert.equal(manifest.display, 'standalone');
  assert.equal(manifest.start_url, '/shop/?source=pwa');
  assert.equal(manifest.prefer_related_applications, false);
  assert.ok(manifest.icons.some(icon => icon.purpose === 'maskable'));
  for (const icon of [...manifest.icons, { src: '/icons/apple-touch-icon.png', sizes: '180x180' }]) {
    const png = fs.readFileSync(path.join(root, icon.src));
    assert.equal(png.subarray(1, 4).toString(), 'PNG');
    assert.equal(`${png.readUInt32BE(16)}x${png.readUInt32BE(20)}`, icon.sizes);
  }
});

test('install stores only the four local offline assets', async () => {
  const h = harness();
  await h.lifecycle('install');
  assert.deepEqual(Array.from([...h.storage.values()][0].keys()).sort(),
    ['/offline.html', '/css/offline.css', '/js/offline.js', '/icons/icon-192.png'].sort());
});

test('activation cleans only older Craftisma caches and claims clients without forced update', async () => {
  const h = harness();
  h.storage.set('another-app-cache', new Map());
  h.storage.set('craftisma-offline-old', new Map());
  await h.lifecycle('install');
  await h.lifecycle('activate');
  assert.equal(h.storage.has('another-app-cache'), true);
  assert.equal(h.storage.has('craftisma-offline-old'), false);
  assert.equal(h.storage.size, 2);
  assert.equal(h.claimed, true);
});

test('public and private navigations always use fresh network HTML and never store it', async () => {
  const h = harness();
  await h.lifecycle('install');
  for (const route of ['/', '/shop/', '/shop/item/', '/account/', '/cart/', '/checkout/', '/checkout/saved/private', '/admin/']) {
    const response = await h.request(route, { mode: 'navigate' });
    assert.equal(await response.text(), 'fresh server response');
    assert.equal(h.calls.at(-1).cache, 'no-store');
  }
  assert.equal([...h.storage.values()][0].size, 4);
});

test('a disconnected GET navigation shows only the generic offline page', async () => {
  const h = harness();
  await h.lifecycle('install');
  h.offline();
  for (const route of ['/shop/', '/account/', '/checkout/success?session_id=secret']) {
    assert.equal(await (await h.request(route, { mode: 'navigate' })).text(), 'offline asset: /offline.html');
  }
  assert.equal([...h.storage.values()][0].size, 4);
});

test('HTTP errors are returned as received, not disguised as offline or success', async () => {
  const h = harness();
  await h.lifecycle('install');
  for (const status of [401, 404, 429, 500]) {
    h.status(status);
    assert.equal((await h.request('/checkout/', { mode: 'navigate' })).status, status);
  }
});

test('POSTs, APIs, uploads and third-party payment requests stay outside worker handling', async () => {
  const h = harness();
  await h.lifecycle('install');
  h.offline();
  for (const [route, options] of [
    ['/checkout/', { method: 'POST', mode: 'navigate' }],
    ['/cart/add', { method: 'POST' }],
    ['/account/login', { method: 'POST', mode: 'navigate' }],
    ['/api/order', {}], ['/uploads/product.jpg', {}], ['/css/site.css', {}],
    ['https://checkout.stripe.com/pay/session', { mode: 'navigate' }],
    ['/offline.html?private=value', {}]
  ]) assert.equal(await h.request(route, options), undefined);
  assert.equal(h.calls.length, 0);
});

test('offline shell assets work disconnected and a missing shell never substitutes private data', async () => {
  const h = harness();
  await h.lifecycle('install');
  h.offline();
  for (const route of ['/css/offline.css', '/js/offline.js', '/icons/icon-192.png']) {
    assert.equal(await (await h.request(route)).text(), `offline asset: ${route}`);
  }
  [...h.storage.values()][0].delete('/offline.html');
  assert.equal((await h.request('/account/', { mode: 'navigate' })).type, 'error');
});

function installHarness({ secure = true, standalone = false, guide = true } = {}) {
  const events = {};
  const button = { hidden: true, addEventListener(name, handler) { this[name] = handler; } };
  const status = { textContent: '' };
  const footerLink = { hidden: false };
  const registrations = [];
  const navigator = { serviceWorker: { register(url, options) {
    registrations.push({ url, options });
    return Promise.resolve();
  } } };
  const window = { navigator, isSecureContext: secure,
    matchMedia: () => ({ matches: standalone }),
    addEventListener(name, handler) { events[name] = handler; } };
  vm.runInNewContext(fs.readFileSync(path.join(root, 'js/pwa.js'), 'utf8'), {
    navigator, window, console,
    document: {
      querySelector(selector) { return guide ? (selector === '[data-install-app]' ? button : status) : null; },
      querySelectorAll() { return [footerLink]; }
    }
  });
  return { events, button, status, footerLink, registrations };
}

test('registration needs a secure context and worker checks bypass the HTTP cache', () => {
  const h = installHarness();
  assert.equal(h.registrations[0].url, '/service-worker.js');
  assert.equal(h.registrations[0].options.scope, '/');
  assert.equal(h.registrations[0].options.updateViaCache, 'none');
  assert.equal(installHarness({ secure: false }).registrations.length, 0);
});

test('installation opens only after a user click; cancellation and completion give useful status', async () => {
  for (const outcome of ['accepted', 'dismissed']) {
    const h = installHarness();
    let prompted = 0;
    let prevented = false;
    h.events.beforeinstallprompt({ preventDefault() { prevented = true; },
      async prompt() { prompted++; }, userChoice: Promise.resolve({ outcome }) });
    assert.equal(prevented, true);
    assert.equal(h.button.hidden, false);
    assert.equal(prompted, 0);
    await h.button.click();
    assert.equal(prompted, 1);
    assert.equal(h.button.hidden, true);
    assert.match(h.status.textContent, outcome === 'accepted' ? /installation steps/ : /later/);
    await h.button.click();
    assert.equal(prompted, 1);
    h.events.appinstalled();
    assert.match(h.status.textContent, /has been added/);
  }
});

test('standalone mode hides redundant installation and ordinary pages leave browser prompts available', () => {
  const standalone = installHarness({ standalone: true });
  let suppressed = false;
  standalone.events.beforeinstallprompt({ preventDefault() { suppressed = true; } });
  assert.equal(standalone.footerLink.hidden, true);
  assert.equal(standalone.button.hidden, true);
  assert.match(standalone.status.textContent, /already using/);
  const shop = installHarness({ guide: false });
  shop.events.beforeinstallprompt({ preventDefault() { suppressed = true; } });
  assert.equal(suppressed, false);
});
