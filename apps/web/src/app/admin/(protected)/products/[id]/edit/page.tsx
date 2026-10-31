import { notFound } from "next/navigation";
import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import type { AdminBrand, AdminCategoryNode, AdminProductDetail } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
import { ProductEditor } from "../../product-editor";
import { AdminApiError } from "@/lib/api/errors";
import { getSiteContext } from "@/lib/config/site-context";



export default async function EditProductPage({ params, searchParams }: Readonly<{ params: Promise<{ id: string }>; searchParams: Promise<{ returnTo?: string }> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogRead)) return <AccessDenied permission={permissions.catalogRead} />;
  const { id } = await params;
  if (!/^[0-9a-f-]{36}$/i.test(id)) notFound();
  let product: AdminProductDetail;
  try { product = await getAdminApi<AdminProductDetail>(`products/${id}`); }
  catch (error) { if (error instanceof AdminApiError && error.status === 404) notFound(); throw error; }
  const [categories, brands, { site }] = await Promise.all([
    getAdminApi<AdminCategoryNode[]>("categories"),
    getAdminApi<AdminBrand[]>("brands"),
    getSiteContext(),
  ]);
  const requestedReturnTo = (await searchParams).returnTo;
  const returnTo = requestedReturnTo?.startsWith("/admin/products?") ? requestedReturnTo : "/admin/products";
  return <ProductEditor initial={product} categories={categories} brands={brands} canonicalOrigin={site.url} returnTo={returnTo} canWrite={hasPermission(user!.permissions, permissions.catalogWrite)} canUpload={hasPermission(user!.permissions, permissions.mediaWrite)} canPublish={hasPermission(user!.permissions, permissions.catalogPublish)} />;
}
