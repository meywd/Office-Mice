/**
 * Office-Mice Cloudflare Worker
 * Serves Unity WebGL build with proper compression headers
 */

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);

    // Get the response from static assets first
    const response = await env.ASSETS.fetch(request);

    // Clone the response so we can modify headers
    const newResponse = new Response(response.body, response);

    // Set appropriate MIME types and compression for Unity files
    if (url.pathname.endsWith('.wasm.gz')) {
      newResponse.headers.set('Content-Type', 'application/wasm');
      newResponse.headers.set('Content-Encoding', 'gzip');
    } else if (url.pathname.endsWith('.wasm.br')) {
      newResponse.headers.set('Content-Type', 'application/wasm');
      newResponse.headers.set('Content-Encoding', 'br');
    } else if (url.pathname.endsWith('.wasm')) {
      newResponse.headers.set('Content-Type', 'application/wasm');
    } else if (url.pathname.endsWith('.data.gz')) {
      newResponse.headers.set('Content-Type', 'application/octet-stream');
      newResponse.headers.set('Content-Encoding', 'gzip');
    } else if (url.pathname.endsWith('.data.br')) {
      newResponse.headers.set('Content-Type', 'application/octet-stream');
      newResponse.headers.set('Content-Encoding', 'br');
    } else if (url.pathname.endsWith('.data')) {
      newResponse.headers.set('Content-Type', 'application/octet-stream');
    } else if (url.pathname.endsWith('.framework.js.gz')) {
      newResponse.headers.set('Content-Type', 'application/javascript');
      newResponse.headers.set('Content-Encoding', 'gzip');
    } else if (url.pathname.endsWith('.framework.js.br')) {
      newResponse.headers.set('Content-Type', 'application/javascript');
      newResponse.headers.set('Content-Encoding', 'br');
    } else if (url.pathname.endsWith('.framework.js')) {
      newResponse.headers.set('Content-Type', 'application/javascript');
    } else if (url.pathname.endsWith('.loader.js')) {
      newResponse.headers.set('Content-Type', 'application/javascript');
    } else if (url.pathname.endsWith('.js.gz')) {
      newResponse.headers.set('Content-Type', 'application/javascript');
      newResponse.headers.set('Content-Encoding', 'gzip');
    } else if (url.pathname.endsWith('.js.br')) {
      newResponse.headers.set('Content-Type', 'application/javascript');
      newResponse.headers.set('Content-Encoding', 'br');
    }

    // Cache control for optimal performance
    if (url.pathname.match(/\.(wasm|data|js|png|jpg|jpeg|gz|br)$/)) {
      // Cache static assets for 1 year
      newResponse.headers.set('Cache-Control', 'public, max-age=31536000, immutable');
    } else if (url.pathname.endsWith('.html')) {
      // Don't cache HTML (allows updates)
      newResponse.headers.set('Cache-Control', 'public, max-age=0, must-revalidate');
    }

    // Security headers
    newResponse.headers.set('X-Content-Type-Options', 'nosniff');
    newResponse.headers.set('X-Frame-Options', 'SAMEORIGIN');
    newResponse.headers.set('X-XSS-Protection', '1; mode=block');

    // CORS headers (if you need cross-origin requests)
    newResponse.headers.set('Access-Control-Allow-Origin', '*');
    newResponse.headers.set('Access-Control-Allow-Methods', 'GET, HEAD, OPTIONS');
    newResponse.headers.set('Access-Control-Allow-Headers', 'Content-Type');

    return newResponse;
  },
};
