import "server-only";
import { cache } from "react";
import { z } from "zod";
import { apiConfig } from "@/lib/api/config";

const url = z.string().url().nullable();
const settingsSchema = z.object({
  isConfigured: z.boolean(),
  siteName: z.string().nullable(), siteDescription: z.string().nullable(), brandTagline: z.string().nullable(),
  logoUrl: url, faviconUrl: url, hotline: z.string().nullable(), zaloUrl: url,
  facebookUrl: url, email: z.string().nullable(), address: z.string().nullable(),
  openingHours: z.string().nullable(), defaultSeoTitle: z.string().nullable(),
  defaultMetaDescription: z.string().nullable(), defaultOgImageUrl: url,
  publicWebsiteUrl: url,
});

export type PublicSiteSettings = z.infer<typeof settingsSchema>;

export const getPublicSettings = cache(async (): Promise<PublicSiteSettings | null> => {
  const response = await fetch(`${apiConfig.baseUrl}/public/settings`, {
    cache: "no-store", headers: { accept: "application/json" }, signal: AbortSignal.timeout(10_000),
  });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Không thể tải cấu hình website.");
  return settingsSchema.parse(await response.json());
});
