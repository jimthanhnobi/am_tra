"use client";

import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import type { AdminSiteSettings, SiteSettingsInput } from "@/lib/api/settings-types";
import { getErrorMessage } from "@/lib/admin-format";
import { SeoImagePicker } from "@/components/admin/seo-image-picker";

type TextField = Exclude<keyof SiteSettingsInput, "logoAssetId" | "faviconAssetId" | "defaultOgImageAssetId">;
const limits: Partial<Record<TextField, number>> = {
  siteName: 160, siteDescription: 500, brandTagline: 200, hotline: 25,
  zaloUrl: 2048, facebookUrl: 2048, email: 254, address: 500,
  openingHours: 300, defaultSeoTitle: 200, defaultMetaDescription: 500,
  publicWebsiteUrl: 2048,
};
const inputClass = "mt-2 w-full rounded-xl border border-[#d8d1c3] bg-white px-4 py-3 text-sm text-[#20362c] outline-none focus:border-[#233b32] focus:ring-2 focus:ring-[#233b32]/10";
const sectionClass = "rounded-2xl border border-[#ddd8cc] bg-[#fffefa] p-6 shadow-sm sm:p-8";

function validSocial(value: string | null, hosts: string[]) {
  if (!value) return true;
  try { const url = new URL(value); return url.protocol === "https:" && !url.username && !url.password && hosts.includes(url.hostname); }
  catch { return false; }
}

function validate(input: SiteSettingsInput): string | null {
  if (!input.siteName?.trim()) return "Nhập tên website.";
  for (const [key, max] of Object.entries(limits)) {
    const value = input[key as TextField];
    if (value && value.length > max) return `${key} vượt quá ${max} ký tự.`;
  }
  if (input.hotline && !/^(?=.*\d)\+?[\d\s().-]{6,25}$/.test(input.hotline)) return "Hotline không hợp lệ.";
  if (input.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.email)) return "Email không hợp lệ.";
  if (!validSocial(input.zaloUrl, ["zalo.me", "www.zalo.me"])) return "Zalo phải là URL HTTPS của zalo.me.";
  if (!validSocial(input.facebookUrl, ["facebook.com", "www.facebook.com", "m.facebook.com", "fb.com"])) return "Facebook phải là URL HTTPS của Facebook.";
  if (input.publicWebsiteUrl) {
    try {
      const url = new URL(input.publicWebsiteUrl);
      if (url.protocol !== "https:" || url.pathname !== "/" || url.search || url.hash || url.username || url.password) return "URL website phải là origin HTTPS, không có đường dẫn hoặc query.";
    } catch { return "URL website không hợp lệ."; }
  }
  return null;
}

export function SiteSettingsForm({ initial }: Readonly<{ initial: AdminSiteSettings }>) {
  const [values, setValues] = useState<SiteSettingsInput>(initial.settings);
  const [busy, setBusy] = useState(false);
  const [updatedAt, setUpdatedAt] = useState(initial.updatedAtUtc);
  const [error, setError] = useState<string | null>(null);

  function textField(key: TextField, label: string, multiline = false) {
    const value = values[key] ?? "";
    const common = { className: inputClass, value, maxLength: limits[key], onChange: (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setValues((current) => ({ ...current, [key]: event.target.value || null })) };
    return <label className="block text-sm font-medium" key={key}>{label}
      {multiline ? <textarea {...common} rows={3} /> : <input {...common} type={key === "email" ? "email" : key.endsWith("Url") ? "url" : "text"} />}
      {limits[key] && <span className="mt-1 block text-right text-xs font-normal text-[#56635b]">{value.length}/{limits[key]}</span>}
    </label>;
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    const message = validate(values);
    if (message) { setError(message); return; }
    setError(null);
    setBusy(true);
    try {
      const saved = await adminRequest<AdminSiteSettings>("settings", { method: "PUT", body: jsonBody(values) });
      setValues(saved.settings);
      setUpdatedAt(saved.updatedAtUtc);
      toast.success("Đã lưu cấu hình website.");
    } catch (reason) { const text = getErrorMessage(reason); setError(text); toast.error(text); }
    finally { setBusy(false); }
  }

  return <main className="mx-auto max-w-5xl space-y-7">
    <header><p className="admin-eyebrow">Website</p><h1 className="admin-title">Cài đặt website</h1><p className="mt-2 max-w-2xl text-sm leading-7 text-[#56635b]">Chỉ nhập thông tin đã được xác minh. Thay đổi được công bố qua Public API; cấu hình hạ tầng và secret vẫn nằm ở môi trường triển khai.</p>{updatedAt && <p className="mt-2 text-xs text-[#56635b]">Lưu gần nhất: {new Date(updatedAt).toLocaleString("vi-VN")}</p>}</header>
    <form onSubmit={(event) => void save(event)} className="space-y-6">
      <section className={sectionClass}><h2 className="font-serif text-xl">General</h2><div className="mt-5 grid gap-5 md:grid-cols-2">{textField("siteName", "Tên website *")}{textField("publicWebsiteUrl", "Public Website URL (HTTPS)")}<div className="md:col-span-2">{textField("siteDescription", "Mô tả website", true)}</div></div></section>
      <section className={sectionClass}><h2 className="font-serif text-xl">Brand Identity</h2><div className="mt-5 space-y-5">{textField("brandTagline", "Brand tagline")}<div className="grid gap-5 md:grid-cols-2"><SeoImagePicker value={values.logoAssetId} onChange={(id) => setValues((current) => ({ ...current, logoAssetId: id }))} label="Logo" /><SeoImagePicker value={values.faviconAssetId} onChange={(id) => setValues((current) => ({ ...current, faviconAssetId: id }))} label="Favicon" /></div></div></section>
      <section className={sectionClass}><h2 className="font-serif text-xl">Contact Information</h2><div className="mt-5 grid gap-5 md:grid-cols-2">{textField("hotline", "Hotline")}{textField("email", "Email")}<div className="md:col-span-2">{textField("address", "Địa chỉ (tùy chọn)", true)}</div><div className="md:col-span-2">{textField("openingHours", "Giờ mở cửa (tùy chọn)", true)}</div></div></section>
      <section className={sectionClass}><h2 className="font-serif text-xl">SEO Defaults</h2><div className="mt-5 space-y-5">{textField("defaultSeoTitle", "SEO title mặc định")}{textField("defaultMetaDescription", "Meta description mặc định", true)}<SeoImagePicker value={values.defaultOgImageAssetId} onChange={(id) => setValues((current) => ({ ...current, defaultOgImageAssetId: id }))} label="OG image mặc định" /><div className="rounded-xl border border-[#d8d1c3] bg-white p-4"><p className="text-xs text-[#56635b]">Xem trước kết quả tìm kiếm</p><p className="mt-2 text-lg text-[#315d90]">{values.defaultSeoTitle || values.siteName || "Tiêu đề chưa cấu hình"}</p><p className="mt-1 text-sm text-[#626f66]">{values.defaultMetaDescription || values.siteDescription || "Mô tả chưa cấu hình"}</p><p className="mt-2 break-all text-xs text-[#477043]">{values.publicWebsiteUrl || "Canonical URL chưa cấu hình"}</p></div></div></section>
      <section className={sectionClass}><h2 className="font-serif text-xl">Social Links</h2><div className="mt-5 grid gap-5 md:grid-cols-2">{textField("zaloUrl", "Zalo URL")}{textField("facebookUrl", "Facebook URL")}</div></section>
      {error && <p role="alert" className="rounded-xl border border-red-300 bg-red-50 p-4 text-sm text-red-800">{error}</p>}
      <button type="submit" disabled={busy} className="rounded-xl bg-[#233b32] px-6 py-3 text-sm font-medium text-white disabled:opacity-50">{busy ? "Đang lưu…" : "Lưu cài đặt"}</button>
    </form>
  </main>;
}
