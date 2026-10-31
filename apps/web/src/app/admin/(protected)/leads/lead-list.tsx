"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";
import type { LeadPage } from "./page";

export function LeadList({ data, canManage }: Readonly<{ data: LeadPage; canManage: boolean }>) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  async function update(id: string, status: string) {
    setBusy(id);
    try { await adminRequest(`leads/${id}/status`, { method: "PATCH", body: jsonBody({ status }) }); toast.success("Đã cập nhật lead."); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(null); }
  }
  return <main className="space-y-6"><header><p className="text-xs uppercase tracking-[0.2em] text-[#9c6349]">Liên hệ</p><h1 className="mt-2 font-serif text-3xl">Leads</h1><p className="mt-1 text-sm text-[#718077]">{data.totalItems} yêu cầu đã gửi form. Lượt nhấn Zalo/Facebook/hotline không được tính là lead.</p></header><div className="grid gap-4">{data.items.map((lead) => <article key={lead.id} className="rounded-xl border border-[#ded7ca] bg-white p-5"><div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="font-semibold">{lead.name}</h2><p className="mt-1 text-xs text-[#718077]">{new Date(lead.createdAtUtc).toLocaleString("vi-VN")} · {lead.status}</p></div>{canManage && <select aria-label={`Trạng thái lead của ${lead.name}`} value={lead.status} disabled={busy === lead.id} onChange={(event) => void update(lead.id, event.target.value)} className="rounded border border-[#ded7ca] px-3 py-2 text-sm"><option value="New">Mới</option><option value="Contacted">Đã liên hệ</option><option value="Closed">Đã đóng</option></select>}</div><p className="mt-4 whitespace-pre-wrap text-sm leading-6">{lead.message}</p><dl className="mt-4 grid gap-2 border-t border-[#eee8dc] pt-4 text-xs sm:grid-cols-2"><div><dt className="text-[#718077]">Điện thoại</dt><dd>{lead.phone || "—"}</dd></div><div><dt className="text-[#718077]">Email</dt><dd>{lead.email || "—"}</dd></div><div><dt className="text-[#718077]">Trang gửi</dt><dd className="break-all">{lead.sourcePage}</dd></div><div><dt className="text-[#718077]">Đồng ý gửi thông tin lúc</dt><dd>{new Date(lead.consentAt).toLocaleString("vi-VN")}</dd></div></dl></article>)}{!data.items.length && <p className="rounded border border-[#ded7ca] bg-white p-8 text-center text-[#718077]">Chưa có yêu cầu liên hệ.</p>}</div><nav aria-label="Phân trang lead" className="flex gap-4 text-sm">{data.page > 1 && <Link href={`/admin/leads?page=${data.page - 1}`}>← Trang trước</Link>}{data.page < data.totalPages && <Link href={`/admin/leads?page=${data.page + 1}`}>Trang sau →</Link>}</nav></main>;
}
