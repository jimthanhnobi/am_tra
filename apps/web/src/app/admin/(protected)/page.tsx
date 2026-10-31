import Link from "next/link";
import { Package, FolderTree, Tags, Images } from "lucide-react";
import { AccessDenied } from "@/components/admin/access-denied";
import { Card } from "@/components/ui/card";
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
  return <main className="space-y-8"><div><p className="text-xs font-semibold uppercase tracking-[0.2em] text-[#9c6349]">Tổng quan catalog</p><h1 className="mt-2 font-serif text-3xl">Bảng điều khiển</h1><p className="mt-2 text-sm text-[#718077]">Số liệu trực tiếp từ Catalog API.</p></div>
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{metrics.map(({ label, value, icon: Icon, href }) => <Link key={label} href={href}><Card className="p-5 transition hover:border-[#b9c9bd]"><Icon className="mb-4 text-[#8d4f34]" size={22} /><p className="text-sm text-[#718077]">{label}</p><p className="mt-2 font-serif text-3xl">{value}</p></Card></Link>)}</div>
    <Card className="p-6"><div className="mb-4 flex items-center justify-between"><h2 className="font-serif text-xl">Sản phẩm cập nhật gần đây</h2><Link className="text-sm font-semibold text-[#8d4f34] hover:underline" href="/admin/products">Xem tất cả</Link></div>{recent.items.length ? <ul className="divide-y divide-[#eee8dd]">{recent.items.map((product) => <li key={product.id} className="flex items-center justify-between gap-4 py-3"><Link className="font-medium hover:underline" href={`/admin/products/${product.id}/edit`}>{product.name}</Link><span className="text-xs text-[#718077]">{new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeZone: "Asia/Ho_Chi_Minh" }).format(new Date(product.updatedAtUtc))}</span></li>)}</ul> : <p className="text-sm text-[#718077]">Chưa có sản phẩm.</p>}</Card>
  </main>;
}
