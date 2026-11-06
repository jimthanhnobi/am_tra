import type { Metadata } from "next";
import { siteConfig } from "@/lib/config/site";

type PageMetadataInput = {
  title?: string;
  description?: string;
  path: `/${string}` | "/";
};

export function createPageMetadata({
  title,
  description = siteConfig.description,
  path,
}: PageMetadataInput): Metadata {
  return {
    title: title ?? siteConfig.name,
    description,
    alternates: {
      canonical: path,
    },
    openGraph: {
      type: "website",
      locale: "vi_VN",
      title: title ?? siteConfig.name,
      description,
      url: path,
      siteName: siteConfig.name,
    },
  };
}
