import Link from "next/link";
import { getSiteContext } from "@/lib/config/site-context";
import { ContactActions } from "@/components/storefront/contact-actions";

export async function ContactLinks({ fallback = true }: Readonly<{ fallback?: boolean }>) {
  const { profile: siteProfile, hasContact } = await getSiteContext();
  const classes = "sf-button sf-button-outline";
  if (!hasContact) return fallback ? <Link className={classes} href="/lien-he">Thông tin liên hệ</Link> : <p className="text-sm leading-7 text-[#66736c]">Kênh tư vấn chưa được công bố. Vui lòng quay lại sau khi thông tin được cập nhật.</p>;
  return <ContactActions links={[...(siteProfile.zalo ? [{ channel: "zalo" as const, href: siteProfile.zalo, label: "Tư vấn qua Zalo ↗" }] : []), ...(siteProfile.facebook ? [{ channel: "facebook" as const, href: siteProfile.facebook, label: "Liên hệ Facebook ↗" }] : []), ...(siteProfile.phoneHref ? [{ channel: "hotline" as const, href: siteProfile.phoneHref, label: `Gọi ${siteProfile.hotline}` }] : [])]} />;
}
