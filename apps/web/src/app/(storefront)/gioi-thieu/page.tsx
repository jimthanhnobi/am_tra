import Link from "next/link";
import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";

export async function generateMetadata() { const { site, profile } = await getSiteContext(); return pageMetadata(site, { path: "/gioi-thieu", title: "Giới thiệu", description: profile.about?.slice(0, 180) || "Hồ sơ thương hiệu đang được cập nhật.", index: Boolean(profile.about) }); }
export default async function AboutPage() {
  const { site, profile } = await getSiteContext();
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Giới thiệu", path: "/gioi-thieu" }]} />
    <header className="grid gap-8 border-b border-[#d9d2c3] pb-12 lg:grid-cols-[1fr_.65fr] lg:items-end"><div><p className="sf-kicker">Về {site.name}</p><h1 className="sf-heading mt-5">Câu chuyện thương hiệu</h1></div><p className="sf-copy max-w-lg">Thông tin giới thiệu được công bố bởi website. Nội dung chưa xác nhận sẽ được để trống.</p></header>
    <article className="sf-section grid gap-10 lg:grid-cols-[.65fr_1fr] lg:gap-20"><div><p className="sf-kicker">Giới thiệu</p><h2 className="mt-4 font-serif text-3xl leading-snug text-[#233b32]">Một lời giới thiệu rõ ràng.</h2></div><div>{profile.about ? <div className="sf-richtext whitespace-pre-line">{profile.about}</div> : <div className="border-l-[3px] border-[#b79b6c] bg-[#fffcf6] p-7"><h3 className="font-serif text-2xl text-[#233b32]">Hồ sơ đang được hoàn thiện</h3><p className="mt-4 text-sm leading-7 text-[#56635b]">Câu chuyện thương hiệu và thông tin cửa hàng chưa được công bố. Nội dung sẽ được bổ sung sau khi được xác nhận.</p></div>}<Link href="/#bo-suu-tap" className="sf-button sf-button-outline mt-8">Khám phá bộ sưu tập <span aria-hidden="true">↗</span></Link></div></article>
  </main>;
}
