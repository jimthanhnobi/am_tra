import { getAdminApi } from "@/lib/api/server";
import { getCurrentAdminUser } from "@/lib/auth/server";
import { hasPermission, permissions } from "@/lib/permissions";
import { AccessDenied } from "@/components/admin/access-denied";

type ChannelMetric = { channel: string; orders: number; revenue: number };
type SalesReport = {
  completedOrders: number; revenue: number; knownCost: number; unknownCostLines: number;
  firstTimeOrders: number; returningOrders: number; unknownHistoryOrders: number;
  byClosingChannel: ChannelMetric[]; byAcquisitionSource: ChannelMetric[];
};
const money = (n: number) => n.toLocaleString("vi-VN", { maximumFractionDigits: 0 }) + " ₫";

function Breakdown({ title, items }: { title: string; items: ChannelMetric[] }) {
  return <section className="admin-panel space-y-3 p-5">
    <h2 className="text-lg font-semibold">{title}</h2>
    <div className="overflow-x-auto"><table className="w-full text-left text-sm">
      <thead><tr><th className="py-2">Kênh / nguồn</th><th className="py-2">Đơn hoàn tất</th><th className="py-2">Doanh thu</th></tr></thead>
      <tbody>{items.map(x => <tr key={x.channel} className="border-t border-[#eeeae1]">
        <td className="py-2">{x.channel}</td><td className="py-2">{x.orders}</td><td className="py-2">{money(x.revenue)}</td>
      </tr>)}</tbody></table>{!items.length && <p className="py-4 text-sm">Chưa có dữ liệu trong kỳ.</p>}</div>
  </section>;
}

export default async function ReportsPage({
  searchParams,
}: Readonly<{ searchParams: Promise<{ from?: string; to?: string }> }>) {
  const user = await getCurrentAdminUser();
  if (!hasPermission(user?.permissions ?? [], permissions.leadsRead))
    return <AccessDenied permission={permissions.leadsRead} />;
  const params = await searchParams;
  const now = new Date();
  const from = params.from || new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1)).toISOString().slice(0,10);
  const to = params.to || new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + 1, 1)).toISOString().slice(0,10);
  const report = await getAdminApi<SalesReport>(
    `reports/sales-overview?from=${encodeURIComponent(from + "T00:00:00Z")}&to=${encodeURIComponent(to + "T00:00:00Z")}`
  );
  return <main className="space-y-6">
    <header className="admin-page-header"><div>
      <p className="admin-eyebrow">Sales Analytics</p><h1 className="admin-title">Báo cáo bán hàng</h1>
      <p className="admin-description">Chỉ tính đơn Completed; khoảng ngày UTC [từ, đến), không tính đơn Confirmed hoặc Cancelled.</p>
    </div></header>
    <form className="admin-panel flex flex-wrap items-end gap-3 p-4" method="get">
      <label className="text-sm">Từ ngày UTC <input className="mt-1 block rounded-lg border border-[#ddd8cc] p-2" type="date" name="from" defaultValue={from} required /></label>
      <label className="text-sm">Đến trước UTC <input className="mt-1 block rounded-lg border border-[#ddd8cc] p-2" type="date" name="to" defaultValue={to} required /></label>
      <button type="submit" className="admin-primary-button rounded-lg px-5 py-2">Xem báo cáo</button>
    </form>
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {[
        ["Đơn hoàn tất", String(report.completedOrders)],
        ["Doanh thu đơn hoàn tất", money(report.revenue)],
        ["Đơn khách mua lần đầu", String(report.firstTimeOrders)],
        ["Đơn khách quay lại", String(report.returningOrders)],
        ["Chưa rõ lịch sử mua", String(report.unknownHistoryOrders)],
      ].map(([label, value]) => <section className="admin-panel p-5" key={label}>
        <p className="text-sm text-[#56635b]">{label}</p><p className="mt-2 text-xl font-semibold">{value}</p>
      </section>)}
    </div>
    <p className="rounded-lg border border-[#ddd8cc] p-4 text-sm">Giá vốn đã ghi nhận: <strong>{money(report.knownCost)}</strong>.
      {report.unknownCostLines > 0 ? ` Có ${report.unknownCostLines} dòng đơn thiếu giá vốn: chưa thể tính lợi nhuận đầy đủ.` :
      " Chưa trừ chi phí vận hành, quảng cáo, hoàn tiền hoặc vận chuyển."}
    </p>
    <div className="grid gap-4 lg:grid-cols-2">
      <Breakdown title="Theo kênh chốt đơn" items={report.byClosingChannel} />
      <Breakdown title="Theo nguồn khách ban đầu" items={report.byAcquisitionSource} />
    </div>
    <p className="text-sm text-[#56635b]">Hai bảng là hai góc nhìn độc lập của cùng các đơn hàng; không cộng hai bảng với nhau.</p>
  </main>;
}
