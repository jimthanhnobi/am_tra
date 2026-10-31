import Image from "next/image";
import Link from "next/link";
import type { PublicImage, PublicPage, PublicProductCard } from "@/lib/api/public-types";
import type { Crumb } from "@/lib/seo/policy";

export const contentWidth = "sf-container";

export function CatalogImage({ image, priority = false, sizes = "(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw" }: Readonly<{ image: PublicImage | null; priority?: boolean; sizes?: string }>) {
  return image?.publicUrl
    ? <Image src={image.publicUrl} alt={image.altText} width={image.width} height={image.height} sizes={sizes} preload={priority} className="h-full w-full object-contain" />
    : <div className="sf-image-empty"><span>Hình ảnh đang được cập nhật</span></div>;
}

export function ProductGrid({ products, headingLevel = 2 }: Readonly<{ products: readonly PublicProductCard[]; headingLevel?: 2 | 3 }>) {
  if (!products.length) return <EmptyCatalog />;
  const Heading = headingLevel === 2 ? "h2" : "h3";
  return <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:gap-7">
    {products.map((product) => <article key={product.slug} className="sf-card group min-w-0">
      <Link href={`/san-pham/${product.slug}`} className="sf-card-media block aspect-[4/5] p-5 sm:p-7">
        <span className="sr-only">{product.name}</span>
        <CatalogImage image={product.coverImage} sizes="(max-width: 640px) 90vw, (max-width: 1024px) 45vw, 30vw" />
      </Link>
      <div className="flex min-h-52 flex-col p-6">
        <p className="sf-kicker">{product.primaryCategory?.name || "Trà cụ"}</p>
        <Heading className="mt-3 font-serif text-[1.45rem] leading-snug text-[#233b32]"><Link href={`/san-pham/${product.slug}`} className="hover:underline">{product.name}</Link></Heading>
        {product.summary && <p className="mt-3 line-clamp-2 text-sm leading-7 text-[#56635b]">{product.summary}</p>}
        <Link href={`/san-pham/${product.slug}`} className="sf-text-link mt-auto pt-5">Xem chi tiết <span aria-hidden="true">↗</span></Link>
      </div>
    </article>)}
  </div>;
}

export function EmptyCatalog({ title = "Bộ sưu tập đang được hoàn thiện", children = "Các sản phẩm được xuất bản sẽ xuất hiện tại đây." }: Readonly<{ title?: string; children?: React.ReactNode }>) {
  return <div className="sf-empty"><div aria-hidden="true" className="mx-auto mb-6 size-14 rounded-full border border-[#92794f]" /><h2 className="font-serif text-2xl text-[#233b32] sm:text-3xl">{title}</h2><p className="mx-auto mt-4 max-w-lg text-sm leading-7 text-[#56635b]">{children}</p></div>;
}

export function Breadcrumbs({ items }: Readonly<{ items: readonly Crumb[] }>) {
  return <nav aria-label="Đường dẫn" className="mb-9 text-sm text-[#56635b]"><ol className="flex flex-wrap items-center gap-x-2 gap-y-1">
    {items.map((item, index) => <li key={item.path} className="flex min-w-0 items-center gap-2">
      {index > 0 && <span aria-hidden="true" className="text-[#92794f]">/</span>}
      {index === items.length - 1 ? <span aria-current="page" className="max-w-[22rem] truncate text-[#233b32]">{item.name}</span>
        : <Link href={item.path} className="underline-offset-4 hover:underline">{item.name}</Link>}
    </li>)}
  </ol></nav>;
}

export function Pagination({ data, path, query = {} }: Readonly<{ data: Pick<PublicPage<unknown>, "page" | "totalPages">; path: string; query?: Record<string, string> }>) {
  if (data.totalPages < 2) return null;
  function href(page: number) { const params = new URLSearchParams(query); if (page > 1) params.set("page", String(page)); return `${path}${params.size ? `?${params}` : ""}`; }
  return <nav aria-label="Phân trang" className="mt-14 flex flex-wrap items-center justify-center gap-4 border-t border-[#d9d2c3] pt-8 text-sm">
    {data.page > 1 && <Link rel="prev" href={href(data.page - 1)} className="sf-button sf-button-outline">← Trang trước</Link>}
    <span className="px-2 text-[#56635b]">Trang {data.page} / {data.totalPages}</span>
    {data.page < data.totalPages && <Link rel="next" href={href(data.page + 1)} className="sf-button sf-button-outline">Trang sau →</Link>}
  </nav>;
}
