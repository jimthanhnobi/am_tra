"use client";

import { AdminImage } from "@/components/admin/admin-image";

import { useState } from "react";
import { ImageOff, Upload } from "lucide-react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import type { AdminMediaAsset, AdminProductDetail, AdminProductMedia, PagedResult } from "@/lib/api/catalog-types";
import { uploadImage, validateImage } from "@/lib/media/upload";
import { getErrorMessage } from "@/lib/admin-format";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/field";

type Props = Readonly<{ product?: AdminProductDetail; canWrite: boolean; canUpload: boolean; onRefresh: () => void }>;

export function ProductGallery({ product, canWrite, canUpload, onRefresh }: Props) {
  const [media, setMedia] = useState<readonly AdminProductMedia[]>(product?.media ?? []);
  const [library, setLibrary] = useState<PagedResult<AdminMediaAsset> | null>(null);
  const [showLibrary, setShowLibrary] = useState(false);
  const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState<Record<string, number>>({});
  const [dragging, setDragging] = useState(false);
  async function refresh() {
    if (!product) return;
    const detail = await adminRequest<AdminProductDetail>(`products/${product.id}`);
    setMedia(detail.media);
    onRefresh();
  }
  async function withBusy(work: () => Promise<void>) {
    if (busy || !product) return;
    setBusy(true);
    try { await work(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { try { await refresh(); } catch (error) { toast.error(getErrorMessage(error)); } setBusy(false); }
  }
  async function attach(asset: AdminMediaAsset) {
    if (!product || media.some((item) => item.assetId === asset.id)) { toast.error("Ảnh đã có trong gallery."); return; }
    await withBusy(async () => { await adminRequest(`products/${product.id}/media`, { method: "POST", body: jsonBody({ assetId: asset.id, altText: "", sortOrder: Math.max(...media.map((item) => item.sortOrder), -1) + 1, isCover: false }) }); toast.success("Đã thêm ảnh."); });
  }
  async function upload(files: FileList | File[]) {
    if (!product || busy || !canUpload) return;
    const selected = Array.from(files);
    if (selected.length === 0) return;
    setBusy(true);
    try {
      for (const [index, file] of selected.entries()) {
        const error = validateImage(file);
        if (error) { toast.error(`${file.name}: ${error}`); continue; }
        try {
          const asset = await uploadImage(file, (percent) => setProgress((current) => ({ ...current, [file.name]: percent })));
          await adminRequest(`products/${product.id}/media`, { method: "POST", body: jsonBody({ assetId: asset.id, altText: "", sortOrder: Math.max(...media.map((item) => item.sortOrder), -1) + 1 + index, isCover: false }) });
          toast.success(`Đã tải ${file.name}.`);
        } catch (uploadError) { toast.error(`${file.name}: ${getErrorMessage(uploadError)}`); }
      }
      await refresh();
    } finally { setProgress({}); setBusy(false); }
  }
  async function loadLibrary(page = 1) {
    try { setLibrary(await adminRequest<PagedResult<AdminMediaAsset>>(`media?page=${page}&pageSize=24`)); setShowLibrary(true); }
    catch (error) { toast.error(getErrorMessage(error)); }
  }
  async function saveMedia(item: AdminProductMedia, altText: string, sortOrder = item.sortOrder) {
    await withBusy(async () => { await adminRequest(`products/${product!.id}/media/${item.id}`, { method: "PATCH", body: jsonBody({ altText, sortOrder }) }); toast.success("Đã lưu ảnh."); });
  }
  async function move(item: AdminProductMedia, direction: -1 | 1) {
    const ordered = [...media].sort((a, b) => a.sortOrder - b.sortOrder);
    const index = ordered.findIndex((entry) => entry.id === item.id);
    const neighbor = ordered[index + direction];
    if (!neighbor) return;
    await withBusy(async () => {
      // Use a temporary order to avoid uniqueness conflicts while swapping.
      const temporary = Math.max(...ordered.map((entry) => entry.sortOrder), 0) + 1;
      await adminRequest(`products/${product!.id}/media/${item.id}`, { method: "PATCH", body: jsonBody({ altText: item.altText, sortOrder: temporary }) });
      await adminRequest(`products/${product!.id}/media/${neighbor.id}`, { method: "PATCH", body: jsonBody({ altText: neighbor.altText, sortOrder: item.sortOrder }) });
      await adminRequest(`products/${product!.id}/media/${item.id}`, { method: "PATCH", body: jsonBody({ altText: item.altText, sortOrder: neighbor.sortOrder }) });
    });
  }
  return <Card className="p-6"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="font-serif text-xl">Gallery sản phẩm</h2><p className="mt-1 text-sm text-[#718077]">Lưu sản phẩm trước khi thêm ảnh. Cover và ALT là bắt buộc để xuất bản.</p></div>{canWrite && product && <div className="flex flex-wrap gap-2"><Button variant="secondary" disabled={busy} onClick={() => void loadLibrary()}>Chọn từ thư viện</Button>{canUpload && <label className={`inline-flex min-h-10 cursor-pointer items-center gap-2 rounded-xl bg-[#183c31] px-4 text-sm font-semibold text-white ${busy ? "opacity-50" : ""}`}><Upload size={16} /> Tải ảnh lên<input className="sr-only" type="file" accept="image/jpeg,image/png,image/webp" multiple disabled={busy} onChange={(event) => { if (event.target.files) void upload(event.target.files); event.target.value = ""; }} /></label>}</div>}</div>
    {!product ? <p className="mt-5 rounded-xl bg-[#f7f2e8] p-4 text-sm">Lưu bản nháp để bắt đầu tải ảnh.</p> : <><div onDragOver={(event) => { if (canWrite && canUpload) { event.preventDefault(); setDragging(true); } }} onDragLeave={() => setDragging(false)} onDrop={(event) => { if (canWrite && canUpload) { event.preventDefault(); setDragging(false); void upload(event.dataTransfer.files); } }} className={`mt-5 rounded-xl border-2 border-dashed p-4 ${dragging ? "border-[#8d4f34] bg-[#faf4ec]" : "border-[#e1dbce]"}`}><p className="text-center text-xs text-[#718077]">Kéo thả ảnh JPEG, PNG, WebP vào đây · tối đa 10 MB mỗi ảnh</p>{Object.entries(progress).map(([name, percent]) => <div key={name} className="mt-2 text-xs">{name}: {percent}%<div className="mt-1 h-1 rounded bg-[#e5dfd2]"><div className="h-full rounded bg-[#183c31]" style={{ width: `${percent}%` }} /></div></div>)}</div>
      <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{[...media].sort((a, b) => a.sortOrder - b.sortOrder).map((item, index) => <GalleryItem key={item.id} item={item} index={index} last={index === media.length - 1} canWrite={canWrite && !busy} onSave={(alt) => saveMedia(item, alt)} onMove={(direction) => move(item, direction)} onCover={() => withBusy(async () => { await adminRequest(`products/${product.id}/media/${item.id}/cover`, { method: "POST" }); toast.success("Đã đặt ảnh cover."); })} onRemove={() => withBusy(async () => { await adminRequest(`products/${product.id}/media/${item.id}`, { method: "DELETE" }); toast.success("Đã gỡ ảnh khỏi sản phẩm."); })} />)}</div>{media.length === 0 && <p className="mt-5 text-center text-sm text-[#718077]">Chưa có ảnh nào.</p>}
      {showLibrary && <div className="mt-6 rounded-xl border border-[#ded7ca] p-4"><div className="flex justify-between"><h3 className="font-semibold">Chọn ảnh đã có</h3><Button size="sm" variant="ghost" onClick={() => setShowLibrary(false)}>Đóng</Button></div>{library?.items.length ? <div className="mt-4 grid grid-cols-3 gap-3 sm:grid-cols-6">{library.items.map((asset) => <button type="button" key={asset.id} disabled={busy || media.some((item) => item.assetId === asset.id)} onClick={() => void attach(asset)} className="overflow-hidden rounded-lg border text-left disabled:opacity-40">{asset.publicUrl ? <AdminImage src={asset.publicUrl} alt={asset.fileName} width={asset.width} height={asset.height} loading="lazy" className="aspect-square w-full object-cover" /> : <div className="grid aspect-square place-items-center"><ImageOff /></div>}<span className="block truncate p-1 text-xs">{asset.fileName}</span></button>)}</div> : <p className="mt-4 text-sm">Thư viện trống.</p>}{library && library.totalPages > 1 && <div className="mt-4 flex gap-2"><Button size="sm" variant="secondary" disabled={library.page <= 1} onClick={() => void loadLibrary(library.page - 1)}>Trước</Button><span className="py-2 text-xs">{library.page}/{library.totalPages}</span><Button size="sm" variant="secondary" disabled={library.page >= library.totalPages} onClick={() => void loadLibrary(library.page + 1)}>Sau</Button></div>}</div>}</>}
  </Card>;
}

function GalleryItem({ item, index, last, canWrite, onSave, onMove, onCover, onRemove }: Readonly<{ item: AdminProductMedia; index: number; last: boolean; canWrite: boolean; onSave: (alt: string) => Promise<void>; onMove: (direction: -1 | 1) => Promise<void>; onCover: () => Promise<void>; onRemove: () => Promise<void> }>) {
  const [alt, setAlt] = useState(item.altText);
  return <div className="overflow-hidden rounded-xl border border-[#e1dbce]"><div className="relative bg-[#eeeae2]">{item.publicUrl ? <AdminImage src={item.publicUrl} alt={item.altText || "Ảnh chưa có ALT"} width={item.width} height={item.height} loading="lazy" className="aspect-[4/3] w-full object-cover" /> : <div className="grid aspect-[4/3] place-items-center"><ImageOff /></div>}{item.isCover && <span className="absolute left-2 top-2 rounded bg-[#183c31] px-2 py-1 text-xs text-white">Cover</span>}</div><div className="space-y-2 p-3"><Input aria-label="ALT text" placeholder="Mô tả ảnh (ALT)" maxLength={500} value={alt} disabled={!canWrite} onChange={(event) => setAlt(event.target.value)} /><div className="flex flex-wrap gap-1"><Button size="sm" variant="secondary" disabled={!canWrite || alt === item.altText} onClick={() => void onSave(alt)}>Lưu ALT</Button><Button size="sm" variant="ghost" disabled={!canWrite || item.isCover} onClick={() => void onCover()}>Đặt cover</Button><Button size="sm" variant="ghost" disabled={!canWrite || index === 0} onClick={() => void onMove(-1)}>↑</Button><Button size="sm" variant="ghost" disabled={!canWrite || last} onClick={() => void onMove(1)}>↓</Button><ConfirmDialog trigger={<Button size="sm" variant="ghost" disabled={!canWrite}>Gỡ</Button>} title="Gỡ ảnh khỏi sản phẩm?" description="Ảnh vẫn được giữ trong thư viện." confirmLabel="Gỡ ảnh" danger onConfirm={onRemove} /></div></div></div>;
}
