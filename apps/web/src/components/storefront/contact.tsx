import Link from "next/link";
import { siteProfile, hasContact } from "@/lib/config/profile";
import { ContactActions } from "@/components/storefront/contact-actions";

export function ContactLinks({ fallback = true }: Readonly<{ fallback?: boolean }>) {
  const classes = "inline-flex items-center justify-center rounded-full border border-[#183c31] px-5 py-3 text-sm font-medium transition hover:bg-[#183c31] hover:text-[#fbf8f1]";
  if (!hasContact) return fallback ? <Link className={classes} href="/lien-he">Thông tin liên hệ</Link> : <p className="text-sm leading-7 text-[#66736c]">Kênh tư vấn chưa được công bố. Vui lòng quay lại sau khi thông tin được cập nhật.</p>;
  return <ContactActions links={[...(siteProfile.zalo ? [{ channel: "zalo" as const, href: siteProfile.zalo, label: "Tư vấn qua Zalo ↗" }] : []), ...(siteProfile.facebook ? [{ channel: "facebook" as const, href: siteProfile.facebook, label: "Liên hệ Facebook ↗" }] : []), ...(siteProfile.phoneHref ? [{ channel: "hotline" as const, href: siteProfile.phoneHref, label: `Gọi ${siteProfile.hotline}` }] : [])]} />;
}
