import type { MetadataRoute } from "next";
import { siteConfig } from "@/lib/config/site";
import { siteProfile, hasContact } from "@/lib/config/profile";
import { getPublicCategories, getPublicCategory, getPublicProducts, getPublicProduct } from "@/lib/api/public";
import { catalogSitemap } from "@/lib/seo/sitemap-catalog";
import { canonicalUrl } from "@/lib/seo/policy";
import { getPublicArticle, getPublicArticles } from "@/lib/api/articles";
import { articleSitemap } from "@/lib/seo/sitemap-articles";

export const dynamic = "force-dynamic";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  if (!siteConfig.indexable) return [];
  const catalog = await catalogSitemap(siteConfig, { categories: getPublicCategories, category: getPublicCategory, products: getPublicProducts, product: getPublicProduct });
  const articles = await articleSitemap(siteConfig, { list: getPublicArticles, detail: getPublicArticle });
  const paths = ["/", ...(siteProfile.about ? ["/gioi-thieu"] : []), ...(hasContact ? ["/lien-he"] : [])];
  return [...paths.map((path) => ({ url: canonicalUrl(siteConfig, path) })), ...catalog, ...articles];
}
