"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { z } from "zod";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { AdminApiError } from "@/lib/api/errors";
import type { AdminBrand, AdminCategoryNode, AdminProductDetail, ProductUpsertPayload, SeoDto } from "@/lib/api/catalog-types";
import { getErrorMessage } from "@/lib/admin-format";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { FieldError, Input, Label, Select, Textarea } from "@/components/ui/field";
import { StatusBadge } from "@/components/ui/status-badge";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { SeoImagePicker } from "@/components/admin/seo-image-picker";
import { ProductGallery } from "./product-gallery";

type Props = Readonly<{ initial?: AdminProductDetail; categories: readonly AdminCategoryNode[]; brands: readonly AdminBrand[]; canWrite: boolean; canUpload: boolean; canPublish: boolean }>;
type SpecRow = { id: number; key: string; value: string };
type FormValues = {
  name: string; sku: string; slug: string; summary: string; productType: string; materialText: string; capacityMl: string;
  primaryCategoryId: string; brandId: string; categoryIds: string[]; descriptionText: string; seo: SeoDto;
};
const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const schema = z.object({ name: z.string().trim().min(1, "Nhập tên sản phẩm.").max(300), slug: z.string().regex(slugPattern, "Slug phải là chữ thường không dấu, ngăn bằng dấu gạch ngang.").max(200), productType: z.string().trim().min(1, "Nhập loại sản phẩm.").max(100), summary: z.string().max(1000), sku: z.string().max(100), materialText: z.string().max(300), capacityMl: z.string(), primaryCategoryId: z.string(), brandId: z.string(), categoryIds: z.array(z.string()), descriptionText: z.string(), seo: z.any() });
const blankSeo: SeoDto = { seoTitle: null, metaDescription: null, canonicalOverride: null, robotsIndex: true, ogTitle: null, ogDescription: null, ogImageAssetId: null };

function allCategories(nodes: readonly AdminCategoryNode[], depth = 0): { id: string; name: string; status: string }[] {
  return nodes.flatMap((node) => [{ id: node.id, name: `${"— ".repeat(depth)}${node.name}`, status: node.status }, ...allCategories(node.children, depth + 1)]);
}
function extractText(value: unknown): string {
  if (!value || typeof value !== "object") return "";
  const node = value as { text?: unknown; content?: unknown };
  if (typeof node.text === "string") return node.text;
  return Array.isArray(node.content) ? node.content.map(extractText).filter(Boolean).join(node.content.some((child: unknown) => typeof child === "object" && child !== null && (child as { type?: string }).type === "paragraph") ? "\n" : "") : "";
}
function makeDescription(text: string): Record<string, unknown> {
  return { type: "doc", content: text.split(/\r?\n/).filter(Boolean).map((line) => ({ type: "paragraph", content: [{ type: "text", text: line }] })) };
}
function escapeHtml(text: string): string { return text.replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character] ?? character); }
function makeHtml(text: string): string { return text.split(/\r?\n/).filter(Boolean).map((line) => `<p>${escapeHtml(line)}</p>`).join(""); }
function emptyToNull(value: string): string | null { return value.trim() || null; }

export function ProductEditor({ initial, categories, brands, canWrite, canUpload, canPublish }: Props) {
  const router = useRouter();
  const flatCategories = allCategories(categories);
  const [product, setProduct] = useState(initial);
  const [values, setValues] = useState<FormValues>(() => ({ name: initial?.name ?? "", sku: initial?.sku ?? "", slug: initial?.slug ?? "", summary: initial?.summary ?? "", productType: initial?.productType ?? "", materialText: initial?.materialText ?? "", capacityMl: initial?.capacityMl?.toString() ?? "", primaryCategoryId: initial?.primaryCategoryId ?? "", brandId: initial?.brandId ?? "", categoryIds: initial?.categories.map((category) => category.id) ?? [], descriptionText: initial ? extractText(initial.description) : "", seo: initial?.seo ?? blankSeo }));
  const [specRows, setSpecRows] = useState<SpecRow[]>(() => Object.entries(initial?.specifications ?? {}).map(([key, value], index) => ({ id: index, key, value: String(value) })));
  const [nextSpecId, setNextSpecId] = useState(Object.keys(initial?.specifications ?? {}).length);
  const [descriptionDirty, setDescriptionDirty] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => { if (dirty) { event.preventDefault(); event.returnValue = ""; } };
    const warnLink = (event: MouseEvent) => {
      const target = event.target;
      const link = target instanceof Element ? target.closest("a[href]") : null;
      if (dirty && link && !window.confirm("Bạn có thay đổi chưa lưu. Rời trang này?")) event.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    document.addEventListener("click", warnLink, true);
    return () => { window.removeEventListener("beforeunload", warn); document.removeEventListener("click", warnLink, true); };
  }, [dirty]);
  function change<K extends keyof FormValues>(key: K, value: FormValues[K]) { setValues((current) => ({ ...current, [key]: value })); setDirty(true); }
  function changeSeo<K extends keyof SeoDto>(key: K, value: SeoDto[K]) { setValues((current) => ({ ...current, seo: { ...current.seo, [key]: value } })); setDirty(true); }
  function toggleCategory(id: string) { change("categoryIds", values.categoryIds.includes(id) ? values.categoryIds.filter((item) => item !== id) : [...values.categoryIds, id]); }
  function updateSpec(id: number, part: "key" | "value", value: string) { setSpecRows((rows) => rows.map((row) => row.id === id ? { ...row, [part]: value } : row)); setDirty(true); }
  function clearError() { setErrors({}); }
  function payload(status?: "InReview"): ProductUpsertPayload | null {
    const result = schema.safeParse(values);
    if (!result.success) { setErrors(Object.fromEntries(result.error.issues.map((issue) => [String(issue.path[0]), issue.message]))); toast.error("Vui lòng kiểm tra các trường bắt buộc."); return null; }
    const specifications: Record<string, string | number | boolean> = {};
    for (const row of specRows) {
      const key = row.key.trim();
      if (!key || key === "__proto__" || key === "constructor" || key === "prototype" || Object.hasOwn(specifications, key)) { setErrors({ specifications: "Mỗi thông số cần tên duy nhất và hợp lệ." }); return null; }
      specifications[key] = row.value.trim();
    }
    const capacityMl = values.capacityMl.trim() ? Number(values.capacityMl) : null;
    if (capacityMl !== null && (!Number.isInteger(capacityMl) || capacityMl <= 0)) { setErrors({ capacityMl: "Dung tích phải là số nguyên dương." }); return null; }
    const categoryIds = [...new Set([...values.categoryIds, ...(values.primaryCategoryId ? [values.primaryCategoryId] : [])])];
    return { sku: emptyToNull(values.sku), slug: values.slug.trim(), name: values.name.trim(), summary: values.summary.trim(), description: descriptionDirty ? makeDescription(values.descriptionText) : product?.description ?? makeDescription(values.descriptionText), descriptionHtml: descriptionDirty ? makeHtml(values.descriptionText) : product?.descriptionHtml ?? makeHtml(values.descriptionText), productType: values.productType.trim(), materialText: emptyToNull(values.materialText), capacityMl, specifications, primaryCategoryId: values.primaryCategoryId || null, brandId: values.brandId || null, categoryIds, seo: values.seo, ...(status ? { status } : {}) };
  }
  async function save(status?: "InReview") {
    if (!canWrite || busy) return;
    clearError();
    const body = payload(status);
    if (!body) return;
    setBusy(true);
    try {
      const updated = await adminRequest<AdminProductDetail>(product ? `products/${product.id}` : "products", { method: product ? "PATCH" : "POST", body: jsonBody(body) });
      setProduct(updated); setDirty(false); setDescriptionDirty(false); toast.success(status ? "Đã gửi duyệt sản phẩm." : "Đã lưu sản phẩm.");
      if (!product) router.replace(`/admin/products/${updated.id}/edit`);
      router.refresh();
    } catch (error) {
      if (error instanceof AdminApiError) setErrors(Object.fromEntries(Object.entries(error.problem.errors ?? {}).map(([key, messages]) => [key.toLowerCase(), messages.join(" ")])));
      toast.error(getErrorMessage(error));
    } finally { setBusy(false); }
  }
  async function transition(action: "publish" | "archive") {
    if (!product || busy) return;
    if (dirty) { toast.error("Lưu các thay đổi trước khi chuyển trạng thái."); return; }
    setBusy(true);
    try { const updated = await adminRequest<AdminProductDetail>(`products/${product.id}/${action}`, { method: "POST" }); setProduct(updated); toast.success(action === "publish" ? "Đã xuất bản." : "Đã lưu trữ."); router.refresh(); }
    catch (error) { if (error instanceof AdminApiError) setErrors(Object.fromEntries(Object.entries(error.problem.errors ?? {}).map(([key, messages]) => [key.toLowerCase(), messages.join(" ")]))); toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }
  const err = (key: string) => errors[key.toLowerCase()];
  return <main className="space-y-6"><div className="flex flex-wrap items-start justify-between gap-4"><div><Link href="/admin/products" className="text-sm text-[#8d4f34] hover:underline">← Sản phẩm</Link><h1 className="mt-2 font-serif text-3xl">{product ? product.name : "Thêm sản phẩm"}</h1><div className="mt-2 flex items-center gap-3 text-sm text-[#718077]">{product ? <StatusBadge status={product.status} /> : <span>Bản nháp mới</span>}{dirty && <span className="font-medium text-[#a33d32]">Có thay đổi chưa lưu</span>}</div></div><div className="flex flex-wrap gap-2">{canWrite && <Button disabled={busy} onClick={() => void save()}>{busy ? "Đang xử lý…" : product ? "Lưu thay đổi" : "Lưu bản nháp"}</Button>}{canWrite && product?.status === "Draft" && <Button variant="secondary" disabled={busy} onClick={() => void save("InReview")}>Gửi duyệt</Button>}{canPublish && product?.status === "InReview" && <Button variant="secondary" disabled={busy} onClick={() => void transition("publish")}>Xuất bản</Button>}{canPublish && product?.status === "Published" && <ConfirmDialog trigger={<Button variant="danger" disabled={busy}>Lưu trữ</Button>} title="Lưu trữ sản phẩm?" description="Sản phẩm sẽ không còn hiển thị công khai." confirmLabel="Lưu trữ" danger onConfirm={() => transition("archive")} />}</div></div>
    {Object.keys(errors).length > 0 && <Card className="border-[#d8a6a0] p-4 text-sm text-[#a33d32]" role="alert"><p className="font-semibold">API từ chối yêu cầu:</p><ul className="mt-2 list-disc pl-5">{Object.entries(errors).map(([key, message]) => <li key={key}>{key}: {message}</li>)}</ul></Card>}
    <fieldset disabled={!canWrite || busy} className="space-y-6"><Card className="p-6"><h2 className="font-serif text-xl">Thông tin cơ bản</h2><div className="mt-5 grid gap-4 md:grid-cols-2"><div><Label htmlFor="name">Tên sản phẩm *</Label><Input id="name" value={values.name} onChange={(event) => change("name", event.target.value)} /><FieldError message={err("name")} /></div><div><Label htmlFor="sku">SKU</Label><Input id="sku" value={values.sku} onChange={(event) => change("sku", event.target.value)} /></div><div><Label htmlFor="slug">Slug *</Label><Input id="slug" value={values.slug} disabled={product?.status === "Published"} onChange={(event) => change("slug", event.target.value)} /><FieldError message={err("slug")} /><p className="mt-1 text-xs text-[#718077]">/san-pham/{values.slug || "slug"}</p></div><div><Label htmlFor="type">Loại sản phẩm *</Label><Input id="type" value={values.productType} onChange={(event) => change("productType", event.target.value)} /><FieldError message={err("productType")} /></div><div className="md:col-span-2"><Label htmlFor="summary">Tóm tắt</Label><Textarea id="summary" value={values.summary} onChange={(event) => change("summary", event.target.value)} maxLength={1000} /></div><div><Label htmlFor="brand">Thương hiệu</Label><Select id="brand" value={values.brandId} onChange={(event) => change("brandId", event.target.value)}><option value="">Không chọn</option>{brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}</Select></div><div><Label htmlFor="primary">Danh mục chính</Label><Select id="primary" value={values.primaryCategoryId} onChange={(event) => change("primaryCategoryId", event.target.value)}><option value="">Không chọn</option>{flatCategories.map((category) => <option key={category.id} value={category.id}>{category.name} ({category.status})</option>)}</Select><FieldError message={err("primaryCategoryId")} /></div><div className="md:col-span-2"><Label>Danh mục bổ sung</Label><div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{flatCategories.map((category) => <label key={category.id} className="flex items-center gap-2 rounded-lg border border-[#e5dfd2] p-2 text-sm"><input type="checkbox" checked={values.categoryIds.includes(category.id) || values.primaryCategoryId === category.id} disabled={values.primaryCategoryId === category.id} onChange={() => toggleCategory(category.id)} />{category.name}</label>)}</div></div></div></Card>
    <Card className="p-6"><h2 className="font-serif text-xl">Thông số sản phẩm</h2><div className="mt-5 grid gap-4 sm:grid-cols-2"><div><Label htmlFor="material">Chất liệu</Label><Input id="material" value={values.materialText} onChange={(event) => change("materialText", event.target.value)} /></div><div><Label htmlFor="capacity">Dung tích (ml)</Label><Input id="capacity" type="number" min="1" step="1" value={values.capacityMl} onChange={(event) => change("capacityMl", event.target.value)} /><FieldError message={err("capacityMl")} /></div></div><div className="mt-5 space-y-2">{specRows.map((row) => <div className="flex gap-2" key={row.id}><Input aria-label="Tên thông số" placeholder="Tên thông số" value={row.key} onChange={(event) => updateSpec(row.id, "key", event.target.value)} /><Input aria-label="Giá trị thông số" placeholder="Giá trị" value={row.value} onChange={(event) => updateSpec(row.id, "value", event.target.value)} /><Button type="button" variant="ghost" onClick={() => { setSpecRows((rows) => rows.filter((item) => item.id !== row.id)); setDirty(true); }}>Xóa</Button></div>)}</div><FieldError message={err("specifications")} /><Button className="mt-3" type="button" variant="secondary" onClick={() => { setSpecRows((rows) => [...rows, { id: nextSpecId, key: "", value: "" }]); setNextSpecId((id) => id + 1); setDirty(true); }}>+ Thêm thông số</Button></Card>
    <Card className="p-6"><h2 className="font-serif text-xl">Mô tả</h2><p className="mt-1 text-sm text-[#718077]">Nhập văn bản thuần; nội dung được chuyển thành đoạn văn an toàn khi lưu.</p><Textarea className="mt-4 min-h-52" aria-label="Mô tả sản phẩm" value={values.descriptionText} onChange={(event) => { change("descriptionText", event.target.value); setDescriptionDirty(true); }} /></Card>
    </fieldset>
    <ProductGallery product={product} canWrite={canWrite} canUpload={canUpload} onRefresh={() => router.refresh()} />
    <fieldset disabled={!canWrite || busy}><Card className="p-6"><h2 className="font-serif text-xl">Thiết lập SEO</h2><div className="mt-5 grid gap-4 md:grid-cols-2"><div><Label htmlFor="seoTitle">SEO title</Label><Input id="seoTitle" maxLength={200} value={values.seo.seoTitle ?? ""} onChange={(event) => changeSeo("seoTitle", emptyToNull(event.target.value))} /></div><div><Label htmlFor="metaDescription">Meta description</Label><Input id="metaDescription" maxLength={500} value={values.seo.metaDescription ?? ""} onChange={(event) => changeSeo("metaDescription", emptyToNull(event.target.value))} /></div><div><Label htmlFor="canonical">Canonical override</Label><Input id="canonical" type="url" value={values.seo.canonicalOverride ?? ""} onChange={(event) => changeSeo("canonicalOverride", emptyToNull(event.target.value))} placeholder="https://…" /></div><div><Label htmlFor="ogTitle">OG title</Label><Input id="ogTitle" maxLength={200} value={values.seo.ogTitle ?? ""} onChange={(event) => changeSeo("ogTitle", emptyToNull(event.target.value))} /></div><div><Label htmlFor="ogDescription">OG description</Label><Input id="ogDescription" maxLength={500} value={values.seo.ogDescription ?? ""} onChange={(event) => changeSeo("ogDescription", emptyToNull(event.target.value))} /></div><SeoImagePicker value={values.seo.ogImageAssetId} onChange={(id) => changeSeo("ogImageAssetId", id)} /><label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={values.seo.robotsIndex} onChange={(event) => changeSeo("robotsIndex", event.target.checked)} /> Cho phép index khi xuất bản</label></div><div className="mt-6 rounded-xl border border-[#ded7ca] bg-[#faf9f5] p-4"><p className="text-xs text-[#718077]">Xem trước kết quả tìm kiếm</p><p className="mt-2 text-lg text-[#315d90]">{values.seo.seoTitle || values.name || "Tiêu đề sản phẩm"}</p><p className="text-xs text-[#477043]">/san-pham/{values.slug || "slug"}</p><p className="mt-1 text-sm text-[#626f66]">{values.seo.metaDescription || values.summary || "Mô tả sản phẩm"}</p></div></Card></fieldset>
  </main>;
}
