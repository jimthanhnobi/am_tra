import "server-only";
import { cache } from "react";
import { z } from "zod";
import { apiConfig } from "@/lib/api/config";

const slug = z.string().regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/).max(200);
const httpUrl = z.string().url().refine((value) => /^https?:\/\//.test(value));
const imageUrl = z.union([httpUrl, z.string().regex(/^\/demo-products\/[a-z0-9-]+\.svg$/)]);
const cover = { coverUrl: imageUrl.nullable(), coverWidth: z.number().int().positive().nullable(), coverHeight: z.number().int().positive().nullable() };
const summary = z.object({ slug, title: z.string(), excerpt: z.string(), publishedAt: z.string(), ...cover });
const seo = z.object({ seoTitle: z.string().nullable(), metaDescription: z.string().nullable(), canonicalOverride: httpUrl.nullable(), robotsIndex: z.boolean(), ogTitle: z.string().nullable(), ogDescription: z.string().nullable(), ogImageAssetId: z.string().nullable().default(null) });
const detail = summary.extend({ contentHtml: z.string(), updatedAtUtc: z.string(), seo, categories: z.array(z.object({ slug, name: z.string() })), ogImageUrl: imageUrl.nullable().default(null), ogImageWidth: z.number().int().positive().nullable().default(null), ogImageHeight: z.number().int().positive().nullable().default(null) });
const page = z.object({ items: z.array(summary), page: z.number().int().positive(), pageSize: z.number().int().positive(), totalItems: z.number().int().nonnegative(), totalPages: z.number().int().nonnegative() });

async function request(path: string) {
  const response = await fetch(`${apiConfig.baseUrl}/public/articles${path}`, { cache: "no-store", headers: { accept: "application/json" }, signal: AbortSignal.timeout(10_000) });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Dịch vụ kiến thức tạm thời không khả dụng.");
  return response.json() as Promise<unknown>;
}

export const getPublicArticles = cache(async (pageNumber = 1, pageSize = 12) => {
  const value = await request(`?page=${pageNumber}&pageSize=${pageSize}`);
  if (!value) throw new Error("Không tìm thấy API bài viết.");
  return page.parse(value);
});

export const getPublicArticle = cache(async (value: string) => {
  if (!slug.safeParse(value).success) return null;
  const result = await request(`/${encodeURIComponent(value)}`);
  if (!result) return null;
  return detail.parse(result);
});

export type PublicArticle = z.infer<typeof detail>;
export type PublicArticlePreview = z.infer<typeof summary>;
