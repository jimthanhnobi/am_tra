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
      className="rounded-full border border-white/20 px-4 py-2 text-sm text-[#f3efe6] transition hover:border-[#d8ab91] hover:text-[#d8ab91] disabled:opacity-60"
      disabled={pending}
      onClick={logout}
      type="button"
    >
      {pending ? "Đang thoát…" : "Đăng xuất"}
    </button>
  );
}
