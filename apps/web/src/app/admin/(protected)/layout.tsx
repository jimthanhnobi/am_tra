import type { ReactNode } from "react";
import { redirect } from "next/navigation";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { LogoutButton } from "./logout-button";

export const instant = false;

export default async function ProtectedAdminLayout({
  children,
}: Readonly<{ children: ReactNode }>) {
  const user = await getCurrentAdminUser();
  if (!user) {
    redirect("/admin/login");
  }

  return (
    <div className="min-h-screen bg-[#14251f] text-[#f3efe6]">
      <header className="border-b border-white/10 bg-black/10 px-6 py-4">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-6">
          <div>
            <p className="text-xs uppercase tracking-[0.24em] text-[#d8ab91]">TeaPremium Admin</p>
            <p className="mt-1 text-sm text-[#cbd5cf]">{user.email}</p>
          </div>
          <LogoutButton />
        </div>
      </header>
      {children}
    </div>
  );
}
