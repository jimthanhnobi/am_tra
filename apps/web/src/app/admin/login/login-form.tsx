"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import { getAntiforgeryToken } from "@/lib/auth/client";

export function LoginForm() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setPending(true);

    const form = event.currentTarget;
    const formData = new FormData(form);

    try {
      const csrf = await getAntiforgeryToken();
      const response = await fetch("/api/admin/auth/login", {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "content-type": "application/json",
          [csrf.headerName]: csrf.requestToken,
        },
        body: JSON.stringify({
          email: String(formData.get("email") ?? "").trim(),
          password: String(formData.get("password") ?? ""),
          rememberMe: formData.get("rememberMe") === "on",
        }),
      });

      if (!response.ok) {
        setError(
          response.status === 429
            ? "Bạn đã thử quá nhiều lần. Vui lòng chờ rồi thử lại."
            : "Không thể đăng nhập. Vui lòng kiểm tra thông tin và thử lại.",
        );
        return;
      }

      router.replace("/admin");
      router.refresh();
    } catch {
      setError("Không thể kết nối dịch vụ đăng nhập. Vui lòng thử lại.");
    } finally {
      setPending(false);
    }
  }

  return (
    <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
      <div>
        <label className="mb-2 block text-sm font-medium text-[#dce4df]" htmlFor="email">
          Email
        </label>
        <input
          autoComplete="username"
          className="w-full rounded-lg border border-white/20 bg-black/15 px-4 py-3 text-white outline-none transition focus:border-[#d8ab91] focus:ring-2 focus:ring-[#d8ab91]/25"
          id="email"
          maxLength={254}
          name="email"
          required
          type="email"
        />
      </div>

      <div>
        <label className="mb-2 block text-sm font-medium text-[#dce4df]" htmlFor="password">
          Mật khẩu
        </label>
        <input
          autoComplete="current-password"
          className="w-full rounded-lg border border-white/20 bg-black/15 px-4 py-3 text-white outline-none transition focus:border-[#d8ab91] focus:ring-2 focus:ring-[#d8ab91]/25"
          id="password"
          maxLength={256}
          name="password"
          required
          type="password"
        />
      </div>

      <label className="flex items-center gap-3 text-sm text-[#cbd5cf]">
        <input className="size-4 accent-[#d8ab91]" name="rememberMe" type="checkbox" />
        Duy trì đăng nhập trên thiết bị này
      </label>

      {error ? (
        <p aria-live="polite" className="rounded-lg border border-red-300/30 bg-red-950/30 px-4 py-3 text-sm text-red-100">
          {error}
        </p>
      ) : null}

      <button
        className="w-full rounded-full bg-[#d8ab91] px-6 py-3 font-semibold text-[#14251f] transition hover:bg-[#e7bfa8] disabled:cursor-not-allowed disabled:opacity-60"
        disabled={pending}
        type="submit"
      >
        {pending ? "Đang đăng nhập…" : "Đăng nhập"}
      </button>
    </form>
  );
}
