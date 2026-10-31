import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";
import { LeadForm } from "@/components/storefront/lead-form";

export async function generateMetadata() { const { site, hasContact } = await getSiteContext(); return pageMetadata(site, { path: "/lien-he", title: "Liên hệ tư vấn", description: `Các kênh liên hệ được công bố của ${site.name}.`, index: hasContact }); }
export default async function ContactPage() {
  const { profile } = await getSiteContext();
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Liên hệ", path: "/lien-he" }]} />
    <header className="mb-12 max-w-4xl"><p className="sf-kicker">Kết nối</p><h1 className="sf-heading mt-5">Cùng trao đổi về trà cụ.</h1><p className="sf-copy mt-6 max-w-2xl">Bạn có thể gửi tên hoặc đường dẫn sản phẩm để thuận tiện trao đổi qua kênh liên hệ đã công bố.</p></header>
    <div className="grid gap-6 lg:grid-cols-[minmax(0,.85fr)_minmax(0,1.15fr)] lg:items-start lg:gap-10">
      <section className="sf-card p-7 sm:p-9"><p className="sf-kicker">Liên hệ trực tiếp</p><h2 className="mt-3 font-serif text-3xl text-[#233b32]">Kênh tư vấn</h2><div className="mt-7"><ContactLinks fallback={false} /></div>
        <div className="mt-10 border-t border-[#d9d2c3] pt-7"><h3 className="font-serif text-xl text-[#233b32]">Thông tin đã công bố</h3><p className="mt-4 whitespace-pre-line text-sm leading-7 text-[#56635b]">{profile.address || "Địa chỉ chưa được công bố."}</p>{profile.openingHours && <p className="mt-3 whitespace-pre-line text-sm leading-7 text-[#56635b]">Giờ mở cửa: {profile.openingHours}</p>}{profile.email && <p className="mt-3 text-sm"><a href={`mailto:${profile.email}`} className="sf-text-link">{profile.email}</a></p>}</div>
      </section>
      <div id="tu-van"><LeadForm /></div>
    </div>
  </main>;
}
