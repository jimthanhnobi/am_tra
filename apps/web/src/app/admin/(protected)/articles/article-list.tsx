"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { adminRequest } from "@/lib/api/client";
import type { ArticlePage } from "@/lib/api/article-types";
import { getErrorMessage } from "@/lib/admin-format";

export function ArticleList({ data, canPublish }: Readonly<{ data: ArticlePage; canPublish: boolean }>) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  async function transition(id: string, action: "publish" | "archive") {
    if (!window.confirm(action === "publish" ? "Xuất bản bài viết này?" : "Lưu trữ bài viết này?")) return;
    setBusy(id);
    try { await adminRequest(`articles/${id}/${action}`, { method: "POST" }); toast.success("Đã cập nhật trạng thái."); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(null); }
  }
  return <div className="overflow-x-auto rounded-xl border border-[#ded7ca] bg-white"><table className="w-full min-w-[650px] text-left text-sm"><thead className="bg-[#f3efe6] text-[#536159]"><tr><th className="p-4">Tiêu đề</th><th className="p-4">Trạng thái</th><th className="p-4">Cập nhật</th><th className="p-4">Thao tác</th></tr></thead><tbody>{data.items.map((article) => <tr key={article.id} className="border-t border-[#eee8dc]"><td className="p-4"><Link className="font-medium text-[#183c31] underline" href={`/admin/articles/${article.id}/edit`}>{article.title}</Link><p className="mt-1 text-xs text-[#718077]">/{article.slug}</p></td><td className="p-4">{article.status}</td><td className="p-4">{new Date(article.updatedAtUtc).toLocaleDateString("vi-VN")}</td><td className="p-4">{canPublish && article.status === "InReview" && <button disabled={busy === article.id} onClick={() => void transition(article.id, "publish")} className="text-[#8d4f34] underline">Publish</button>}{canPublish && article.status === "Published" && <button disabled={busy === article.id} onClick={() => void transition(article.id, "archive")} className="text-[#8d4f34] underline">Archive</button>}</td></tr>)}</tbody></table>{!data.items.length && <p className="p-8 text-center text-[#718077]">Chưa có bài viết.</p>}</div>;
}
