import Image from "next/image";
import Link from "next/link";
import type { PublicImage, PublicPage, PublicProductCard } from "@/lib/api/public-types";
import type { Crumb } from "@/lib/seo/policy";

export const contentWidth = "mx-auto max-w-7xl px-5 sm:px-8 lg:px-10";

export function CatalogImage({ image, priority = false, sizes = "(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw" }: Readonly<{ image: PublicImage | null; priority?: boolean; sizes?: string }>) {
  return image?.publicUrl ? <Image src={image.publicUrl} alt={image.altText} width={image.width} height={image.height} sizes={sizes} preload={priority} className="h-full w-full object-contain" /> : <div className="grid h-full min-h-48 place-items-center bg-[#eee9df] px-6 text-center text-sm text-[#718077]">Hình ảnh đang được cập nhật</div>;
}

export function ProductGrid({ products, headingLevel = 2 }: Readonly<{ products: readonly PublicProductCard[]; headingLevel?: 2 | 3 }>) {
  if (!products.length) return <EmptyCatalog />;
  const Heading = headingLevel === 2 ? "h2" : "h3";
  return <div className="grid gap-x-7 gap-y-10 sm:grid-cols-2 lg:grid-cols-3">{products.map((product) => <article key={product.slug} className="group">
    <Link href={`/san-pham/${product.slug}`} className="block overflow-hidden rounded-sm border border-[#d8d1c3]/70 bg-[#eeeadf] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#183c31]">
      <div className="aspect-[4/3] p-4 transition duration-300 group-hover:scale-[1.02]"><CatalogImage image={product.coverImage} /></div>
    </Link>
    <p className="mt-5 text-xs uppercase tracking-[0.16em] text-[#8d4f34]">{product.primaryCategory?.name || "Trà cụ"}</p>
    <Heading className="mt-2 font-serif text-2xl leading-snug"><Link className="hover:underline" href={`/san-pham/${product.slug}`}>{product.name}</Link></Heading>
    {product.summary && <p className="mt-3 line-clamp-3 text-sm leading-7 text-[#66736c]">{product.summary}</p>}
    <Link href={`/san-pham/${product.slug}`} className="mt-4 inline-block border-b border-[#8d4f34]/40 pb-1 text-sm text-[#8d4f34]">Khám phá sản phẩm <span aria-hidden="true">↗</span></Link>
  </article>)}</div>;
}

export function EmptyCatalog({ title = "Bộ sưu tập đang được hoàn thiện", children = "Những sản phẩm đã sẵn sàng sẽ được giới thiệu tại đây." }: Readonly<{ title?: string; children?: React.ReactNode }>) {
  return <div className="border-y border-[#d8d1c3] py-16 text-center"><p className="mb-4 font-serif text-4xl text-[#8d4f34]" aria-hidden="true">葉</p><h2 className="font-serif text-2xl">{title}</h2><p className="mx-auto mt-4 max-w-lg text-sm leading-7 text-[#66736c]">{children}</p></div>;
}

export function Breadcrumbs({ items }: Readonly<{ items: readonly Crumb[] }>) {
  return <nav aria-label="Đường dẫn" className="mb-8 text-sm text-[#66736c]"><ol className="flex flex-wrap items-center gap-2">{items.map((item, index) => <li key={item.path} className="flex items-center gap-2">{index > 0 && <span aria-hidden="true">/</span>}{index === items.length - 1 ? <span aria-current="page">{item.name}</span> : <Link href={item.path} className="hover:underline">{item.name}</Link>}</li>)}</ol></nav>;
}

export function Pagination({ data, path, query = {} }: Readonly<{ data: Pick<PublicPage<unknown>, "page" | "totalPages">; path: string; query?: Record<string, string> }>) {
  if (data.totalPages < 2) return null;
  function href(page: number) { const params = new URLSearchParams(query); if (page > 1) params.set("page", String(page)); return `${path}${params.size ? `?${params}` : ""}`; }
  return <nav aria-label="Phân trang" className="mt-12 flex flex-wrap items-center justify-center gap-4 border-t border-[#d8d1c3] pt-8 text-sm">{data.page > 1 && <Link rel="prev" href={href(data.page - 1)} className="rounded-full border border-[#c9c0b1] px-5 py-3">← Trang trước</Link>}<span>Trang {data.page} / {data.totalPages}</span>{data.page < data.totalPages && <Link rel="next" href={href(data.page + 1)} className="rounded-full border border-[#c9c0b1] px-5 py-3">Trang sau →</Link>}</nav>;
}
