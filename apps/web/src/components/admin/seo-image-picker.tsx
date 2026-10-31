"use client";

import { useState } from "react";
import { toast } from "sonner";
import { ImageOff } from "lucide-react";
import { AdminImage } from "@/components/admin/admin-image";
import { Button } from "@/components/ui/button";
import { Input, Label } from "@/components/ui/field";
import { adminRequest } from "@/lib/api/client";
import type { AdminMediaAsset, PagedResult } from "@/lib/api/catalog-types";
import { getErrorMessage } from "@/lib/admin-format";

export function SeoImagePicker({ value, onChange, label = "Ảnh Open Graph", disabled = false }: Readonly<{ value: string | null; onChange: (id: string | null) => void; label?: string; disabled?: boolean }>) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [results, setResults] = useState<PagedResult<AdminMediaAsset> | null>(null);
  const [loading, setLoading] = useState(false);
  async function load(page = 1, term = search) {
    setLoading(true);
    try { const query = new URLSearchParams({ page: String(page), pageSize: "24" }); if (term.trim()) query.set("search", term.trim()); setResults(await adminRequest<PagedResult<AdminMediaAsset>>(`media?${query}`)); setOpen(true); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setLoading(false); }
  }
  return <div><Label>{label}</Label><div className="flex flex-wrap items-center gap-2"><Button type="button" variant="secondary" disabled={disabled || loading} onClick={() => void load()}>{value ? "Đổi ảnh" : "Chọn từ thư viện"}</Button>{value && <Button type="button" variant="ghost" disabled={disabled} onClick={() => onChange(null)}>Bỏ ảnh</Button>}</div>{value && <p className="mt-1 break-all text-xs text-[#718077]">Asset: {value}</p>}{open && !disabled && <div className="mt-3 rounded-xl border border-[#ded7ca] p-3"><div className="flex gap-2"><Input aria-label="Tìm ảnh Open Graph" placeholder="Tìm ảnh" value={search} onChange={(event) => setSearch(event.target.value)} /><Button type="button" variant="secondary" disabled={loading} onClick={() => void load(1)}>Tìm</Button><Button type="button" variant="ghost" onClick={() => setOpen(false)}>Đóng</Button></div>{results?.items.length ? <div className="mt-3 grid grid-cols-4 gap-2">{results.items.map((asset) => <button type="button" key={asset.id} onClick={() => { onChange(asset.id); setOpen(false); }} className={`overflow-hidden rounded-lg border text-left ${value === asset.id ? "border-[#8d4f34]" : "border-[#e3ddd1]"}`}>{asset.publicUrl ? <AdminImage src={asset.publicUrl} alt={asset.fileName} width={asset.width} height={asset.height} className="aspect-square w-full object-cover" /> : <div className="grid aspect-square place-items-center"><ImageOff size={20} /></div>}<span className="block truncate p-1 text-xs">{asset.fileName}</span></button>)}</div> : <p className="mt-3 text-sm text-[#718077]">Không có ảnh phù hợp.</p>}{results && results.totalPages > 1 && <div className="mt-3 flex items-center gap-2"><Button type="button" size="sm" variant="secondary" disabled={loading || results.page <= 1} onClick={() => void load(results.page - 1)}>Trước</Button><span className="text-xs">{results.page}/{results.totalPages}</span><Button type="button" size="sm" variant="secondary" disabled={loading || results.page >= results.totalPages} onClick={() => void load(results.page + 1)}>Sau</Button></div>}</div>}</div>;
}
