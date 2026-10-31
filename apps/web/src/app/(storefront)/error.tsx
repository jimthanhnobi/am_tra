"use client";
export default function StorefrontError({ reset }: Readonly<{ reset: () => void }>) {
  return <main id="noi-dung" className="mx-auto max-w-2xl px-6 py-24 text-center" role="alert"><h1 className="font-serif text-3xl">Tạm thời chưa thể tải bộ sưu tập</h1><p className="mt-5 text-sm text-[#66736c]">Vui lòng thử lại sau ít phút.</p><button type="button" onClick={reset} className="mt-7 rounded-full bg-[#183c31] px-6 py-3 text-sm text-white">Thử lại</button></main>;
}
