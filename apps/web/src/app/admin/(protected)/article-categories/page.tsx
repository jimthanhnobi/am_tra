import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import type { ArticleCategory } from "@/lib/api/article-types";
import { ArticleCategoriesManager } from "./article-categories-manager";

export default async function ArticleCategoriesPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.contentRead)) return <AccessDenied permission={permissions.contentRead} />;
  const categories = await getAdminApi<ArticleCategory[]>("article-categories");
  return <ArticleCategoriesManager initial={categories} canWrite={hasPermission(user!.permissions, permissions.contentWrite)} />;
}
