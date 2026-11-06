const defaultSiteUrl = "http://localhost:3000";
const defaultDescription = "Không gian giới thiệu trà cụ cao cấp, kiến thức thưởng trà và kết nối tư vấn.";

function toAbsoluteUrl(value: string, variableName: string): string {
  try {
    const url = new URL(value);
    if (!['https:', 'http:'].includes(url.protocol) || url.username || url.password || url.pathname !== '/' || url.search || url.hash) throw new Error('Invalid site origin');
    return url.origin;
  } catch {
    throw new Error(`${variableName} phải là URL tuyệt đối hợp lệ.`);
  }
}

export const siteConfig = Object.freeze({
  name: process.env.NEXT_PUBLIC_SITE_NAME?.trim() || "TeaPremium",
  url: toAbsoluteUrl(process.env.NEXT_PUBLIC_SITE_URL || defaultSiteUrl, "NEXT_PUBLIC_SITE_URL"),
  description: process.env.NEXT_PUBLIC_SITE_DESCRIPTION?.trim() || defaultDescription,
  locale: "vi-VN",
  indexable: process.env.SITE_INDEXABLE === "true" && Boolean(process.env.NEXT_PUBLIC_SITE_NAME?.trim()) &&
    Boolean(process.env.NEXT_PUBLIC_SITE_URL?.startsWith("https://")) &&
    !/localhost|127\.0\.0\.1|\.example(?:\/|$)|example\.(com|org|net)/i.test(process.env.NEXT_PUBLIC_SITE_URL ?? ""),
});

