// ======================
// Fenyra Optimized Service Worker
// ======================


const CACHE_VERSION = 'v1.1.1'; // increment this with every deployment
const CACHE_NAME = `Ignis-Edu-cache-${CACHE_VERSION}`;

// During install, cache all assets from the manifest
self.addEventListener('install', event => {
    console.log('[SW] Installing and caching assets...');
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(
                self.assetsManifest?.assets.map(asset => asset.url) || []
            );
        }).then(() => self.skipWaiting())
    );
});

// On activate, remove old caches
self.addEventListener('activate', event => {
    console.log('[SW] Activating and clearing old caches...');
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys
                    .filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            )
        ).then(() => self.clients.claim())
    );
});

// Fetch handler
self.addEventListener('fetch', event => {
    const url = event.request.url;

    // Network-first strategy for Blazor framework files
    if (url.includes('_framework/')) {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    return caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, response.clone());
                        return response;
                    });
                })
                .catch(() => caches.match(event.request))
        );
    } else {
        // Cache-first for everything else (images, CSS, HTML)
        event.respondWith(
            caches.match(event.request).then(response => response || fetch(event.request))
        );
    }
});

// Optional: Listen for message from client to force update
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
