import type { Metadata } from "next";
import type { PublicProduct, PublicSeo } from "../api/public-types";

export type SeoSite = Readonly<{ name: string; url: string; indexable: boolean }>;
export type Crumb = Readonly<{ name: string; path: string }>;

export function plainText(html: string): string {
  return html.replace(/<[^>]*>/g, " ").replace(/&nbsp;/g, " ").replace(/&amp;/g, "&").replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/\s+/g, " ").trim();
}

export function canonicalUrl(site: SeoSite, path: string, override?: string | null): string {
  const url = new URL(override || path, site.url);
  if (!["https:", "http:"].includes(url.protocol)) throw new Error("Canonical URL không hợp lệ.");
  url.hash = "";
  return url.pathname === "/" && !url.search ? url.origin : url.href;
}

export function pageMetadata(site: SeoSite, input: Readonly<{
  path: string; title: string; description: string; seo?: PublicSeo;
  index?: boolean; image?: { url: string; width: number; height: number; alt: string };
}>): Metadata {
  const title = input.seo?.seoTitle || input.title;
  const description = input.seo?.metaDescription || input.description;
  const canonical = canonicalUrl(site, input.path, input.seo?.canonicalOverride);
  const images = input.image ? [{ ...input.image }] : [];
  return {
    title: { absolute: `${title} · ${site.name}` }, description,
    alternates: { canonical },
    robots: { index: site.indexable && input.index !== false && input.seo?.robotsIndex !== false, follow: true },
    openGraph: { type: "website", locale: "vi_VN", siteName: site.name, title: input.seo?.ogTitle || title, description: input.seo?.ogDescription || description, url: canonical, images },
    twitter: { card: images.length ? "summary_large_image" : "summary", title: input.seo?.ogTitle || title, description: input.seo?.ogDescription || description, images: images.map((image) => image.url) },
  };
}

export function isSitemapCandidate(site: SeoSite, path: string, seo: PublicSeo): boolean {
  return site.indexable && seo.robotsIndex && canonicalUrl(site, path, seo.canonicalOverride) === new URL(path, site.url).href;
}

export function breadcrumbJsonLd(site: SeoSite, crumbs: readonly Crumb[]) {
  return { "@context": "https://schema.org", "@type": "BreadcrumbList", itemListElement: crumbs.map((crumb, index) => ({ "@type": "ListItem", position: index + 1, name: crumb.name, item: new URL(crumb.path, site.url).href })) };
}

export function productJsonLd(site: SeoSite, product: PublicProduct) {
  return {
    "@context": "https://schema.org", "@type": "Product", name: product.name,
    description: product.summary, url: canonicalUrl(site, `/san-pham/${product.slug}`, product.seo.canonicalOverride),
    ...(product.sku ? { sku: product.sku } : {}),
    ...(product.materialText ? { material: product.materialText } : {}),
    ...(product.brand ? { brand: { "@type": "Brand", name: product.brand.name } } : {}),
    ...(product.media.some((image) => image.publicUrl) ? { image: product.media.flatMap((image) => image.publicUrl ? [image.publicUrl] : []) } : {}),
  };
}

// Script content must not allow CMS text to close its JSON-LD script element.
export function serializeJsonLd(value: unknown): string {
  return JSON.stringify(value).replace(/</g, "\\u003c").replace(/>/g, "\\u003e").replace(/&/g, "\\u0026").replace(/\u2028/g, "\\u2028").replace(/\u2029/g, "\\u2029");
}

export function parsePage(value: string | string[] | undefined): number | null {
  if (value === undefined) return 1;
  if (typeof value !== "string" || !/^[1-9]\d{0,6}$/.test(value)) return null;
  return Number(value);
}
