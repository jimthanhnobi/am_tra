// HTTP integration against a real production Next build. Fixtures stay in this
// test process; this is not a browser E2E or a substitute for PostgreSQL tests.
import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { once } from "node:events";
import { createServer } from "node:http";
import { fileURLToPath } from "node:url";
import { setTimeout as delay } from "node:timers/promises";
import test from "node:test";

const cwd = fileURLToPath(new URL("../", import.meta.url));
const next = fileURLToPath(new URL("../node_modules/next/dist/bin/next", import.meta.url));
const seo = { seoTitle: "Ấm thử SEO", metaDescription: "Mô tả từ API thử nghiệm", canonicalOverride: null, robotsIndex: true, ogTitle: null, ogDescription: null, ogImageAssetId: null };
const category = { slug: "am-tra", name: "Danh mục thử nghiệm" };
const media = { assetId: "test-image", publicUrl: "https://media.catalog.test/am.webp", width: 800, height: 600, altText: "Ảnh thử nghiệm", sortOrder: 0, isCover: true };
const product = { slug: "am-public", name: "Ấm API thử nghiệm", summary: "Tóm tắt sản phẩm từ API", sku: "TEST-ONLY", descriptionHtml: "<p>Nội dung HTML từ API thử nghiệm.</p>", productType: "Teapot", materialText: "Gốm thử nghiệm", capacityMl: 180, specifications: { color: "Màu thử nghiệm", price: "PRIVATE_PRICE_SENTINEL", adminNotes: "PRIVATE_SPEC_SENTINEL" }, seo, primaryCategory: category, categories: [category], brand: null, media: [media], price: "PRIVATE_PRICE_SENTINEL", adminNotes: "PRIVATE_ADMIN_SENTINEL" };
const card = { ...product, coverImage: media };
const noindexProduct = { ...product, slug: "noindex", seo: { ...seo, robotsIndex: false } };
const article = { slug: "bai-viet-test", title: "Bài viết API thử nghiệm", excerpt: "Tóm tắt bài viết thử nghiệm.", contentHtml: "<p>Nội dung bài viết từ API.</p>", publishedAt: "2026-10-30T00:00:00Z", updatedAtUtc: "2026-10-30T01:00:00Z", seo: { seoTitle: "Bài viết SEO", metaDescription: seo.metaDescription, canonicalOverride: null, robotsIndex: true, ogTitle: null, ogDescription: null }, categories: [], coverUrl: null, coverWidth: null, coverHeight: null, ogImageUrl: "https://media.catalog.test/article-og.webp", ogImageWidth: 1200, ogImageHeight: 630, contentJson: "PRIVATE_ADMIN_SENTINEL", price: "PRIVATE_PRICE_SENTINEL" };
const noindexArticle = { ...article, slug: "bai-viet-noindex", seo: { ...article.seo, robotsIndex: false } };

async function listen(server) {
  server.listen(0, "127.0.0.1");
  await once(server, "listening");
  return server.address().port;
}
function launch(args, env) {
  const child = spawn(process.execPath, [next, ...args], { cwd, env, windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
  let output = "";
  for (const stream of [child.stdout, child.stderr]) stream.on("data", (chunk) => { output = (output + chunk).slice(-50_000); });
  return { child, output: () => output };
}
async function stop(processInfo) {
  if (processInfo && processInfo.child.exitCode === null) {
    const exited = once(processInfo.child, "exit");
    processInfo.child.kill();
    await exited;
  }
}

test("production storefront HTTP contract", { timeout: 180_000 }, async (t) => {
  let hidden = false;
  let hiddenArticle = false;
  let empty = false;
  let failApi = false;
  let settingsConfigured = false;
  let visualWithoutImages = false;
  const calls = [];
  const api = createServer((req, res) => {
    calls.push({ path: req.url, cookie: req.headers.cookie, authorization: req.headers.authorization });
    const url = new URL(req.url, "http://fixture.test");
    const page = Number(url.searchParams.get("page") || 1);
    const pageSize = Number(url.searchParams.get("pageSize") || 12);
    const visualCard = visualWithoutImages ? { ...card, coverImage: null } : card;
    const pageData = { items: empty || hidden ? [] : [visualCard, { ...visualCard, slug: "noindex" }], page, pageSize, totalItems: empty || hidden ? 0 : 13, totalPages: empty || hidden ? 0 : Math.ceil(13 / pageSize) };
    let body;
    if (failApi) { res.writeHead(503).end(); return; }
    if (url.pathname === "/api/v1/public/seo-redirects" &&
        url.searchParams.get("sourcePath") === "/san-pham/old-product")
      body = { destinationPath: "/san-pham/am-public", redirectType: 301 };
    if (url.pathname === "/api/v1/public/seo-redirects" &&
        url.searchParams.get("sourcePath") === "/kien-thuc/old-article")
      body = { destinationPath: "/kien-thuc/bai-viet-test", redirectType: 301 };
    if (url.pathname === "/api/v1/public/seo-redirects" &&
        url.searchParams.get("sourcePath") === "/danh-muc/old-category")
      body = { destinationPath: "/danh-muc/am-tra", redirectType: 301 };
    if (url.pathname === "/api/v1/public/settings" && settingsConfigured)
      body = { isConfigured: true, siteName: "Configured catalog", siteDescription: "Configured description",
        brandTagline: "Configured tagline", logoUrl: null, faviconUrl: null, hotline: "0912345678",
        zaloUrl: "https://zalo.me/0912345678", facebookUrl: null, email: null, address: null,
        openingHours: null, defaultSeoTitle: "Configured SEO", defaultMetaDescription: "Configured meta",
        defaultOgImageUrl: null, publicWebsiteUrl: "https://configured.catalog.test" };
    if (url.pathname === "/api/v1/public/categories") body = empty ? [] : [{ ...category, children: [] }, { slug: "noindex", name: "Danh mục noindex", children: [] }];
    if (url.pathname === "/api/v1/public/categories/am-tra") body = { ...category, descriptionHtml: "<p>Danh mục từ API</p>", seo: { ...seo, seoTitle: "Danh mục thử nghiệm" }, products: pageData };
    if (url.pathname === "/api/v1/public/categories/noindex") body = { slug: "noindex", name: "Danh mục noindex", descriptionHtml: "<p>Danh mục Published, không index.</p>", seo: { ...seo, robotsIndex: false }, products: pageData };
    if (url.pathname === "/api/v1/public/products") body = pageData;
    if (url.pathname === "/api/v1/public/products/am-public" && !hidden) body = visualWithoutImages ? { ...product, media: [] } : product;
    if (url.pathname === "/api/v1/public/products/noindex") body = noindexProduct;
    if (url.pathname === "/api/v1/public/articles") body = { items: hiddenArticle ? [noindexArticle] : [article, noindexArticle], page, pageSize, totalItems: hiddenArticle ? 1 : 2, totalPages: 1 };
    if (url.pathname === "/api/v1/public/articles/bai-viet-test" && !hiddenArticle) body = article;
    if (url.pathname === "/api/v1/public/articles/bai-viet-noindex") body = noindexArticle;
    res.writeHead(body ? 200 : 404, { "Content-Type": "application/json" });
    res.end(JSON.stringify(body || { title: "Not found" }));
  });
  let build;
  let server;
  try {
    const apiPort = await listen(api);
    const portProbe = createServer();
    const webPort = await listen(portProbe);
    await new Promise((resolve) => portProbe.close(resolve));
    const origin = `http://127.0.0.1:${webPort}`;
    const env = { ...process.env, NODE_ENV: "production", NEXT_TELEMETRY_DISABLED: "1", API_BASE_URL: `http://127.0.0.1:${apiPort}/api/v1`, NEXT_PUBLIC_SITE_URL: "https://catalog.test", NEXT_PUBLIC_SITE_NAME: "Test catalog", SITE_INDEXABLE: "true", SITE_ABOUT: "", SITE_ADDRESS: "", SITE_HOTLINE: "", SITE_ZALO_URL: "https://zalo.me/123456789", SITE_FACEBOOK_URL: "", R2_PUBLIC_BASE_URL: "https://media.catalog.test" };
    build = launch(["build"], env);
    const [buildCode] = await once(build.child, "exit");
    assert.equal(buildCode, 0, build.output());
    server = launch(["start", "--hostname", "127.0.0.1", "--port", String(webPort)], env);
    let ready = false;
    for (let attempt = 0; attempt < 100; attempt++) {
      if (server.child.exitCode !== null) throw new Error(server.output());
      try { if ((await fetch(`${origin}/robots.txt`)).ok) { ready = true; break; } } catch { /* wait for owned server */ }
      await delay(100);
    }
    assert.ok(ready, server.output());
    await t.test("old public slugs return a direct 301 to a 200 target", async () => {
      for (const [oldPath, newPath] of [["/san-pham/old-product", "/san-pham/am-public"],
        ["/kien-thuc/old-article", "/kien-thuc/bai-viet-test"],
        ["/danh-muc/old-category", "/danh-muc/am-tra"]]) {
        const redirect = await fetch(origin + oldPath, { redirect: "manual" });
        assert.equal(redirect.status, 301);
        assert.equal(new URL(redirect.headers.get("location"), origin).pathname, newPath);
        const target = await fetch(origin + newPath);
        assert.equal(target.status, 200);
        assert.match(await target.text(), new RegExp(`rel="canonical" href="https://catalog.test${newPath}"`));
      }
    });
    async function get(path, status = 200) {
      const response = await fetch(origin + path, { redirect: "manual", headers: { Cookie: "session=PRIVATE_COOKIE", Authorization: "Bearer PRIVATE_AUTH" } });
      const html = await response.text();
      assert.equal(response.status, status, `${path}: ${html.slice(0, 300)}\n${server.output()}`);
      assert.doesNotMatch(html, /PRIVATE_(PRICE|SPEC|ADMIN|COOKIE|AUTH)_?\w*/);
      return html;
    }
    await t.test("editorial home and contextual product consultation use published data", async () => {
      const home = await get("/");
      assert.match(home, /<h1[^>]*>Ấm API thử nghiệm<\/h1>/);
      assert.match(home, /Bắt đầu từ/);
      assert.match(home, /Chọn Ấm API thử nghiệm|Xem chi tiết Ấm API thử nghiệm/);
      assert.match(home, /href="\/san-pham\/am-public"/);
      assert.doesNotMatch(home, /Giỏ hàng|Thanh toán ngay|PRIVATE_PRICE_SENTINEL/);
      const contact = await get("/lien-he?san-pham=am-public");
      assert.match(contact, /Sản phẩm bạn đang quan tâm/);
      assert.match(contact, /Ấm API thử nghiệm/);
      assert.match(contact, /Tôi muốn được tư vấn sản phẩm/);
    });
    await t.test("bundled demo art is served locally without R2 and visibly marked as demo", async () => {
      const response = await fetch(origin + "/demo-products/am-tu-sa-tay-thi.svg");
      assert.equal(response.status, 200);
      assert.match(response.headers.get("content-type") || "", /image\/svg\+xml/);
      const svg = await response.text();
      assert.match(svg, /DEMO ILLUSTRATION/);
      assert.match(svg, /<svg/);
    });
    await t.test("public product has server HTML, metadata, gallery, contact and honest JSON-LD", async () => {
      const html = await get("/san-pham/am-public");
      assert.match(html, /<h1[^>]*>Ấm API thử nghiệm<\/h1>/);
      assert.match(html, /Nội dung HTML từ API thử nghiệm/);
      assert.match(html, /<title>Ấm thử SEO · Test catalog<\/title>/);
      assert.match(html, /rel="canonical" href="https:\/\/catalog.test\/san-pham\/am-public"/);
      assert.match(html, /property="og:title"/);
      assert.match(html, /name="robots" content="index, follow"/);
      assert.match(html, /href="https:\/\/zalo.me\/123456789"/);
      assert.match(html, /width="800" height="600"/);
      const jsonLd = [...html.matchAll(/<script type="application\/ld\+json">(.*?)<\/script>/gs)].map((match) => JSON.parse(match[1]));
      assert.equal(jsonLd.find((item) => item["@type"] === "Product").name, product.name);
      assert.ok(jsonLd.some((item) => item["@type"] === "BreadcrumbList"));
      assert.doesNotMatch(JSON.stringify(jsonLd), /"(?:offers|price|availability|aggregateRating|review)"/i);
    });
    await t.test("missing, draft and archived return actual HTTP 404", async () => {
      for (const slug of ["missing", "draft", "archived"]) {
        const html = await get(`/san-pham/${slug}`, 404);
        assert.match(html, /noindex/);
        assert.doesNotMatch(html, /<h1[^>]*>Ấm API thử nghiệm/);
      }
      await get("/danh-muc/missing", 404);
      await get("/kien-thuc/draft", 404);
    });
    await t.test("Published noindex product, category and article return 200 with noindex metadata", async () => {
      for (const path of ["/san-pham/noindex", "/danh-muc/noindex", "/kien-thuc/bai-viet-noindex"]) {
        const html = await get(path);
        assert.match(html, /<h1[^>]*>/);
        assert.match(html, /name="robots" content="noindex, follow"/);
        assert.ok(html.includes(`rel="canonical" href="https://catalog.test${path}"`));
      }
      assert.doesNotMatch(await get("/sitemap.xml"), /<loc>[^<]*(?:\/san-pham\/noindex|\/danh-muc\/noindex|\/kien-thuc\/bai-viet-noindex)<\/loc>/);
    });
    await t.test("Admin BFF rejects cross-origin writes and never permits public caching", async () => {
      const path = `${origin}/api/admin/auth/login`;
      const before = calls.length;
      const foreign = await fetch(path, { method: "POST", headers: { origin: "https://foreign.test", "content-type": "application/json" }, body: "{}" });
      assert.equal(foreign.status, 403);
      assert.equal(foreign.headers.get("cache-control"), "no-store");
      assert.equal(calls.length, before);
      const sameOrigin = await fetch(path, { method: "POST", headers: { origin: "https://catalog.test", "content-type": "application/json" }, body: "{}" });
      assert.equal(sameOrigin.status, 404); // Fixture has no Admin login handler; request reached the upstream.
      assert.equal(sameOrigin.headers.get("cache-control"), "no-store");
      assert.ok(calls.some((call) => call.path === "/api/v1/admin/auth/login"));
    });
    await t.test("published article renders server HTML, metadata and honest Article JSON-LD", async () => {
      const html = await get("/kien-thuc/bai-viet-test");
      assert.match(html, /<h1[^>]*>Bài viết API thử nghiệm<\/h1>/);
      assert.match(html, /Nội dung bài viết từ API/);
      assert.match(html, /<title>Bài viết SEO · Test catalog<\/title>/);
      assert.match(html, /article-og.webp/);
      const jsonLd = [...html.matchAll(/<script type="application\/ld\+json">(.*?)<\/script>/gs)].map((match) => JSON.parse(match[1]));
      const item = jsonLd.find((value) => value["@type"] === "Article");
      assert.equal(item.headline, article.title);
      assert.equal(item.datePublished, article.publishedAt);
      assert.equal(item.author, undefined);
      const list = await get("/kien-thuc");
      assert.match(list, /Bài viết API thử nghiệm/);
    });
    await t.test("category pagination and real search query have correct canonical and robots", async () => {
      const html = await get("/danh-muc/am-tra?page=2");
      assert.match(html, /<title>Danh mục thử nghiệm — Trang 2 · Test catalog<\/title>/);
      assert.match(html, /rel="canonical" href="https:\/\/catalog.test\/danh-muc\/am-tra\?page=2"/);
      await get("/danh-muc/am-tra?page=3", 404);
      await get("/danh-muc/am-tra?page=-1", 404);
      const search = await get("/tim-kiem?q=am");
      assert.match(search, /name="robots" content="noindex, follow"/);
      assert.ok(calls.some((call) => call.path.includes("search=am")));
    });
    await t.test("sitemap URLs resolve to canonical indexable 200 pages and omit placeholders", async () => {
      const xml = await get("/sitemap.xml");
      assert.doesNotMatch(xml, /\/admin|\/preview|\/tim-kiem|\/gioi-thieu|draft|archived|noindex/);
      const urls = [...xml.matchAll(/<loc>(.*?)<\/loc>/g)].map((match) => match[1]);
      assert.equal(urls.length, 6);
      for (const url of urls) {
        const html = await get(new URL(url).pathname);
        assert.ok(html.includes(`rel="canonical" href="${url}"`), `${url}: ${html.match(/<link rel="canonical"[^>]*>/)?.[0]}`);
        assert.match(html, /name="robots" content="index, follow"/);
      }
      const about = await get("/gioi-thieu");
      assert.match(about, /name="robots" content="noindex, follow"/);
      const robots = await get("/robots.txt");
      assert.match(robots, /Disallow: \/admin/);
      assert.doesNotMatch(robots, /Disallow: \/tim-kiem/);
    });
    await t.test("fresh requests remove an archived product without cache invalidation", async () => {
      hidden = true;
      await get("/san-pham/am-public", 404);
      assert.doesNotMatch(await get("/sitemap.xml"), /san-pham\/am-public/);
      hidden = false;
      hiddenArticle = true;
      await get("/kien-thuc/bai-viet-test", 404);
      assert.doesNotMatch(await get("/sitemap.xml"), /kien-thuc\/bai-viet-test/);
      hiddenArticle = false;
    });
    await t.test("empty catalog renders honestly and API failures do not fabricate a successful sitemap", async () => {
      empty = true;
      const html = await get("/");
      assert.match(html, /<h1/);
      assert.doesNotMatch(html, /Ấm API thử nghiệm/);
      empty = false;
      failApi = true;
      await get("/sitemap.xml", 500);
      failApi = false;
    });
    await t.test("public settings update server-rendered name, contact and canonical origin", async () => {
      settingsConfigured = true;
      const html = await get("/");
      assert.match(html, /Configured catalog/);
      assert.match(html, /Configured tagline/);
      assert.match(html, /href="tel:0912345678"/);
      assert.equal(html.match(/<link rel="canonical" href="([^"]+)"/)?.[1], "https://configured.catalog.test");
      assert.match(await get("/robots.txt"), /configured\.catalog\.test\/sitemap\.xml/);
      settingsConfigured = false;
    });
    if (process.env.STOREFRONT_CAPTURE_DIR) {
      const { chromium } = await import("@playwright/test");
      const { mkdir } = await import("node:fs/promises");
      const { resolve, join } = await import("node:path");
      const directory = resolve(process.env.STOREFRONT_CAPTURE_DIR);
      await mkdir(directory, { recursive: true });
      visualWithoutImages = true;
      const browser = await chromium.launch({ channel: "chrome", headless: true });
      try {
        for (const [label, width, height] of [["desktop", 1440, 900], ["tablet", 768, 1024], ["mobile", 390, 844]]) {
          const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
          for (const [name, path] of [["home", "/"], ["search", "/tim-kiem?q=am"], ["contact", "/lien-he"], ["category", "/danh-muc/am-tra"], ["product", "/san-pham/am-public"]]) {
            const response = await page.goto(origin + path, { waitUntil: "networkidle" });
            assert.equal(response.status(), 200);
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, `${name} ${label} horizontal overflow`);
            await page.screenshot({ path: join(directory, `fixture-${name}-${label}.png`), fullPage: true });
            assert.equal(await page.locator("img").evaluateAll((images) => images.filter((image) => image.complete && image.naturalWidth === 0).length), 0, `${name} ${label} broken images`);
          }
          if (label === "mobile") {
            await page.locator(".sf-mobile-nav summary").click();
            assert.equal(await page.locator(".sf-mobile-panel").isVisible(), true);
            await page.screenshot({ path: join(directory, "fixture-mobile-nav-open.png"), fullPage: false });
            await page.goto(origin + "/lien-he", { waitUntil: "networkidle" });
            await page.locator("button[type=submit]").click();
            assert.equal(await page.locator("#lead-name-error").isVisible(), true);
            assert.equal(await page.locator("#lead-message-error").isVisible(), true);
            await page.screenshot({ path: join(directory, "fixture-contact-errors-mobile.png"), fullPage: true });
          }
          await page.close();
        }
      } finally { await browser.close(); visualWithoutImages = false; }
    }
    assert.ok(calls.length > 0);
    const adminProbes = calls.filter((call) => call.path === "/api/v1/admin/auth/login");
    assert.equal(adminProbes.length, 1, "Only the explicit BFF origin probe may call Admin API");
    assert.ok(calls.every((call) =>
      (call.path.startsWith("/api/v1/public/") || call.path === "/api/v1/admin/auth/login") &&
      !call.cookie && !call.authorization), "Fixture calls must not carry credentials");
  } finally {
    await stop(server);
    await stop(build);
    api.closeAllConnections();
    await new Promise((resolve) => api.close(resolve));
  }
});
