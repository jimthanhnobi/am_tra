"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Inbox } from "lucide-react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";
import { EmptyState } from "@/components/ui/empty-state";
import { Select } from "@/components/ui/field";
import type { AdminLead, LeadPage } from "./page";

const labels: Record<AdminLead["status"], string> = { New: "Mới", Contacted: "Đã liên hệ", Closed: "Đã đóng" };
const styles: Record<AdminLead["status"], string> = { New: "bg-[#e7eee8] text-[#245b3d]", Contacted: "bg-[#fff0c7] text-[#765410]", Closed: "bg-[#eeeae2] text-[#56635b]" };

export function LeadList({ data, canManage }: Readonly<{ data: LeadPage; canManage: boolean }>) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  async function update(id: string, status: string) {
    if (busy) return;
    setBusy(id);
    try { await adminRequest(`leads/${id}/status`, { method: "PATCH", body: jsonBody({ status }) }); toast.success("Đã cập nhật lead."); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(null); }
  }
  return <main className="space-y-6"><header className="admin-page-header"><div><p className="admin-eyebrow">Khách liên hệ</p><h1 className="admin-title">Yêu cầu tư vấn</h1><p className="admin-description">{data.totalItems} yêu cầu từ form liên hệ. Lượt nhấn Zalo/Facebook/hotline không được tính là lead.</p></div></header>
    <div className="grid gap-3">{data.items.map((lead) => <article key={lead.id} className="admin-panel p-5 sm:p-6"><div className="flex flex-wrap items-start justify-between gap-4"><div className="min-w-0"><div className="flex flex-wrap items-center gap-3"><h2 className="text-base font-semibold text-[#233b32]">{lead.name}</h2><span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${styles[lead.status]}`}>{labels[lead.status]}</span></div><p className="mt-1 text-xs text-[#56635b]">Gửi lúc {new Date(lead.createdAtUtc).toLocaleString("vi-VN")}</p></div>{canManage && <div className="min-w-40"><label htmlFor={`lead-status-${lead.id}`} className="mb-1 block text-xs font-semibold text-[#56635b]">Trạng thái xử lý</label><Select id={`lead-status-${lead.id}`} aria-label={`Trạng thái lead của ${lead.name}`} value={lead.status} disabled={Boolean(busy)} onChange={(event) => void update(lead.id, event.target.value)}><option value="New">Mới</option><option value="Contacted">Đã liên hệ</option><option value="Closed">Đã đóng</option></Select></div>}</div><p className="mt-5 whitespace-pre-wrap break-words border-l-[3px] border-[#b79b6c] pl-4 text-sm leading-7 text-[#202722]">{lead.message}</p><dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-[#eeeae1] pt-5 text-sm sm:grid-cols-2 lg:grid-cols-4"><div className="min-w-0"><dt className="text-xs text-[#56635b]">Điện thoại</dt><dd className="mt-1 break-all font-medium">{lead.phone || "—"}</dd></div><div className="min-w-0"><dt className="text-xs text-[#56635b]">Email</dt><dd className="mt-1 break-all font-medium">{lead.email || "—"}</dd></div><div className="min-w-0"><dt className="text-xs text-[#56635b]">Trang gửi</dt><dd className="mt-1 break-all">{lead.sourcePage}</dd></div><div className="min-w-0"><dt className="text-xs text-[#56635b]">Đồng ý lúc</dt><dd className="mt-1">{new Date(lead.consentAt).toLocaleString("vi-VN")}</dd></div></dl></article>)}{!data.items.length && <div className="admin-panel"><EmptyState icon={Inbox} title="Chưa có yêu cầu liên hệ" description="Yêu cầu từ form liên hệ sẽ xuất hiện tại đây." /></div>}</div>
    {data.totalPages > 1 && <nav aria-label="Phân trang lead" className="flex flex-wrap items-center justify-between gap-3 text-sm"><span>Trang {data.page}/{data.totalPages}</span><div className="flex gap-3">{data.page > 1 && <Link href={`/admin/leads?page=${data.page - 1}`} className="admin-link inline-flex min-h-10 items-center">Trước</Link>}{data.page < data.totalPages && <Link href={`/admin/leads?page=${data.page + 1}`} className="admin-link inline-flex min-h-10 items-center">Sau</Link>}</div></nav>}
  </main>;
}
