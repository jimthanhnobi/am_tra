import Link from "next/link";
import { notFound } from "next/navigation";
import { getPublicCategory, getPublicProduct } from "@/lib/api/public";
import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata, breadcrumbJsonLd, productJsonLd, serializeJsonLd } from "@/lib/seo/policy";
import { Breadcrumbs, CatalogImage, ProductGrid, contentWidth } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";

type Props = Readonly<{ params: Promise<{ slug: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }>;

async function requireProduct(slug: string) {
  const product = await getPublicProduct(slug);
  if (!product) notFound();
  return product;
}

export async function generateMetadata({ params, searchParams }: Props) {
  const product = await requireProduct((await params).slug);
  const { site } = await getSiteContext();
  const image = product.media.find((item) => item.assetId === product.seo.ogImageAssetId && item.publicUrl) || product.media.find((item) => item.isCover && item.publicUrl);
  return pageMetadata(site, {
    path: `/san-pham/${product.slug}`, title: product.name, description: product.summary, seo: product.seo,
    index: Object.keys(await searchParams).length === 0,
    image: image?.publicUrl ? { url: image.publicUrl, width: image.width, height: image.height, alt: image.altText } : undefined,
  });
}

export default async function ProductPage({ params }: Props) {
  const product = await requireProduct((await params).slug);
  const [{ site }, relatedCategory] = await Promise.all([getSiteContext(), getPublicCategory(product.primaryCategory.slug)]);
  const crumbs = [{ name: "Trang chủ", path: "/" }, { name: product.primaryCategory.name, path: `/danh-muc/${product.primaryCategory.slug}` }, { name: product.name, path: `/san-pham/${product.slug}` }];
  const gallery = [...product.media].sort((a, b) => Number(b.isCover) - Number(a.isCover) || a.sortOrder - b.sortOrder);
  const related = relatedCategory?.products.items.filter((item) => item.slug !== product.slug).slice(0, 3) ?? [];
  const specs = [
    ...(product.materialText ? [["Chất liệu", product.materialText]] : []),
    ...(product.capacityMl ? [["Dung tích", `${product.capacityMl} ml`]] : []),
    ...(product.brand?.origin ? [["Xuất xứ thương hiệu", product.brand.origin]] : []),
    ...Object.entries(product.specifications).filter(([key]) => !["capacityMl", "materialText"].includes(key)).map(([key, value]) => [key, typeof value === "boolean" ? value ? "Có" : "Không" : String(value)]),
  ];
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(productJsonLd(site, product)) }} />
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(site, crumbs)) }} />
    <Breadcrumbs items={crumbs} />
    <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,1.07fr)_minmax(0,.93fr)] lg:gap-16">
      <section aria-label="Hình ảnh sản phẩm" className="min-w-0">
        <div className="sf-card-media flex snap-x snap-mandatory overflow-x-auto border border-[#d9d2c3]">
          {gallery.length ? gallery.map((image, index) => <figure key={image.assetId} id={`anh-${index + 1}`} className="w-full shrink-0 snap-center scroll-mt-4">
            <div className="aspect-[4/5] p-6 sm:p-10"><CatalogImage image={image} priority={index === 0} sizes="(max-width: 1024px) 90vw, 600px" /></div>
            {image.altText && <figcaption className="px-6 pb-5 text-center text-sm text-[#56635b]">{image.altText}</figcaption>}
          </figure>) : <div className="aspect-[4/5] w-full"><CatalogImage image={null} /></div>}
        </div>
        {gallery.length > 1 && <nav aria-label="Chọn ảnh sản phẩm" className="mt-4 flex gap-3 overflow-x-auto pb-2">
          {gallery.map((image, index) => <a key={image.assetId} href={`#anh-${index + 1}`} aria-label={`Xem ảnh ${index + 1}: ${image.altText}`} className="block size-20 shrink-0 border border-[#d9d2c3] bg-[#eae4d8] p-1 focus-visible:outline-3"><CatalogImage image={image} sizes="80px" /></a>)}
        </nav>}
      </section>
      <section className="min-w-0 lg:pt-5">
        <Link className="sf-kicker hover:underline" href={`/danh-muc/${product.primaryCategory.slug}`}>{product.primaryCategory.name}</Link>
        <h1 className="sf-heading mt-5">{product.name}</h1>
        {(product.brand || product.sku) && <div className="mt-5 flex flex-wrap gap-x-5 gap-y-2 text-sm text-[#56635b]">{product.brand && <span>Thương hiệu: {product.brand.name}</span>}{product.sku && <span>Mã sản phẩm: {product.sku}</span>}</div>}
        {product.summary && <p className="sf-copy mt-7 whitespace-pre-line">{product.summary}</p>}
        <div className="mt-8 border-l-[3px] border-[#b79b6c] bg-[#efeae0] p-6 sm:p-8">
          <h2 className="font-serif text-2xl text-[#233b32]">Tìm hiểu thêm về sản phẩm</h2>
          <p className="mt-3 text-sm leading-7 text-[#56635b]">Chọn kênh liên hệ đã được công bố để trao đổi về sản phẩm này.</p>
          <div className="mt-6"><ContactLinks /></div>
        </div>
        {specs.length > 0 && <div className="mt-10"><h2 className="font-serif text-2xl text-[#233b32]">Thông tin sản phẩm</h2><dl className="mt-5 divide-y divide-[#d9d2c3] border-y border-[#d9d2c3]">
          {specs.map(([key, value]) => <div key={key} className="grid grid-cols-[minmax(0,.85fr)_minmax(0,1.15fr)] gap-4 py-4 text-sm"><dt className="text-[#56635b]">{key}</dt><dd className="break-words text-right font-medium text-[#233b32]">{value}</dd></div>)}
        </dl></div>}
      </section>
    </div>
    <section className="sf-section mt-10 border-t border-[#d9d2c3]"><div className="grid gap-8 lg:grid-cols-[minmax(0,.3fr)_minmax(0,.7fr)] lg:gap-20"><h2 className="font-serif text-3xl text-[#233b32]">Chi tiết sản phẩm</h2><div className="sf-richtext" dangerouslySetInnerHTML={{ __html: product.descriptionHtml }} /></div></section>
    {product.categories.length > 1 && <nav aria-label="Danh mục liên quan" className="flex flex-wrap gap-3 border-t border-[#d9d2c3] py-8">{product.categories.map((category) => <Link key={category.slug} href={`/danh-muc/${category.slug}`} className="sf-button sf-button-outline">{category.name}</Link>)}</nav>}
    {related.length > 0 && <section className="sf-section border-t border-[#d9d2c3]"><div className="mb-10 flex flex-wrap items-end justify-between gap-5"><div><p className="sf-kicker">Cùng danh mục</p><h2 className="sf-heading mt-3">Khám phá thêm</h2></div><Link href={`/danh-muc/${product.primaryCategory.slug}`} className="sf-text-link">Xem danh mục <span aria-hidden="true">↗</span></Link></div><ProductGrid products={related} headingLevel={3} /></section>}
  </main>;
}
