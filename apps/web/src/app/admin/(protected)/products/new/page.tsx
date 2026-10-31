import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import type { AdminBrand, AdminCategoryNode } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
import { ProductEditor } from "../product-editor";
import { getSiteContext } from "@/lib/config/site-context";



export default async function NewProductPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogWrite)) return <AccessDenied permission={permissions.catalogWrite} />;
  const [categories, brands, { site }] = await Promise.all([
    getAdminApi<AdminCategoryNode[]>("categories"),
    getAdminApi<AdminBrand[]>("brands"),
    getSiteContext(),
  ]);
  return <ProductEditor categories={categories} brands={brands} canonicalOrigin={site.url} canWrite canUpload={hasPermission(user!.permissions, permissions.mediaWrite)} canPublish={hasPermission(user!.permissions, permissions.catalogPublish)} />;
}
