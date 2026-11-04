import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import { CustomerImport } from "./customer-import";

export default async function CustomerImportPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.leadsManage))
    return <AccessDenied permission={permissions.leadsManage} />;
  return <CustomerImport />;
}
