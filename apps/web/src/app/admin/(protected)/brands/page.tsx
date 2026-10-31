import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import type { AdminBrand } from "@/lib/api/catalog-types";
import { AccessDenied } from "@/components/admin/access-denied";
import { BrandsManager } from "./brands-manager";



export default async function BrandsPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogRead)) return <AccessDenied permission={permissions.catalogRead} />;
  return <BrandsManager initial={await getAdminApi<AdminBrand[]>("brands")} canWrite={hasPermission(user!.permissions, permissions.catalogWrite)} />;
}
