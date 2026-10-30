const defaultSiteUrl = "http://localhost:3000";
const defaultDescription = "Không gian giới thiệu trà cụ cao cấp, kiến thức thưởng trà và kết nối tư vấn.";

function toAbsoluteUrl(value: string, variableName: string): string {
  try {
    return new URL(value).toString();
  } catch {
    throw new Error(`${variableName} phải là URL tuyệt đối hợp lệ.`);
  }
}

export const siteConfig = Object.freeze({
  name: process.env.NEXT_PUBLIC_SITE_NAME?.trim() || "TeaPremium",
  url: toAbsoluteUrl(process.env.NEXT_PUBLIC_SITE_URL || defaultSiteUrl, "NEXT_PUBLIC_SITE_URL"),
  description: process.env.NEXT_PUBLIC_SITE_DESCRIPTION?.trim() || defaultDescription,
  locale: "vi-VN",
});

