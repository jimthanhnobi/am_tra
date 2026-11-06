import Link from "next/link";
import Image from "next/image";
import type { ReactNode } from "react";
import { ArrowUpRight, Menu, Search, MessageCircle } from "lucide-react";
import { getSiteContext } from "@/lib/config/site-context";
import { AnalyticsConsent } from "@/components/storefront/analytics-consent";
import "./storefront.css";

export const dynamic = "force-dynamic";

const navigation = [
  { href: "/#bo-suu-tap", label: "Bộ sưu tập" },
  { href: "/tim-kiem", label: "Sản phẩm" },
  { href: "/kien-thuc", label: "Góc kiến thức" },
  { href: "/gioi-thieu", label: "Câu chuyện" },
];

export default async function StorefrontLayout({ children }: Readonly<{ children: ReactNode }>) {
  const { site, logoUrl } = await getSiteContext();
  const tagline = "tagline" in site ? site.tagline : null;

  return <div className="sf-site">
    <a href="#noi-dung" className="sr-only z-50 bg-white p-3 focus:not-sr-only focus:fixed focus:left-2 focus:top-2">Đến nội dung chính</a>
    <div className="sf-announcement border-b border-[#d9d2c3] bg-[#233b32] text-[#f7f4ec]">
      <div className="sf-container flex min-h-9 items-center justify-between gap-3 py-1 text-[.67rem] font-medium tracking-[.12em]">
        <span>{tagline || "TRÀ CỤ · CHUYỆN THƯỞNG TRÀ"}</span>
        <Link href="/lien-he" className="inline-flex items-center gap-2 hover:underline">KẾT NỐI TƯ VẤN <ArrowUpRight size={13} aria-hidden="true" /></Link>
      </div>
    </div>
    <header className="sf-header">
      <div className="sf-container flex min-h-20 items-center justify-between gap-4 py-3 lg:min-h-24">
        <Link href="/" className="min-w-0 shrink-0 text-[#233b32] no-underline" aria-label={`${site.name} — về trang chủ`}>
          {logoUrl
            ? <span className="relative block h-11 w-36 sm:w-48"><Image src={logoUrl} alt={site.name} fill sizes="192px" className="object-contain object-left" /></span>
            : <span className="block font-serif text-[1.6rem] leading-none tracking-[-.05em] sm:text-[1.9rem]">{site.name}</span>}
          {tagline
            ? <span className="mt-1.5 block max-w-48 truncate text-[.59rem] font-semibold uppercase tracking-[.17em] text-[#925b3d]">{tagline}</span>
            : <span className="mt-1.5 block text-[.58rem] font-semibold uppercase tracking-[.2em] text-[#925b3d]">TRÀ CỤ · CHUYỆN THƯỞNG TRÀ</span>}
        </Link>
        <nav aria-label="Điều hướng chính" className="hidden items-center gap-5 lg:flex xl:gap-8">
          {navigation.map((item) => <Link key={item.href} href={item.href} className="sf-nav-link">{item.label}</Link>)}
        </nav>
        <div className="hidden items-center gap-3 lg:flex">
          <Link href="/tim-kiem" className="grid size-11 place-items-center text-[#233b32] hover:text-[#925b3d]" aria-label="Tìm kiếm trà cụ"><Search size={20} strokeWidth={1.7} aria-hidden="true" /></Link>
          <Link href="/lien-he" className="sf-button min-h-11 px-5">Liên hệ tư vấn <ArrowUpRight size={16} aria-hidden="true" /></Link>
        </div>
        <details className="sf-mobile-nav relative lg:hidden">
          <summary className="flex min-h-11 items-center gap-2 border border-[#d9d2c3] px-3 text-sm font-semibold text-[#233b32]">
            <span className="sr-only">Mở hoặc đóng điều hướng</span><Menu size={19} aria-hidden="true" /><span className="sf-mobile-label">Danh mục</span>
          </summary>
          <nav aria-label="Điều hướng trên điện thoại" className="sf-mobile-panel absolute right-0 top-[calc(100%+.85rem)] z-30 hidden w-[min(19rem,calc(100vw-2rem))] gap-1 border border-[#d9d2c3] bg-[#fffcf6] p-4 shadow-xl">
            {navigation.map((item) => <Link key={item.href} href={item.href} className="sf-nav-link px-3">{item.label}</Link>)}
            <Link href="/lien-he" className="sf-button mt-2"><MessageCircle size={17} aria-hidden="true" /> Gửi yêu cầu tư vấn</Link>
          </nav>
        </details>
      </div>
    </header>

    {children}
    {process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID &&
      /^G-[A-Z0-9]+$/.test(process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID) &&
      <AnalyticsConsent measurementId={process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID} />}

    <footer className="border-t border-[#d9d2c3] bg-[#ebe5d9]">
      <div className="sf-container grid gap-10 py-16 md:grid-cols-[minmax(0,1.35fr)_repeat(2,minmax(0,1fr))] md:gap-14 lg:py-20">
        <div>
          <p className="sf-kicker">Một khoảng lặng giữa thường nhật</p>
          <Link href="/" className="mt-4 block font-serif text-3xl tracking-[-.04em] text-[#233b32]">{site.name}</Link>
          <p className="mt-5 max-w-sm text-sm leading-8 text-[#56635b]">{site.description}</p>
          <p className="mt-5 text-xs leading-6 text-[#56635b]">Thông tin sản phẩm dùng để tham khảo. Vui lòng liên hệ để được tư vấn cụ thể.</p>
        </div>
        <nav aria-label="Khám phá" className="flex flex-col items-start gap-4 text-sm">
          <p className="sf-kicker mb-1">Khám phá</p>
          <Link href="/#bo-suu-tap" className="hover:text-[#925b3d]">Bộ sưu tập</Link>
          <Link href="/tim-kiem" className="hover:text-[#925b3d]">Danh sách trà cụ</Link>
          <Link href="/kien-thuc" className="hover:text-[#925b3d]">Góc kiến thức</Link>
        </nav>
        <nav aria-label="Thông tin" className="flex flex-col items-start gap-4 text-sm">
          <p className="sf-kicker mb-1">Đồng hành</p>
          <Link href="/gioi-thieu" className="hover:text-[#925b3d]">Về thương hiệu</Link>
          <Link href="/lien-he" className="hover:text-[#925b3d]">Liên hệ tư vấn</Link>
          <Link href="/lien-he#tu-van" className="hover:text-[#925b3d]">Gửi lời nhắn</Link>
        </nav>
      </div>
      <div className="sf-container flex flex-wrap items-center justify-between gap-3 border-t border-[#cfc6b6] py-5 text-xs text-[#56635b]">
        <span>© {new Date().getUTCFullYear()} {site.name}</span>
        <span>Trà cụ · Kiến thức · Chăm sóc bằng sự thấu hiểu</span>
      </div>
    </footer>
  </div>;
}
