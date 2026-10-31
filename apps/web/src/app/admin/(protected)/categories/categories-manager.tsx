"use client";

import { useState } from "react";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { AdminApiError } from "@/lib/api/errors";
import type { AdminCategoryDetail, AdminCategoryNode, CategoryUpsertPayload, SeoDto } from "@/lib/api/catalog-types";
import { getErrorMessage } from "@/lib/admin-format";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input, Label, Select, Textarea } from "@/components/ui/field";
import { StatusBadge } from "@/components/ui/status-badge";
import { SeoImagePicker } from "@/components/admin/seo-image-picker";

const blankSeo: SeoDto = { seoTitle: null, metaDescription: null, canonicalOverride: null, robotsIndex: true, ogTitle: null, ogDescription: null, ogImageAssetId: null };
const blank: CategoryUpsertPayload = { parentId: null, slug: "", name: "", descriptionHtml: "", sortOrder: 0, seo: blankSeo };
const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
function flat(nodes: readonly AdminCategoryNode[], depth = 0): { node: AdminCategoryNode; label: string }[] { return nodes.flatMap((node) => [{ node, label: `${"— ".repeat(depth)}${node.name}` }, ...flat(node.children, depth + 1)]); }
function descendants(node: AdminCategoryNode): string[] { return [node.id, ...node.children.flatMap(descendants)]; }
function escapeHtml(text: string) { return text.replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character] ?? character); }
function stripHtml(html: string) {
  const document = new DOMParser().parseFromString(html.replace(/<\/p>/gi, "</p>\n"), "text/html");
  return document.body.textContent?.trim() ?? "";
}

export function CategoriesManager({ initial, canWrite }: Readonly<{ initial: readonly AdminCategoryNode[]; canWrite: boolean }>) {
  const [tree, setTree] = useState(initial);
  const [selected, setSelected] = useState<AdminCategoryDetail | null>(null);
  const [form, setForm] = useState<CategoryUpsertPayload>(blank);
  const [descriptionText, setDescriptionText] = useState("");
  const [descriptionDirty, setDescriptionDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const flattened = flat(tree);
  const ownNode = flattened.find(({ node }) => node.id === selected?.id)?.node;
  const forbiddenParents = ownNode ? descendants(ownNode) : [];
  function set<K extends keyof CategoryUpsertPayload>(key: K, value: CategoryUpsertPayload[K]) { setForm((current) => ({ ...current, [key]: value })); setDirty(true); }
  function setSeo<K extends keyof SeoDto>(key: K, value: SeoDto[K]) { setForm((current) => ({ ...current, seo: { ...current.seo, [key]: value } })); setDirty(true); }
  async function select(id: string | null) {
    if (dirty && !window.confirm("Bỏ các thay đổi chưa lưu?")) return;
    setErrors({}); setDirty(false);
    if (!id) { setSelected(null); setForm(blank); setDescriptionText(""); setDescriptionDirty(false); return; }
    try { const detail = await adminRequest<AdminCategoryDetail>(`categories/${id}`); setSelected(detail); setForm({ parentId: detail.parentId, slug: detail.slug, name: detail.name, descriptionHtml: detail.descriptionHtml, sortOrder: detail.sortOrder, seo: detail.seo }); setDescriptionText(stripHtml(detail.descriptionHtml)); setDescriptionDirty(false); }
    catch (error) { toast.error(getErrorMessage(error)); }
  }
  async function reload() { setTree(await adminRequest<AdminCategoryNode[]>("categories")); }
  async function save(status?: "InReview" | "Published" | "Archived") {
    if (busy || !canWrite) return;
    const validation: Record<string, string> = {};
    if (!form.name.trim()) validation.name = "Nhập tên danh mục.";
    if (!slugPattern.test(form.slug)) validation.slug = "Slug phải là chữ thường không dấu.";
    if (form.sortOrder < 0 || !Number.isInteger(form.sortOrder)) validation.sortOrder = "Thứ tự phải là số nguyên không âm.";
    if (Object.keys(validation).length) { setErrors(validation); return; }
    setBusy(true); setErrors({});
    try {
      const payload = { ...form, descriptionHtml: descriptionDirty ? `<p>${escapeHtml(descriptionText).replace(/\r?\n/g, "</p><p>")}</p>` : form.descriptionHtml, ...(status ? { status } : {}) };
      const detail = await adminRequest<AdminCategoryDetail>(selected ? `categories/${selected.id}` : "categories", { method: selected ? "PATCH" : "POST", body: jsonBody(payload) });
      setSelected(detail); setForm({ parentId: detail.parentId, slug: detail.slug, name: detail.name, descriptionHtml: detail.descriptionHtml, sortOrder: detail.sortOrder, seo: detail.seo }); setDirty(false); setDescriptionDirty(false); await reload(); toast.success("Đã lưu danh mục.");
    } catch (error) { if (error instanceof AdminApiError) setErrors(Object.fromEntries(Object.entries(error.problem.errors ?? {}).map(([key, messages]) => [key.toLowerCase(), messages.join(" ")]))); toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }
  return <main className="space-y-6"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[#9c6349]">Catalog</p><h1 className="mt-2 font-serif text-3xl">Danh mục</h1><p className="mt-1 text-sm text-[#718077]">Tổ chức danh mục theo cây và kiểm soát trạng thái xuất bản.</p></div><div className="grid gap-5 lg:grid-cols-[minmax(220px,1fr)_minmax(0,2fr)]"><Card className="p-4"><div className="mb-4 flex items-center justify-between"><h2 className="font-serif text-lg">Cây danh mục</h2>{canWrite && <Button size="sm" onClick={() => void select(null)}>+ Thêm</Button>}</div>{flattened.length ? <div className="space-y-1">{flattened.map(({ node, label }) => <button type="button" key={node.id} onClick={() => void select(node.id)} className={`flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-[#f4f0e7] ${selected?.id === node.id ? "bg-[#edf2ed] font-semibold" : ""}`}><span>{label}</span><StatusBadge status={node.status} /></button>)}</div> : <p className="text-sm text-[#718077]">Chưa có danh mục.</p>}</Card><Card className="p-6"><h2 className="font-serif text-xl">{selected ? `Sửa: ${selected.name}` : "Danh mục mới"}</h2>{selected && <div className="mt-2"><StatusBadge status={selected.status} /></div>}{Object.keys(errors).length > 0 && <div role="alert" className="mt-4 rounded-lg bg-[#f9e8e6] p-3 text-sm text-[#a33d32]">{Object.entries(errors).map(([key, value]) => <p key={key}>{key}: {value}</p>)}</div>}<fieldset disabled={!canWrite || busy} className="mt-5 grid gap-4 sm:grid-cols-2"><div><Label htmlFor="categoryName">Tên *</Label><Input id="categoryName" value={form.name} onChange={(event) => set("name", event.target.value)} /></div><div><Label htmlFor="categorySlug">Slug *</Label><Input id="categorySlug" value={form.slug} disabled={selected?.status === "Published"} onChange={(event) => set("slug", event.target.value)} /></div><div><Label htmlFor="categoryParent">Danh mục cha</Label><Select id="categoryParent" value={form.parentId ?? ""} onChange={(event) => set("parentId", event.target.value || null)}><option value="">Không có</option>{flattened.map(({ node, label }) => <option key={node.id} value={node.id} disabled={forbiddenParents.includes(node.id)}>{label}</option>)}</Select></div><div><Label htmlFor="categorySort">Thứ tự</Label><Input id="categorySort" type="number" min="0" step="1" value={form.sortOrder} onChange={(event) => set("sortOrder", Number(event.target.value))} /></div><div className="sm:col-span-2"><Label htmlFor="categoryDescription">Mô tả</Label><Textarea id="categoryDescription" value={descriptionText} onChange={(event) => { setDescriptionText(event.target.value); setDescriptionDirty(true); setDirty(true); }} /></div><div><Label htmlFor="categorySeoTitle">SEO title</Label><Input id="categorySeoTitle" maxLength={200} value={form.seo.seoTitle ?? ""} onChange={(event) => setSeo("seoTitle", event.target.value || null)} /></div><div><Label htmlFor="categoryMeta">Meta description</Label><Input id="categoryMeta" maxLength={500} value={form.seo.metaDescription ?? ""} onChange={(event) => setSeo("metaDescription", event.target.value || null)} /></div><div><Label htmlFor="categoryCanonical">Canonical override</Label><Input id="categoryCanonical" type="url" value={form.seo.canonicalOverride ?? ""} onChange={(event) => setSeo("canonicalOverride", event.target.value || null)} /></div><div><Label htmlFor="categoryOgTitle">OG title</Label><Input id="categoryOgTitle" value={form.seo.ogTitle ?? ""} onChange={(event) => setSeo("ogTitle", event.target.value || null)} /></div><div><Label htmlFor="categoryOgDescription">OG description</Label><Input id="categoryOgDescription" value={form.seo.ogDescription ?? ""} onChange={(event) => setSeo("ogDescription", event.target.value || null)} /></div><SeoImagePicker value={form.seo.ogImageAssetId} onChange={(id) => setSeo("ogImageAssetId", id)} /><label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.seo.robotsIndex} onChange={(event) => setSeo("robotsIndex", event.target.checked)} /> Cho phép index khi xuất bản</label></fieldset>{canWrite && <div className="mt-6 flex flex-wrap gap-2"><Button disabled={busy} onClick={() => void save()}>{busy ? "Đang lưu…" : "Lưu"}</Button>{selected?.status === "Draft" && <Button variant="secondary" disabled={busy} onClick={() => void save("InReview")}>Gửi duyệt</Button>}{selected?.status === "InReview" && <Button variant="secondary" disabled={busy} onClick={() => void save("Published")}>Xuất bản</Button>}{selected?.status === "Published" && <Button variant="danger" disabled={busy} onClick={() => { if (window.confirm("Lưu trữ danh mục này?")) void save("Archived"); }}>Lưu trữ</Button>}</div>}</Card></div></main>;
}
