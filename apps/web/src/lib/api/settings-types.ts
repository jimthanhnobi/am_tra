export type SiteSettingsInput = {
  siteName: string | null;
  siteDescription: string | null;
  brandTagline: string | null;
  logoAssetId: string | null;
  faviconAssetId: string | null;
  hotline: string | null;
  zaloUrl: string | null;
  facebookUrl: string | null;
  email: string | null;
  address: string | null;
  openingHours: string | null;
  defaultSeoTitle: string | null;
  defaultMetaDescription: string | null;
  defaultOgImageAssetId: string | null;
  publicWebsiteUrl: string | null;
};

export type AdminSiteSettings = {
  settings: SiteSettingsInput;
  updatedAtUtc: string | null;
  updatedByUserId: string | null;
};
