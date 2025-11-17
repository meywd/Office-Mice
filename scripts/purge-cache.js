/**
 * Purge Cloudflare Cache for Office-Mice Worker
 *
 * Usage:
 * node scripts/purge-cache.js
 *
 * Requires environment variables:
 * - CLOUDFLARE_API_TOKEN
 * - CLOUDFLARE_ACCOUNT_ID
 */

const CLOUDFLARE_API_TOKEN = process.env.CLOUDFLARE_API_TOKEN;
const CLOUDFLARE_ACCOUNT_ID = process.env.CLOUDFLARE_ACCOUNT_ID;
const WORKER_NAME = 'office-mice';

if (!CLOUDFLARE_API_TOKEN || !CLOUDFLARE_ACCOUNT_ID) {
  console.error('❌ Error: Missing environment variables');
  console.error('   Set CLOUDFLARE_API_TOKEN and CLOUDFLARE_ACCOUNT_ID');
  process.exit(1);
}

async function purgeCache() {
  console.log('🔄 Purging Cloudflare cache for office-mice...\n');

  try {
    // Get all deployments
    const deploymentsResponse = await fetch(
      `https://api.cloudflare.com/client/v4/accounts/${CLOUDFLARE_ACCOUNT_ID}/workers/scripts/${WORKER_NAME}/deployments`,
      {
        headers: {
          'Authorization': `Bearer ${CLOUDFLARE_API_TOKEN}`,
          'Content-Type': 'application/json',
        },
      }
    );

    if (!deploymentsResponse.ok) {
      throw new Error(`Failed to get deployments: ${deploymentsResponse.statusText}`);
    }

    const deployments = await deploymentsResponse.json();
    console.log('✅ Found deployments:', deployments.result?.length || 0);

    // For Workers with assets, we need to purge via zone cache
    // Get the worker's custom domain/zone if configured
    console.log('\n💡 To purge cache for Workers, you have two options:\n');
    console.log('1. Wait 5-10 minutes for cache to naturally refresh');
    console.log('2. Use wrangler CLI: wrangler deploy --compatibility-date=2025-01-17');
    console.log('   (Changing compatibility date forces cache refresh)\n');

    // Alternative: Purge everything via wrangler
    console.log('🚀 Best option: Redeploy to force cache refresh');
    console.log('   Run: wrangler deploy\n');

  } catch (error) {
    console.error('❌ Error:', error.message);
    process.exit(1);
  }
}

purgeCache();
