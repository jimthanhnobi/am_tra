"use client";

import { AdminImage } from "@/components/admin/admin-image";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { ImageOff, Images, Upload } from "lucide-react";
import { toast } from "sonner";
import type { AdminMediaAsset, PagedResult } from "@/lib/api/catalog-types";
import { formatAdminDate, getErrorMessage } from "@/lib/admin-format";
import { uploadImage, validateImage } from "@/lib/media/upload";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input, Select } from "@/components/ui/field";

export function MediaLibrary({ initial, query, canUpload }: Readonly<{ initial: PagedResult<AdminMediaAsset>; query: string; canUpload: boolean }>) {
  const router = useRouter();
  const params = new URLSearchParams(query);
  const [selected, setSelected] = useState<AdminMediaAsset | null>(null);
  const [busy, setBusy] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [progress, setProgress] = useState<Record<string, number>>({});
  async function upload(files: FileList | File[]) {
    if (!canUpload || busy) return;
    setBusy(true);
    try { for (const file of Array.from(files)) {
      const error = validateImage(file);
      if (error) { toast.error(`${file.name}: ${error}`); continue; }
      try { await uploadImage(file, (percent) => setProgress((current) => ({ ...current, [file.name]: percent }))); toast.success(`Đã tải ${file.name}.`); }
      catch (uploadError) { toast.error(`${file.name}: ${getErrorMessage(uploadError)}`); }
    } router.refresh(); }
    finally { setProgress({}); setBusy(false); }
  }
  function pageHref(page: number) { const next = new URLSearchParams(query); next.set("page", String(page)); return `/admin/media?${next}`; }
  return <main className="space-y-6"><div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[#9c6349]">Catalog</p><h1 className="mt-2 font-serif text-3xl">Thư viện ảnh</h1><p className="mt-1 text-sm text-[#718077]">{initial.totalItems} ảnh · JPEG, PNG, WebP</p></div>{canUpload && <label className={`inline-flex min-h-10 cursor-pointer items-center gap-2 rounded-xl bg-[#183c31] px-4 text-sm font-semibold text-white ${busy ? "opacity-50" : ""}`}><Upload size={16} /> Tải ảnh lên<input type="file" className="sr-only" accept="image/jpeg,image/png,image/webp" multiple disabled={busy} onChange={(event) => { if (event.target.files) void upload(event.target.files); event.target.value = ""; }} /></label>}</div><Card className="p-4"><form action="/admin/media" className="flex flex-wrap gap-3"><Input name="search" aria-label="Tìm ảnh" placeholder="Tìm tên hoặc object key" defaultValue={params.get("search") ?? ""} className="min-w-56 flex-1" /><Select name="contentType" aria-label="Loại ảnh" defaultValue={params.get("contentType") ?? ""} className="sm:w-48"><option value="">Mọi loại ảnh</option><option value="image/jpeg">JPEG</option><option value="image/png">PNG</option><option value="image/webp">WebP</option></Select><Button type="submit">Lọc</Button></form></Card>
    {canUpload && <div onDragOver={(event) => { event.preventDefault(); setDragging(true); }} onDragLeave={() => setDragging(false)} onDrop={(event) => { event.preventDefault(); setDragging(false); void upload(event.dataTransfer.files); }} className={`rounded-xl border-2 border-dashed p-6 text-center text-sm text-[#718077] ${dragging ? "border-[#8d4f34] bg-[#faf4ec]" : "border-[#ded7ca]"}`}>Kéo thả ảnh vào đây · tối đa 10 MB mỗi ảnh{Object.entries(progress).map(([name, percent]) => <div key={name} className="mt-2 text-xs">{name}: {percent}%<div className="mt-1 h-1 rounded bg-[#e5dfd2]"><div className="h-full rounded bg-[#183c31]" style={{ width: `${percent}%` }} /></div></div>)}</div>}
    <Card className="p-4">{initial.items.length === 0 ? <EmptyState icon={Images} title="Thư viện trống" description="Tải ảnh lên hoặc thay đổi bộ lọc." /> : <div className="grid gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">{initial.items.map((asset) => <button type="button" key={asset.id} onClick={() => setSelected(asset)} className={`overflow-hidden rounded-xl border text-left transition hover:border-[#8d4f34] ${selected?.id === asset.id ? "border-[#8d4f34]" : "border-[#e3ddd1]"}`}>{asset.publicUrl ? <AdminImage src={asset.publicUrl} alt={asset.fileName} width={asset.width} height={asset.height} loading="lazy" className="aspect-square w-full bg-[#eeeae2] object-cover" /> : <div className="grid aspect-square place-items-center bg-[#eeeae2]"><ImageOff /></div>}<span className="block truncate p-2 text-xs font-medium">{asset.fileName}</span></button>)}</div>}</Card>
    {selected && <Card className="p-5"><div className="flex items-center justify-between"><h2 className="font-serif text-xl">Chi tiết ảnh</h2><Button variant="ghost" size="sm" onClick={() => setSelected(null)}>Đóng</Button></div><dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2"><div><dt className="text-[#718077]">Tên tệp</dt><dd>{selected.fileName}</dd></div><div><dt className="text-[#718077]">Kích thước</dt><dd>{selected.width} × {selected.height} px</dd></div><div><dt className="text-[#718077]">Dung lượng</dt><dd>{(selected.byteSize / 1024).toFixed(1)} KB</dd></div><div><dt className="text-[#718077]">MIME</dt><dd>{selected.mimeType}</dd></div><div><dt className="text-[#718077]">Ngày tải</dt><dd>{formatAdminDate(selected.createdAtUtc)}</dd></div><div><dt className="text-[#718077]">Asset ID</dt><dd className="break-all">{selected.id}</dd></div></dl><p className="mt-3 text-xs text-[#718077]">ALT text được quản lý riêng theo từng sản phẩm trong gallery.</p></Card>}
    {initial.totalPages > 1 && <nav aria-label="Phân trang ảnh" className="flex items-center justify-between text-sm"><span>Trang {initial.page}/{initial.totalPages}</span><div className="flex gap-2">{initial.page > 1 && <Link href={pageHref(initial.page - 1)} className="rounded-lg border bg-white px-4 py-2">Trước</Link>}{initial.page < initial.totalPages && <Link href={pageHref(initial.page + 1)} className="rounded-lg border bg-white px-4 py-2">Sau</Link>}</div></nav>}</main>;
}
