import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import { CustomerManager, type CustomerPage } from "./customer-manager";

export default async function CustomersPage({
  searchParams,
}: Readonly<{ searchParams: Promise<{ page?: string; search?: string }> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.leadsRead))
    return <AccessDenied permission={permissions.leadsRead} />;
  const params = await searchParams;
  const page = Math.max(1, Number.parseInt(params.page || "1", 10) || 1);
  const search = params.search || "";
  const data = await getAdminApi<CustomerPage>(
    `customers?page=${page}&pageSize=50&search=${encodeURIComponent(search)}`
  );
  return <CustomerManager data={data} search={search}
    canManage={hasPermission(user!.permissions, permissions.leadsManage)} />;
}
