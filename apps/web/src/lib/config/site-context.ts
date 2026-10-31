import "server-only";
import { cache } from "react";
import { getPublicSettings } from "@/lib/api/settings";
import { siteConfig } from "@/lib/config/site";
import { siteProfile } from "@/lib/config/profile";

function indexableUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === "https:" && !/^(localhost|127\.0\.0\.1)$|\.example$|^example\.(com|org|net)$/.test(url.hostname);
  } catch { return false; }
}

export const getSiteContext = cache(async () => {
  const settings = await getPublicSettings();
  if (!settings?.isConfigured) return {
    site: siteConfig,
    profile: siteProfile,
    logoUrl: null as string | null,
    faviconUrl: null as string | null,
    defaultOgImageUrl: null as string | null,
    hasContact: Boolean(siteProfile.hotline || siteProfile.zalo || siteProfile.facebook),
  };
  const url = settings.publicWebsiteUrl || siteConfig.url;
  const hotline = settings.hotline;
  const profile = {
    about: siteProfile.about,
    address: settings.address,
    openingHours: settings.openingHours,
    email: settings.email,
    hotline,
    phoneHref: hotline ? `tel:${hotline.replace(/[^+\d]/g, "")}` : null,
    zalo: settings.zaloUrl,
    facebook: settings.facebookUrl,
  };
  return {
    site: {
      ...siteConfig,
      name: settings.siteName || siteConfig.name,
      description: settings.siteDescription || siteConfig.description,
      tagline: settings.brandTagline,
      url,
      defaultSeoTitle: settings.defaultSeoTitle,
      defaultMetaDescription: settings.defaultMetaDescription,
      defaultOgImageUrl: settings.defaultOgImageUrl,
      indexable: process.env.SITE_INDEXABLE === "true" && Boolean(settings.siteName) &&
        Boolean(settings.publicWebsiteUrl) && indexableUrl(url),
    },
    profile,
    logoUrl: settings.logoUrl,
    faviconUrl: settings.faviconUrl,
    defaultOgImageUrl: settings.defaultOgImageUrl,
    hasContact: Boolean(hotline || settings.zaloUrl || settings.facebookUrl || settings.email),
  };
});
