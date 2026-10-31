import Link from "next/link";
import { notFound } from "next/navigation";
import { getPublicProduct } from "@/lib/api/public";
import { siteConfig } from "@/lib/config/site";
import { pageMetadata, breadcrumbJsonLd, productJsonLd, serializeJsonLd } from "@/lib/seo/policy";
import { Breadcrumbs, CatalogImage, contentWidth } from "@/components/storefront/catalog";
import { ContactLinks } from "@/components/storefront/contact";

type Props = Readonly<{ params: Promise<{ slug: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }>;

async function requireProduct(slug: string) {
  const product = await getPublicProduct(slug);
  if (!product) notFound();
  return product;
}

export async function generateMetadata({ params, searchParams }: Props) {
  const product = await requireProduct((await params).slug);
  const image = product.media.find((item) => item.assetId === product.seo.ogImageAssetId && item.publicUrl) || product.media.find((item) => item.isCover && item.publicUrl);
  return pageMetadata(siteConfig, {
    path: `/san-pham/${product.slug}`, title: product.name, description: product.summary, seo: product.seo,
    index: Object.keys(await searchParams).length === 0,
    image: image?.publicUrl ? { url: image.publicUrl, width: image.width, height: image.height, alt: image.altText } : undefined,
  });
}

export default async function ProductPage({ params }: Props) {
  const product = await requireProduct((await params).slug);
  const crumbs = [{ name: "Trang chủ", path: "/" }, { name: product.primaryCategory.name, path: `/danh-muc/${product.primaryCategory.slug}` }, { name: product.name, path: `/san-pham/${product.slug}` }];
  const gallery = [...product.media].sort((a, b) => Number(b.isCover) - Number(a.isCover) || a.sortOrder - b.sortOrder);
  const specs = [
    ...(product.materialText ? [["Chất liệu", product.materialText]] : []),
    ...(product.capacityMl ? [["Dung tích", `${product.capacityMl} ml`]] : []),
    ...(product.brand?.origin ? [["Xuất xứ thương hiệu", product.brand.origin]] : []),
    ...Object.entries(product.specifications).filter(([key]) => !["capacityMl", "materialText"].includes(key)).map(([key, value]) => [key, typeof value === "boolean" ? value ? "Có" : "Không" : String(value)]),
  ];
  return <main id="noi-dung" className={`${contentWidth} py-9 sm:py-12`}>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(productJsonLd(siteConfig, product)) }} />
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(siteConfig, crumbs)) }} />
    <Breadcrumbs items={crumbs} />
    <div className="grid items-start gap-10 lg:grid-cols-[1.15fr_1fr] lg:gap-16">
      <section aria-label="Hình ảnh sản phẩm" className="min-w-0"><div className="flex snap-x snap-mandatory overflow-x-auto rounded-sm border border-[#d8d1c3] bg-[#eeeadf]">{gallery.length ? gallery.map((image, index) => <figure key={image.assetId} id={`anh-${index + 1}`} className="w-full shrink-0 snap-center scroll-mt-4"><div className="aspect-square p-5 sm:p-8"><CatalogImage image={image} priority={index === 0} sizes="(max-width: 1024px) 90vw, 600px" /></div>{image.altText && <figcaption className="px-5 pb-4 text-center text-xs text-[#66736c]">{image.altText}</figcaption>}</figure>) : <div className="aspect-square w-full"><CatalogImage image={null} /></div>}</div>{gallery.length > 1 && <nav aria-label="Chọn ảnh sản phẩm" className="mt-4 flex gap-3 overflow-x-auto">{gallery.map((image, index) => <a key={image.assetId} href={`#anh-${index + 1}`} aria-label={`Xem ảnh ${index + 1}: ${image.altText}`} className="block size-20 shrink-0 overflow-hidden border border-[#d8d1c3] bg-[#eeeadf] p-1"><CatalogImage image={image} sizes="80px" /></a>)}</nav>}</section>
      <section><Link className="text-xs uppercase tracking-[0.2em] text-[#8d4f34]" href={`/danh-muc/${product.primaryCategory.slug}`}>{product.primaryCategory.name}</Link><h1 className="mt-4 font-serif text-4xl leading-tight text-[#183c31] sm:text-5xl">{product.name}</h1>{product.brand && <p className="mt-4 text-sm text-[#66736c]">Thương hiệu: {product.brand.name}</p>}{product.sku && <p className="mt-2 text-xs text-[#66736c]">Mã sản phẩm: {product.sku}</p>}<p className="mt-7 whitespace-pre-line text-base leading-8 text-[#536159]">{product.summary}</p><div className="mt-8 border-y border-[#d8d1c3] py-7"><h2 className="mb-4 font-serif text-xl">Bạn quan tâm đến sản phẩm này?</h2><ContactLinks /></div>{specs.length > 0 && <div className="mt-8"><h2 className="font-serif text-2xl">Thông số</h2><dl className="mt-4 divide-y divide-[#d8d1c3]">{specs.map(([key, value]) => <div key={key} className="grid grid-cols-2 gap-4 py-3 text-sm"><dt className="text-[#66736c]">{key}</dt><dd className="break-words text-right">{value}</dd></div>)}</dl></div>}</section>
    </div>
    <section className="mt-14 max-w-3xl border-t border-[#d8d1c3] pt-10"><h2 className="mb-6 font-serif text-3xl">Chi tiết sản phẩm</h2><div className="catalog-prose" dangerouslySetInnerHTML={{ __html: product.descriptionHtml }} /></section>
    {product.categories.length > 1 && <nav aria-label="Danh mục liên quan" className="mt-10 flex flex-wrap gap-3">{product.categories.map((category) => <Link key={category.slug} href={`/danh-muc/${category.slug}`} className="rounded-full border border-[#c9c0b1] px-4 py-2 text-sm">{category.name}</Link>)}</nav>}
  </main>;
}
