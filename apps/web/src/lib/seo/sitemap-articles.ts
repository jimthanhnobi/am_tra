import type { PublicArticle } from "../api/articles";
import { isSitemapCandidate, type SeoSite } from "./policy.ts";

type Reader = {
  list: (page: number, pageSize: number) => Promise<{ items: readonly { slug: string }[]; totalItems: number; totalPages: number }>;
  detail: (slug: string) => Promise<PublicArticle | null>;
};

export async function articleSitemap(site: SeoSite, reader: Reader): Promise<{ url: string }[]> {
  if (!site.indexable) return [];
  const urls: { url: string }[] = [];
  let page = 1;
  let totalPages = 1;
  let hasPublishedArticles = false;
  do {
    const result = await reader.list(page, 48);
    if (result.totalItems > 49_000) throw new Error("Articles require split sitemaps.");
    hasPublishedArticles ||= result.totalItems > 0;
    totalPages = result.totalPages;
    for (const item of result.items) {
      const article = await reader.detail(item.slug);
      const path = `/kien-thuc/${item.slug}`;
      if (article && isSitemapCandidate(site, path, article.seo))
        urls.push({ url: new URL(path, site.url).href });
    }
    page++;
  } while (page <= totalPages);
  if (hasPublishedArticles) urls.unshift({ url: new URL("/kien-thuc", site.url).href });
  return urls;
}
