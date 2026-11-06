import type { PublicCategory, PublicCategoryNode, PublicPage, PublicProduct, PublicProductCard } from "../api/public-types";
import { isSitemapCandidate, type SeoSite } from "./policy.ts";

type CatalogReader = {
  categories: () => Promise<readonly PublicCategoryNode[]>;
  products: (page: number, pageSize: number) => Promise<PublicPage<PublicProductCard>>;
  product: (slug: string) => Promise<PublicProduct | null>;
  category: (slug: string) => Promise<PublicCategory | null>;
};

export async function catalogSitemap(site: SeoSite, reader: CatalogReader): Promise<{ url: string }[]> {
  if (!site.indexable) return [];
  const urls = new Set<string>();
  const categorySlugs = new Set<string>();
  function visit(nodes: readonly PublicCategoryNode[]) {
    for (const node of nodes) { categorySlugs.add(node.slug); visit(node.children); }
  }
  visit(await reader.categories());
  async function add(path: string, item: PublicProduct | PublicCategory | null) {
    if (item && isSitemapCandidate(site, path, item.seo)) urls.add(new URL(path, site.url).href);
  }
  // Bound detail lookups to avoid overwhelming the API; list DTOs have no SEO flags.
  async function batches<T>(items: readonly T[], action: (item: T) => Promise<void>) {
    for (let offset = 0; offset < items.length; offset += 4) await Promise.all(items.slice(offset, offset + 4).map(action));
  }
  await batches([...categorySlugs], async (slug) => add(`/danh-muc/${slug}`, await reader.category(slug)));
  let page = 1;
  let totalPages = 1;
  do {
    const result = await reader.products(page, 48);
    if (result.page !== page) throw new Error("Public API returned an unexpected page.");
    totalPages = result.totalPages;
    if (result.totalItems + categorySlugs.size > 49_990) throw new Error("Catalog requires split sitemaps before publishing this size.");
    await batches(result.items, async (item) => add(`/san-pham/${item.slug}`, await reader.product(item.slug)));
    page++;
  } while (page <= totalPages);
  return [...urls].sort().map((url) => ({ url }));
}
