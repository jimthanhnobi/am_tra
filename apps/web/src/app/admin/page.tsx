import Link from "next/link";

export default function AdminPage() {
  return (
    <main className="grid min-h-screen place-items-center bg-[#14251f] px-6 py-16 text-[#f3efe6]">
      <section className="w-full max-w-3xl border border-white/15 bg-white/[0.04] p-8 shadow-2xl shadow-black/20 sm:p-12">
        <div className="flex items-center justify-between gap-4 border-b border-white/15 pb-8">
          <div className="flex items-center gap-3">
            <span className="grid size-10 place-items-center rounded-full border border-[#d8ab91] font-serif text-sm text-[#d8ab91]">TP</span>
            <span className="text-sm uppercase tracking-[0.24em] text-[#cbd5cf]">Admin</span>
          </div>
          <span className="rounded-full border border-white/15 px-4 py-2 text-xs uppercase tracking-[0.18em] text-[#d8ab91]">Foundation</span>
        </div>

        <p className="mt-12 text-xs font-semibold uppercase tracking-[0.28em] text-[#d8ab91]">Chưa kích hoạt</p>
        <h1 className="mt-5 max-w-2xl font-serif text-4xl leading-tight font-medium sm:text-5xl">Khu vực quản trị đang được chuẩn bị.</h1>
        <p className="mt-6 max-w-2xl leading-8 text-[#cbd5cf]">
          Foundation chỉ xác lập route và trạng thái noindex. Chưa có đăng nhập, chưa gọi API cần quyền và chưa hiển thị dữ liệu quản trị.
        </p>

        <Link className="mt-10 inline-flex rounded-full border border-[#d8ab91] px-6 py-3 text-sm font-medium text-[#f3efe6] transition hover:bg-[#d8ab91] hover:text-[#14251f] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#d8ab91]" href="/">
          Về trang chủ
        </Link>
      </section>
    </main>
  );
}
