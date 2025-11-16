# Cloudflare Workers Deployment Guide

This guide explains how to deploy Office-Mice to Cloudflare Workers using GitHub Actions.

## 🚀 Quick Start

The game automatically deploys to Cloudflare Workers when you push to the `master` or `main` branch.

**Live URL:** `https://office-mice.<your-subdomain>.workers.dev`

---

## 📋 Prerequisites

Before deploying, you need to set up the following:

### 1. Unity License Activation

You need a Unity license to build in CI/CD. Choose one option:

#### Option A: Personal License (Free)
1. Request a Personal License file from Unity
2. Convert to base64: `cat Unity_v20XX.x.ulf | base64 -w 0`
3. Add as GitHub Secret: `UNITY_LICENSE`

#### Option B: Professional License
1. Add your Unity email as `UNITY_EMAIL` secret
2. Add your Unity password as `UNITY_PASSWORD` secret
3. Add your Unity serial key as `UNITY_SERIAL` secret

### 2. Cloudflare Configuration

1. **Get Cloudflare Account ID:**
   - Login to [Cloudflare Dashboard](https://dash.cloudflare.com)
   - Go to **Workers & Pages** → **Overview**
   - Copy your **Account ID** from the right sidebar

2. **Create Cloudflare API Token:**
   - Go to [API Tokens](https://dash.cloudflare.com/profile/api-tokens)
   - Click **Create Token**
   - Use the **Edit Cloudflare Workers** template
   - Permissions needed:
     - Account → Workers Scripts → Edit
     - Account → Workers KV Storage → Edit (optional)
   - Copy the generated token

### 3. Configure GitHub Secrets

Add these secrets to your GitHub repository:

1. Go to your repo → **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret** and add:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `CLOUDFLARE_API_TOKEN` | `your-api-token` | Cloudflare API token from step 2 |
| `CLOUDFLARE_ACCOUNT_ID` | `your-account-id` | Cloudflare Account ID from step 1 |
| `UNITY_LICENSE` | `<base64-license>` | Unity license file (base64 encoded) |
| `UNITY_EMAIL` | `your@email.com` | Unity account email (if using serial) |
| `UNITY_PASSWORD` | `your-password` | Unity account password (if using serial) |

---

## 🔧 Configuration Files

### `wrangler.toml`
Main configuration for Cloudflare Workers:

```toml
name = "office-mice"              # Your project name
compatibility_date = "2025-01-16" # Latest compatibility date

[assets]
directory = "./build/WebGL/office-mice"  # Unity WebGL build output
not_found_handling = "single-page-application"
html_handling = "auto-trailing-slash"
```

**Key settings:**
- `directory`: Path to Unity WebGL build (matches GitHub Actions output)
- `not_found_handling`: Treats app as SPA (all routes go to index.html)
- `html_handling`: Automatically handles trailing slashes

### `src/index.js`
Worker script with optimizations:

- **MIME types:** Correct types for `.wasm`, `.data`, `.js` files
- **Compression:** Brotli compression for all assets
- **Caching:** Long-term caching for static assets, no cache for HTML
- **Security headers:** XSS protection, content sniffing protection
- **CORS:** Enabled for cross-origin requests

### `.github/workflows/deploy-cloudflare.yml`
GitHub Actions workflow:

1. Checks out repository
2. Caches Unity Library folder (speeds up builds)
3. Builds Unity WebGL with Brotli compression
4. Deploys to Cloudflare Workers

---

## 🛠️ Local Development

### Install Wrangler CLI

```bash
npm install -g wrangler
```

### Login to Cloudflare

```bash
wrangler login
```

### Build Unity WebGL Locally

1. Open project in Unity
2. Go to **File** → **Build Settings**
3. Select **WebGL** platform
4. Configure settings:
   - **Compression Format:** Brotli
   - **Code Optimization:** Fastest
5. Click **Build** and select output folder: `build/WebGL/office-mice`

### Test Locally

```bash
# Run local development server
wrangler dev

# Open browser to http://localhost:8787
```

### Manual Deploy

```bash
# Deploy to Cloudflare Workers
wrangler deploy

# Deploy with specific environment
wrangler deploy --env production
```

---

## 📊 Deployment Workflow

### Automatic Deployment

**Triggered by:**
- Push to `master` or `main` branch
- Manual trigger via GitHub Actions UI

**Steps:**
1. GitHub Actions checks out code
2. Caches Unity Library folder (faster builds)
3. Frees disk space (Unity needs ~20GB)
4. Builds Unity WebGL with Brotli compression
5. Lists build contents (for debugging)
6. Deploys to Cloudflare Workers
7. Outputs deployment URL

**Build time:** ~15-25 minutes (first build), ~5-10 minutes (cached)

### Manual Deployment

Run the workflow manually:
1. Go to **Actions** tab in GitHub
2. Select **Deploy to Cloudflare Workers**
3. Click **Run workflow**
4. Select branch and click **Run workflow**

---

## 🔍 Troubleshooting

### Build Fails: Unity License Error

**Problem:** `Unity license is not valid`

**Solution:**
- Verify `UNITY_LICENSE` secret is correctly encoded (base64)
- Or use `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL`
- Check Unity version matches in workflow: `unityVersion: 6000.0.12f1`

### Build Fails: Out of Disk Space

**Problem:** `No space left on device`

**Solution:**
- The workflow already includes disk cleanup
- If still failing, reduce Unity cache in workflow
- Or reduce build size in Unity settings

### Deployment Fails: File Too Large

**Problem:** `File exceeds 25MB limit`

**Solution:**
- Unity WebGL files can be large
- Use Brotli compression (already configured)
- Enable code stripping in Unity: **Player Settings** → **Other Settings** → **Strip Engine Code**
- Split large assets into smaller chunks

### Game Doesn't Load

**Problem:** White screen or loading errors

**Solution:**
1. Check browser console for errors
2. Verify MIME types in `src/index.js`
3. Check Unity compression format matches Worker expectations
4. Ensure `not_found_handling` is set to `single-page-application`

### CORS Errors

**Problem:** `Access-Control-Allow-Origin` errors

**Solution:**
- `src/index.js` already includes CORS headers
- If using custom domain, update headers in Worker script

---

## 🌐 Custom Domain

To use a custom domain instead of `.workers.dev`:

1. **Add domain to Cloudflare:**
   - Go to Cloudflare Dashboard → **Websites**
   - Add your domain (e.g., `officemice.com`)

2. **Update `wrangler.toml`:**
   ```toml
   routes = [
     { pattern = "officemice.com", custom_domain = true }
   ]
   ```

3. **Deploy:**
   ```bash
   wrangler deploy
   ```

4. **Configure DNS:**
   - Cloudflare automatically creates DNS records
   - Your game will be live at `https://officemice.com`

---

## 📈 Monitoring and Analytics

### Cloudflare Analytics

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com)
2. Select **Workers & Pages** → **office-mice**
3. View metrics:
   - Requests per second
   - CPU time
   - Errors
   - Bandwidth usage

### Custom Logging

Add to `src/index.js`:

```javascript
console.log(`[${new Date().toISOString()}] ${request.method} ${url.pathname}`);
```

View logs:
```bash
wrangler tail
```

---

## 💰 Costs

**Cloudflare Workers Free Tier:**
- ✅ 100,000 requests/day
- ✅ 10ms CPU time per request
- ✅ Unlimited bandwidth
- ✅ Global CDN

**For Office-Mice:**
- Free tier should handle ~1,000-5,000 players/day
- Unity WebGL assets cached at edge (minimal CPU usage)
- Upgrade to paid plan ($5/month) for unlimited requests

---

## 🎯 Performance Optimization

### Unity Settings

1. **Player Settings** → **Publishing Settings:**
   - Compression Format: **Brotli**
   - Code Optimization: **Fastest**
   - Strip Engine Code: **Enabled**

2. **Player Settings** → **Other Settings:**
   - Managed Stripping Level: **High**
   - IL2CPP Code Generation: **Faster Runtime**

3. **Quality Settings:**
   - Reduce texture quality for WebGL
   - Disable shadows if not needed
   - Reduce audio quality

### Worker Optimization

The `src/index.js` already includes:
- ✅ Long-term caching (1 year for assets)
- ✅ Brotli compression
- ✅ Security headers
- ✅ CORS enabled
- ✅ Correct MIME types

---

## 🔐 Security

**Implemented:**
- ✅ `X-Content-Type-Options: nosniff` (prevents MIME sniffing)
- ✅ `X-Frame-Options: SAMEORIGIN` (prevents clickjacking)
- ✅ `X-XSS-Protection: 1; mode=block` (XSS protection)
- ✅ HTTPS enforced (Cloudflare default)

**Recommended:**
- Add Content Security Policy (CSP) headers
- Enable rate limiting for API endpoints (if added)
- Use Cloudflare Bot Management (for DDoS protection)

---

## 📚 Resources

- [Cloudflare Workers Docs](https://developers.cloudflare.com/workers/)
- [Wrangler CLI Reference](https://developers.cloudflare.com/workers/wrangler/)
- [Unity WebGL Documentation](https://docs.unity3d.com/Manual/webgl-building.html)
- [Game CI Unity Builder](https://game.ci/docs/github/builder)

---

## 🆘 Support

**Issues with deployment?**
1. Check GitHub Actions logs
2. Run `wrangler tail` to view Worker logs
3. Test locally with `wrangler dev`
4. Open an issue in the repository

**Cloudflare specific issues:**
- [Cloudflare Community](https://community.cloudflare.com/)
- [Cloudflare Discord](https://discord.gg/cloudflaredev)

---

## ✅ Deployment Checklist

Before deploying:

- [ ] Unity license configured in GitHub Secrets
- [ ] Cloudflare API token added to GitHub Secrets
- [ ] Cloudflare Account ID added to GitHub Secrets
- [ ] Unity WebGL build settings optimized (Brotli compression)
- [ ] `wrangler.toml` configured with correct paths
- [ ] `.gitignore` updated to exclude build artifacts
- [ ] Tested build locally with `wrangler dev`
- [ ] Committed all changes to `master` or `main` branch

**Ready to deploy!** 🚀

Push to GitHub and watch the magic happen in the Actions tab!
