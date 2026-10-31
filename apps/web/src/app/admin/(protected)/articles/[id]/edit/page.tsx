import { notFound } from "next/navigation";
import { getAdminApi } from "@/lib/api/server";
import { AdminApiError } from "@/lib/api/errors";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import type { AdminArticleDetail, ArticleCategory } from "@/lib/api/article-types";
import { ArticleForm } from "../../article-form";
import { getSiteContext } from "@/lib/config/site-context";

export default async function EditArticlePage({ params, searchParams }: Readonly<{ params: Promise<{ id: string }>; searchParams: Promise<{ returnTo?: string }> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.contentRead)) return <AccessDenied permission={permissions.contentRead} />;
  const { id } = await params;
  if (!/^[0-9a-f-]{36}$/i.test(id)) notFound();
  let article: AdminArticleDetail;
  try { article = await getAdminApi<AdminArticleDetail>(`articles/${id}`); }
  catch (error) { if (error instanceof AdminApiError && error.status === 404) notFound(); throw error; }
  const [categories, { site }] = await Promise.all([getAdminApi<ArticleCategory[]>("article-categories"), getSiteContext()]);
  const requestedReturnTo = (await searchParams).returnTo;
  const returnTo = /^\/admin\/articles\?page=\d+$/.test(requestedReturnTo ?? "") ? requestedReturnTo : "/admin/articles";
  return <ArticleForm initial={article} categories={categories} canonicalOrigin={site.url} returnTo={returnTo} canWrite={hasPermission(user!.permissions, permissions.contentWrite)} canPublish={hasPermission(user!.permissions, permissions.contentPublish)} />;
}
