import Link from "next/link";
import { Package, FolderTree, Tags, Images, ArrowUpRight } from "lucide-react";
import { AccessDenied } from "@/components/admin/access-denied";
import { Card } from "@/components/ui/card";
import { StatusBadge } from "@/components/ui/status-badge";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { getAdminApi } from "@/lib/api/server";
import type { AdminCatalogSummary, AdminProductListItem, PagedResult } from "@/lib/api/catalog-types";
import { hasPermission, permissions } from "@/lib/permissions";

export default async function AdminPage() {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.catalogRead)) return <AccessDenied permission={permissions.catalogRead} />;
  const [summary, recent] = await Promise.all([
    getAdminApi<AdminCatalogSummary>("catalog/summary"),
    getAdminApi<PagedResult<AdminProductListItem>>("products?page=1&pageSize=5&sort=updated_desc"),
  ]);
  const metrics = [
    { label: "Tổng sản phẩm", value: summary.total, icon: Package, href: "/admin/products" },
    { label: "Đã xuất bản", value: summary.published, icon: Package, href: "/admin/products?status=Published" },
    { label: "Bản nháp", value: summary.draft, icon: Package, href: "/admin/products?status=Draft" },
    { label: "Danh mục", value: summary.categories, icon: FolderTree, href: "/admin/categories" },
    { label: "Thương hiệu", value: summary.brands, icon: Tags, href: "/admin/brands" },
    { label: "Ảnh trong thư viện", value: summary.mediaAssets, icon: Images, href: "/admin/media" },
  ];
  return <main className="space-y-7"><header className="admin-page-header"><div><p className="admin-eyebrow">Không gian làm việc</p><h1 className="admin-title">Tổng quan catalog</h1><p className="admin-description">Số liệu trực tiếp từ Catalog API, cập nhật theo dữ liệu hiện có.</p></div><Link href="/admin/products" className="admin-link inline-flex min-h-11 items-center gap-1 text-sm">Mở danh sách sản phẩm <ArrowUpRight size={16} aria-hidden="true" /></Link></header>
    <section aria-label="Số liệu catalog" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">{metrics.map(({ label, value, icon: Icon, href }) => <Link key={label} href={href} className="group"><Card className="flex min-h-36 items-start justify-between p-5 transition-colors group-hover:border-[#b79b6c]"><div><p className="text-sm font-medium text-[#56635b]">{label}</p><p className="mt-4 font-serif text-4xl leading-none text-[#233b32]">{value}</p></div><span className="grid size-10 place-items-center rounded-lg bg-[#e7eee8] text-[#233b32]"><Icon size={20} aria-hidden="true" /></span></Card></Link>)}</section>
    <Card className="p-5 sm:p-6"><div className="mb-4 flex flex-wrap items-center justify-between gap-3"><div><p className="admin-eyebrow">Mới cập nhật</p><h2 className="admin-section-title mt-1">Sản phẩm gần đây</h2></div><Link className="admin-link text-sm" href="/admin/products">Xem tất cả</Link></div>{recent.items.length ? <ul className="divide-y divide-[#eeeae1]">{recent.items.map((product) => <li key={product.id} className="flex flex-wrap items-center justify-between gap-2 py-3"><div className="min-w-0"><Link className="admin-link text-sm" href={`/admin/products/${product.id}/edit`}>{product.name}</Link><p className="mt-1 text-xs text-[#56635b]">{new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeZone: "Asia/Ho_Chi_Minh" }).format(new Date(product.updatedAtUtc))}</p></div><StatusBadge status={product.status} /></li>)}</ul> : <p className="py-8 text-center text-sm text-[#56635b]">Chưa có sản phẩm để hiển thị.</p>}</Card>
  </main>;
}
