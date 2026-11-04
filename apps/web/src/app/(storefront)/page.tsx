import Link from "next/link";
import { ArrowRight, ArrowUpRight, BookOpenText, MessageCircle, Search } from "lucide-react";
import { getSiteContext } from "@/lib/config/site-context";
import { getPublicCategories, getPublicProducts } from "@/lib/api/public";
import { getPublicArticles } from "@/lib/api/articles";
import { pageMetadata } from "@/lib/seo/policy";
import { ArticleCard } from "@/components/storefront/article-card";
import { CatalogImage, EmptyCatalog, ProductGrid } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";

export async function generateMetadata() {
  const { site } = await getSiteContext();
  return pageMetadata(site, {
    path: "/",
    title: "defaultSeoTitle" in site && site.defaultSeoTitle || site.name,
    description: "defaultMetaDescription" in site && site.defaultMetaDescription || site.description,
  });
}

export default async function HomePage() {
  const [categories, products, articles, { profile }] = await Promise.all([
    getPublicCategories(),
    getPublicProducts(1, 9),
    getPublicArticles(1, 3),
    getSiteContext(),
  ]);
  const editorial = products.items.find((item) => item.coverImage?.publicUrl);
  const mainCategories = categories.slice(0, 5);
  const story = profile.about?.trim();

  return <main id="noi-dung">
    <section className="sf-home-hero">
      <div className="sf-container sf-home-hero-grid">
        <div className="sf-home-hero-copy">
          <p className="sf-kicker flex items-center gap-3"><span className="sf-kicker-line" aria-hidden="true" /> Trà cụ &amp; phong vị sống chậm</p>
          <h1 className="sf-home-title">Một chiếc ấm.<br /><em>Một khoảng tĩnh.</em><br />Một đời trà.</h1>
          <p className="sf-copy mt-7 max-w-lg">Khám phá ấm trà và trà cụ qua hình dáng, chất liệu, câu chuyện sử dụng. Dành một chút thời gian để chọn món đồ thật sự phù hợp với cách thưởng trà của bạn.</p>
          <div className="mt-9 flex flex-wrap gap-3">
            <Link href="#bo-suu-tap" className="sf-button">Khám phá trà cụ <ArrowUpRight size={16} aria-hidden="true" /></Link>
            <Link href="/kien-thuc" className="sf-button sf-button-outline">Đọc chuyện trà <ArrowRight size={16} aria-hidden="true" /></Link>
          </div>
          <div className="sf-hero-footnote"><span className="sf-hero-rule" aria-hidden="true" /> <span>Không vội chọn. Hãy bắt đầu từ điều bạn muốn hiểu.</span></div>
        </div>
        <div className="sf-home-hero-visual">
          <div className="sf-home-hero-image">
            {editorial?.coverImage?.publicUrl
              ? <Link href={`/san-pham/${editorial.slug}`} className="block h-full" aria-label={`Khám phá ${editorial.name}`}>
                  <CatalogImage image={editorial.coverImage} priority sizes="(max-width: 900px) 92vw, 560px" />
                </Link>
              : <div className="sf-hero-editorial-empty">
                  <span className="sf-hero-ring" aria-hidden="true" />
                  <span className="sf-kicker">Tĩnh · Mộc · Sâu</span>
                  <p className="font-serif text-[clamp(1.7rem,3vw,2.4rem)] leading-snug">Thưởng trà,<br />thưởng một khoảng lặng.</p>
                </div>}
          </div>
          <div className="sf-home-image-caption">
            <span className="sf-kicker text-[#d1b88c]!">{editorial ? "Trong bộ sưu tập" : "Tinh thần TeaPremium"}</span>
            <p className="mt-2 font-serif text-xl leading-snug">{editorial ? editorial.name : "Không gian dành cho trà cụ và câu chuyện thưởng trà."}</p>
            {editorial && <Link href={`/san-pham/${editorial.slug}`} className="mt-3 inline-flex items-center gap-2 text-sm text-[#f1e9d9] underline underline-offset-4">Xem sản phẩm <ArrowUpRight size={15} aria-hidden="true" /></Link>}
          </div>
          <span className="sf-hero-side-note" aria-hidden="true">THE ART OF TEAWARE</span>
        </div>
      </div>
    </section>

    <section id="bo-suu-tap" className="sf-container sf-section scroll-mt-20">
      <div className="sf-section-head">
        <div><p className="sf-kicker">01 / Khám phá</p><h2 className="sf-heading mt-4">Trà cụ theo <em className="sf-serif-em">cảm hứng.</em></h2>
          <p className="sf-copy mt-5 max-w-xl">Mỗi dáng ấm, chất liệu và cách dùng mang đến một trải nghiệm riêng. Chọn bộ sưu tập để tìm hiểu sâu hơn.</p>
        </div>
        <Link href="/tim-kiem" className="sf-text-link inline-flex items-center gap-2">Tìm sản phẩm <ArrowUpRight size={16} aria-hidden="true" /></Link>
      </div>
      {mainCategories.length
        ? <div className="sf-collections">
            {mainCategories.map((category, index) => {
              const representative = products.items.find((item) => item.primaryCategory?.slug === category.slug && item.coverImage?.publicUrl);
              return <Link href={`/danh-muc/${category.slug}`} key={category.slug} className="sf-collection-tile group">
                <div className="sf-collection-picture">
                  {representative?.coverImage?.publicUrl
                    ? <CatalogImage image={representative.coverImage} sizes="(max-width: 640px) 80vw, (max-width: 1024px) 40vw, 30vw" />
                    : <span className="sf-collection-symbol" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span>}
                </div>
                <div className="sf-collection-details">
                  <p className="sf-kicker">Bộ sưu tập / {String(index + 1).padStart(2, "0")}</p>
                  <div className="mt-3 flex items-end justify-between gap-3">
                    <h3 className="font-serif text-[clamp(1.4rem,2vw,2rem)] leading-tight text-[#233b32]">{category.name}</h3>
                    <ArrowUpRight size={22} strokeWidth={1.5} className="shrink-0 text-[#925b3d] transition-transform group-hover:translate-x-1 group-hover:-translate-y-1" aria-hidden="true" />
                  </div>
                </div>
              </Link>;
            })}
          </div>
        : <EmptyCatalog title="Bộ sưu tập đang được cập nhật">Khi danh mục được xuất bản, bạn có thể khám phá từng nhóm trà cụ ở đây.</EmptyCatalog>}
    </section>

    <section className="sf-featured-section">
      <div className="sf-container sf-section">
        <div className="sf-section-head">
          <div><p className="sf-kicker">02 / Tuyển chọn để khám phá</p><h2 className="sf-heading mt-4">Những trà cụ <em className="sf-serif-em">mới giới thiệu.</em></h2>
            <p className="sf-copy mt-5 max-w-xl">Hình ảnh và thông số được trình bày theo từng sản phẩm đã công bố. Liên hệ trực tiếp khi cần tư vấn.</p>
          </div>
          <Link className="sf-text-link inline-flex items-center gap-2" href="/tim-kiem">Xem toàn bộ <ArrowUpRight size={16} aria-hidden="true" /></Link>
        </div>
        <ProductGrid products={products.items.slice(0, 6)} headingLevel={3} />
      </div>
    </section>

    <section className="sf-philosophy" aria-label="Lựa chọn trà cụ">
      <div className="sf-container sf-philosophy-grid">
        <div className="max-w-lg">
          <p className="sf-kicker text-[#d1b88c]!">03 / Tĩnh · Mộc · Sâu</p>
          <h2 className="mt-6 font-serif text-[clamp(2.5rem,4.5vw,4.8rem)] leading-[1.2] tracking-[-.045em] text-[#fff9ec]">Trà ngon cần sự tĩnh.<br /><em className="font-normal text-[#d1b88c]">Trà cụ cần sự hiểu.</em></h2>
          <p className="mt-7 max-w-md text-base leading-8 text-[#e1e8df]">Mỗi lựa chọn bắt đầu bằng việc hiểu chất liệu, dung tích, thói quen thưởng trà và nhu cầu sử dụng — trước khi quyết định món đồ nào sẽ đồng hành cùng mình.</p>
          <Link href="/gioi-thieu" className="mt-9 inline-flex items-center gap-2 border-b border-[#d1b88c] pb-2 text-sm font-semibold text-[#f7f4ec]">Về không gian trà cụ <ArrowUpRight size={16} aria-hidden="true" /></Link>
        </div>
        <div className="sf-philosophy-steps">
          {[
            { n: "01", title: "Nhìn chất liệu", text: "Tìm hiểu bề mặt, hình dáng và thông số được công bố của mỗi sản phẩm." },
            { n: "02", title: "Hiểu nhu cầu", text: "Lựa chọn theo cách pha trà, số người dùng, sở thích và mục đích sử dụng." },
            { n: "03", title: "Trao đổi khi cần", text: "Gửi sản phẩm bạn quan tâm để được tư vấn thêm trước khi quyết định." },
          ].map((step) => <div key={step.n} className="sf-philosophy-step">
            <span className="font-serif text-2xl text-[#d1b88c]">{step.n}</span>
            <div><h3 className="font-serif text-2xl text-[#fff9ec]">{step.title}</h3><p className="mt-3 text-sm leading-7 text-[#d4e0d6]">{step.text}</p></div>
          </div>)}
        </div>
      </div>
    </section>

    <section className="sf-container sf-section">
      <div className="sf-section-head">
        <div><p className="sf-kicker">04 / Chuyện bên chén trà</p><h2 className="sf-heading mt-4">Đọc để <em className="sf-serif-em">hiểu sâu hơn.</em></h2>
          <p className="sf-copy mt-5 max-w-xl">Kiến thức trà cụ, cách dùng và những câu chuyện được chia sẻ từ góc nhìn trân trọng trà đạo.</p>
        </div>
        <Link href="/kien-thuc" className="sf-text-link inline-flex items-center gap-2">Góc kiến thức <BookOpenText size={17} aria-hidden="true" /></Link>
      </div>
      {articles.items.length
        ? <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">{articles.items.map((article) => <ArticleCard key={article.slug} article={article} headingLevel={3} />)}</div>
        : <div className="sf-empty"><BookOpenText className="mx-auto mb-5 text-[#92794f]" size={30} strokeWidth={1.5} aria-hidden="true" />
            <h3 className="font-serif text-2xl text-[#233b32]">Góc đọc đang được biên tập</h3><p className="mt-3 text-sm leading-7 text-[#56635b]">Những bài viết đã xuất bản sẽ xuất hiện tại đây.</p></div>}
    </section>

    {story && <section className="sf-story-band"><div className="sf-container grid gap-7 py-12 lg:grid-cols-[.4fr_1fr] lg:items-center lg:py-16">
      <p className="sf-kicker">Đằng sau không gian này</p>
      <div><p className="max-w-3xl font-serif text-[clamp(1.35rem,2.6vw,2.2rem)] leading-relaxed text-[#233b32]">{story.slice(0, 260)}{story.length > 260 ? "…" : ""}</p>
        <Link href="/gioi-thieu" className="sf-text-link mt-5 inline-flex items-center gap-2">Đọc câu chuyện <ArrowUpRight size={15} aria-hidden="true" /></Link>
      </div>
    </div></section>}

    <section className="sf-final-contact">
      <div className="sf-container sf-section grid items-center gap-10 lg:grid-cols-[1.05fr_.95fr] lg:gap-20">
        <div><p className="sf-kicker">Một cuộc trò chuyện bắt đầu từ trà</p>
          <h2 className="sf-heading mt-4">Bạn đang tìm <em className="sf-serif-em">một chiếc ấm phù hợp?</em></h2>
          <p className="sf-copy mt-5 max-w-lg">Tìm hiểu để mua dùng, chọn làm quà hay quan tâm đến nguồn hàng sỉ — hãy gửi sản phẩm và nhu cầu của bạn. Không cần vội quyết định.</p>
          <Link href="/lien-he" className="sf-button mt-8">Gửi yêu cầu tư vấn <MessageCircle size={18} aria-hidden="true" /></Link>
        </div>
        <div className="sf-final-contact-card">
          <p className="sf-kicker">Kết nối với chúng tôi</p>
          <h3 className="mt-4 font-serif text-3xl leading-snug text-[#233b32]">Hỏi về một sản phẩm, bắt đầu bằng một câu chuyện.</h3>
          <p className="mt-4 text-sm leading-7 text-[#56635b]">Bạn có thể chọn Zalo, Facebook hoặc điện thoại nếu kênh đó đã được cửa hàng công bố.</p>
          <div className="mt-7"><ContactLinks /></div>
          <Link href="/tim-kiem" className="sf-text-link mt-6 inline-flex items-center gap-2">Tìm lại sản phẩm <Search size={15} aria-hidden="true" /></Link>
        </div>
      </div>
    </section>
  </main>;
}
