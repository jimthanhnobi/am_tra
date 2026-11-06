import type { MetadataRoute } from "next";
import { getSiteContext } from "@/lib/config/site-context";
import { getPublicCategories, getPublicCategory, getPublicProducts, getPublicProduct } from "@/lib/api/public";
import { catalogSitemap } from "@/lib/seo/sitemap-catalog";
import { canonicalUrl } from "@/lib/seo/policy";
import { getPublicArticle, getPublicArticles } from "@/lib/api/articles";
import { articleSitemap } from "@/lib/seo/sitemap-articles";

export const dynamic = "force-dynamic";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const { site, profile, hasContact } = await getSiteContext();
  if (!site.indexable) return [];
  const catalog = await catalogSitemap(site, { categories: getPublicCategories, category: getPublicCategory, products: getPublicProducts, product: getPublicProduct });
  const articles = await articleSitemap(site, { list: getPublicArticles, detail: getPublicArticle });
  const paths = ["/", ...(profile.about ? ["/gioi-thieu"] : []), ...(hasContact ? ["/lien-he"] : [])];
  return [...paths.map((path) => ({ url: canonicalUrl(site, path) })), ...catalog, ...articles];
}
