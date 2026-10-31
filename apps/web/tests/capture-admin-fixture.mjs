// Visual fixture only. All endpoints bind to loopback and reject writes.
// This does not test Identity, permissions, CSRF or the Development database.
import { spawn } from "node:child_process";
import { once } from "node:events";
import { createServer } from "node:http";
import { mkdir } from "node:fs/promises";
import { join, resolve } from "node:path";
import { setTimeout as delay } from "node:timers/promises";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";

const cwd = fileURLToPath(new URL("../", import.meta.url));
const next = fileURLToPath(new URL("../node_modules/next/dist/bin/next", import.meta.url));
const output = resolve(process.env.ADMIN_CAPTURE_DIR || "../../artifacts/prompt9b");
const id = "11111111-1111-4111-8111-111111111111";
const now = "2026-10-31T03:00:00Z";
const seo = { seoTitle: null, metaDescription: null, canonicalOverride: null, robotsIndex: true, ogTitle: null, ogDescription: null, ogImageAssetId: null };
const category = { id, parentId: null, slug: "danh-muc-kiem-thu", name: "Danh mục kiểm thử", sortOrder: 1, status: "Published", children: [] };
const product = { id, sku: "VISUAL-ONLY", slug: "san-pham-kiem-thu", name: "Sản phẩm kiểm thử giao diện", summary: "Nội dung mẫu chỉ có trong fixture chụp ảnh.", coverImage: null, status: "Published", primaryCategoryId: id, primaryCategoryName: category.name, brandId: null, brandName: null, publishedAt: now, updatedAtUtc: now };
const productDetail = { ...product, description: { type: "doc", content: [] }, descriptionHtml: "", productType: "Trà cụ kiểm thử", materialText: null, capacityMl: null, specifications: {}, seo, categories: [category], media: [], createdAtUtc: now };
const article = { id, slug: "bai-viet-kiem-thu", title: "Bài viết kiểm thử giao diện", excerpt: "Nội dung mẫu chỉ có trong fixture chụp ảnh.", status: "InReview", publishedAt: null, updatedAtUtc: now, coverUrl: null };
const articleDetail = { ...article, contentJson: JSON.stringify({ type: "doc", content: [{ type: "paragraph", content: [{ type: "text", text: "Nội dung mẫu cho trình biên tập." }] }] }), contentHtml: "<p>Nội dung mẫu cho trình biên tập.</p>", coverAssetId: null, categories: [], seo };
const page = (items, pageSize = 20) => ({ items, page: 1, pageSize, totalItems: items.length, totalPages: 1 });
const settings = { settings: { siteName: null, siteDescription: null, brandTagline: null, logoAssetId: null, faviconAssetId: null, hotline: null, zaloUrl: null, facebookUrl: null, email: null, address: null, openingHours: null, defaultSeoTitle: null, defaultMetaDescription: null, defaultOgImageAssetId: null, publicWebsiteUrl: null }, updatedAtUtc: null, updatedByUserId: null };
const lead = { id, name: "Khách kiểm thử", phone: null, email: "example@example.test", message: "Yêu cầu kiểm thử giao diện quản lý lead.", productId: null, sourcePage: "/lien-he", channel: "web_form", status: "New", utmJson: "{}", consentAt: now, createdAtUtc: now };
const user = { id, email: "admin@example.test", roles: ["Admin"], permissions: ["Catalog.Read", "Catalog.Write", "Catalog.Publish", "Media.Write", "Content.Read", "Content.Write", "Content.Publish", "Leads.Read", "Leads.Manage", "Settings.Manage"] };

function fixture(path) {
  if (path === "/api/v1/admin/auth/me") return user;
  if (path === "/api/v1/admin/catalog/summary") return { total: 3, draft: 1, inReview: 1, published: 1, archived: 0, categories: 1, brands: 0, mediaAssets: 0 };
  if (path === "/api/v1/admin/products") return page([product, { ...product, id: "22222222-2222-4222-8222-222222222222", slug: "ban-nhap-kiem-thu", name: "Bản nháp kiểm thử", status: "Draft" }, { ...product, id: "33333333-3333-4333-8333-333333333333", slug: "cho-duyet-kiem-thu", name: "Sản phẩm chờ duyệt", status: "InReview" }]);
  if (path === `/api/v1/admin/products/${id}`) return productDetail;
  if (path === "/api/v1/admin/categories") return [category];
  if (path === "/api/v1/admin/brands") return [];
  if (path === "/api/v1/admin/media") return page([]);
  if (path === "/api/v1/admin/articles") return page([article]);
  if (path === `/api/v1/admin/articles/${id}`) return articleDetail;
  if (path === "/api/v1/admin/article-categories") return [];
  if (path === "/api/v1/admin/leads") return page([lead]);
  if (path === "/api/v1/admin/settings") return settings;
  return null;
}

const api = createServer((request, response) => {
  if (request.method !== "GET") { response.writeHead(405).end(); return; }
  const path = new URL(request.url, "http://fixture.test").pathname;
  const body = fixture(path);
  response.writeHead(body ? 200 : 404, { "Content-Type": "application/json", "Cache-Control": "no-store" });
  response.end(JSON.stringify(body || { title: "Not found" }));
});

async function listen(server) { server.listen(0, "127.0.0.1"); await once(server, "listening"); return server.address().port; }
const apiPort = await listen(api);
const probe = createServer();
const webPort = await listen(probe);
await new Promise((done) => probe.close(done));
const origin = `http://127.0.0.1:${webPort}`;
const child = spawn(process.execPath, [next, "start", "--hostname", "127.0.0.1", "--port", String(webPort)], {
  cwd, windowsHide: true, stdio: ["ignore", "pipe", "pipe"],
  env: { ...process.env, NODE_ENV: "production", NEXT_TELEMETRY_DISABLED: "1", API_BASE_URL: `http://127.0.0.1:${apiPort}/api/v1`, NEXT_PUBLIC_SITE_NAME: "TeaPremium" },
});
let logs = "";
for (const stream of [child.stdout, child.stderr]) stream.on("data", (data) => { logs = (logs + data).slice(-4000); });
let browser;
try {
  let ready = false;
  for (let attempt = 0; attempt < 100; attempt++) {
    if (child.exitCode !== null) throw new Error(logs);
    try { if ((await fetch(`${origin}/admin/login`)).ok) { ready = true; break; } } catch { /* wait */ }
    await delay(100);
  }
  if (!ready) throw new Error(`Next server not ready: ${logs}`);
  await mkdir(output, { recursive: true });
  browser = await chromium.launch({ channel: "chrome", headless: true });
  const routes = [
    ["login", "/admin/login"], ["dashboard", "/admin"], ["products", "/admin/products"],
    ["product-new", "/admin/products/new"], ["product-edit", `/admin/products/${id}/edit`],
    ["categories", "/admin/categories"], ["brands", "/admin/brands"], ["media", "/admin/media"],
    ["articles", "/admin/articles"], ["article-new", "/admin/articles/new"], ["article-edit", `/admin/articles/${id}/edit`],
    ["article-categories", "/admin/article-categories"], ["leads", "/admin/leads"], ["settings", "/admin/settings"],
  ];
  for (const [label, width, height] of [["desktop", 1440, 900], ["mobile", 390, 844]]) {
    const tab = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
    for (const [name, path] of routes) {
      const result = await tab.goto(origin + path, { waitUntil: "networkidle" });
      if (result?.status() !== 200) throw new Error(`${name} ${label}: HTTP ${result?.status()} ${logs.slice(-500)}`);
      if (await tab.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error(`${name} ${label}: horizontal overflow`);
      await tab.screenshot({ path: join(output, `${name}-${label}.png`), fullPage: true });
      const broken = await tab.locator("img").evaluateAll((images) => images.filter((image) => image.complete && image.naturalWidth === 0).length);
      if (broken) throw new Error(`${name} ${label}: ${broken} broken images`);
      process.stdout.write(`${name} ${label}: 200, no overflow/broken images\n`);
    }
    if (label === "mobile") {
      await tab.goto(origin + "/admin");
      await tab.getByRole("button", { name: "Mở menu" }).click();
      if (!await tab.getByRole("navigation", { name: "Quản trị" }).isVisible()) throw new Error("mobile menu did not open");
      await tab.screenshot({ path: join(output, "navigation-open-mobile.png"), fullPage: false });
      await tab.keyboard.press("Escape");
      if (await tab.getByRole("navigation", { name: "Quản trị" }).isVisible()) throw new Error("mobile menu did not close with Escape");
      if (!await tab.getByRole("button", { name: "Mở menu" }).evaluate((button) => button === document.activeElement)) throw new Error("mobile menu focus was not restored");
      await tab.locator("header details > summary").click();
      if (!await tab.getByRole("button", { name: "Đăng xuất" }).isVisible()) throw new Error("logout action is missing from account menu");
      await tab.goto(origin + "/admin/products?status=Draft");
      await tab.getByRole("link", { name: "Sản phẩm kiểm thử giao diện" }).first().click();
      await tab.locator("main").getByRole("link", { name: "← Sản phẩm" }).click();
      await tab.waitForURL((url) => url.pathname === "/admin/products" && url.searchParams.get("status") === "Draft", { timeout: 5000 });
      await tab.goto(origin + "/admin/products/new");
      await tab.getByLabel("Tên sản phẩm").fill("Thay đổi chưa lưu");
      await tab.getByRole("navigation", { name: "Các phần của sản phẩm" }).getByRole("link", { name: "Danh mục" }).click();
      await tab.waitForURL((url) => url.hash === "#categories", { timeout: 5000 });
      await tab.getByRole("button", { name: "Lưu bản nháp" }).click();
      if (!await tab.locator('main [role="alert"]').isVisible()) throw new Error("product validation summary missing");
      await tab.screenshot({ path: join(output, "product-validation-mobile.png"), fullPage: true });
      await tab.goto(origin + "/admin/articles/new");
      if (await tab.locator("form form").count()) throw new Error("article editor has nested forms");
      await tab.getByRole("button", { name: "Lưu bản nháp" }).click();
      if (!await tab.locator('main [role="alert"]').isVisible()) throw new Error("article validation summary missing");
      await tab.screenshot({ path: join(output, "article-validation-mobile.png"), fullPage: true });
    }
    await tab.close();
  }
} finally {
  await browser?.close();
  const exited = child.exitCode === null ? once(child, "exit") : null;
  if (exited) { child.kill(); await exited; }
  await new Promise((done) => api.close(done));
}
