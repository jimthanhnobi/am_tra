import Link from "next/link";
import { siteConfig } from "@/lib/config/site";
import { getPublicCategories, getPublicProducts } from "@/lib/api/public";
import { pageMetadata } from "@/lib/seo/policy";
import { CatalogImage, contentWidth, ProductGrid } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";

export function generateMetadata() { return pageMetadata(siteConfig, { path: "/", title: "Trà cụ & những khoảng lặng", description: siteConfig.description }); }

export default async function HomePage() {
  const [categories, products] = await Promise.all([getPublicCategories(), getPublicProducts(1, 6)]);
  const cover = products.items.find((product) => product.coverImage?.publicUrl)?.coverImage;
  return <main id="noi-dung"><section className="border-b border-[#d8d1c3] bg-[#eae4d7]"><div className={`${contentWidth} grid items-center gap-10 py-16 md:grid-cols-2 md:py-24`}><div><p className="text-xs uppercase tracking-[0.3em] text-[#8d4f34]">Trà cụ · {siteConfig.name}</p><h1 className="mt-6 max-w-xl font-serif text-5xl leading-[1.12] text-[#183c31] lg:text-6xl">Một khoảng lặng,<br />một chén trà.</h1><p className="mt-7 max-w-lg text-base leading-8 text-[#536159]">Khám phá trà cụ qua hình dáng, chất liệu và câu chuyện được chia sẻ cùng mỗi sản phẩm.</p><a href="#bo-suu-tap" className="mt-8 inline-flex rounded-full bg-[#183c31] px-6 py-3 text-sm text-[#fbf8f1]">Khám phá bộ sưu tập ↓</a></div><div className="aspect-square max-h-[440px] overflow-hidden rounded-t-full border border-[#c9c0b1] bg-[#f5f0e6] p-8">{cover ? <CatalogImage image={cover} priority sizes="(max-width: 768px) 90vw, 440px" /> : <div className="flex h-full flex-col items-center justify-center text-center"><span aria-hidden="true" className="font-serif text-7xl text-[#8d4f34]/60">茶</span><p className="mt-8 font-serif text-2xl leading-relaxed text-[#536159]">Chậm một chút.<br />Để thưởng trà.</p></div>}</div></div></section>
    <section id="bo-suu-tap" className={`${contentWidth} py-16 sm:py-20`}><div className="mb-10 flex flex-wrap items-end justify-between gap-5"><div><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Bộ sưu tập</p><h2 className="mt-3 font-serif text-3xl sm:text-4xl">Khám phá trà cụ</h2></div>{categories.length > 0 && <nav aria-label="Danh mục trà cụ" className="flex flex-wrap gap-2">{categories.map((category) => <Link key={category.slug} href={`/danh-muc/${category.slug}`} className="rounded-full border border-[#c9c0b1] px-4 py-2 text-sm hover:bg-[#eae4d7]">{category.name} ↗</Link>)}</nav>}</div><ProductGrid products={products.items} />{products.totalItems > 6 && <Link className="mt-10 inline-block border-b border-[#8d4f34] pb-1 text-sm text-[#8d4f34]" href="/tim-kiem">Xem tất cả sản phẩm →</Link>}</section>
    <section className="border-y border-[#d8d1c3] bg-[#fbf8f1]"><div className={`${contentWidth} grid gap-8 py-14 md:grid-cols-2`}><div><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Kết nối</p><h2 className="mt-3 font-serif text-3xl">Tìm trà cụ phù hợp với bạn</h2></div><div><p className="mb-6 text-sm leading-7 text-[#66736c]">Trao đổi về sản phẩm bạn quan tâm qua kênh liên hệ được công bố.</p><ContactLinks /></div></div></section>
  </main>;
}
