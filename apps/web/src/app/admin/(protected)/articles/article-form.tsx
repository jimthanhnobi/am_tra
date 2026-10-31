"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { AdminApiError } from "@/lib/api/errors";
import { getErrorMessage } from "@/lib/admin-format";
import type { AdminArticleDetail, ArticleCategory } from "@/lib/api/article-types";
import { TiptapArticleEditor } from "@/components/admin/tiptap-article-editor";
import { SeoImagePicker } from "@/components/admin/seo-image-picker";
import { SeoPreview } from "@/components/admin/seo-preview";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { FieldError, Input, Textarea } from "@/components/ui/field";
import { StatusBadge } from "@/components/ui/status-badge";

const emptyDocument = JSON.stringify({ type: "doc", content: [{ type: "paragraph" }] });
const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function ArticleForm({ initial, categories, canWrite, canPublish, canonicalOrigin, returnTo = "/admin/articles" }: Readonly<{
  initial?: AdminArticleDetail; categories: ArticleCategory[]; canWrite: boolean; canPublish: boolean; canonicalOrigin: string; returnTo?: string;
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
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    function warn(event: BeforeUnloadEvent) { if (dirty) { event.preventDefault(); event.returnValue = ""; } }
    function warnLink(event: MouseEvent) {
      const link = event.target instanceof Element ? event.target.closest("a[href]") : null;
      if (dirty && link && !link.getAttribute("href")?.startsWith("#") && !window.confirm("Bạn có thay đổi chưa lưu. Rời trang này?")) event.preventDefault();
    }
    window.addEventListener("beforeunload", warn);
    document.addEventListener("click", warnLink, true);
    return () => { window.removeEventListener("beforeunload", warn); document.removeEventListener("click", warnLink, true); };
  }, [dirty]);

  async function save(event: FormEvent) {
    event.preventDefault();
    if (!canWrite || busy) return;
    const validation: Record<string, string> = {};
    if (!title.trim()) validation.title = "Nhập tiêu đề bài viết.";
    if (!slugPattern.test(slug.trim())) validation.slug = "Slug dùng chữ thường không dấu và dấu gạch ngang.";
    if (Object.keys(validation).length) { setErrors(validation); toast.error("Kiểm tra các trường bắt buộc."); return; }
    if (initial?.status === "Published" && slug.trim() !== initial.slug &&
      !window.confirm(`Đổi slug Published từ /kien-thuc/${initial.slug} sang /kien-thuc/${slug.trim()}? URL cũ sẽ chuyển hướng 301 vĩnh viễn.`)) return;
    setErrors({}); setBusy(true);
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
    } catch (error) {
      if (error instanceof AdminApiError) setErrors(Object.fromEntries(Object.entries(error.problem.errors ?? {}).map(([key, messages]) => [key.toLowerCase(), messages.join(" ")])));
      toast.error(getErrorMessage(error));
    } finally { setBusy(false); }
  }

  async function action(name: "review" | "publish" | "archive") {
    if (!initial || dirty || busy) return;
    setBusy(true);
    try { await adminRequest(`articles/${initial.id}/${name}`, { method: "POST" }); toast.success("Đã cập nhật trạng thái."); router.refresh(); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function loadPreview() {
    if (!initial || dirty || busy) return;
    setBusy(true);
    try { const result = await adminRequest<AdminArticleDetail>(`articles/${initial.id}/preview`); setPreview(result.contentHtml); }
    catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  const field = "block text-sm font-semibold text-[#233b32]";
  return <main className="mx-auto max-w-5xl space-y-6">
    <header className="admin-page-header"><div><Link href={returnTo} className="admin-link text-sm">← Bài viết</Link><p className="admin-eyebrow mt-4">Biên tập nội dung</p><h1 className="admin-title">{initial ? "Biên tập bài viết" : "Tạo bài viết"}</h1><div className="mt-3 flex flex-wrap items-center gap-3 text-sm text-[#56635b]"><StatusBadge status={initial?.status ?? "Draft"} /><span>HTML công khai được tạo và làm sạch trên server.</span>{dirty && <span className="font-semibold text-[#925b3d]">Có thay đổi chưa lưu</span>}</div></div>{initial?.status === "Published" && <Link href={`/kien-thuc/${initial.slug}`} target="_blank" rel="noopener noreferrer" className="admin-link text-sm">Xem trang public ↗</Link>}</header>
    <nav aria-label="Các phần của bài viết" className="admin-editor-nav"><a href="#article-basic">Thông tin</a><a href="#article-content">Nội dung</a><a href="#article-media">Phân loại & ảnh</a><a href="#article-seo">SEO</a><a href="#article-publishing">Xuất bản</a></nav>
    {Object.values(errors).some(Boolean) && <div role="alert" className="rounded-xl border border-[#d8a6a0] bg-[#fff5f3] p-4 text-sm text-[#8d3028]"><p className="font-semibold">Kiểm tra thông tin sau:</p><ul className="mt-2 list-disc pl-5">{Object.entries(errors).filter(([, message]) => Boolean(message)).map(([key, message]) => <li key={key}>{message}</li>)}</ul></div>}
    <form onSubmit={(event) => void save(event)} noValidate className="space-y-6">
      <section id="article-basic" className="admin-editor-section admin-panel grid gap-5 p-5 sm:p-7"><div><h2 className="admin-section-title">Thông tin cơ bản</h2><p className="admin-description">Tiêu đề và slug xác định cách bài viết xuất hiện trên website.</p></div><label className={field}>Tiêu đề <span aria-hidden="true">*</span><Input className="mt-2" value={title} onChange={(event) => { setTitle(event.target.value); setDirty(true); setErrors((current) => ({ ...current, title: "" })); }} maxLength={300} aria-invalid={Boolean(errors.title)} disabled={!canWrite} /><FieldError message={errors.title} /></label><label className={field}>Slug <span aria-hidden="true">*</span><Input className="mt-2" value={slug} onChange={(event) => { setSlug(event.target.value); setDirty(true); setErrors((current) => ({ ...current, slug: "" })); }} maxLength={200} aria-invalid={Boolean(errors.slug)} disabled={!canWrite} /><FieldError message={errors.slug} /><span className="mt-1 block text-xs font-normal text-[#56635b]">/kien-thuc/{slug || "slug"}</span></label><label className={field}>Tóm tắt<Textarea className="mt-2" rows={3} value={excerpt} onChange={(event) => { setExcerpt(event.target.value); setDirty(true); }} maxLength={1000} disabled={!canWrite} /></label></section>
      <section id="article-content" className="admin-editor-section admin-panel p-5 sm:p-7"><h2 className="admin-section-title">Nội dung bài viết</h2><p className="admin-description mb-5">Chọn đoạn văn rồi dùng thanh công cụ để định dạng. Nội dung chưa lưu sẽ được nhắc trước khi rời trang.</p><TiptapArticleEditor value={contentJson} onChange={(value) => { setContentJson(value); setDirty(true); }} disabled={!canWrite} /></section>
      <section id="article-media" className="admin-editor-section admin-panel grid gap-5 p-5 sm:p-7"><h2 className="admin-section-title">Phân loại và ảnh bìa</h2><div><p className={field}>Danh mục bài viết</p><div className="mt-3 flex flex-wrap gap-3">{categories.map((category) => <label key={category.id} className="flex min-h-11 items-center gap-2 rounded-lg border border-[#ddd8cc] bg-white px-3 text-sm"><input type="checkbox" checked={categoryIds.includes(category.id)} disabled={!canWrite} onChange={(event) => { setCategoryIds(event.target.checked ? [...categoryIds, category.id] : categoryIds.filter((id) => id !== category.id)); setDirty(true); }} />{category.name}</label>)}</div>{!categories.length && <Link href="/admin/article-categories" className="admin-link mt-2 inline-block text-sm">Tạo danh mục bài viết</Link>}</div><SeoImagePicker value={coverAssetId} onChange={(id) => { setCoverAssetId(id); setDirty(true); }} label="Ảnh bìa" disabled={!canWrite} /></section>
      <details id="article-seo" className="admin-editor-section admin-panel p-5 sm:p-7"><summary className="cursor-pointer text-[#233b32]"><span className="admin-section-title">Thiết lập SEO</span><span className="admin-description block">Mở để chỉnh metadata, canonical và trạng thái index.</span></summary><div className="mt-6 grid gap-5"><label className={field}>SEO title<Input className="mt-2" value={seoTitle} onChange={(event) => { setSeoTitle(event.target.value); setDirty(true); }} maxLength={200} disabled={!canWrite} /></label><label className={field}>Meta description<Textarea className="mt-2" rows={3} value={metaDescription} onChange={(event) => { setMetaDescription(event.target.value); setDirty(true); }} maxLength={500} disabled={!canWrite} /></label><label className="flex min-h-11 items-center gap-2 text-sm"><input type="checkbox" checked={robotsIndex} disabled={!canWrite} onChange={(event) => { setRobotsIndex(event.target.checked); setDirty(true); }} />Cho phép index sau Publish</label><label className={field}>Canonical override (HTTPS, để trống nếu dùng URL hiện tại)<Input className="mt-2" type="url" value={canonicalOverride} onChange={(event) => { setCanonicalOverride(event.target.value); setDirty(true); }} disabled={!canWrite} /></label><label className={field}>OG title<Input className="mt-2" value={ogTitle} onChange={(event) => { setOgTitle(event.target.value); setDirty(true); }} maxLength={200} disabled={!canWrite} /></label><label className={field}>OG description<Textarea className="mt-2" rows={2} value={ogDescription} onChange={(event) => { setOgDescription(event.target.value); setDirty(true); }} maxLength={500} disabled={!canWrite} /></label><SeoImagePicker value={ogImageAssetId} onChange={(id) => { setOgImageAssetId(id); setDirty(true); }} disabled={!canWrite} /><SeoPreview origin={canonicalOrigin} title={seoTitle || title} description={metaDescription || excerpt} canonical={canonicalOverride || `/kien-thuc/${slug || "slug"}`} robotsIndex={robotsIndex} published={initial?.status === "Published"} titleInputLength={seoTitle.length} descriptionInputLength={metaDescription.length} /></div></details>
      <section id="article-publishing" className="admin-editor-section admin-panel p-5 sm:p-7"><h2 className="admin-section-title">Xuất bản</h2><p className="admin-description">Lưu nội dung trước khi xem preview, gửi duyệt hoặc xuất bản. Bài viết chỉ hiện công khai sau khi Publish.</p><div className="mt-4"><StatusBadge status={initial?.status ?? "Draft"} /></div></section>
      <div className="admin-save-bar flex flex-wrap items-center gap-2"><Button type="submit" size="sm" disabled={!canWrite || busy}>{busy ? "Đang xử lý…" : initial ? "Lưu thay đổi" : "Lưu bản nháp"}</Button>{initial && <Button type="button" size="sm" variant="secondary" disabled={busy || dirty} onClick={() => void loadPreview()}>Preview đã lưu</Button>}{initial?.status === "Draft" && canWrite && <ConfirmDialog trigger={<Button type="button" size="sm" variant="secondary" disabled={busy || dirty}>Gửi duyệt</Button>} title="Gửi duyệt bài viết?" description="Lưu mọi thay đổi trước khi gửi duyệt." confirmLabel="Gửi duyệt" onConfirm={() => action("review")} />}{initial?.status === "InReview" && canPublish && <ConfirmDialog trigger={<Button type="button" size="sm" variant="secondary" disabled={busy || dirty}>Xuất bản</Button>} title="Xuất bản bài viết?" description="Bài viết sẽ hiển thị công khai theo thiết lập SEO đã lưu." confirmLabel="Xuất bản" onConfirm={() => action("publish")} />}{initial?.status === "Published" && canPublish && <ConfirmDialog trigger={<Button type="button" size="sm" variant="danger" disabled={busy || dirty}>Lưu trữ</Button>} title="Lưu trữ bài viết?" description="Bài viết sẽ không còn hiển thị công khai." confirmLabel="Lưu trữ" danger onConfirm={() => action("archive")} />}{dirty && <span className="text-xs font-medium text-[#925b3d]">Có thay đổi chưa lưu</span>}</div>
    </form>
    {preview !== null && <section className="admin-panel p-6"><h2 className="admin-section-title mb-4">Preview HTML đã lưu</h2><div className="catalog-prose" dangerouslySetInnerHTML={{ __html: preview }} /></section>}
  </main>;
}
