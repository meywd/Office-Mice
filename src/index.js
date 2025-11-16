/**
 * Office-Mice Cloudflare Worker
 * Serves Unity WebGL build with optimized caching and compression
 */

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);

    // Custom headers for Unity WebGL files
    const headers = new Headers();

    // Set appropriate MIME types for Unity files
    if (url.pathname.endsWith('.wasm')) {
      headers.set('Content-Type', 'application/wasm');
    } else if (url.pathname.endsWith('.data')) {
      headers.set('Content-Type', 'application/octet-stream');
    } else if (url.pathname.endsWith('.framework.js')) {
      headers.set('Content-Type', 'application/javascript');
    } else if (url.pathname.endsWith('.loader.js')) {
      headers.set('Content-Type', 'application/javascript');
    }

    // Enable compression for all responses
    headers.set('Content-Encoding', 'br'); // Brotli compression

    // Cache control for optimal performance
    if (url.pathname.match(/\.(wasm|data|js|png|jpg|jpeg)$/)) {
      // Cache static assets for 1 year
      headers.set('Cache-Control', 'public, max-age=31536000, immutable');
    } else if (url.pathname.endsWith('.html')) {
      // Don't cache HTML (allows updates)
      headers.set('Cache-Control', 'public, max-age=0, must-revalidate');
    }

    // Security headers
    headers.set('X-Content-Type-Options', 'nosniff');
    headers.set('X-Frame-Options', 'SAMEORIGIN');
    headers.set('X-XSS-Protection', '1; mode=block');

    // CORS headers (if you need cross-origin requests)
    headers.set('Access-Control-Allow-Origin', '*');
    headers.set('Access-Control-Allow-Methods', 'GET, OPTIONS');

    // Handle OPTIONS requests (CORS preflight)
    if (request.method === 'OPTIONS') {
      return new Response(null, { headers });
    }

    // Let Cloudflare serve the static assets
    // The Workers Static Assets feature handles this automatically
    return env.ASSETS.fetch(request);
  },
};
