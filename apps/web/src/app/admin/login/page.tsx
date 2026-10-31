import type { Metadata } from "next";
import { LoginForm } from "./login-form";

export const metadata: Metadata = {
  title: "Đăng nhập quản trị",
};

export default function AdminLoginPage() {
  return (
    <main className="grid min-h-screen place-items-center bg-[#14251f] px-6 py-16 text-[#f7f4ec]">
      <section className="w-full max-w-md rounded-2xl border border-white/15 bg-white/[0.05] p-8 shadow-2xl shadow-black/25 sm:p-10">
        <div className="flex items-center gap-3">
          <span className="grid size-11 place-items-center rounded-full border border-[#d8ab91] font-serif text-sm text-[#d8ab91]">
            TP
          </span>
          <span className="text-sm uppercase tracking-[0.24em] text-[#cbd5cf]">TeaPremium Admin</span>
        </div>

        <h1 className="mt-10 font-serif text-4xl font-medium">Đăng nhập quản trị</h1>
        <p className="mt-3 text-sm leading-6 text-[#aebbb4]">
          Phiên đăng nhập được bảo vệ bằng cookie HttpOnly và không được lưu trong Web Storage.
        </p>

        <LoginForm />
      </section>
    </main>
  );
}
