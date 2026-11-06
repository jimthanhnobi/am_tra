import type { MetadataRoute } from "next";
import { getSiteContext } from "@/lib/config/site-context";

export const dynamic = "force-dynamic";
export default async function robots(): Promise<MetadataRoute.Robots> {
  const { site } = await getSiteContext();
  // Keep /tim-kiem crawlable so crawlers can read its noindex metadata.
  return site.indexable ? {
    rules: { userAgent: "*", allow: "/", disallow: ["/admin", "/api/", "/preview"] },
    sitemap: new URL("/sitemap.xml", site.url).href,
  } : { rules: { userAgent: "*", disallow: "/" } };
}
