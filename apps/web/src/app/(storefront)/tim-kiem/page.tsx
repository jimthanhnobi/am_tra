import { notFound } from "next/navigation";
import { getPublicProducts } from "@/lib/api/public";
import { getSiteContext } from "@/lib/config/site-context";
import { pageMetadata, parsePage } from "@/lib/seo/policy";
import { contentWidth, EmptyCatalog, Pagination, ProductGrid } from "@/components/storefront/catalog";

export async function generateMetadata() { const { site } = await getSiteContext(); return pageMetadata(site, { path: "/tim-kiem", title: "Tìm kiếm trà cụ", description: "Tìm sản phẩm theo tên, mã sản phẩm hoặc đường dẫn.", index: false }); }

export default async function SearchPage({ searchParams }: Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>) {
  const query = await searchParams;
  const page = parsePage(query.page);
  if (page === null) notFound();
  const validQuery = query.q === undefined || typeof query.q === "string" && query.q.trim().length <= 120;
  const q = typeof query.q === "string" ? query.q.trim() : "";
  const products = validQuery ? await getPublicProducts(page, 12, q) : null;
  if (products && page > Math.max(1, products.totalPages)) notFound();
  return <main id="noi-dung" className={`${contentWidth} py-12`}><header className="mx-auto mb-12 max-w-2xl"><h1 className="font-serif text-4xl">Tìm kiếm trà cụ</h1><form action="/tim-kiem" method="get" className="mt-6 flex flex-wrap gap-3"><label htmlFor="q" className="sr-only">Tên, SKU hoặc slug sản phẩm</label><input id="q" type="search" name="q" defaultValue={q} maxLength={120} placeholder="Tên, mã sản phẩm…" className="min-w-48 flex-1 rounded-full border border-[#c9c0b1] bg-[#fbf8f1] px-5 py-3 outline-offset-4" /><button type="submit" className="rounded-full bg-[#183c31] px-6 py-3 text-sm text-[#fbf8f1]">Tìm kiếm</button></form>{!validQuery && <p role="alert" className="mt-3 text-sm text-[#a33d32]">Từ khóa phải là văn bản, tối đa 120 ký tự.</p>}{products && <p className="mt-5 text-sm text-[#66736c]">{q ? `${products.totalItems} kết quả cho “${q}”` : `${products.totalItems} sản phẩm trong bộ sưu tập`}</p>}</header>{products && (products.items.length ? <ProductGrid products={products.items} /> : <EmptyCatalog title="Chưa tìm thấy sản phẩm">Thử một từ khóa khác hoặc quay lại khi bộ sưu tập được cập nhật.</EmptyCatalog>)}{products && <Pagination data={products} path="/tim-kiem" query={q ? { q } : {}} />}</main>;
}
