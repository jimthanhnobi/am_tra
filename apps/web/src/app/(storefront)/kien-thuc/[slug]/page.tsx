import Link from "next/link";
import Image from "next/image";
import { notFound } from "next/navigation";
import { getPublicArticle } from "@/lib/api/articles";
import { getSiteContext } from "@/lib/config/site-context";
import { breadcrumbJsonLd, canonicalUrl, pageMetadata, serializeJsonLd } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";

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
  const { site } = await getSiteContext();
  const path = `/kien-thuc/${article.slug}`;
  const crumbs = [{ name: "Trang chủ", path: "/" }, { name: "Kiến thức", path: "/kien-thuc" }, { name: article.title, path }];
  const articleJsonLd = { "@context": "https://schema.org", "@type": "Article", headline: article.title,
    description: article.excerpt, mainEntityOfPage: canonicalUrl(site, path, article.seo.canonicalOverride),
    datePublished: article.publishedAt, dateModified: article.updatedAtUtc,
    ...(article.coverUrl ? { image: article.coverUrl } : {}) };
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(articleJsonLd) }} />
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(breadcrumbJsonLd(site, crumbs)) }} />
    <Breadcrumbs items={crumbs} />
    <article className="mx-auto max-w-3xl"><header><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Kiến thức</p><h1 className="mt-4 font-serif text-4xl leading-tight sm:text-5xl">{article.title}</h1><p className="mt-6 text-lg leading-8 text-[#66736c]">{article.excerpt}</p><time dateTime={article.publishedAt} className="mt-5 block text-sm text-[#66736c]">Đăng ngày {new Date(article.publishedAt).toLocaleDateString("vi-VN")}</time></header>
      {article.coverUrl && article.coverWidth && article.coverHeight && <figure className="mt-9"><Image src={article.coverUrl} alt={article.title} width={article.coverWidth} height={article.coverHeight} sizes="(max-width: 768px) 92vw, 768px" priority className="h-auto w-full" /></figure>}
      <div className="catalog-prose mt-10" dangerouslySetInnerHTML={{ __html: article.contentHtml }} />
      <nav aria-label="Danh mục bài viết" className="mt-10 flex flex-wrap gap-3">{article.categories.map((category) => <span key={category.slug} className="rounded-full border border-[#c9c0b1] px-4 py-2 text-sm">{category.name}</span>)}</nav>
      <Link href="/kien-thuc" className="mt-10 inline-block text-sm text-[#8d4f34]">← Xem các bài viết</Link>
    </article>
  </main>;
}
