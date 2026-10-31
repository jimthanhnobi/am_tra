"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { getAntiforgeryToken } from "@/lib/auth/client";

export function LogoutButton() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function logout() {
    setPending(true);

    try {
      const csrf = await getAntiforgeryToken();
      const response = await fetch("/api/admin/auth/logout", {
        method: "POST",
        credentials: "same-origin",
        headers: { [csrf.headerName]: csrf.requestToken },
      });

      if (!response.ok) {
        throw new Error("Logout failed");
      }

      router.replace("/admin/login");
      router.refresh();
    } catch {
      toast.error("Không thể đăng xuất. Vui lòng thử lại.");
    } finally {
      setPending(false);
    }
  }

  return (
    <button
      className="flex min-h-10 w-full items-center rounded-lg px-3 text-left text-sm font-semibold text-[#233b32] transition-colors hover:bg-[#f4f1e9] disabled:opacity-60"
      disabled={pending}
      onClick={logout}
      type="button"
    >
      {pending ? "Đang thoát…" : "Đăng xuất"}
    </button>
  );
}
