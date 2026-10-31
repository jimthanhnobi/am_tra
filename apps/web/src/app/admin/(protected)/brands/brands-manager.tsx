"use client";

import { useState } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { AdminApiError } from "@/lib/api/errors";
import type { AdminBrand, BrandUpsertPayload } from "@/lib/api/catalog-types";
import { getErrorMessage } from "@/lib/admin-format";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input, Label } from "@/components/ui/field";

const blank: BrandUpsertPayload = { name: "", slug: "", origin: null };
export function BrandsManager({ initial, canWrite }: Readonly<{ initial: readonly AdminBrand[]; canWrite: boolean }>) {
  const [brands, setBrands] = useState(initial);
  const [selected, setSelected] = useState<AdminBrand | null>(null);
  const [form, setForm] = useState<BrandUpsertPayload>(blank);
  const [search, setSearch] = useState("");
  const [busy, setBusy] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  function select(brand: AdminBrand | null) { if (dirty && !window.confirm("Bỏ thay đổi chưa lưu?")) return; setSelected(brand); setForm(brand ? { name: brand.name, slug: brand.slug, origin: brand.origin } : blank); setErrors({}); setDirty(false); }
  function change<K extends keyof BrandUpsertPayload>(key: K, value: BrandUpsertPayload[K]) { setForm((current) => ({ ...current, [key]: value })); setDirty(true); }
  async function save() {
    if (busy || !canWrite) return;
    const validation: Record<string, string> = {};
    if (!form.name.trim()) validation.name = "Nhập tên thương hiệu.";
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.slug)) validation.slug = "Slug phải là chữ thường không dấu.";
    if (Object.keys(validation).length) { setErrors(validation); return; }
    setBusy(true); setErrors({});
    try { const updated = await adminRequest<AdminBrand>(selected ? `brands/${selected.id}` : "brands", { method: selected ? "PATCH" : "POST", body: jsonBody({ ...form, origin: form.origin?.trim() || null }) }); setSelected(updated); setForm({ name: updated.name, slug: updated.slug, origin: updated.origin }); setBrands(await adminRequest<AdminBrand[]>("brands")); setDirty(false); toast.success("Đã lưu thương hiệu."); }
    catch (error) { if (error instanceof AdminApiError) setErrors(Object.fromEntries(Object.entries(error.problem.errors ?? {}).map(([key, messages]) => [key.toLowerCase(), messages.join(" ")]))); toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }
  return <main className="space-y-6"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[#9c6349]">Catalog</p><h1 className="mt-2 font-serif text-3xl">Thương hiệu</h1><p className="mt-1 text-sm text-[#718077]">Thông tin thương hiệu chỉ nhập theo dữ liệu đã xác thực.</p></div><div className="grid gap-5 lg:grid-cols-[minmax(220px,1fr)_minmax(0,1.5fr)]"><Card className="p-4"><div className="flex items-center justify-between gap-3"><h2 className="font-serif text-lg">Danh sách</h2>{canWrite && <Button size="sm" onClick={() => select(null)}>+ Thêm</Button>}</div><Input className="mt-4" aria-label="Tìm thương hiệu" placeholder="Tìm tên hoặc slug" value={search} onChange={(event) => setSearch(event.target.value)} /><div className="mt-3 divide-y divide-[#eee8dd]">{brands.filter((brand) => `${brand.name} ${brand.slug}`.toLowerCase().includes(search.toLowerCase())).map((brand) => <button type="button" key={brand.id} onClick={() => select(brand)} className={`block w-full px-2 py-3 text-left hover:bg-[#f7f3ea] ${selected?.id === brand.id ? "bg-[#edf2ed]" : ""}`}><span className="font-medium">{brand.name}</span><span className="ml-2 text-xs text-[#718077]">/{brand.slug}</span></button>)}</div>{brands.length === 0 && <p className="mt-4 text-sm text-[#718077]">Chưa có thương hiệu.</p>}</Card><Card className="p-6"><h2 className="font-serif text-xl">{selected ? `Sửa: ${selected.name}` : "Thương hiệu mới"}</h2>{Object.keys(errors).length > 0 && <div role="alert" className="mt-4 rounded-lg bg-[#f9e8e6] p-3 text-sm text-[#a33d32]">{Object.entries(errors).map(([key, value]) => <p key={key}>{key}: {value}</p>)}</div>}<fieldset disabled={!canWrite || busy} className="mt-5 space-y-4"><div><Label htmlFor="brandName">Tên *</Label><Input id="brandName" maxLength={300} value={form.name} onChange={(event) => change("name", event.target.value)} /></div><div><Label htmlFor="brandSlug">Slug *</Label><Input id="brandSlug" maxLength={200} value={form.slug} onChange={(event) => change("slug", event.target.value)} /></div><div><Label htmlFor="brandOrigin">Xuất xứ (tùy chọn)</Label><Input id="brandOrigin" maxLength={300} value={form.origin ?? ""} onChange={(event) => change("origin", event.target.value || null)} /></div></fieldset>{canWrite && <Button className="mt-6" disabled={busy} onClick={() => void save()}>{busy ? "Đang lưu…" : "Lưu thương hiệu"}</Button>}</Card></div></main>;
}
