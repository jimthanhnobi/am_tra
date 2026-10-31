import { siteConfig } from "@/lib/config/site";
import { hasContact, siteProfile } from "@/lib/config/profile";
import { pageMetadata } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";
import { LeadForm } from "@/components/storefront/lead-form";

export function generateMetadata() { return pageMetadata(siteConfig, { path: "/lien-he", title: "Liên hệ tư vấn", description: `Các kênh liên hệ được công bố của ${siteConfig.name}.`, index: hasContact }); }
export default function ContactPage() {
  return <main id="noi-dung" className={`${contentWidth} py-12`}><Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Liên hệ", path: "/lien-he" }]} /><div className="grid gap-10 md:grid-cols-2"><div><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Kết nối</p><h1 className="mt-4 font-serif text-4xl sm:text-5xl">Cùng trao đổi<br />về trà cụ.</h1><p className="mt-6 max-w-md text-base leading-8 text-[#66736c]">Khi liên hệ, bạn có thể gửi tên hoặc đường dẫn sản phẩm để thuận tiện trao đổi.</p></div><section className="border border-[#d8d1c3] bg-[#fbf8f1] p-7 sm:p-9"><h2 className="mb-6 font-serif text-2xl">Kênh tư vấn</h2><ContactLinks fallback={false} /><h2 className="mb-3 mt-9 font-serif text-xl">Địa chỉ cửa hàng</h2><p className="whitespace-pre-line text-sm leading-7 text-[#66736c]">{siteProfile.address || "Địa chỉ chưa được công bố."}</p></section></div><LeadForm /></main>;
}
