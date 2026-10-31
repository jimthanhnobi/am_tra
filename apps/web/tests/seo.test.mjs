import assert from "node:assert/strict";
import test from "node:test";
import { breadcrumbJsonLd, canonicalUrl, pageMetadata, parsePage, productJsonLd, serializeJsonLd } from "../src/lib/seo/policy.ts";
import { catalogSitemap } from "../src/lib/seo/sitemap-catalog.ts";
import { articleSitemap } from "../src/lib/seo/sitemap-articles.ts";

const site = { name: "Test catalog", url: "https://catalog.test", indexable: true };
const seo = { robotsIndex: true, seoTitle: null, metaDescription: null, canonicalOverride: null, ogTitle: null, ogDescription: null, ogImageAssetId: null };

test("entity metadata uses SEO overrides, canonical and social fields", () => {
  const result = pageMetadata(site, { path: "/san-pham/am", title: "Ấm", description: "Mô tả", seo: { ...seo, seoTitle: "Ấm trà", ogTitle: "Ảnh ấm", canonicalOverride: "https://catalog.test/san-pham/khac" } });
  assert.deepEqual(result.title, { absolute: "Ấm trà · Test catalog" });
  assert.equal(result.alternates.canonical, "https://catalog.test/san-pham/khac");
  assert.equal(result.openGraph.url, result.alternates.canonical);
  assert.equal(result.openGraph.title, "Ảnh ấm");
  assert.equal(result.description, "Mô tả");
  assert.equal(result.robots.index, true);
  assert.throws(() => canonicalUrl(site, "/", "javascript:alert(1)"));
});

test("deployment gate, search and entity flag independently prevent indexing", () => {
  const input = { path: "/tim-kiem", title: "Tìm kiếm", description: "Tìm" };
  assert.equal(pageMetadata({ ...site, indexable: false }, input).robots.index, false);
  assert.equal(pageMetadata(site, { ...input, index: false }).robots.index, false);
  assert.equal(pageMetadata(site, { ...input, seo: { ...seo, robotsIndex: false } }).robots.index, false);
});

test("Product JSON-LD only projects real public facts and escapes script injection", () => {
  const product = { slug: "am", name: "Ấm </script><script>alert(1)</script>", summary: "Mô tả", seo, sku: null, materialText: null, brand: null, media: [], price: 0, availability: "InStock", adminNotes: "PRIVATE" };
  const result = productJsonLd(site, product);
  assert.deepEqual(Object.keys(result).sort(), ["@context", "@type", "name", "description", "url"].sort());
  const encoded = serializeJsonLd(result);
  assert.equal(encoded.includes("<"), false);
  assert.deepEqual(JSON.parse(encoded), result);
  const crumbs = breadcrumbJsonLd(site, [{ name: "Trang chủ", path: "/" }, { name: "Ấm", path: "/san-pham/am" }]);
  assert.equal(crumbs.itemListElement[1].position, 2);
  assert.equal(crumbs.itemListElement[1].item, "https://catalog.test/san-pham/am");
});

test("pagination rejects ambiguous, negative and excessive page values", () => {
  assert.equal(parsePage(undefined), 1);
  assert.equal(parsePage("12"), 12);
  for (const value of ["0", "-1", "01", "1.5", "1e3", "10000000", ["1", "2"]]) assert.equal(parsePage(value), null);
});

test("sitemap traverses categories, all product pages and excludes missing/noindex/noncanonical entities", async () => {
  const pages = [];
  const reader = {
    categories: async () => [{ slug: "am", children: [{ slug: "child", children: [] }, { slug: "am", children: [] }] }],
    category: async (slug) => ({ slug, seo: { ...seo, robotsIndex: slug !== "child" } }),
    products: async (page, pageSize) => {
      pages.push([page, pageSize]);
      return { page, totalPages: 2, totalItems: 5, items: (page === 1 ? ["public", "draft", "noindex"] : ["canonical-elsewhere", "public"]).map((slug) => ({ slug })) };
    },
    product: async (slug) => slug === "draft" ? null : ({ seo: { ...seo, robotsIndex: slug !== "noindex", canonicalOverride: slug === "canonical-elsewhere" ? "https://another.test/product" : null } }),
  };
  assert.deepEqual(await catalogSitemap(site, reader), [{ url: "https://catalog.test/danh-muc/am" }, { url: "https://catalog.test/san-pham/public" }]);
  assert.deepEqual(pages, [[1, 48], [2, 48]]);
  assert.deepEqual(await catalogSitemap({ ...site, indexable: false }, {}), []);
  await assert.rejects(catalogSitemap(site, { ...reader, products: async () => { throw new Error("API unavailable"); } }), /API unavailable/);
  await assert.rejects(catalogSitemap(site, { ...reader, products: async () => ({ page: 2 }) }), /unexpected page/);
});

test("article sitemap only includes detail URLs that are public and canonical", async () => {
  const reader = {
    list: async () => ({ items: ["public", "draft", "noindex", "elsewhere"].map((slug) => ({ slug })), totalItems: 4, totalPages: 1 }),
    detail: async (slug) => slug === "draft" ? null : ({ seo: { ...seo, robotsIndex: slug !== "noindex", canonicalOverride: slug === "elsewhere" ? "https://another.test/article" : null } }),
  };
  assert.deepEqual(await articleSitemap(site, reader), [
    { url: "https://catalog.test/kien-thuc" },
    { url: "https://catalog.test/kien-thuc/public" },
  ]);
  assert.deepEqual(await articleSitemap(site, {
    list: async () => ({ items: [{ slug: "noindex" }], totalItems: 1, totalPages: 1 }),
    detail: async () => ({ seo: { ...seo, robotsIndex: false } }),
  }), [{ url: "https://catalog.test/kien-thuc" }]);
  assert.deepEqual(await articleSitemap({ ...site, indexable: false }, {}), []);
});
