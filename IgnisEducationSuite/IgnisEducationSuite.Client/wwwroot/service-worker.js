// ======================
// Ignis Service Worker
// ======================
//
// Scope is the whole origin, so this worker sees every request the app makes. That makes what it
// declines to handle more important than what it caches: any request passed to respondWith() has
// its failures reported from inside this file, so a server restart or a blocked third-party script
// surfaced as "TypeError: Failed to fetch at service-worker.js" instead of the real error.
//
// The rule below is therefore: if this worker is not certain it should be involved, it returns
// without calling respondWith() at all and the browser handles the request natively.

// Bump on every deployment. The activate handler deletes every cache that is not this one, so a
// stale version left serving an old index.html is cleared by changing this line.
const CACHE_VERSION = 'v1.2.4';
const CACHE_NAME = `Ignis-Edu-cache-${CACHE_VERSION}`;

// Paths this worker must never touch.
//
//  - /api/            application data; caching it would serve one school's answer to another
//  - /_blazor         Blazor's own circuit transport (negotiate is a POST, then long-poll)
//  - /ChatHub         SignalR negotiate, likewise a POST
//  - /Account/, /Identity/  sign-in, sign-out and cookie flows must always hit the server
//  - /_framework/blazor.boot.json  handled below, but never served stale
const BYPASS_PATTERNS = [
    /^\/api\//i,
    /^\/_blazor/i,
    /^\/ChatHub/i,
    /^\/Account\//i,
    /^\/Identity\//i,
];

self.addEventListener('install', event => {
    console.log('[SW] Installing and caching assets...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(self.assetsManifest?.assets.map(asset => asset.url) || []))
            // A single missing asset used to fail the whole install and leave the previous worker in
            // place. Installing without a warm cache is better than not installing.
            .catch(error => console.warn('[SW] Pre-cache incomplete:', error))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    console.log('[SW] Activating and clearing old caches...');
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const request = event.request;

    // Only GET is cacheable, and only GET is safe to replay. Everything else — every POST, PUT and
    // DELETE the app makes — is left to the browser. This guard is the one the published worker has
    // and this one did not.
    if (request.method !== 'GET') return;

    let url;
    try {
        url = new URL(request.url);
    } catch {
        return;
    }

    // Third-party scripts (Zoom's SDK, CDN libraries) are none of this worker's business, and
    // wrapping them meant an adblocked or unreachable CDN reported as a worker failure.
    if (url.origin !== self.location.origin) return;

    if (BYPASS_PATTERNS.some(pattern => pattern.test(url.pathname))) return;

    // Range requests (audio, video, PDF viewers) must not be answered from a whole-body cache entry.
    if (request.headers.has('range')) return;

    // Navigations go to the network first. Cache-first here is what pinned the app to whatever
    // index.html happened to be cached, so a new deployment stayed invisible until the browser's
    // site data was cleared by hand.
    if (request.mode === 'navigate') {
        event.respondWith(networkFirst(request));
        return;
    }

    if (url.pathname.includes('/_framework/')) {
        event.respondWith(networkFirst(request));
        return;
    }

    event.respondWith(cacheFirst(request));
});

/// Network, falling back to whatever was cached last. Used where being current matters more than
/// being fast: the app shell and the Blazor framework files.
async function networkFirst(request) {
    try {
        const response = await fetch(request);
        await put(request, response);
        return response;
    } catch (error) {
        const cached = await caches.match(request);
        if (cached) return cached;
        throw error;
    }
}

/// Cache, falling back to the network. Used for static assets, where a stale copy is acceptable
/// and a round trip is not.
async function cacheFirst(request) {
    const cached = await caches.match(request);
    if (cached) return cached;

    const response = await fetch(request);
    await put(request, response);
    return response;
}

/// Stores a response, ignoring the ones a cache cannot hold.
///
/// cache.put() throws on partial (206) and opaque responses, and that rejection previously escaped
/// into respondWith and failed the request that had already succeeded.
async function put(request, response) {
    if (!response || !response.ok || response.status === 206 || response.type === 'opaque') return;

    try {
        const cache = await caches.open(CACHE_NAME);
        await cache.put(request, response.clone());
    } catch (error) {
        console.warn('[SW] Could not cache', request.url, error);
    }
}

// Lets the page tell a waiting worker to take over immediately.
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
