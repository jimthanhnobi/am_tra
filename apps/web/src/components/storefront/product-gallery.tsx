"use client";

import { useState } from "react";
import { ChevronLeft, ChevronRight, Images } from "lucide-react";
import type { PublicImage } from "@/lib/api/public-types";
import { CatalogImage } from "@/components/storefront/catalog";

export function ProductGallery({ images, name }: Readonly<{ images: readonly PublicImage[]; name: string }>) {
  const [active, setActive] = useState(0);
  const selected = images[active] ?? null;
  function select(next: number) { setActive((next + images.length) % images.length); }

  return <section aria-label={`Bộ ảnh ${name}`} className="min-w-0">
    <div className="relative border border-[#d9d2c3] bg-[#eae4d8]">
      <figure className="relative">
        <div className="aspect-[4/5] overflow-hidden p-7 sm:aspect-[5/6] sm:p-12">
          <CatalogImage image={selected} priority sizes="(max-width: 900px) 92vw, 600px" />
        </div>
        {selected?.altText && <figcaption className="px-5 pb-6 text-center text-xs leading-6 text-[#56635b]">{selected.altText}</figcaption>}
      </figure>
      {images.length > 1 && <>
        <button type="button" aria-label="Ảnh sản phẩm trước" onClick={() => select(active - 1)}
          className="absolute top-1/2 left-3 grid size-11 -translate-y-1/2 place-items-center border border-[#d9d2c3] bg-[#fffcf6] text-[#233b32] shadow-sm sm:left-5">
          <ChevronLeft size={19} aria-hidden="true" />
        </button>
        <button type="button" aria-label="Ảnh sản phẩm tiếp theo" onClick={() => select(active + 1)}
          className="absolute top-1/2 right-3 grid size-11 -translate-y-1/2 place-items-center border border-[#d9d2c3] bg-[#fffcf6] text-[#233b32] shadow-sm sm:right-5">
          <ChevronRight size={19} aria-hidden="true" />
        </button>
      </>}
      <span className="absolute top-4 right-4 inline-flex items-center gap-2 bg-[#fffcf6] px-3 py-2 text-xs text-[#233b32]">
        <Images size={15} aria-hidden="true" /> {images.length ? `${active + 1} / ${images.length}` : "Chưa có ảnh"}
      </span>
    </div>
    {images.length > 1 && <div role="group" aria-label="Chọn ảnh sản phẩm" className="mt-4 flex gap-3 overflow-x-auto pb-2">
      {images.map((image, index) => <button
        key={image.assetId} type="button"
        onClick={() => setActive(index)}
        aria-label={`Xem ảnh ${index + 1}: ${image.altText || name}`}
        aria-pressed={index === active}
        className={`size-20 shrink-0 overflow-hidden border-2 bg-[#eae4d8] p-1 transition-colors sm:size-24 ${index === active ? "border-[#925b3d]" : "border-[#d9d2c3] hover:border-[#b79b6c]"}`}>
        <CatalogImage image={image} sizes="96px" />
      </button>)}
    </div>}
    <p className="mt-3 text-xs leading-6 text-[#56635b]">Hình ảnh được lấy từ thư viện sản phẩm đã công bố. Xem thêm thông tin bên dưới để lựa chọn phù hợp.</p>
  </section>;
}
