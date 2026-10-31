import { notFound } from "next/navigation";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { getAdminApi } from "@/lib/api/server";
import type { AdminSiteSettings } from "@/lib/api/settings-types";
import { hasPermission, permissions } from "@/lib/permissions";
import { SiteSettingsForm } from "./site-settings-form";

export const dynamic = "force-dynamic";

export default async function SettingsPage() {
  const user = await getCurrentAdminUser();
  if (!user || !hasPermission(user.permissions, permissions.settingsManage)) notFound();
  const initial = await getAdminApi<AdminSiteSettings>("settings");
  return <SiteSettingsForm initial={initial} />;
}
