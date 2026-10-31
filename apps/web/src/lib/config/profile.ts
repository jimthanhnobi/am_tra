import "server-only";

function socialUrl(value: string | undefined, hosts: string[]): string | null {
  if (!value?.trim()) return null;
  const url = new URL(value);
  if (url.protocol !== "https:" || url.username || url.password || !hosts.includes(url.hostname)) {
    throw new Error("Kênh liên hệ phải là URL HTTPS thuộc Zalo/Facebook.");
  }
  return url.href;
}
const hotline = process.env.SITE_HOTLINE?.trim() || null;
if (hotline && !/^\+?[\d\s().-]{6,25}$/.test(hotline)) throw new Error("SITE_HOTLINE không hợp lệ.");

export const siteProfile = Object.freeze({
  about: process.env.SITE_ABOUT?.trim() || null,
  address: process.env.SITE_ADDRESS?.trim() || null,
  openingHours: null as string | null,
  email: null as string | null,
  hotline,
  phoneHref: hotline ? `tel:${hotline.replace(/[^+\d]/g, "")}` : null,
  zalo: socialUrl(process.env.SITE_ZALO_URL, ["zalo.me", "www.zalo.me"]),
  facebook: socialUrl(process.env.SITE_FACEBOOK_URL, ["facebook.com", "www.facebook.com", "m.facebook.com", "fb.com"]),
});
export const hasContact = Boolean(siteProfile.hotline || siteProfile.zalo || siteProfile.facebook);
