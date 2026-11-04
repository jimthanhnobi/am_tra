import Link from "next/link";
import { ArrowUpRight, MessageCircle, PencilLine } from "lucide-react";
import { getSiteContext } from "@/lib/config/site-context";
import { getPublicProduct } from "@/lib/api/public";
import { pageMetadata } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";
import { LeadForm } from "@/components/storefront/lead-form";

export async function generateMetadata() {
  const { site, hasContact } = await getSiteContext();
  return pageMetadata(site, {
    path: "/lien-he", title: "Liên hệ tư vấn",
    description: `Các kênh liên hệ được công bố của ${site.name}.`, index: hasContact,
  });
}

export default async function ContactPage({
  searchParams,
}: Readonly<{ searchParams: Promise<{ "san-pham"?: string }> }>) {
  const [{ profile }, params] = await Promise.all([getSiteContext(), searchParams]);
  const productSlug = typeof params["san-pham"] === "string" ? params["san-pham"] : "";
  const selected = productSlug ? await getPublicProduct(productSlug) : null;
  const initialMessage = selected
    ? `Tôi muốn được tư vấn sản phẩm: ${selected.name} (${selected.slug}). Nhu cầu của tôi: `
    : "";

  return <main id="noi-dung" className={`${contentWidth} py-8 sm:py-12`}>
    <Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Liên hệ", path: "/lien-he" }]} />
    <header className="sf-page-intro mb-12">
      <div><p className="sf-kicker">Kết nối</p><h1 className="sf-heading mt-4">Từ một câu hỏi,<br /><em className="sf-serif-em">bắt đầu chuyện trà.</em></h1></div>
      <p className="sf-copy sf-page-intro-description">Bạn muốn tìm hiểu một trà cụ, mua sử dụng hoặc hỏi thông tin nhập sỉ? Hãy chia sẻ nhu cầu để cuộc trao đổi đi thẳng vào điều bạn quan tâm.</p>
    </header>
    {selected && <div className="mb-9 flex flex-wrap items-center justify-between gap-3 border-l-[3px] border-[#b79b6c] bg-[#eee8dc] p-5">
      <div><p className="sf-kicker">Sản phẩm bạn đang quan tâm</p><p className="mt-2 font-serif text-xl text-[#233b32]">{selected.name}</p></div>
      <Link href={`/san-pham/${selected.slug}`} className="sf-text-link inline-flex items-center gap-2">Xem lại sản phẩm <ArrowUpRight size={15} aria-hidden="true" /></Link>
    </div>}
    <div className="grid gap-6 lg:grid-cols-[minmax(0,.8fr)_minmax(0,1.2fr)] lg:items-start lg:gap-10">
      <section className="sf-card p-6 sm:p-9">
        <MessageCircle className="mb-5 text-[#925b3d]" size={28} strokeWidth={1.6} aria-hidden="true" />
        <p className="sf-kicker">Liên hệ trực tiếp</p><h2 className="mt-3 font-serif text-3xl text-[#233b32]">Một cuộc trò chuyện gần gũi.</h2>
        <p className="mt-4 text-sm leading-7 text-[#56635b]">Chọn Zalo, Facebook hoặc gọi điện nếu đó là kênh thuận tiện cho bạn. Những kênh chưa được cửa hàng thiết lập sẽ không hiển thị.</p>
        <div className="mt-7"><ContactLinks fallback={false} /></div>
        <div className="mt-10 border-t border-[#d9d2c3] pt-7">
          <p className="sf-kicker">Thông tin công bố</p>
          {profile.address && <p className="mt-4 whitespace-pre-line text-sm leading-7 text-[#56635b]">{profile.address}</p>}
          {profile.openingHours && <p className="mt-3 whitespace-pre-line text-sm leading-7 text-[#56635b]">Giờ liên hệ: {profile.openingHours}</p>}
          {profile.email && <p className="mt-3 text-sm"><a href={`mailto:${profile.email}`} className="sf-text-link">{profile.email}</a></p>}
          {!profile.address && !profile.openingHours && !profile.email && <p className="mt-3 text-sm leading-7 text-[#56635b]">Thông tin liên hệ sẽ xuất hiện khi được công bố trong cài đặt website.</p>}
        </div>
      </section>
      <section id="tu-van" className="scroll-mt-28">
        <div className="mb-4 flex items-center gap-3 text-xs text-[#56635b]"><PencilLine size={17} aria-hidden="true" /> Yêu cầu được ghi nhận để phản hồi, không phải đặt hàng online.</div>
        <LeadForm initialMessage={initialMessage} />
      </section>
    </div>
  </main>;
}
