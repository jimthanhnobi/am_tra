import Link from "next/link";
import { siteConfig } from "@/lib/config/site";
import { siteProfile } from "@/lib/config/profile";
import { pageMetadata } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";

export function generateMetadata() { return pageMetadata(siteConfig, { path: "/gioi-thieu", title: "Giới thiệu", description: siteProfile.about?.slice(0, 180) || "Hồ sơ thương hiệu đang được cập nhật.", index: Boolean(siteProfile.about) }); }
export default function AboutPage() {
  return <main id="noi-dung" className={`${contentWidth} py-12`}><Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Giới thiệu", path: "/gioi-thieu" }]} /><article className="max-w-3xl"><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Về {siteConfig.name}</p><h1 className="mt-4 font-serif text-4xl sm:text-5xl">Câu chuyện thương hiệu</h1>{siteProfile.about ? <div className="mt-8 whitespace-pre-line text-base leading-8 text-[#536159]">{siteProfile.about}</div> : <div className="mt-8 border-l-2 border-[#8d4f34] pl-6"><h2 className="font-serif text-2xl">Hồ sơ đang được hoàn thiện</h2><p className="mt-4 text-sm leading-7 text-[#66736c]">Câu chuyện thương hiệu và thông tin cửa hàng chưa được công bố. Nội dung sẽ được bổ sung sau khi được xác nhận.</p></div>}<Link href="/#bo-suu-tap" className="mt-9 inline-flex rounded-full border border-[#183c31] px-6 py-3 text-sm">Khám phá bộ sưu tập →</Link></article></main>;
}
