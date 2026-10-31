import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import type { AdminCategoryNode } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
import { CategoriesManager } from "./categories-manager";
import { getSiteContext } from "@/lib/config/site-context";



export default async function CategoriesPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogRead)) return <AccessDenied permission={permissions.catalogRead} />;
  const [categories, { site }] = await Promise.all([getAdminApi<AdminCategoryNode[]>("categories"), getSiteContext()]);
  return <CategoriesManager initial={categories} canonicalOrigin={site.url} canWrite={hasPermission(user!.permissions, permissions.catalogWrite)} />;
}
