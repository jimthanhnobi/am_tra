import type { Metadata } from "next";
import { createPageMetadata } from "@/lib/seo/metadata";

export const metadata: Metadata = createPageMetadata({ path: "/" });

const foundations = [
  {
    index: "01",
    title: "Danh mục có ngữ cảnh",
    description: "Sản phẩm sẽ chỉ xuất hiện khi nội dung, hình ảnh và trạng thái xuất bản đã được kiểm duyệt.",
  },
  {
    index: "02",
    title: "Kiến thức để lựa chọn",
    description: "Không gian nội dung được chuẩn bị để kết nối câu chuyện chất liệu, cách dùng và bảo quản trà cụ.",
  },
  {
    index: "03",
    title: "Liên hệ đúng lúc",
    description: "Kênh tư vấn sẽ lấy từ cấu hình shop đã xác nhận, không dùng địa chỉ hay thông tin liên hệ giả.",
  },
] as const;

export default function HomePage() {
  return (
    <main>
      <section className="relative isolate border-b border-[#d8d1c3]">
        <div className="tea-grid absolute inset-0 -z-20" aria-hidden="true" />
        <div className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_72%_36%,rgba(141,79,52,0.16),transparent_32%),linear-gradient(120deg,rgba(243,239,230,0.78),rgba(243,239,230,0.98))]" aria-hidden="true" />

        <div className="mx-auto grid min-h-[700px] max-w-7xl items-center gap-12 px-6 py-20 lg:grid-cols-[1.15fr_0.85fr] lg:px-10 lg:py-28">
          <div>
            <p className="mb-7 text-xs font-semibold uppercase tracking-[0.3em] text-[#8d4f34]">
              TeaPremium · Nền tảng đang xây dựng
            </p>
            <h1 className="max-w-3xl font-serif text-5xl leading-[1.08] font-medium tracking-[-0.035em] text-[#183c31] sm:text-6xl lg:text-7xl">
              Không gian trà cụ được chuẩn bị cho những lựa chọn có chiều sâu.
            </h1>
            <p className="mt-8 max-w-2xl text-base leading-8 text-[#536159] sm:text-lg">
              Foundation kết nối catalog, nội dung kiến thức và hành trình tư vấn trong một trải nghiệm tiếng Việt tinh gọn — không giá công khai, không giỏ hàng, không đặt mua trực tuyến.
            </p>
            <div className="mt-10 flex flex-wrap items-center gap-4">
              <a className="rounded-full bg-[#183c31] px-6 py-3 text-sm font-medium text-[#fbf8f1] transition hover:bg-[#245244] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#183c31]" href="#dinh-huong">
                Xem định hướng
              </a>
              <span className="rounded-full border border-[#c9c0b1] bg-[#fbf8f1]/70 px-5 py-3 text-sm text-[#536159]">
                Foundation · Local-ready
              </span>
            </div>
          </div>

          <div className="relative mx-auto grid aspect-square w-full max-w-[480px] place-items-center" aria-hidden="true">
            <div className="absolute inset-[8%] rounded-full border border-[#183c31]/20" />
            <div className="absolute inset-[20%] rounded-full border border-[#8d4f34]/25" />
            <svg className="h-[58%] w-[58%] text-[#183c31]" viewBox="0 0 320 320" fill="none" role="presentation">
              <path d="M84 143c5 73 31 108 76 108s71-35 76-108H84Z" fill="currentColor" fillOpacity="0.1" stroke="currentColor" strokeWidth="3" />
              <path d="M111 142c0-25 19-45 49-45s49 20 49 45" stroke="currentColor" strokeWidth="3" />
              <path d="M145 95V77h30v18" stroke="currentColor" strokeWidth="3" />
              <path d="M236 151c31-4 43 9 37 28-5 18-21 27-42 24" stroke="currentColor" strokeWidth="3" />
              <path d="M84 153c-26 3-38-4-43-17 19-4 33-1 45 8" stroke="currentColor" strokeWidth="3" />
              <path d="M116 267h88" stroke="currentColor" strokeLinecap="round" strokeWidth="3" />
            </svg>
            <span className="absolute right-[6%] bottom-[18%] font-serif text-sm italic text-[#8d4f34]">tĩnh · chuẩn · thật</span>
          </div>
        </div>
      </section>

      <section id="dinh-huong" className="bg-[#fbf8f1] px-6 py-24 lg:px-10">
        <div className="mx-auto max-w-7xl">
          <div className="grid gap-8 border-b border-[#d8d1c3] pb-12 lg:grid-cols-[0.75fr_1.25fr]">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[#8d4f34]">Định hướng trải nghiệm</p>
            <h2 className="max-w-4xl font-serif text-3xl leading-tight font-medium text-[#183c31] sm:text-5xl">
              Nội dung đáng tin trước, kỹ thuật làm nền, tư vấn là bước tiếp theo.
            </h2>
          </div>

          <div className="grid gap-px overflow-hidden border-x border-b border-[#d8d1c3] bg-[#d8d1c3] md:grid-cols-3">
            {foundations.map((foundation) => (
              <article className="bg-[#fbf8f1] p-8 sm:p-10" key={foundation.index}>
                <span className="text-xs tracking-[0.24em] text-[#8d4f34]">{foundation.index}</span>
                <h3 className="mt-12 font-serif text-2xl font-medium text-[#183c31]">{foundation.title}</h3>
                <p className="mt-5 text-sm leading-7 text-[#66736c]">{foundation.description}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section id="foundation" className="bg-[#183c31] px-6 py-20 text-[#f3efe6] lg:px-10">
        <div className="mx-auto grid max-w-7xl gap-10 lg:grid-cols-[1fr_1.25fr] lg:items-end">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[#d8ab91]">Foundation status</p>
            <h2 className="mt-5 font-serif text-4xl font-medium">Khung kỹ thuật đã sẵn sàng để phát triển theo milestone.</h2>
          </div>
          <div className="grid gap-4 text-sm leading-7 text-[#cbd5cf] sm:grid-cols-2">
            <p>Storefront và Admin được tách theo route, metadata lấy từ cấu hình môi trường, giao diện render phía server.</p>
            <p>Catalog, CMS, authentication và dữ liệu shop chưa được giả lập; chúng sẽ được triển khai ở các milestone kế tiếp.</p>
          </div>
        </div>
      </section>
    </main>
  );
}

