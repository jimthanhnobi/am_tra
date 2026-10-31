"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { BookOpen } from "lucide-react";
import { toast } from "sonner";
import { adminRequest } from "@/lib/api/client";
import type { AdminArticleSummary, ArticlePage } from "@/lib/api/article-types";
import { getErrorMessage } from "@/lib/admin-format";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { EmptyState } from "@/components/ui/empty-state";
import { StatusBadge } from "@/components/ui/status-badge";

export function ArticleList({ data, canPublish }: Readonly<{ data: ArticlePage; canPublish: boolean }>) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  const editHref = (id: string) => `/admin/articles/${id}/edit?returnTo=${encodeURIComponent(`/admin/articles?page=${data.page}`)}`;

  async function transition(id: string, action: "publish" | "archive") {
    if (busy) return;
    setBusy(id);
    try { await adminRequest(`articles/${id}/${action}`, { method: "POST" }); toast.success("Đã cập nhật trạng thái."); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(null); }
  }
  function actions(article: AdminArticleSummary) {
    return <div className="flex flex-wrap items-center gap-2"><Link href={editHref(article.id)} className="admin-link text-sm">Xem / sửa</Link>
      {canPublish && article.status === "InReview" && <ConfirmDialog trigger={<Button size="sm" disabled={Boolean(busy)}>Xuất bản</Button>} title="Xuất bản bài viết?" description="Bài viết sẽ hiển thị công khai theo thiết lập SEO đã lưu." confirmLabel="Xuất bản" onConfirm={() => transition(article.id, "publish")} />}
      {canPublish && article.status === "Published" && <ConfirmDialog trigger={<Button size="sm" variant="ghost" disabled={Boolean(busy)}>Lưu trữ</Button>} title="Lưu trữ bài viết?" description="Bài viết sẽ không còn hiển thị công khai." confirmLabel="Lưu trữ" danger onConfirm={() => transition(article.id, "archive")} />}
    </div>;
  }
  return <div className="space-y-4"><div className="admin-panel overflow-hidden">
    {!data.items.length ? <EmptyState icon={BookOpen} title="Chưa có bài viết" description="Tạo bài viết đầu tiên hoặc quay lại khi nội dung được cập nhật." /> : <>
      <div className="grid gap-3 p-3 md:hidden">{data.items.map((article) => <article key={article.id} className="rounded-xl border border-[#ddd8cc] bg-white p-4"><Link href={editHref(article.id)} className="admin-link block text-base">{article.title}</Link><p className="mt-1 truncate text-xs text-[#56635b]">/{article.slug}</p><div className="mt-3 flex flex-wrap items-center justify-between gap-2"><StatusBadge status={article.status} /><span className="text-xs text-[#56635b]">{new Date(article.updatedAtUtc).toLocaleDateString("vi-VN")}</span></div><div className="mt-4 border-t border-[#eeeae1] pt-3">{actions(article)}</div></article>)}</div>
      <div className="hidden overflow-x-auto md:block"><table className="admin-table min-w-[640px]"><thead><tr><th>Tiêu đề</th><th>Trạng thái</th><th>Cập nhật</th><th>Thao tác</th></tr></thead><tbody>{data.items.map((article) => <tr key={article.id}><td><Link href={editHref(article.id)} className="admin-link">{article.title}</Link><p className="mt-1 max-w-72 truncate text-xs text-[#56635b]">/{article.slug}</p></td><td><StatusBadge status={article.status} /></td><td className="whitespace-nowrap text-xs">{new Date(article.updatedAtUtc).toLocaleDateString("vi-VN")}</td><td>{actions(article)}</td></tr>)}</tbody></table></div>
    </>}
  </div>{data.totalPages > 1 && <nav aria-label="Phân trang bài viết" className="flex flex-wrap items-center justify-between gap-3 text-sm"><span>Trang {data.page}/{data.totalPages}</span><div className="flex gap-3">{data.page > 1 && <Link href={`/admin/articles?page=${data.page - 1}`} className="admin-link inline-flex min-h-10 items-center">Trước</Link>}{data.page < data.totalPages && <Link href={`/admin/articles?page=${data.page + 1}`} className="admin-link inline-flex min-h-10 items-center">Sau</Link>}</div></nav>}</div>;
}
