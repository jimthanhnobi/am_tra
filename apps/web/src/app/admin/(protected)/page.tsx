import { getCurrentAdminUser } from "@/lib/auth/server";

export const instant = false;

export default async function AdminPage() {
  const user = await getCurrentAdminUser();

  return (
    <main className="mx-auto max-w-6xl px-6 py-12">
      <section className="rounded-2xl border border-white/15 bg-white/[0.04] p-8 shadow-xl shadow-black/10">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[#d8ab91]">Phiên đang hoạt động</p>
        <h1 className="mt-4 font-serif text-4xl font-medium">Khu vực quản trị</h1>
        <p className="mt-5 max-w-3xl leading-7 text-[#cbd5cf]">
          Bạn đang đăng nhập bằng tài khoản <strong className="text-white">{user?.email}</strong>.
          API xác thực và phân quyền lại mỗi yêu cầu ở phía máy chủ.
        </p>

        <dl className="mt-8 grid gap-5 sm:grid-cols-2">
          <div className="rounded-xl border border-white/10 bg-black/10 p-5">
            <dt className="text-sm text-[#9eaca5]">Vai trò</dt>
            <dd className="mt-2 text-lg">{user?.roles.join(", ") || "Chưa cấp"}</dd>
          </div>
          <div className="rounded-xl border border-white/10 bg-black/10 p-5">
            <dt className="text-sm text-[#9eaca5]">Số quyền hiệu lực</dt>
            <dd className="mt-2 text-lg">{user?.permissions.length ?? 0}</dd>
          </div>
        </dl>
      </section>
    </main>
  );
}
