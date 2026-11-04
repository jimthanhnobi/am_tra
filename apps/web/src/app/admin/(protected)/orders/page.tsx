import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import { OrdersManager, type OrderPage, type ProductOption } from "./orders-manager";
import type { PagedResult, AdminProductListItem } from "@/lib/api/catalog-types";

export default async function OrdersPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.leadsRead))
    return <AccessDenied permission={permissions.leadsRead} />;
  const [orders, products] = await Promise.all([
    getAdminApi<OrderPage>("orders?page=1&pageSize=50"),
    getAdminApi<PagedResult<AdminProductListItem>>("products?page=1&pageSize=50&status=Published"),
  ]);
  const productOptions: ProductOption[] = products.items.map(p => ({
    id: p.id, name: p.name, sku: p.sku,
  }));
  return <OrdersManager orders={orders} products={productOptions}
    canManage={hasPermission(user!.permissions, permissions.leadsManage)} />;
}
