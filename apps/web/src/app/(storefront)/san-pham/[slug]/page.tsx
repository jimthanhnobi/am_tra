import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowRight, ArrowUpRight, MessageCircle, BookOpenText } from "lucide-react";
import { getPublicCategory, getPublicProduct } from "@/lib/api/public";
import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata, breadcrumbJsonLd, productJsonLd, serializeJsonLd } from "@/lib/seo/policy";
import { Breadcrumbs, ProductGrid, contentWidth } from "@/components/storefront/catalog";
import { ProductGallery } from "@/components/storefront/product-gallery";
import { ContactLinks } from "@/components/storefront/contact";

type Props = Readonly<{
  params: Promise<{ slug: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}>;

async function requireProduct(slug: string) {
  const product = await getPublicProduct(slug);
  if (!product) notFound();
  return product;
}

export async function generateMetadata({ params, searchParams }: Props) {
  const product = await requireProduct((await params).slug);
  const { site } = await getSiteContext();
  const image = product.media.find((item) => item.assetId === product.seo.ogImageAssetId && item.publicUrl)
    || product.media.find((item) => item.isCover && item.publicUrl);
  return pageMetadata(site, {
    path: `/san-pham/${product.slug}`,
    title: product.name,
    description: product.summary,
    seo: product.seo,
    index: Object.keys(await searchParams).length === 0,
    image: image?.publicUrl ? { url: image.publicUrl, width: image.width, height: image.height, alt: image.altText } : undefined,
  });
}

export default async function ProductPage({ params }: Props) {
  const product = await requireProduct((await params).slug);
  const [{ site }, relatedCategory] = await Promise.all([
    getSiteContext(), getPublicCategory(product.primaryCategory.slug),
  ]);
  const crumbs = [
    { name: "Trang chủ", path: "/" },
    { name: product.primaryCategory.name, path: `/danh-muc/${product.primaryCategory.slug}` },
    { name: product.name, path: `/san-pham/${product.slug}` },
  ];
  const gallery = [...product.media].sort((a, b) =>
    Number(b.isCover) - Number(a.isCover) || a.sortOrder - b.sortOrder);
  const related = relatedCategory?.products.items.filter((item) => item.slug !== product.slug).slice(0, 3) ?? [];
  const specs: [string, string][] = [
    ...(product.materialText ? [["Chất liệu", product.materialText] as [string, string]] : []),
    ...(product.capacityMl ? [["Dung tích", `${product.capacityMl} ml`] as [string, string]] : []),
    ...(product.brand?.origin ? [["Xuất xứ thương hiệu", product.brand.origin] as [string, string]] : []),
    ...Object.entries(product.specifications)
      .filter(([key]) => !["capacityMl", "materialText"].includes(key))
      .map(([key, value]): [string, string] => [key, typeof value === "boolean" ? value ? "Có" : "Không" : String(value)]),
  ];
  const contactHref = `/lien-he?san-pham=${encodeURIComponent(product.slug)}`;

  return <main id="noi-dung" className={`${contentWidth} py-8 sm:py-12`}>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(productJsonLd(site, product)) }} />
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(site, crumbs)) }} />
    <Breadcrumbs items={crumbs} />

    <div className="sf-product-layout">
      <ProductGallery images={gallery} name={product.name} />
      <section className="min-w-0 lg:pt-4">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
          <Link href={`/danh-muc/${product.primaryCategory.slug}`} className="sf-kicker hover:underline">{product.primaryCategory.name}</Link>
          {product.brand && <><span className="text-[#b79b6c]" aria-hidden="true">/</span><span className="sf-kicker">{product.brand.name}</span></>}
        </div>
        <h1 className="sf-heading mt-5">{product.name}</h1>
        {product.sku && <p className="mt-4 text-xs font-medium tracking-[.12em] text-[#56635b]">MÃ SẢN PHẨM / {product.sku}</p>}
        {product.summary && <p className="sf-copy mt-8 whitespace-pre-line">{product.summary}</p>}

        <div className="sf-product-consult mt-9">
          <div className="flex items-start gap-3">
            <MessageCircle className="mt-1 shrink-0 text-[#925b3d]" size={20} strokeWidth={1.6} aria-hidden="true" />
            <div>
              <h2 className="font-serif text-[1.45rem] text-[#233b32]">Tìm hiểu trước khi lựa chọn</h2>
              <p className="mt-2 text-sm leading-7 text-[#56635b]">Hãy gửi yêu cầu kèm tên hoặc mã sản phẩm để được tư vấn về thông số, cách sử dụng và nhu cầu mua dùng hoặc nhập sỉ.</p>
            </div>
          </div>
          <Link href={contactHref} className="sf-button mt-6 w-full sm:w-auto">Gửi yêu cầu về sản phẩm <ArrowUpRight size={16} aria-hidden="true" /></Link>
          <details className="mt-5 group">
            <summary className="cursor-pointer text-sm font-semibold text-[#233b32] underline decoration-[#b79b6c] underline-offset-4">Hoặc liên hệ trực tiếp qua kênh có sẵn</summary>
            <div className="mt-5"><ContactLinks /></div>
          </details>
        </div>

        {specs.length > 0 && <section className="mt-9">
          <h2 className="font-serif text-2xl text-[#233b32]">Thông tin trà cụ</h2>
          <dl className="sf-product-specs mt-4 border-t border-[#d9d2c3]">
            {specs.map(([key, value]) => <div key={key}><dt>{key}</dt><dd>{value}</dd></div>)}
          </dl>
          <p className="mt-4 text-xs leading-6 text-[#56635b]">Thông tin dựa trên dữ liệu sản phẩm đã công bố.</p>
        </section>}
      </section>
    </div>

    {product.descriptionHtml && <section className="sf-section mt-12 border-t border-[#d9d2c3]">
      <div className="grid gap-8 lg:grid-cols-[minmax(0,.35fr)_minmax(0,.65fr)] lg:gap-16">
        <div><p className="sf-kicker">Tìm hiểu sâu hơn</p><h2 className="mt-4 font-serif text-3xl leading-snug text-[#233b32]">Câu chuyện &amp; chi tiết sản phẩm</h2></div>
        <div className="sf-richtext" dangerouslySetInnerHTML={{ __html: product.descriptionHtml }} />
      </div>
    </section>}

    {product.categories.length > 1 && <nav aria-label="Các bộ sưu tập liên quan" className="flex flex-wrap gap-3 border-t border-[#d9d2c3] py-8">
      {product.categories.map((category) => <Link key={category.slug} href={`/danh-muc/${category.slug}`} className="sf-button sf-button-outline">{category.name}</Link>)}
    </nav>}
    {related.length > 0 && <section className="sf-section border-t border-[#d9d2c3]">
      <div className="sf-section-head"><div><p className="sf-kicker">Dành cho bạn khám phá</p><h2 className="sf-heading mt-4">Cùng bộ sưu tập</h2></div>
        <Link href={`/danh-muc/${product.primaryCategory.slug}`} className="sf-text-link inline-flex items-center gap-2">Xem danh mục <ArrowRight size={16} aria-hidden="true" /></Link></div>
      <ProductGrid products={related} headingLevel={3} />
    </section>}

    <section className="mt-8 mb-10 border-t border-[#d9d2c3] py-10 text-center">
      <BookOpenText className="mx-auto text-[#925b3d]" size={24} strokeWidth={1.4} aria-hidden="true" />
      <p className="mt-4 font-serif text-2xl text-[#233b32]">Tìm hiểu nhiều hơn về trà cụ</p>
      <Link href="/kien-thuc" className="sf-text-link mt-3 inline-flex items-center gap-2">Góc kiến thức <ArrowUpRight size={16} aria-hidden="true" /></Link>
    </section>
  </main>;
}
