import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";
import { LeadList } from "./lead-list";

export type AdminLead = { id: string; name: string; phone: string | null; email: string | null;
  message: string; productId: string | null; sourcePage: string; channel: string;
  status: "New" | "Contacted" | "Closed"; utmJson: string; consentAt: string; createdAtUtc: string };
export type LeadPage = { items: AdminLead[]; page: number; pageSize: number; totalItems: number; totalPages: number };

export default async function LeadsPage({ searchParams }: Readonly<{ searchParams: Promise<{ page?: string }> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.leadsRead)) return <AccessDenied permission={permissions.leadsRead} />;
  const requested = Number((await searchParams).page ?? 1);
  const page = Number.isInteger(requested) && requested > 0 ? requested : 1;
  const data = await getAdminApi<LeadPage>(`leads?page=${page}&pageSize=20`);
  return <LeadList data={data} canManage={hasPermission(user!.permissions, permissions.leadsManage)} />;
}
