"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";
import type { ArticleCategory } from "@/lib/api/article-types";

export function ArticleCategoriesManager({ initial, canWrite }: Readonly<{ initial: ArticleCategory[]; canWrite: boolean }>) {
  const router = useRouter();
  const [selected, setSelected] = useState<ArticleCategory | null>(null);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [busy, setBusy] = useState(false);
  async function save(event: FormEvent) {
    event.preventDefault(); setBusy(true);
    try { await adminRequest(`article-categories${selected ? `/${selected.id}` : ""}`, { method: selected ? "PATCH" : "POST", body: jsonBody({ name: name.trim(), slug: slug.trim() }) }); toast.success("Đã lưu danh mục."); setSelected(null); setName(""); setSlug(""); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }
  return <main className="mx-auto max-w-3xl space-y-6"><header><Link href="/admin/articles" className="text-sm text-[#8d4f34]">← Bài viết</Link><h1 className="mt-2 font-serif text-3xl">Danh mục bài viết</h1></header><div className="rounded-xl border border-[#ded7ca] bg-white p-5">{initial.map((category) => <div key={category.id} className="flex items-center justify-between border-b border-[#eee8dc] py-3"><div><strong>{category.name}</strong><p className="text-xs text-[#718077]">{category.slug}</p></div>{canWrite && <button type="button" className="text-sm underline" onClick={() => { setSelected(category); setName(category.name); setSlug(category.slug); }}>Sửa</button>}</div>)}{!initial.length && <p className="text-sm text-[#718077]">Chưa có danh mục.</p>}</div>{canWrite && <form onSubmit={(event) => void save(event)} className="grid gap-4 rounded-xl border border-[#ded7ca] bg-[#fbf9f4] p-5"><h2 className="font-serif text-xl">{selected ? "Sửa danh mục" : "Tạo danh mục"}</h2><label className="text-sm">Tên<input className="mt-1 w-full rounded border p-2" value={name} maxLength={300} required onChange={(event) => setName(event.target.value)} /></label><label className="text-sm">Slug<input className="mt-1 w-full rounded border p-2" value={slug} pattern="[a-z0-9]+(-[a-z0-9]+)*" maxLength={200} required onChange={(event) => setSlug(event.target.value)} /></label><div className="flex gap-3"><button type="submit" disabled={busy} className="rounded bg-[#183c31] px-5 py-2 text-sm text-white">Lưu</button>{selected && <button type="button" onClick={() => { setSelected(null); setName(""); setSlug(""); }} className="text-sm underline">Hủy sửa</button>}</div></form>}</main>;
}
