import type { MetadataRoute } from "next";
import { siteConfig } from "@/lib/config/site";

export const dynamic = "force-dynamic";
export default function robots(): MetadataRoute.Robots {
  // Keep /tim-kiem crawlable so crawlers can read its noindex metadata.
  return siteConfig.indexable ? {
    rules: { userAgent: "*", allow: "/", disallow: ["/admin", "/api/", "/preview"] },
    sitemap: new URL("/sitemap.xml", siteConfig.url).href,
  } : { rules: { userAgent: "*", disallow: "/" } };
}
