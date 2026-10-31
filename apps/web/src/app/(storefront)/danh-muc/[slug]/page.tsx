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
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(site, crumbs)) }} />
    <Breadcrumbs items={crumbs} />
    <header className="mb-12 border-b border-[#d9d2c3] pb-10 sm:mb-14 sm:pb-12">
      <p className="sf-kicker">Bộ sưu tập</p>
      <h1 className="sf-heading mt-4">{category.name}{page > 1 ? ` — Trang ${page}` : ""}</h1>
      {category.descriptionHtml && <div className="sf-richtext mt-6 max-w-2xl text-base!" dangerouslySetInnerHTML={{ __html: category.descriptionHtml }} />}
      <p className="mt-8 text-sm font-medium text-[#56635b]">{category.products.totalItems} sản phẩm được giới thiệu</p>
    </header>
    <ProductGrid products={category.products.items} />
    <Pagination data={category.products} path={`/danh-muc/${category.slug}`} />
  </main>;
}
