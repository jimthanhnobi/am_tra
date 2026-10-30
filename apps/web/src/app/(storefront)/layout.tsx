import Link from "next/link";
import type { ReactNode } from "react";
import { siteConfig } from "@/lib/config/site";

export default function StorefrontLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <div className="min-h-screen overflow-hidden">
      <header className="border-b border-[#d8d1c3] bg-[#f3efe6]/95">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-5 lg:px-10">
          <Link className="flex items-center gap-3" href="/" aria-label={`${siteConfig.name} — trang chủ`}>
            <span className="grid size-10 place-items-center rounded-full border border-[#183c31] font-serif text-sm text-[#183c31]">
              TP
            </span>
            <span>
              <strong className="block font-serif text-lg font-medium tracking-[0.08em]">{siteConfig.name}</strong>
              <span className="block text-[0.65rem] uppercase tracking-[0.22em] text-[#66736c]">Trà cụ &amp; tri thức</span>
            </span>
          </Link>
          <nav aria-label="Điều hướng chính" className="hidden items-center gap-8 text-sm text-[#435149] sm:flex">
            <a href="#dinh-huong">Định hướng</a>
            <a href="#foundation">Foundation</a>
          </nav>
        </div>
      </header>

      {children}

      <footer className="border-t border-[#d8d1c3] bg-[#ece6da]">
        <div className="mx-auto flex max-w-7xl flex-col gap-3 px-6 py-8 text-sm text-[#66736c] sm:flex-row sm:items-center sm:justify-between lg:px-10">
          <p>{siteConfig.name} · Foundation cho catalog ưu tiên SEO.</p>
          <p>Nội dung thực tế chỉ hiển thị sau khi được kiểm duyệt.</p>
        </div>
      </footer>
    </div>
  );
}

