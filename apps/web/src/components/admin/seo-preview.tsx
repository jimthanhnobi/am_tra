type SeoPreviewProps = {
  title: string;
  description: string;
  canonical: string;
  origin: string;
  robotsIndex: boolean;
  published: boolean;
  titleInputLength: number;
  descriptionInputLength: number;
};

export function SeoPreview({ title, description, canonical, robotsIndex, published,
  titleInputLength, descriptionInputLength, origin }: Readonly<SeoPreviewProps>) {
  let canonicalUrl: string;
  try { canonicalUrl = new URL(canonical, origin).href; }
  catch { canonicalUrl = "URL canonical chưa hợp lệ"; }
  return <div className="rounded-xl border border-[#ded7ca] bg-[#faf9f5] p-4 text-sm">
    <h3 className="font-semibold text-[#183c31]">Xem trước SEO</h3>
    <p className="mt-1 text-xs text-[#718077]">Tiêu đề SEO: {titleInputLength}/200 ký tự · Meta description: {descriptionInputLength}/500 ký tự</p>
    <p className="mt-3 text-lg text-[#315d90]">{title || "Chưa có tiêu đề"}</p>
    <p className="break-all text-xs text-[#477043]">Canonical dự kiến: {canonicalUrl}</p>
    <p className="mt-1 text-[#626f66]">{description || "Chưa có mô tả"}</p>
    <p className="mt-3 text-xs font-medium text-[#536159]">{!published
      ? "Bản chưa xuất bản: không hiển thị công khai."
      : robotsIndex
        ? "Published · Cho phép index khi cấu hình site cho phép. Google tự quyết định thời điểm index."
        : "Published · Trang công khai 200 nhưng có noindex và không nằm trong sitemap."}</p>
    <p className="mt-1 text-xs text-[#718077]">Bản xem trước chỉ minh họa; kết quả tìm kiếm thực tế có thể khác.</p>
  </div>;
}
