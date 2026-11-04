import Link from "next/link";
import { ArrowRight, ArrowUpRight, BookOpenText, ClipboardList, FolderTree, Images,
  Inbox, Package, Plus, ShoppingBag, Users, ChartNoAxesCombined } from "lucide-react";
import { AccessDenied } from "@/components/admin/access-denied";
import { Card } from "@/components/ui/card";
import { StatusBadge } from "@/components/ui/status-badge";
import { AdminImage } from "@/components/admin/admin-image";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { getAdminApi } from "@/lib/api/server";
import type { AdminCatalogSummary, AdminProductListItem, PagedResult } from "@/lib/api/catalog-types";
import { hasPermission, permissions } from "@/lib/permissions";

type SalesSummary = Readonly<{
  completedOrders: number; revenue: number; returningOrders: number;
  firstTimeOrders: number; unknownHistoryOrders: number;
}>;
const money = (value: number) => value.toLocaleString("vi-VN", { maximumFractionDigits: 0 }) + " ₫";

function vietnamMonthRange() {
  const parts = new Intl.DateTimeFormat("en-US", { timeZone: "Asia/Ho_Chi_Minh", year: "numeric", month: "numeric" }).formatToParts(new Date());
  const year = Number(parts.find((part) => part.type === "year")?.value);
  const month = Number(parts.find((part) => part.type === "month")?.value);
  return {
    from: new Date(Date.UTC(year, month - 1, 1, -7)).toISOString(),
    to: new Date(Date.UTC(year, month, 1, -7)).toISOString(),
  };
}

export default async function AdminPage() {
  const user = await getCurrentAdminUser();
  const canCatalog = hasPermission(user?.permissions ?? [], permissions.catalogRead);
  const canLeads = hasPermission(user?.permissions ?? [], permissions.leadsRead);
  const canManageLeads = hasPermission(user?.permissions ?? [], permissions.leadsManage);
  const canWriteCatalog = hasPermission(user?.permissions ?? [], permissions.catalogWrite);
  if (!canCatalog && !canLeads) return <AccessDenied permission={permissions.catalogRead} />;

  const range = vietnamMonthRange();
  const [summary, recent, sales, leads] = await Promise.all([
    canCatalog ? getAdminApi<AdminCatalogSummary>("catalog/summary") : Promise.resolve(null),
    canCatalog ? getAdminApi<PagedResult<AdminProductListItem>>("products?page=1&pageSize=5&sort=updated_desc") : Promise.resolve(null),
    canLeads ? getAdminApi<SalesSummary>(`reports/sales-overview?from=${encodeURIComponent(range.from)}&to=${encodeURIComponent(range.to)}`) : Promise.resolve(null),
    canLeads ? getAdminApi<{ totalItems: number }>("leads?page=1&pageSize=1") : Promise.resolve(null),
  ]);
  const catalogMetrics = summary ? [
    { label: "Tổng sản phẩm", value: summary.total, icon: Package, href: "/admin/products" },
    { label: "Đã công bố", value: summary.published, icon: Package, href: "/admin/products?status=Published" },
    { label: "Chờ duyệt", value: summary.inReview, icon: ClipboardList, href: "/admin/products?status=InReview" },
    { label: "Bản nháp", value: summary.draft, icon: BookOpenText, href: "/admin/products?status=Draft" },
    { label: "Danh mục", value: summary.categories, icon: FolderTree, href: "/admin/categories" },
    { label: "Ảnh", value: summary.mediaAssets, icon: Images, href: "/admin/media" },
  ] : [];
  const shortcuts = [
    ...(canManageLeads ? [
      { href: "/admin/customers", label: "Khách hàng", description: "Xem hồ sơ & lịch sử tư vấn", icon: Users },
      { href: "/admin/orders", label: "Tạo đơn", description: "Ghi nhận đơn chốt qua tư vấn", icon: ShoppingBag },
    ] : []),
    ...(canWriteCatalog ? [
      { href: "/admin/products/new", label: "Thêm trà cụ", description: "Tạo sản phẩm & hình ảnh", icon: Plus },
    ] : []),
    ...(canLeads ? [
      { href: "/admin/leads", label: "Yêu cầu tư vấn", description: "Xem nhu cầu từ website", icon: Inbox },
      { href: "/admin/reports", label: "Báo cáo", description: "Theo dõi doanh thu & nguồn khách", icon: ChartNoAxesCombined },
    ] : []),
  ];

  return <main className="space-y-8">
    <section className="admin-dashboard-hero">
      <div className="relative z-10 max-w-2xl">
        <p className="admin-eyebrow text-[#d7bd91]!">TeaPremium / Không gian làm việc</p>
        <h1 className="mt-4 font-serif text-[clamp(2.15rem,3.8vw,3.6rem)] leading-[1.16] tracking-[-.045em] text-[#fffaf0]">Tổng quan cửa hàng.</h1>
        <p className="mt-4 max-w-xl text-sm leading-7 text-[#dce8df]">Sản phẩm, khách hàng và đơn tư vấn — nhìn từ cùng một nơi. Dữ liệu được lấy trực tiếp từ hệ thống quản trị.</p>
        <div className="mt-7 flex flex-wrap gap-3">
          {canManageLeads && <Link href="/admin/orders" className="admin-hero-button">Ghi đơn mới <ArrowUpRight size={17} aria-hidden="true" /></Link>}
          {canWriteCatalog && <Link href="/admin/products/new" className="admin-hero-button admin-hero-button-outline">Thêm sản phẩm <Plus size={17} aria-hidden="true" /></Link>}
        </div>
      </div>
      <div className="admin-dashboard-ornament" aria-hidden="true" />
    </section>

    {sales && <section aria-labelledby="sales-kpis">
      <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
        <div><p className="admin-eyebrow">Nhịp bán hàng</p><h2 id="sales-kpis" className="admin-section-title mt-1">Hoạt động trong tháng</h2></div>
        <Link href="/admin/reports" className="admin-link inline-flex items-center gap-2 text-sm">Xem báo cáo <ArrowUpRight size={16} aria-hidden="true" /></Link>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {[
          { label: "Doanh thu đơn hoàn tất", value: money(sales.revenue), note: "Không đồng nghĩa tiền đã thu" },
          { label: "Đơn hoàn tất", value: String(sales.completedOrders), note: "Tháng hiện tại (giờ Việt Nam)" },
          { label: "Khách quay lại mua", value: String(sales.returningOrders), note: "Theo lịch sử đã xác nhận" },
          { label: "Yêu cầu tư vấn", value: String(leads?.totalItems ?? "—"), note: "Tổng lead website" },
        ].map((metric) => <Card key={metric.label} className="admin-stat-card p-5 sm:p-6">
          <p className="text-sm text-[#56635b]">{metric.label}</p>
          <p className="mt-4 font-serif text-[clamp(1.65rem,2.4vw,2.45rem)] leading-tight tracking-[-.04em] text-[#233b32]">{metric.value}</p>
          <p className="mt-3 text-xs leading-6 text-[#68726b]">{metric.note}</p>
        </Card>)}
      </div>
    </section>}

    <section aria-labelledby="shortcuts-title">
      <div className="mb-4"><p className="admin-eyebrow">Thao tác thường dùng</p><h2 id="shortcuts-title" className="admin-section-title mt-1">Bắt đầu từ đây</h2></div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {shortcuts.map(({ href, label, description, icon: Icon }) => <Link href={href} key={href} className="admin-quick-link group">
          <span className="admin-quick-icon"><Icon size={20} strokeWidth={1.7} aria-hidden="true" /></span>
          <span className="min-w-0 flex-1"><span className="block font-semibold text-[#233b32]">{label}</span><span className="mt-1 block text-xs leading-6 text-[#56635b]">{description}</span></span>
          <ArrowUpRight size={18} className="text-[#925b3d] transition-transform group-hover:translate-x-1 group-hover:-translate-y-1" aria-hidden="true" />
        </Link>)}
      </div>
    </section>

    {summary && <section aria-labelledby="catalog-kpis">
      <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
        <div><p className="admin-eyebrow">Sản phẩm & nội dung</p><h2 id="catalog-kpis" className="admin-section-title mt-1">Tình trạng Catalog</h2></div>
        <Link href="/admin/products" className="admin-link inline-flex items-center gap-2 text-sm">Quản lý sản phẩm <ArrowUpRight size={16} aria-hidden="true" /></Link>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
        {catalogMetrics.map(({ label, value, icon: Icon, href }) => <Link href={href} key={label} className="group">
          <Card className="admin-stat-card flex h-full min-h-40 flex-col justify-between p-4 transition-colors group-hover:border-[#b79b6c]">
            <span className="flex items-center justify-between gap-3 text-sm text-[#56635b]">{label}<Icon size={18} className="shrink-0 text-[#925b3d]" aria-hidden="true" /></span>
            <span className="mt-5 font-serif text-4xl text-[#233b32]">{value}</span>
          </Card>
        </Link>)}
      </div>
    </section>}

    {recent && <section className="admin-panel overflow-hidden" aria-labelledby="recent-products-title">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#ddd8cc] p-5 sm:px-7">
        <div><p className="admin-eyebrow">Mới cập nhật</p><h2 id="recent-products-title" className="admin-section-title mt-1">Trà cụ gần đây</h2></div>
        <Link href="/admin/products" className="admin-link inline-flex items-center gap-2 text-sm">Danh sách đầy đủ <ArrowRight size={16} aria-hidden="true" /></Link>
      </div>
      {recent.items.length ? <ul className="divide-y divide-[#eeeae1]">
        {recent.items.map((product) => <li key={product.id} className="flex flex-wrap items-center gap-4 px-5 py-3 sm:px-7">
          <div className="grid size-16 shrink-0 place-items-center overflow-hidden rounded-md bg-[#eae4d8]">
            {product.coverImage?.publicUrl
              ? <AdminImage src={product.coverImage.publicUrl} alt={product.coverImage.altText} width={product.coverImage.width} height={product.coverImage.height} className="size-full object-contain" loading="lazy" />
              : <Package size={20} className="text-[#8c806e]" aria-hidden="true" />}
          </div>
          <div className="min-w-0 flex-1"><Link href={`/admin/products/${product.id}/edit`} className="admin-link text-sm">{product.name}</Link>
            <p className="mt-1 text-xs text-[#56635b]">{product.primaryCategoryName || "Chưa gán danh mục"} · {product.sku || product.slug}</p></div>
          <StatusBadge status={product.status} />
          <Link href={`/admin/products/${product.id}/edit`} className="admin-link inline-flex min-h-11 items-center text-xs">Chi tiết</Link>
        </li>)}
      </ul> : <p className="p-9 text-center text-sm text-[#56635b]">Chưa có sản phẩm trong Catalog. Tạo một sản phẩm và gắn hình ảnh để bắt đầu.</p>}
    </section>}
    <p className="text-xs leading-6 text-[#56635b]">Tài khoản chỉ nhìn thấy chức năng phù hợp với quyền đã được cấp. Số liệu bán hàng dựa trên đơn nội bộ hoàn tất.</p>
  </main>;
}
