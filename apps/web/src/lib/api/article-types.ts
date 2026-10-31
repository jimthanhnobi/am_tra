export type ArticleStatus = "Draft" | "InReview" | "Published" | "Archived";
export type AdminArticleSummary = {
  id: string; slug: string; title: string; excerpt: string; status: ArticleStatus;
  publishedAt: string | null; updatedAtUtc: string; coverUrl: string | null;
};
export type ArticleCategory = { id: string; slug: string; name: string };
export type AdminArticleDetail = AdminArticleSummary & {
  contentHtml: string; contentJson: string; coverAssetId: string | null;
  categories: ArticleCategory[];
  seo: { seoTitle: string | null; metaDescription: string | null; canonicalOverride: string | null;
    robotsIndex: boolean; ogTitle: string | null; ogDescription: string | null; ogImageAssetId: string | null };
};
export type ArticlePage = { items: AdminArticleSummary[]; page: number; pageSize: number; totalItems: number; totalPages: number };
