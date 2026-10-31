import Link from "next/link";
import Image from "next/image";
import type { ReactNode } from "react";
import { getSiteContext } from "@/lib/config/site-context";
import { AnalyticsConsent } from "@/components/storefront/analytics-consent";
import "./storefront.css";

export const dynamic = "force-dynamic";

const navigation = [
  { href: "/#bo-suu-tap", label: "Bộ sưu tập" },
  { href: "/kien-thuc", label: "Kiến thức" },
  { href: "/gioi-thieu", label: "Giới thiệu" },
  { href: "/lien-he", label: "Liên hệ" },
];

export default async function StorefrontLayout({ children }: Readonly<{ children: ReactNode }>) {
  const { site, logoUrl } = await getSiteContext();
  const tagline = "tagline" in site ? site.tagline : null;
  return <div className="sf-site">
    <a href="#noi-dung" className="sr-only z-50 bg-white p-3 focus:not-sr-only focus:fixed focus:left-2 focus:top-2">Đến nội dung chính</a>
    <header className="sf-header">
      <div className="sf-container flex min-h-20 items-center justify-between gap-5 py-3 lg:min-h-24">
        <Link href="/" aria-label={`${site.name} — trang chủ`} className="min-w-0 shrink-0 text-[#233b32] no-underline">
          {logoUrl ? <span className="relative block h-10 w-40 sm:w-48"><Image src={logoUrl} alt={site.name} fill sizes="192px" className="object-contain object-left" /></span>
            : <span className="block font-serif text-[1.55rem] leading-none tracking-[-0.04em] sm:text-[1.8rem]">{site.name}</span>}
          {tagline && <span className="mt-1.5 block text-[.58rem] font-semibold uppercase tracking-[.22em] text-[#925b3d] sm:text-[.65rem]">{tagline}</span>}
        </Link>
        <nav aria-label="Điều hướng chính" className="hidden items-center gap-6 lg:flex xl:gap-9">
          {navigation.map((item) => <Link key={item.href} href={item.href} className="sf-nav-link">{item.label}</Link>)}
        </nav>
        <div className="hidden items-center gap-3 lg:flex">
          <Link href="/tim-kiem" className="sf-nav-link px-2">Tìm kiếm</Link>
          <Link href="/lien-he" className="sf-button min-h-11 px-5">Tư vấn trà cụ <span aria-hidden="true">↗</span></Link>
        </div>
        <details className="sf-mobile-nav relative lg:hidden">
          <summary className="flex min-h-11 items-center gap-2 border border-[#d9d2c3] px-4 text-sm font-semibold text-[#233b32]">Menu <span aria-hidden="true">☰</span></summary>
          <nav aria-label="Điều hướng trên điện thoại" className="sf-mobile-panel absolute right-0 top-[calc(100%+1rem)] z-30 hidden w-[min(19rem,calc(100vw-2rem))] gap-1 border border-[#d9d2c3] bg-[#fffcf6] p-4 shadow-lg">
            {navigation.map((item) => <Link key={item.href} href={item.href} className="sf-nav-link px-3">{item.label}</Link>)}
            <Link href="/tim-kiem" className="sf-nav-link px-3">Tìm kiếm</Link>
            <Link href="/lien-he" className="sf-button mt-2">Liên hệ tư vấn <span aria-hidden="true">↗</span></Link>
          </nav>
        </details>
      </div>
    </header>
    {children}
    {process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID && /^G-[A-Z0-9]+$/.test(process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID) && <AnalyticsConsent measurementId={process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID} />}
    <footer className="border-t border-[#d9d2c3] bg-[#e9e2d5]">
      <div className="sf-container grid gap-12 py-16 md:grid-cols-[minmax(0,1.4fr)_repeat(2,minmax(0,1fr))] md:gap-16">
        <div><Link href="/" className="font-serif text-3xl text-[#233b32]">{site.name}</Link><p className="mt-5 max-w-sm text-sm leading-7 text-[#56635b]">{site.description}</p></div>
        <nav aria-label="Khám phá" className="flex flex-col items-start gap-4 text-sm"><p className="sf-kicker mb-1">Khám phá</p><Link href="/#bo-suu-tap">Bộ sưu tập</Link><Link href="/kien-thuc">Kiến thức trà cụ</Link><Link href="/tim-kiem">Tìm kiếm</Link></nav>
        <nav aria-label="Thông tin" className="flex flex-col items-start gap-4 text-sm"><p className="sf-kicker mb-1">Thông tin</p><Link href="/gioi-thieu">Giới thiệu</Link><Link href="/lien-he">Liên hệ tư vấn</Link></nav>
      </div>
      <div className="sf-container flex flex-wrap justify-between gap-3 border-t border-[#cfc6b6] py-5 text-xs text-[#56635b]"><span>© {new Date().getUTCFullYear()} {site.name}</span><span>Trà cụ · Kiến thức · Tư vấn</span></div>
    </footer>
  </div>;
}
