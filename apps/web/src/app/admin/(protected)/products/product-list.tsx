"use client";

import { AdminImage } from "@/components/admin/admin-image";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { ImageOff, PackageSearch } from "lucide-react";
import { toast } from "sonner";
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
  async function act(id: string, action: "publish" | "archive") {
    if (pendingId) return;
    setPendingId(id);
    try {
      await adminRequest(`products/${id}/${action}`, { method: "POST" });
      toast.success(action === "publish" ? "Đã xuất bản sản phẩm." : "Đã lưu trữ sản phẩm.");
      router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); } finally { setPendingId(null); }
  }
  function pageHref(page: number) { const next = new URLSearchParams(query); next.set("page", String(page)); return `/admin/products?${next}`; }
  return <div className="space-y-4"><Card className="p-4"><form action="/admin/products" className="grid gap-3 md:grid-cols-[minmax(12rem,1fr)_repeat(3,minmax(9rem,auto))_auto]">
    <Input name="search" aria-label="Tìm sản phẩm" placeholder="Tên, SKU hoặc slug" defaultValue={params.get("search") ?? ""} />
    <Select name="status" aria-label="Trạng thái" defaultValue={params.get("status") ?? ""}><option value="">Mọi trạng thái</option><option value="Draft">Bản nháp</option><option value="InReview">Chờ duyệt</option><option value="Published">Đã xuất bản</option><option value="Archived">Đã lưu trữ</option></Select>
    <Select name="categoryId" aria-label="Danh mục" defaultValue={params.get("categoryId") ?? ""}><option value="">Mọi danh mục</option>{flatten(categories).map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</Select>
    <Select name="sort" aria-label="Sắp xếp" defaultValue={params.get("sort") ?? "updated_desc"}><option value="updated_desc">Mới cập nhật</option><option value="created_desc">Mới tạo</option><option value="name_asc">Tên A–Z</option><option value="name_desc">Tên Z–A</option><option value="published_desc">Mới xuất bản</option></Select>
    <Button type="submit">Lọc</Button>
  </form></Card>
  <Card className="overflow-hidden">{data.items.length === 0 ? <EmptyState icon={PackageSearch} title="Chưa có sản phẩm" description="Thử thay đổi bộ lọc hoặc tạo sản phẩm đầu tiên." /> : <div className="overflow-x-auto"><table className="w-full min-w-[900px] text-left text-sm"><thead className="bg-[#f8f5ee] text-xs uppercase tracking-wider text-[#718077]"><tr>{["Ảnh", "Sản phẩm", "SKU", "Danh mục", "Thương hiệu", "Trạng thái", "Cập nhật", "Thao tác"].map((label) => <th className="px-4 py-4 font-semibold" key={label}>{label}</th>)}</tr></thead><tbody className="divide-y divide-[#eee8dd]">{data.items.map((product) => <tr key={product.id} className="hover:bg-[#faf8f2]"><td className="px-4 py-3">{product.coverImage?.publicUrl ? <div className="size-12 overflow-hidden rounded-lg bg-[#e8e5dc]"><AdminImage src={product.coverImage.publicUrl} alt={product.coverImage.altText} width={product.coverImage.width} height={product.coverImage.height} loading="lazy" className="size-full object-cover" /></div> : <div className="grid size-12 place-items-center rounded-lg bg-[#eeeae2]"><ImageOff size={18} /></div>}</td><td className="px-4 py-3"><Link className="font-semibold hover:underline" href={`/admin/products/${product.id}/edit`}>{product.name}</Link><p className="mt-1 max-w-50 truncate text-xs text-[#718077]">/{product.slug}</p></td><td className="px-4 py-3">{product.sku || "—"}</td><td className="px-4 py-3">{product.primaryCategoryName || "—"}</td><td className="px-4 py-3">{product.brandName || "—"}</td><td className="px-4 py-3"><StatusBadge status={product.status} /></td><td className="px-4 py-3 whitespace-nowrap text-xs">{formatAdminDate(product.updatedAtUtc)}</td><td className="px-4 py-3"><div className="flex items-center gap-2"><Link className="text-sm font-semibold text-[#8d4f34] hover:underline" href={`/admin/products/${product.id}/edit`}>{canWrite ? "Sửa" : "Xem"}</Link>{canPublish && product.status === "InReview" && <Button size="sm" disabled={pendingId === product.id} onClick={() => void act(product.id, "publish")}>Xuất bản</Button>}{canPublish && product.status === "Published" && <ConfirmDialog trigger={<Button size="sm" variant="ghost" disabled={pendingId === product.id}>Lưu trữ</Button>} title="Lưu trữ sản phẩm?" description="Sản phẩm sẽ không còn xuất hiện trên API công khai." confirmLabel="Lưu trữ" danger onConfirm={() => act(product.id, "archive")} />}</div></td></tr>)}</tbody></table></div>}</Card>
  {data.totalPages > 1 && <nav aria-label="Phân trang sản phẩm" className="flex items-center justify-between text-sm"><span>Trang {data.page}/{data.totalPages}</span><div className="flex gap-2">{data.page > 1 && <Link href={pageHref(data.page - 1)} className="rounded-lg border bg-white px-4 py-2">Trước</Link>}{data.page < data.totalPages && <Link href={pageHref(data.page + 1)} className="rounded-lg border bg-white px-4 py-2">Sau</Link>}</div></nav>}</div>;
}
