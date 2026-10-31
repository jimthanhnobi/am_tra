export type PublicSeo = Readonly<{
  seoTitle: string | null;
  metaDescription: string | null;
  canonicalOverride: string | null;
  robotsIndex: boolean;
  ogTitle: string | null;
  ogDescription: string | null;
  ogImageAssetId: string | null;
}>;

export type PublicImage = Readonly<{
  assetId: string;
  publicUrl: string | null;
  width: number;
  height: number;
  altText: string;
  sortOrder: number;
  isCover: boolean;
}>;

export type PublicCategorySummary = Readonly<{ slug: string; name: string }>;
export type PublicCategoryNode = PublicCategorySummary & Readonly<{ children: readonly PublicCategoryNode[] }>;
export type PublicBrand = Readonly<{ name: string; slug: string; origin: string | null }>;
export type PublicProductCard = Readonly<{
  slug: string;
  name: string;
  summary: string;
  coverImage: PublicImage | null;
  primaryCategory: PublicCategorySummary | null;
  brand: PublicBrand | null;
}>;
export type PublicPage<T> = Readonly<{ items: readonly T[]; page: number; pageSize: number; totalItems: number; totalPages: number }>;
export type PublicProduct = Readonly<{
  slug: string;
  name: string;
  sku: string | null;
  summary: string;
  descriptionHtml: string;
  productType: string;
  materialText: string | null;
  capacityMl: number | null;
  specifications: Readonly<Record<string, string | number | boolean>>;
  seo: PublicSeo;
  primaryCategory: PublicCategorySummary;
  categories: readonly PublicCategorySummary[];
  brand: PublicBrand | null;
  media: readonly PublicImage[];
}>;
export type PublicCategory = PublicCategorySummary & Readonly<{
  descriptionHtml: string;
  seo: PublicSeo;
  products: PublicPage<PublicProductCard>;
}>;
