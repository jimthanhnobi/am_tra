"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { ImageOff, PackageSearch } from "lucide-react";
import { toast } from "sonner";
import { AdminImage } from "@/components/admin/admin-image";
import { adminRequest } from "@/lib/api/client";
import type { AdminProductListItem, AdminCategoryNode, PagedResult } from "@/lib/api/catalog-types";
import { formatAdminDate, getErrorMessage } from "@/lib/admin-format";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Select, Input } from "@/components/ui/field";
import { StatusBadge } from "@/components/ui/status-badge";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { EmptyState } from "@/components/ui/empty-state";

type Props = Readonly<{ data: PagedResult<AdminProductListItem>; categories: readonly AdminCategoryNode[]; query: string; canWrite: boolean; canPublish: boolean }>;

function flatten(nodes: readonly AdminCategoryNode[], prefix = ""): { id: string; name: string }[] {
  return nodes.flatMap((node) => [{ id: node.id, name: `${prefix}${node.name}` }, ...flatten(node.children, `${prefix}— `)]);
}

export function ProductList({ data, categories, query, canWrite, canPublish }: Props) {
  const router = useRouter();
  const params = new URLSearchParams(query);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const returnTo = `/admin/products?${query}`;
  const editHref = (id: string) => `/admin/products/${id}/edit?returnTo=${encodeURIComponent(returnTo)}`;

  async function act(id: string, action: "publish" | "archive") {
    if (pendingId) return;
    setPendingId(id);
    try {
      await adminRequest(`products/${id}/${action}`, { method: "POST" });
      toast.success(action === "publish" ? "Đã xuất bản sản phẩm." : "Đã lưu trữ sản phẩm.");
      router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setPendingId(null); }
  }
  function pageHref(page: number) { const next = new URLSearchParams(query); next.set("page", String(page)); return `/admin/products?${next}`; }
  function actions(product: AdminProductListItem) {
    return <div className="flex flex-wrap items-center gap-2">
      <Link className="admin-link text-sm" href={editHref(product.id)}>{canWrite ? "Sửa" : "Xem"}</Link>
      {canPublish && product.status === "InReview" && <Button size="sm" disabled={Boolean(pendingId)} onClick={() => void act(product.id, "publish")}>Xuất bản</Button>}
      {canPublish && product.status === "Published" && <ConfirmDialog trigger={<Button size="sm" variant="ghost" disabled={Boolean(pendingId)}>Lưu trữ</Button>} title="Lưu trữ sản phẩm?" description="Sản phẩm sẽ không còn xuất hiện trên API công khai." confirmLabel="Lưu trữ" danger onConfirm={() => act(product.id, "archive")} />}
    </div>;
  }
  function thumbnail(product: AdminProductListItem) {
    return product.coverImage?.publicUrl
      ? <div className="size-12 overflow-hidden rounded-lg bg-[#e8e5dc]"><AdminImage src={product.coverImage.publicUrl} alt={product.coverImage.altText} width={product.coverImage.width} height={product.coverImage.height} loading="lazy" className="size-full object-cover" /></div>
      : <div className="grid size-12 place-items-center rounded-lg bg-[#eeeae2] text-[#56635b]"><ImageOff size={18} aria-hidden="true" /></div>;
  }

  return <div className="space-y-4">
    <Card className="p-4 sm:p-5"><form action="/admin/products" className="admin-filter-bar sm:grid-cols-2 xl:grid-cols-[minmax(12rem,1fr)_repeat(3,minmax(9rem,auto))_auto_auto]">
      <Input name="search" aria-label="Tìm sản phẩm" placeholder="Tên, SKU hoặc slug" defaultValue={params.get("search") ?? ""} />
      <Select name="status" aria-label="Trạng thái" defaultValue={params.get("status") ?? ""}><option value="">Mọi trạng thái</option><option value="Draft">Bản nháp</option><option value="InReview">Chờ duyệt</option><option value="Published">Đã xuất bản</option><option value="Archived">Đã lưu trữ</option></Select>
      <Select name="categoryId" aria-label="Danh mục" defaultValue={params.get("categoryId") ?? ""}><option value="">Mọi danh mục</option>{flatten(categories).map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</Select>
      <Select name="sort" aria-label="Sắp xếp" defaultValue={params.get("sort") ?? "updated_desc"}><option value="updated_desc">Mới cập nhật</option><option value="created_desc">Mới tạo</option><option value="name_asc">Tên A–Z</option><option value="name_desc">Tên Z–A</option><option value="published_desc">Mới xuất bản</option></Select>
      <Button type="submit">Áp dụng</Button><Link href="/admin/products" className="admin-link inline-flex min-h-11 items-center justify-center px-2 text-sm">Xóa lọc</Link>
    </form></Card>
    <Card className="overflow-hidden">{!data.items.length ? <EmptyState icon={PackageSearch} title="Chưa có sản phẩm" description="Thử thay đổi bộ lọc hoặc tạo sản phẩm đầu tiên." /> : <>
      <div className="grid gap-3 p-3 md:hidden">{data.items.map((product) => <article key={product.id} className="rounded-xl border border-[#ddd8cc] bg-white p-4"><div className="flex items-start gap-3">{thumbnail(product)}<div className="min-w-0 flex-1"><Link className="admin-link block truncate" href={editHref(product.id)}>{product.name}</Link><p className="mt-1 truncate text-xs text-[#56635b]">{product.sku || product.slug}</p></div></div><div className="mt-4 flex flex-wrap items-center justify-between gap-2"><StatusBadge status={product.status} /><span className="text-xs text-[#56635b]">{formatAdminDate(product.updatedAtUtc)}</span></div><p className="mt-3 text-xs text-[#56635b]">{product.primaryCategoryName || "Chưa phân loại"}{product.brandName ? ` · ${product.brandName}` : ""}</p><div className="mt-4 border-t border-[#eeeae1] pt-3">{actions(product)}</div></article>)}</div>
      <div className="hidden overflow-x-auto md:block"><table className="admin-table min-w-[850px]"><thead><tr>{["Ảnh", "Sản phẩm", "SKU", "Danh mục", "Thương hiệu", "Trạng thái", "Cập nhật", "Thao tác"].map((label) => <th key={label}>{label}</th>)}</tr></thead><tbody>{data.items.map((product) => <tr key={product.id}><td>{thumbnail(product)}</td><td><Link className="admin-link" href={editHref(product.id)}>{product.name}</Link><p className="mt-1 max-w-48 truncate text-xs text-[#56635b]">/{product.slug}</p></td><td>{product.sku || "—"}</td><td>{product.primaryCategoryName || "—"}</td><td>{product.brandName || "—"}</td><td><StatusBadge status={product.status} /></td><td className="whitespace-nowrap text-xs">{formatAdminDate(product.updatedAtUtc)}</td><td>{actions(product)}</td></tr>)}</tbody></table></div>
    </>}</Card>
    {data.totalPages > 1 && <nav aria-label="Phân trang sản phẩm" className="flex flex-wrap items-center justify-between gap-3 text-sm"><span>Trang {data.page}/{data.totalPages}</span><div className="flex gap-2">{data.page > 1 && <Link href={pageHref(data.page - 1)} className="admin-link inline-flex min-h-10 items-center px-2">Trước</Link>}{data.page < data.totalPages && <Link href={pageHref(data.page + 1)} className="admin-link inline-flex min-h-10 items-center px-2">Sau</Link>}</div></nav>}
  </div>;
}
