import Link from "next/link";
import type { ReactNode } from "react";
import { siteConfig } from "@/lib/config/site";
import { contentWidth } from "@/components/storefront/catalog";
import { AnalyticsConsent } from "@/components/storefront/analytics-consent";

export const dynamic = "force-dynamic";

export default function StorefrontLayout({ children }: Readonly<{ children: ReactNode }>) {
  return <div className="min-h-screen"><a href="#noi-dung" className="sr-only z-50 bg-white p-3 focus:not-sr-only focus:fixed focus:left-2 focus:top-2">Đến nội dung chính</a>
    <header className="border-b border-[#d8d1c3] bg-[#f3efe6]"><div className={`${contentWidth} flex flex-wrap items-center justify-between gap-5 py-5`}>
      <Link href="/" aria-label={`${siteConfig.name} — trang chủ`} className="font-serif text-2xl tracking-wide text-[#183c31]">{siteConfig.name}<span className="mt-1 block font-sans text-[10px] uppercase tracking-[0.3em] text-[#8d4f34]">Trà cụ &amp; thưởng trà</span></Link>
      <nav aria-label="Điều hướng chính" className="flex flex-wrap gap-x-5 gap-y-3 text-sm text-[#435149]"><Link href="/#bo-suu-tap">Bộ sưu tập</Link><Link href="/kien-thuc">Kiến thức</Link><Link href="/gioi-thieu">Giới thiệu</Link><Link href="/lien-he">Liên hệ</Link><Link href="/tim-kiem">Tìm kiếm</Link></nav>
    </div></header>
    {children}
    {process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID && /^G-[A-Z0-9]+$/.test(process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID) && <AnalyticsConsent measurementId={process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID} />}
    <footer className="mt-16 border-t border-[#d8d1c3] bg-[#eae4d7]"><div className={`${contentWidth} grid gap-8 py-12 sm:grid-cols-2`}><div><p className="font-serif text-2xl">{siteConfig.name}</p><p className="mt-3 max-w-md text-sm leading-7 text-[#66736c]">{siteConfig.description}</p></div><nav aria-label="Liên kết cuối trang" className="flex flex-wrap items-start gap-5 text-sm sm:justify-end"><Link href="/kien-thuc">Kiến thức</Link><Link href="/gioi-thieu">Giới thiệu</Link><Link href="/lien-he">Liên hệ tư vấn</Link><Link href="/tim-kiem">Tìm trà cụ</Link></nav></div></footer>
  </div>;
}
