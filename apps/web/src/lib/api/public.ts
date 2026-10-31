import "server-only";
import { cache } from "react";
import { z } from "zod";
import { apiConfig } from "@/lib/api/config";
import type { PublicCategoryNode } from "@/lib/api/public-types";

// Zod strips unlisted fields: never pass an upstream object wholesale to the UI.
const slug = z.string().regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/).max(200);
const httpUrl = z.string().url().refine((value) => /^https?:\/\//.test(value));
const image = z.object({ assetId: z.string(), publicUrl: httpUrl.nullable(), width: z.number().int().positive(), height: z.number().int().positive(), altText: z.string(), sortOrder: z.number().int(), isCover: z.boolean() });
const categorySummary = z.object({ slug, name: z.string() });
const brand = z.object({ name: z.string(), slug, origin: z.string().nullable() });
const seo = z.object({ seoTitle: z.string().nullable(), metaDescription: z.string().nullable(), canonicalOverride: httpUrl.nullable(), robotsIndex: z.boolean(), ogTitle: z.string().nullable(), ogDescription: z.string().nullable(), ogImageAssetId: z.string().nullable() });
const productCard = z.object({ slug, name: z.string(), summary: z.string(), coverImage: image.nullable(), primaryCategory: categorySummary.nullable(), brand: brand.nullable() });
const productsPage = z.object({ items: z.array(productCard), page: z.number().int().positive(), pageSize: z.number().int().min(1).max(48), totalItems: z.number().int().nonnegative(), totalPages: z.number().int().nonnegative() });
const categoryNode: z.ZodType<PublicCategoryNode> = z.lazy(() => categorySummary.extend({ children: z.array(categoryNode) }));
const product = z.object({
  slug, name: z.string(), sku: z.string().nullable(), summary: z.string(), descriptionHtml: z.string(),
  productType: z.string(), materialText: z.string().nullable(), capacityMl: z.number().int().positive().nullable(),
  specifications: z.record(z.string(), z.unknown()).transform((values) => Object.fromEntries(Object.entries(values).filter(([key, value]) => !/price|cost|inventory|stock|cart|checkout|payment|order|admin|token|secret|password|createdby|updatedby/i.test(key) && ["string", "number", "boolean"].includes(typeof value))) as Record<string, string | number | boolean>),
  seo, primaryCategory: categorySummary, categories: z.array(categorySummary), brand: brand.nullable(), media: z.array(image),
});
const category = categorySummary.extend({ descriptionHtml: z.string(), seo, products: productsPage });

async function request<T>(path: string, schema: z.ZodType<T>): Promise<T | null> {
  const response = await fetch(`${apiConfig.baseUrl}/public/${path}`, {
    cache: "no-store", redirect: "error", signal: AbortSignal.timeout(10_000),
    headers: { accept: "application/json" },
  });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Dịch vụ catalog tạm thời không khả dụng.");
  return schema.parse(await response.json());
}

export const getPublicProduct = cache(async (value: string) => {
  if (!slug.safeParse(value).success) return null;
  const result = await request(`products/${encodeURIComponent(value)}`, product);
  return result;
});
export const getPublicCategory = cache(async (value: string, page = 1) => {
  if (!slug.safeParse(value).success) return null;
  const result = await request(`categories/${encodeURIComponent(value)}?page=${page}&pageSize=12`, category);
  return result;
});
export const getPublicCategories = cache(async () => {
  const result = await request("categories", z.array(categoryNode));
  if (!result) throw new Error("Không tìm thấy API danh mục.");
  return result;
});
export const getPublicProducts = cache(async (page = 1, pageSize = 12, search = "") => {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize), sort: "latest" });
  if (search) query.set("search", search);
  const result = await request(`products?${query}`, productsPage);
  if (!result) throw new Error("Không tìm thấy API sản phẩm.");
  return result;
});
