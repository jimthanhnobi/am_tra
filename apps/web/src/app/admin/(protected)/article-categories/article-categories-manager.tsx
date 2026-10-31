"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";
import type { ArticleCategory } from "@/lib/api/article-types";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input, Label } from "@/components/ui/field";

export function ArticleCategoriesManager({ initial, canWrite }: Readonly<{ initial: ArticleCategory[]; canWrite: boolean }>) {
  const router = useRouter();
  const [selected, setSelected] = useState<ArticleCategory | null>(null);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [busy, setBusy] = useState(false);
  async function save(event: FormEvent) {
    event.preventDefault();
    if (!canWrite || busy) return;
    setBusy(true);
    try { await adminRequest(`article-categories${selected ? `/${selected.id}` : ""}`, { method: selected ? "PATCH" : "POST", body: jsonBody({ name: name.trim(), slug: slug.trim() }) }); toast.success("Đã lưu danh mục."); setSelected(null); setName(""); setSlug(""); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }
  return <main className="mx-auto max-w-5xl space-y-6"><header className="admin-page-header"><div><Link href="/admin/articles" className="admin-link text-sm">← Bài viết</Link><p className="admin-eyebrow mt-4">CMS</p><h1 className="admin-title">Danh mục bài viết</h1><p className="admin-description">Phân nhóm nội dung để người đọc tìm đúng chủ đề.</p></div></header>
    <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]"><Card className="p-5 sm:p-6"><h2 className="admin-section-title">Danh sách</h2><div className="mt-4 divide-y divide-[#eeeae1]">{initial.map((category) => <div key={category.id} className="flex items-center justify-between gap-3 py-3"><div className="min-w-0"><strong className="block truncate text-sm text-[#233b32]">{category.name}</strong><p className="mt-1 truncate text-xs text-[#56635b]">/{category.slug}</p></div>{canWrite && <Button type="button" variant="ghost" size="sm" onClick={() => { setSelected(category); setName(category.name); setSlug(category.slug); }}>Sửa</Button>}</div>)}{!initial.length && <p className="py-6 text-sm text-[#56635b]">Chưa có danh mục bài viết.</p>}</div></Card>
      {canWrite && <form onSubmit={(event) => void save(event)} className="admin-panel grid gap-4 p-5 sm:p-6"><h2 className="admin-section-title">{selected ? "Sửa danh mục" : "Tạo danh mục"}</h2><div><Label htmlFor="article-category-name">Tên *</Label><Input id="article-category-name" value={name} maxLength={300} required onChange={(event) => setName(event.target.value)} /></div><div><Label htmlFor="article-category-slug">Slug *</Label><Input id="article-category-slug" value={slug} pattern="[a-z0-9]+(-[a-z0-9]+)*" maxLength={200} required onChange={(event) => setSlug(event.target.value)} /></div><div className="flex flex-wrap gap-2"><Button type="submit" disabled={busy}>{busy ? "Đang lưu…" : "Lưu danh mục"}</Button>{selected && <Button type="button" variant="ghost" onClick={() => { setSelected(null); setName(""); setSlug(""); }}>Hủy sửa</Button>}</div></form>}
    </div>
  </main>;
}
