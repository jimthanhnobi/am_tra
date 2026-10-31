import { notFound } from "next/navigation";
import { getPublicCategory } from "@/lib/api/public";
import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata, parsePage, plainText, breadcrumbJsonLd, serializeJsonLd } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth, Pagination, ProductGrid } from "@/components/storefront/catalog";

type Props = Readonly<{ params: Promise<{ slug: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }>;
async function read({ params, searchParams }: Props) {
  const query = await searchParams;
  const page = parsePage(query.page);
  if (page === null) notFound();
  const category = await getPublicCategory((await params).slug, page);
  if (!category || page > Math.max(1, category.products.totalPages)) notFound();
  return { category, page, query };
}

export async function generateMetadata(props: Props) {
  const { category, page, query } = await read(props);
  const { site } = await getSiteContext();
  const suffix = page > 1 ? ` — Trang ${page}` : "";
  const description = category.seo.metaDescription || plainText(category.descriptionHtml).slice(0, 180) || `Khám phá ${category.name} tại ${site.name}.`;
  return pageMetadata(site, {
    path: `/danh-muc/${category.slug}${page > 1 ? `?page=${page}` : ""}`,
    title: category.name + suffix, description: description + suffix,
    seo: { ...category.seo, seoTitle: (category.seo.seoTitle || category.name) + suffix, metaDescription: description + suffix, canonicalOverride: page === 1 ? category.seo.canonicalOverride : null },
    index: Object.keys(query).every((key) => key === "page") && (page === 1 || !category.seo.canonicalOverride),
  });
}

export default async function CategoryPage(props: Props) {
  const { category, page } = await read(props);
  const { site } = await getSiteContext();
  const crumbs = [{ name: "Trang chủ", path: "/" }, { name: category.name, path: `/danh-muc/${category.slug}` }];
  return <main id="noi-dung" className={`${contentWidth} py-9 sm:py-12`}><script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(site, crumbs)) }} /><Breadcrumbs items={crumbs} /><header className="mb-12 max-w-3xl"><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Bộ sưu tập</p><h1 className="mt-4 font-serif text-4xl sm:text-5xl">{category.name}{page > 1 ? ` — Trang ${page}` : ""}</h1><div className="catalog-prose mt-6" dangerouslySetInnerHTML={{ __html: category.descriptionHtml }} /><p className="mt-5 text-sm text-[#66736c]">{category.products.totalItems} sản phẩm</p></header><ProductGrid products={category.products.items} /><Pagination data={category.products} path={`/danh-muc/${category.slug}`} /></main>;
}
