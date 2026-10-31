"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";
import type { AdminArticleDetail, ArticleCategory } from "@/lib/api/article-types";
import { TiptapArticleEditor } from "@/components/admin/tiptap-article-editor";
import { SeoImagePicker } from "@/components/admin/seo-image-picker";
import { SeoPreview } from "@/components/admin/seo-preview";

const emptyDocument = JSON.stringify({ type: "doc", content: [{ type: "paragraph" }] });

export function ArticleForm({ initial, categories, canWrite, canPublish, canonicalOrigin }: Readonly<{
  initial?: AdminArticleDetail; categories: ArticleCategory[]; canWrite: boolean; canPublish: boolean; canonicalOrigin: string;
}>) {
  const router = useRouter();
  const [slug, setSlug] = useState(initial?.slug ?? "");
  const [title, setTitle] = useState(initial?.title ?? "");
  const [excerpt, setExcerpt] = useState(initial?.excerpt ?? "");
  const [contentJson, setContentJson] = useState(initial?.contentJson ?? emptyDocument);
  const [coverAssetId, setCoverAssetId] = useState(initial?.coverAssetId ?? null);
  const [categoryIds, setCategoryIds] = useState<string[]>(initial?.categories.map((category) => category.id) ?? []);
  const [seoTitle, setSeoTitle] = useState(initial?.seo.seoTitle ?? "");
  const [metaDescription, setMetaDescription] = useState(initial?.seo.metaDescription ?? "");
  const [robotsIndex, setRobotsIndex] = useState(initial?.seo.robotsIndex ?? true);
  const [canonicalOverride, setCanonicalOverride] = useState(initial?.seo.canonicalOverride ?? "");
  const [ogTitle, setOgTitle] = useState(initial?.seo.ogTitle ?? "");
  const [ogDescription, setOgDescription] = useState(initial?.seo.ogDescription ?? "");
  const [ogImageAssetId, setOgImageAssetId] = useState(initial?.seo.ogImageAssetId ?? null);
  const [busy, setBusy] = useState(false);
  const [preview, setPreview] = useState<string | null>(null);
  const [dirty, setDirty] = useState(false);
  const inputClass = "mt-1 w-full rounded border border-[#d8d1c3] bg-white px-3 py-2 text-sm";
  const markDirty = () => setDirty(true);

  async function save(event: FormEvent) {
    event.preventDefault();
    if (!canWrite) return;
    if (initial?.status === "Published" && slug.trim() !== initial.slug &&
      !window.confirm(`Đổi slug Published từ /kien-thuc/${initial.slug} sang /kien-thuc/${slug.trim()}? URL cũ sẽ chuyển hướng 301 vĩnh viễn.`)) return;
    setBusy(true);
    try {
      const payload = { slug: slug.trim(), title: title.trim(), excerpt: excerpt.trim(), contentJson,
        coverAssetId, categoryIds, seo: { seoTitle: seoTitle.trim() || null, metaDescription: metaDescription.trim() || null,
          canonicalOverride: canonicalOverride.trim() || null, robotsIndex, ogTitle: ogTitle.trim() || null,
          ogDescription: ogDescription.trim() || null, ogImageAssetId } };
      const saved = await adminRequest<AdminArticleDetail>(initial ? `articles/${initial.id}` : "articles",
        { method: initial ? "PATCH" : "POST", body: jsonBody(payload) });
      setDirty(false);
      toast.success("Đã lưu bài viết.");
      if (!initial) router.push(`/admin/articles/${saved.id}/edit`); else router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function action(name: "review" | "publish" | "archive") {
    if (!initial || dirty || !window.confirm(`Xác nhận ${name} bài viết?`)) return;
    setBusy(true);
    try { await adminRequest(`articles/${initial.id}/${name}`, { method: "POST" }); toast.success("Đã cập nhật trạng thái."); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function loadPreview() {
    if (!initial || dirty) return;
    try { const result = await adminRequest<AdminArticleDetail>(`articles/${initial.id}/preview`); setPreview(result.contentHtml); }
    catch (error) { toast.error(getErrorMessage(error)); }
  }

  return <main className="mx-auto max-w-4xl space-y-7"><header className="flex flex-wrap items-end justify-between gap-3"><div><Link href="/admin/articles" className="text-sm text-[#8d4f34]">← Bài viết</Link><h1 className="mt-2 font-serif text-3xl">{initial ? "Biên tập bài viết" : "Tạo bài viết"}</h1><p className="mt-2 text-sm text-[#718077]">{initial?.status ?? "Draft"} · Nội dung HTML được tạo và làm sạch trên server.</p></div>{initial?.status === "Published" && <Link href={`/kien-thuc/${initial.slug}`} target="_blank" className="text-sm underline">Xem trang public ↗</Link>}</header>
    <form onSubmit={(event) => void save(event)} className="space-y-7">
      <section className="grid gap-5 rounded-xl border border-[#ded7ca] bg-[#fbf9f4] p-5 sm:p-7"><label className="text-sm font-medium">Tiêu đề<input className={inputClass} value={title} onChange={(event) => { setTitle(event.target.value); markDirty(); }} maxLength={300} required disabled={!canWrite} /></label><label className="text-sm font-medium">Slug<input className={inputClass} value={slug} onChange={(event) => { setSlug(event.target.value); markDirty(); }} maxLength={200} pattern="[a-z0-9]+(-[a-z0-9]+)*" required disabled={!canWrite} /></label><label className="text-sm font-medium">Tóm tắt<textarea className={inputClass} rows={3} value={excerpt} onChange={(event) => { setExcerpt(event.target.value); markDirty(); }} maxLength={1000} disabled={!canWrite} /></label><div><p className="mb-2 text-sm font-medium">Nội dung</p><TiptapArticleEditor value={contentJson} onChange={(value) => { setContentJson(value); markDirty(); }} disabled={!canWrite} /></div></section>
      <section className="grid gap-5 rounded-xl border border-[#ded7ca] bg-[#fbf9f4] p-5 sm:p-7"><h2 className="font-serif text-xl">Phân loại và ảnh</h2><div><p className="mb-2 text-sm font-medium">Danh mục bài viết</p><div className="flex flex-wrap gap-4">{categories.map((category) => <label key={category.id} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={categoryIds.includes(category.id)} disabled={!canWrite} onChange={(event) => { setCategoryIds(event.target.checked ? [...categoryIds, category.id] : categoryIds.filter((id) => id !== category.id)); markDirty(); }} />{category.name}</label>)}</div>{!categories.length && <Link href="/admin/article-categories" className="text-sm underline">Tạo danh mục bài viết</Link>}</div><SeoImagePicker value={coverAssetId} onChange={(id) => { setCoverAssetId(id); markDirty(); }} label="Ảnh bìa" disabled={!canWrite} /></section>
      <section className="grid gap-5 rounded-xl border border-[#ded7ca] bg-[#fbf9f4] p-5 sm:p-7"><h2 className="font-serif text-xl">SEO</h2><label className="text-sm font-medium">SEO title<input className={inputClass} value={seoTitle} onChange={(event) => { setSeoTitle(event.target.value); markDirty(); }} maxLength={200} disabled={!canWrite} /></label><label className="text-sm font-medium">Meta description<textarea className={inputClass} rows={3} value={metaDescription} onChange={(event) => { setMetaDescription(event.target.value); markDirty(); }} maxLength={500} disabled={!canWrite} /></label><label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={robotsIndex} disabled={!canWrite} onChange={(event) => { setRobotsIndex(event.target.checked); markDirty(); }} />Cho phép index sau Publish</label><label className="text-sm font-medium">Canonical override (HTTPS, để trống nếu dùng URL hiện tại)<input className={inputClass} type="url" value={canonicalOverride} onChange={(event) => { setCanonicalOverride(event.target.value); markDirty(); }} disabled={!canWrite} /></label><label className="text-sm font-medium">OG title<input className={inputClass} value={ogTitle} onChange={(event) => { setOgTitle(event.target.value); markDirty(); }} maxLength={200} disabled={!canWrite} /></label><label className="text-sm font-medium">OG description<textarea className={inputClass} rows={2} value={ogDescription} onChange={(event) => { setOgDescription(event.target.value); markDirty(); }} maxLength={500} disabled={!canWrite} /></label><SeoImagePicker value={ogImageAssetId} onChange={(id) => { setOgImageAssetId(id); markDirty(); }} disabled={!canWrite} /></section>
      <SeoPreview origin={canonicalOrigin} title={seoTitle || title} description={metaDescription || excerpt} canonical={canonicalOverride || `/kien-thuc/${slug || "slug"}`} robotsIndex={robotsIndex} published={initial?.status === "Published"} titleInputLength={seoTitle.length} descriptionInputLength={metaDescription.length} />
      <div className="flex flex-wrap gap-3"><button type="submit" disabled={!canWrite || busy} className="rounded bg-[#183c31] px-5 py-3 text-sm text-white disabled:opacity-50">{busy ? "Đang xử lý…" : "Lưu bài viết"}</button>{initial && <button type="button" disabled={busy || dirty} onClick={() => void loadPreview()} className="rounded border border-[#183c31] px-5 py-3 text-sm">Preview đã lưu</button>}{initial?.status === "Draft" && canWrite && <button type="button" disabled={busy || dirty} onClick={() => void action("review")} className="rounded border border-[#183c31] px-5 py-3 text-sm">Gửi duyệt</button>}{initial?.status === "InReview" && canPublish && <button type="button" disabled={busy || dirty} onClick={() => void action("publish")} className="rounded border border-[#183c31] px-5 py-3 text-sm">Publish</button>}{initial?.status === "Published" && canPublish && <button type="button" disabled={busy || dirty} onClick={() => void action("archive")} className="rounded border border-[#a33d32] px-5 py-3 text-sm text-[#a33d32]">Archive</button>}</div>
      {dirty && initial && <p className="text-sm text-[#8d4f34]">Lưu thay đổi trước khi chuyển trạng thái hoặc preview.</p>}
    </form>
    {preview !== null && <section className="rounded-xl border border-[#ded7ca] bg-white p-6"><h2 className="mb-4 font-serif text-xl">Preview HTML đã lưu</h2><div className="catalog-prose" dangerouslySetInnerHTML={{ __html: preview }} /></section>}
  </main>;
}
