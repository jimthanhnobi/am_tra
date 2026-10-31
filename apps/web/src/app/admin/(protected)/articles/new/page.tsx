import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import type { ArticleCategory } from "@/lib/api/article-types";
import { ArticleForm } from "../article-form";
import { getSiteContext } from "@/lib/config/site-context";

export default async function NewArticlePage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.contentWrite)) return <AccessDenied permission={permissions.contentWrite} />;
  const [categories, { site }] = await Promise.all([getAdminApi<ArticleCategory[]>("article-categories"), getSiteContext()]);
  return <ArticleForm categories={categories} canonicalOrigin={site.url} canWrite canPublish={hasPermission(user!.permissions, permissions.contentPublish)} />;
}
