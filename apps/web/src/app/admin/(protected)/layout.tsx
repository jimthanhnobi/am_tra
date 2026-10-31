import type { ReactNode } from "react";
import { redirect } from "next/navigation";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { AdminShell } from "@/components/admin/admin-shell";

export const dynamic = "force-dynamic";

export default async function ProtectedAdminLayout({ children }: Readonly<{ children: ReactNode }>) {
  const user = await getCurrentAdminUser();
  if (!user) redirect("/admin/login");
  return <AdminShell user={user}>{children}</AdminShell>;
}
