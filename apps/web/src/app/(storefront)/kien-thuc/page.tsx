import Link from "next/link";
import Image from "next/image";
import { notFound } from "next/navigation";
import { getPublicArticles } from "@/lib/api/articles";
import { siteConfig } from "@/lib/config/site";
import { pageMetadata, parsePage } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth } from "@/components/storefront/catalog";

type Props = Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>;
async function read(props: Props) {
  const query = await props.searchParams;
  const page = parsePage(query.page);
  if (page === null) notFound();
  const result = await getPublicArticles(page);
  if (page > Math.max(1, result.totalPages)) notFound();
  return { query, page, result };
}

export async function generateMetadata(props: Props) {
  const { query, page, result } = await read(props);
  return pageMetadata(siteConfig, { path: `/kien-thuc${page > 1 ? `?page=${page}` : ""}`,
    title: `Kiến thức${page > 1 ? ` — Trang ${page}` : ""}`,
    description: "Các bài viết đã xuất bản về trà cụ và thưởng trà.",
    index: result.totalItems > 0 && Object.keys(query).every((key) => key === "page"),
  });
}

export default async function ArticlesPage(props: Props) {
  const { page, result } = await read(props);
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Kiến thức", path: "/kien-thuc" }]} />
    <header className="mb-10 max-w-3xl"><p className="text-xs uppercase tracking-[0.24em] text-[#8d4f34]">Bài viết</p><h1 className="mt-4 font-serif text-4xl sm:text-5xl">Kiến thức{page > 1 ? ` — Trang ${page}` : ""}</h1><p className="mt-5 text-[#66736c]">Nội dung được biên tập và xuất bản trên website.</p></header>
    {result.items.length ? <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">{result.items.map((article) => <article key={article.slug} className="border border-[#d8d1c3] bg-[#fbf8f1]">
      {article.coverUrl && article.coverWidth && article.coverHeight && <Link href={`/kien-thuc/${article.slug}`} className="block aspect-[3/2] overflow-hidden"><Image src={article.coverUrl} alt="" width={article.coverWidth} height={article.coverHeight} sizes="(max-width: 640px) 90vw, 32vw" className="h-full w-full object-cover" /></Link>}
      <div className="p-6"><time dateTime={article.publishedAt} className="text-xs text-[#66736c]">{new Date(article.publishedAt).toLocaleDateString("vi-VN")}</time><h2 className="mt-3 font-serif text-2xl"><Link href={`/kien-thuc/${article.slug}`}>{article.title}</Link></h2><p className="mt-3 text-sm leading-7 text-[#66736c]">{article.excerpt}</p></div>
    </article>)}</div> : <p className="rounded border border-[#d8d1c3] p-8 text-[#66736c]">Chưa có bài viết được xuất bản.</p>}
    {result.totalPages > 1 && <nav aria-label="Phân trang bài viết" className="mt-10 flex gap-4">{page > 1 && <Link href={page === 2 ? "/kien-thuc" : `/kien-thuc?page=${page - 1}`}>← Trang trước</Link>}{page < result.totalPages && <Link href={`/kien-thuc?page=${page + 1}`}>Trang sau →</Link>}</nav>}
  </main>;
}
