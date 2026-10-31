"use client";
export default function StorefrontError({ reset }: Readonly<{ reset: () => void }>) {
  return <main id="noi-dung" className="sf-container sf-section mx-auto max-w-2xl text-center" role="alert"><p className="sf-kicker">Tạm gián đoạn</p><h1 className="sf-heading mt-4">Chưa thể tải nội dung</h1><p className="sf-copy mt-5">Vui lòng thử lại sau ít phút.</p><button type="button" onClick={reset} className="sf-button mt-8">Thử lại</button></main>;
}
