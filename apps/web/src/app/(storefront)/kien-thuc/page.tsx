import { notFound } from "next/navigation";
import { getPublicArticles } from "@/lib/api/articles";
import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata, parsePage } from "@/lib/seo/policy";
import { Breadcrumbs, contentWidth, Pagination } from "@/components/storefront/catalog";
import { ArticleCard } from "@/components/storefront/article-card";

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
  const { site } = await getSiteContext();
  return pageMetadata(site, { path: `/kien-thuc${page > 1 ? `?page=${page}` : ""}`,
    title: `Kiến thức${page > 1 ? ` — Trang ${page}` : ""}`,
    description: "Các bài viết đã xuất bản về trà cụ và thưởng trà.",
    index: result.totalItems > 0 && Object.keys(query).every((key) => key === "page"),
  });
}

export default async function ArticlesPage(props: Props) {
  const { page, result } = await read(props);
  return <main id="noi-dung" className={`${contentWidth} py-10 sm:py-14`}>
    <Breadcrumbs items={[{ name: "Trang chủ", path: "/" }, { name: "Kiến thức", path: "/kien-thuc" }]} />
    <header className="sf-page-intro mb-12">
      <div><p className="sf-kicker">Góc đọc</p><h1 className="sf-heading mt-4">Kiến thức trà cụ{page > 1 ? ` — Trang ${page}` : ""}</h1></div>
      <p className="sf-copy sf-page-intro-description">Góc đọc về trà cụ, cách chăm sóc và những câu chuyện bên chén trà. Chỉ những bài viết đã công bố mới hiển thị.</p>
    </header>
    {result.items.length ? <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:gap-7">{result.items.map((article) => <ArticleCard key={article.slug} article={article} />)}</div>
      : <div className="sf-empty"><h2 className="font-serif text-2xl text-[#233b32]">Chưa có bài viết được xuất bản</h2><p className="mt-4 text-sm leading-7 text-[#56635b]">Nội dung sẽ xuất hiện tại đây sau khi được công bố.</p></div>}
    <Pagination data={result} path="/kien-thuc" />
  </main>;
}
