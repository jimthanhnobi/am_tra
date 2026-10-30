export type PagedResult<T> = Readonly<{
  items: readonly T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}>;

export type SeoDto = Readonly<{
  seoTitle: string | null;
  metaDescription: string | null;
  canonicalOverride: string | null;
  robotsIndex: boolean;
  ogTitle: string | null;
  ogDescription: string | null;
  ogImageAssetId: string | null;
}>;

export type AdminProductThumbnail = Readonly<{
  assetId: string;
  publicUrl: string | null;
  width: number;
  height: number;
  altText: string;
}>;

export type AdminProductListItem = Readonly<{
  id: string;
  sku: string | null;
  slug: string;
  name: string;
  summary: string;
  coverImage: AdminProductThumbnail | null;
  status: PublicationStatus;
  primaryCategoryId: string | null;
  primaryCategoryName: string | null;
  brandId: string | null;
  brandName: string | null;
  publishedAt: string | null;
  updatedAtUtc: string;
}>;

export type AdminCategorySummary = Readonly<{
  id: string;
  slug: string;
  name: string;
  status: PublicationStatus;
}>;

export type AdminProductMedia = Readonly<{
  id: string;
  assetId: string;
  storageKey: string;
  publicUrl: string | null;
  mimeType: string;
  width: number;
  height: number;
  altText: string;
  sortOrder: number;
  isCover: boolean;
}>;

export type AdminProductDetail = Readonly<{
  id: string;
  sku: string | null;
  slug: string;
  name: string;
  summary: string;
  description: Record<string, unknown>;
  descriptionHtml: string;
  status: PublicationStatus;
  primaryCategoryId: string | null;
  brandId: string | null;
  productType: string;
  materialText: string | null;
  capacityMl: number | null;
  specifications: Record<string, unknown>;
  publishedAt: string | null;
  seo: SeoDto;
  categories: readonly AdminCategorySummary[];
  media: readonly AdminProductMedia[];
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type AdminCategoryNode = Readonly<{
  id: string;
  parentId: string | null;
  slug: string;
  name: string;
  sortOrder: number;
  status: PublicationStatus;
  children: readonly AdminCategoryNode[];
}>;

export type AdminCategoryDetail = Readonly<{
  id: string;
  parentId: string | null;
  slug: string;
  name: string;
  descriptionHtml: string;
  sortOrder: number;
  status: PublicationStatus;
  seo: SeoDto;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type AdminBrand = Readonly<{
  id: string;
  name: string;
  slug: string;
  origin: string | null;
}>;

export type AdminMediaAsset = Readonly<{
  id: string;
  fileName: string;
  storageKey: string;
  mimeType: string;
  byteSize: number;
  width: number;
  height: number;
  checksum: string | null;
  publicUrl: string | null;
  createdAtUtc: string;
}>;

export type MediaUploadIntent = Readonly<{
  uploadId: string;
  presignedUrl: string;
  objectKey: string;
  expiresAt: string;
  requiredHeaders: Readonly<Record<string, string>>;
}>;

export type AdminCatalogSummary = Readonly<{
  total: number;
  draft: number;
  inReview: number;
  published: number;
  archived: number;
  categories: number;
  brands: number;
  mediaAssets: number;
}>;

export type PublicationStatus = "Draft" | "InReview" | "Published" | "Archived";

export type ProductUpsertPayload = Readonly<{
  sku: string | null;
  slug: string;
  name: string;
  summary: string;
  description: Record<string, unknown>;
  descriptionHtml: string;
  productType: string;
  materialText: string | null;
  capacityMl: number | null;
  specifications: Record<string, string | number | boolean>;
  primaryCategoryId: string | null;
  brandId: string | null;
  categoryIds: readonly string[];
  seo: SeoDto;
  status?: PublicationStatus;
}>;

export type CategoryUpsertPayload = Readonly<{
  parentId: string | null;
  slug: string;
  name: string;
  descriptionHtml: string;
  sortOrder: number;
  seo: SeoDto;
  status?: PublicationStatus;
}>;

export type BrandUpsertPayload = Readonly<{
  name: string;
  slug: string;
  origin: string | null;
}>;
