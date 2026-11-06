import Link from "next/link";
import Image from "next/image";
import { notFound } from "next/navigation";
import { getPublicArticle, getPublicArticles } from "@/lib/api/articles";
import { getSiteContext } from "@/lib/config/site-context";
import { breadcrumbJsonLd, canonicalUrl, pageMetadata, serializeJsonLd } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";
import { ArticleCard } from "@/components/storefront/article-card";

type Props = Readonly<{ params: Promise<{ slug: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> }>;
async function requireArticle(slug: string) {
  const article = await getPublicArticle(slug);
  if (!article) notFound();
  return article;
}
export async function generateMetadata({ params, searchParams }: Props) {
  const article = await requireArticle((await params).slug);
  const { site } = await getSiteContext();
  const image = article.ogImageUrl && article.ogImageWidth && article.ogImageHeight
    ? { url: article.ogImageUrl, width: article.ogImageWidth, height: article.ogImageHeight, alt: article.title }
    : article.coverUrl && article.coverWidth && article.coverHeight
      ? { url: article.coverUrl, width: article.coverWidth, height: article.coverHeight, alt: article.title }
      : undefined;
  return pageMetadata(site, { path: `/kien-thuc/${article.slug}`, title: article.title,
    description: article.excerpt, seo: article.seo, index: Object.keys(await searchParams).length === 0,
    image,
  });
}
export default async function ArticlePage({ params }: Props) {
  const article = await requireArticle((await params).slug);
  const [{ site }, moreArticles] = await Promise.all([getSiteContext(), getPublicArticles(1, 4)]);
  const path = `/kien-thuc/${article.slug}`;
  const crumbs = [{ name: "Trang chủ", path: "/" }, { name: "Kiến thức", path: "/kien-thuc" }, { name: article.title, path }];
  const articleJsonLd = { "@context": "https://schema.org", "@type": "Article", headline: article.title,
    description: article.excerpt, mainEntityOfPage: canonicalUrl(site, path, article.seo.canonicalOverride),
    datePublished: article.publishedAt, dateModified: article.updatedAtUtc,
    ...(article.coverUrl ? { image: article.coverUrl } : {}) };
  const more = moreArticles.items.filter((item) => item.slug !== article.slug).slice(0, 3);
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(articleJsonLd) }} />
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(site, crumbs)) }} />
    <Breadcrumbs items={crumbs} />
    <article><header className="mx-auto max-w-4xl border-b border-[#d9d2c3] pb-10 text-center"><p className="sf-kicker">Kiến thức trà cụ</p><h1 className="sf-heading mt-5">{article.title}</h1><p className="mx-auto mt-7 max-w-2xl text-lg leading-8 text-[#56635b]">{article.excerpt}</p><time dateTime={article.publishedAt} className="mt-6 block text-sm text-[#56635b]">Đăng ngày {new Date(article.publishedAt).toLocaleDateString("vi-VN")}</time></header>
      {article.coverUrl && article.coverWidth && article.coverHeight && <figure className="mx-auto mt-10 max-w-5xl overflow-hidden bg-[#eae4d8]"><Image src={article.coverUrl} alt={article.title} width={article.coverWidth} height={article.coverHeight} sizes="(max-width: 768px) 92vw, 1024px" preload className="h-auto w-full" /></figure>}
      <div className="sf-richtext mx-auto mt-12 max-w-[46rem]" dangerouslySetInnerHTML={{ __html: article.contentHtml }} />
      {article.categories.length > 0 && <div className="mx-auto mt-12 flex max-w-[46rem] flex-wrap gap-3 border-t border-[#d9d2c3] pt-6" aria-label="Danh mục bài viết">{article.categories.map((category) => <span key={category.slug} className="border border-[#d9d2c3] px-4 py-2 text-sm text-[#56635b]">{category.name}</span>)}</div>}
      <div className="mx-auto mt-9 max-w-[46rem]"><Link href="/kien-thuc" className="sf-text-link">← Xem các bài viết</Link></div>
    </article>
    {more.length > 0 && <section className="sf-section mt-16 border-t border-[#d9d2c3]"><div className="mb-10 flex flex-wrap items-end justify-between gap-5"><div><p className="sf-kicker">Đọc tiếp</p><h2 className="sf-heading mt-3">Bài viết khác</h2></div><Link href="/kien-thuc" className="sf-text-link">Xem tất cả <span aria-hidden="true">↗</span></Link></div><div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">{more.map((item) => <ArticleCard key={item.slug} article={item} headingLevel={3} />)}</div></section>}
  </main>;
}
