import Link from "next/link";
import { getSiteContext } from "@/lib/config/site-context";
import { getPublicCategories, getPublicProducts } from "@/lib/api/public";
import { getPublicArticles } from "@/lib/api/articles";
import { pageMetadata } from "@/lib/seo/policy";
import { ArticleCard } from "@/components/storefront/article-card";
import { CatalogImage, EmptyCatalog, ProductGrid } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";

export async function generateMetadata() {
  const { site } = await getSiteContext();
  return pageMetadata(site, { path: "/", title: "defaultSeoTitle" in site && site.defaultSeoTitle || site.name,
    description: "defaultMetaDescription" in site && site.defaultMetaDescription || site.description });
}

export default async function HomePage() {
  const [categories, products, articles, { site, profile }] = await Promise.all([
    getPublicCategories(), getPublicProducts(1, 6), getPublicArticles(1, 3), getSiteContext(),
  ]);
  const cover = products.items.find((product) => product.coverImage?.publicUrl)?.coverImage;
  const story = profile.about?.trim();
  return <main id="noi-dung">
    <section className="sf-hero border-b border-[#d9d2c3]">
      <div className="sf-container grid min-h-[37rem] items-center gap-10 py-12 md:grid-cols-[1.05fr_.95fr] md:gap-16 md:py-16 lg:min-h-[43rem]">
        <div className="max-w-2xl">
          <p className="sf-kicker">Không gian trà cụ · {site.name}</p>
          <h1 className="sf-heading mt-6 text-[clamp(3.2rem,5.7vw,5.7rem)]! leading-[1.08]!">Một khoảng lặng,<br /><em className="font-normal">một chén trà.</em></h1>
          <p className="sf-copy mt-7 max-w-lg">Khám phá những trà cụ đã được công bố, tìm hiểu thông tin sản phẩm và chọn cách liên hệ phù hợp với bạn.</p>
          <div className="mt-9 flex flex-wrap gap-3"><Link className="sf-button" href="#bo-suu-tap">Khám phá bộ sưu tập <span aria-hidden="true">↗</span></Link><Link className="sf-button sf-button-outline" href="/gioi-thieu">Tìm hiểu thêm</Link></div>
        </div>
        <div className="sf-hero-art mx-auto w-full max-w-[34rem]">
          {cover ? <div className="absolute inset-0 p-7 sm:p-10"><CatalogImage image={cover} priority sizes="(max-width: 768px) 90vw, 540px" /></div>
            : <div className="sf-hero-empty"><p className="font-serif text-2xl text-[#233b32]">Một nơi để khám phá trà cụ.</p><p className="mt-3 text-sm text-[#37483e]">Hình ảnh sản phẩm sẽ xuất hiện khi được công bố.</p></div>}
        </div>
      </div>
    </section>

    <section id="bo-suu-tap" className="sf-container sf-section scroll-mt-8">
      <div className="mb-10 flex flex-wrap items-end justify-between gap-6"><div><p className="sf-kicker">Bộ sưu tập</p><h2 className="sf-heading mt-3">Khám phá theo danh mục</h2></div><Link href="/tim-kiem" className="sf-text-link">Tìm trong bộ sưu tập <span aria-hidden="true">↗</span></Link></div>
      {categories.length ? <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {categories.slice(0, 6).map((category, index) => <Link href={`/danh-muc/${category.slug}`} key={category.slug} className="sf-collection-card group">
          <span className="sf-kicker">Bộ sưu tập {String(index + 1).padStart(2, "0")}</span>
          <span><span className="block font-serif text-3xl leading-tight text-[#233b32]">{category.name}</span><span className="sf-text-link mt-4 inline-block">Khám phá danh mục <span aria-hidden="true">↗</span></span></span>
        </Link>)}
      </div> : <EmptyCatalog title="Danh mục đang được hoàn thiện">Danh mục được xuất bản sẽ xuất hiện ở đây. Bạn vẫn có thể đọc các bài viết đã công bố hoặc xem thông tin liên hệ.</EmptyCatalog>}
    </section>

    <section className="border-y border-[#d9d2c3] bg-[#efeae0]"><div className="sf-container sf-section">
      <div className="mb-10 flex flex-wrap items-end justify-between gap-6"><div><p className="sf-kicker">Vừa cập nhật</p><h2 className="sf-heading mt-3">Trà cụ mới được giới thiệu</h2></div>{products.totalItems > 6 && <Link href="/tim-kiem" className="sf-text-link">Xem tất cả sản phẩm <span aria-hidden="true">↗</span></Link>}</div>
      <ProductGrid products={products.items} headingLevel={3} />
    </div></section>

    <section className="sf-container sf-section grid items-start gap-10 lg:grid-cols-[.8fr_1.2fr] lg:gap-24">
      <div><p className="sf-kicker">Về thương hiệu</p><h2 className="sf-heading mt-4">Không gian dành cho người yêu trà cụ.</h2></div>
      <div className="border-l border-[#b79b6c] pl-6 sm:pl-10"><p className="sf-copy">{story ? story.slice(0, 340) : "Thông tin giới thiệu thương hiệu chưa được công bố. Chúng tôi sẽ bổ sung sau khi nội dung được xác nhận."}</p><Link href="/gioi-thieu" className="sf-text-link mt-7 inline-block">Đến trang giới thiệu <span aria-hidden="true">↗</span></Link></div>
    </section>

    <section className="border-y border-[#d9d2c3] bg-[#fffcf6]"><div className="sf-container sf-section">
      <div className="mb-10 flex flex-wrap items-end justify-between gap-6"><div><p className="sf-kicker">Góc đọc</p><h2 className="sf-heading mt-3">Kiến thức trà cụ</h2></div><Link href="/kien-thuc" className="sf-text-link">Xem các bài viết <span aria-hidden="true">↗</span></Link></div>
      {articles.items.length ? <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:gap-7">{articles.items.map((article) => <ArticleCard key={article.slug} article={article} headingLevel={3} />)}</div>
        : <div className="sf-empty"><h3 className="font-serif text-2xl text-[#233b32]">Bài viết đang được chuẩn bị</h3><p className="mt-3 text-sm leading-7 text-[#56635b]">Chỉ bài viết đã xuất bản mới xuất hiện ở đây.</p></div>}
    </div></section>

    <section className="bg-[#233b32] text-[#f7f4ec]"><div className="sf-container sf-section grid gap-10 lg:grid-cols-[1fr_1fr] lg:items-center lg:gap-20">
      <div><p className="sf-kicker text-[#d1b88c]!">Tư vấn</p><h2 className="mt-4 font-serif text-[clamp(2.3rem,4vw,4.2rem)] leading-tight">Bạn đang tìm hiểu một sản phẩm?</h2></div>
      <div><p className="max-w-lg text-base leading-8 text-[#e2e7e0]">Xem thông tin liên hệ đã được công bố và trao đổi qua kênh thuận tiện. Bạn cũng có thể gửi yêu cầu tư vấn trên trang liên hệ.</p><div className="mt-7 sf-contact-on-dark"><ContactLinks /></div></div>
    </div></section>
  </main>;
}
