import Link from "next/link";
import { ArrowUpRight, ChartNoAxesCombined, CircleHelp, Wallet } from "lucide-react";
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
const labels: Record<string, string> = {
  google_organic: "Google / SEO", zalo_personal: "Zalo cá nhân", zalo_oa: "Zalo OA",
  facebook: "Facebook", direct: "Trực tiếp", phone: "Điện thoại",
  in_person: "Tại cửa hàng", website: "Website", referral: "Được giới thiệu",
  legacy_base: "Tệp khách cũ", unknown: "Chưa xác định", other: "Khác",
};

function Breakdown({ title, subtitle, items }: { title: string; subtitle: string; items: ChannelMetric[] }) {
  const max = Math.max(1, ...items.map((item) => item.revenue));
  return <section className="admin-panel p-5 sm:p-7">
    <p className="admin-eyebrow">Phân bổ doanh thu</p>
    <h2 className="admin-card-heading mt-2">{title}</h2>
    <p className="mt-2 text-xs leading-6 text-[#56635b]">{subtitle}</p>
    {items.length ? <div className="mt-7 space-y-6">
      {items.map((item) => <div key={item.channel}>
        <div className="mb-2 flex flex-wrap items-start justify-between gap-3 text-sm">
          <div><p className="font-semibold text-[#233b32]">{labels[item.channel] ?? item.channel}</p>
            <p className="mt-1 text-xs text-[#56635b]">{item.orders} đơn hoàn tất</p></div>
          <p className="font-semibold tabular-nums text-[#233b32]">{money(item.revenue)}</p>
        </div>
        <div className="h-2 overflow-hidden rounded-full bg-[#eee7da]" role="img" aria-label={`${labels[item.channel] ?? item.channel}: ${money(item.revenue)}`}>
          <div className="h-full rounded-full bg-[#476554]" style={{ width: `${(item.revenue / max) * 100}%` }} />
        </div>
      </div>)}
    </div> : <div className="mt-6 rounded-lg border border-dashed border-[#ddd8cc] p-8 text-center text-sm leading-7 text-[#56635b]">Chưa có đơn hoàn tất trong khoảng thời gian này.</div>}
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
  const from = params.from || new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1)).toISOString().slice(0, 10);
  const to = params.to || new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + 1, 1)).toISOString().slice(0, 10);
  // Dates here are UTC boundaries to match the stored reporting contract.
  const report = await getAdminApi<SalesReport>(
    `reports/sales-overview?from=${encodeURIComponent(from + "T00:00:00Z")}&to=${encodeURIComponent(to + "T00:00:00Z")}`
  );
  const metrics = [
    { title: "Doanh thu đơn hoàn tất", value: money(report.revenue), note: "Không đồng nghĩa tổng tiền đã thu" },
    { title: "Đơn hoàn tất", value: String(report.completedOrders), note: "Không tính đơn xác nhận/chưa xong" },
    { title: "Đơn khách mua lần đầu", value: String(report.firstTimeOrders), note: "Lịch sử mua được xác định" },
    { title: "Đơn khách quay lại", value: String(report.returningOrders), note: "Có lịch sử mua trước" },
    { title: "Chưa rõ lịch sử mua", value: String(report.unknownHistoryOrders), note: "Không tính nhầm là khách mới" },
  ];

  return <main className="space-y-7">
    <header className="admin-page-header">
      <div><p className="admin-eyebrow">Phân tích hoạt động bán hàng</p><h1 className="admin-title">Báo cáo &amp; kênh bán</h1>
        <p className="admin-description">So sánh nơi chốt đơn với nguồn tạo khách — hai góc nhìn khác nhau của cùng giao dịch.</p></div>
      <Link href="/admin/orders" className="admin-secondary-button">Xem đơn hàng <ArrowUpRight size={16} aria-hidden="true" /></Link>
    </header>

    <section className="admin-panel flex flex-wrap items-end gap-5 p-5 sm:p-6">
      <form className="flex flex-wrap items-end gap-3" method="get">
        <label className="text-sm font-medium text-[#233b32]">Từ ngày (UTC)
          <input className="admin-field mt-2" type="date" name="from" defaultValue={from} required /></label>
        <label className="text-sm font-medium text-[#233b32]">Đến trước ngày (UTC)
          <input className="admin-field mt-2" type="date" name="to" defaultValue={to} required /></label>
        <button className="admin-primary-button" type="submit">Xem báo cáo</button>
      </form>
      <p className="max-w-xs text-xs leading-6 text-[#56635b]">Thời điểm dùng chuẩn UTC trong báo cáo này. Ngày kết thúc không được tính vào kỳ báo cáo.</p>
    </section>

    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
      {metrics.map(({ title, value, note }) => <section key={title} className="admin-panel admin-stat-card p-5">
        <p className="text-xs leading-5 text-[#56635b]">{title}</p>
        <p className="mt-4 font-serif text-[clamp(1.6rem,2.2vw,2.1rem)] leading-tight tracking-[-.035em] text-[#233b32]">{value}</p>
        <p className="mt-3 text-xs leading-5 text-[#72796f]">{note}</p>
      </section>)}
    </div>

    <section className="admin-panel flex flex-wrap items-start gap-4 p-5 sm:p-6">
      <span className="grid size-11 shrink-0 place-items-center rounded-lg bg-[#e7eee8] text-[#233b32]"><Wallet size={20} aria-hidden="true" /></span>
      <div className="min-w-0 flex-1">
        <h2 className="text-sm font-semibold text-[#233b32]">Giá vốn đã ghi nhận: {money(report.knownCost)}</h2>
        <p className="mt-2 text-sm leading-7 text-[#56635b]">
          {report.unknownCostLines > 0 ? `Có ${report.unknownCostLines} dòng đơn chưa có giá vốn. Chưa thể kết luận lợi nhuận chính xác.` :
            "Chưa khấu trừ chi phí vận hành, vận chuyển, quảng cáo hoặc hoàn tiền."}
        </p>
      </div>
    </section>

    <div className="grid gap-5 lg:grid-cols-2">
      <Breakdown title="Khách chốt qua đâu?" subtitle="Kênh xác nhận đơn hàng thực tế." items={report.byClosingChannel} />
      <Breakdown title="Khách biết đến từ đâu?" subtitle="Nguồn ban đầu được lưu tại thời điểm tạo hồ sơ." items={report.byAcquisitionSource} />
    </div>
    <div className="flex items-start gap-3 rounded-lg border border-[#ddd8cc] bg-[#efeae0] p-5 text-sm leading-7 text-[#42574a]">
      <CircleHelp className="mt-1 shrink-0" size={20} strokeWidth={1.7} aria-hidden="true" />
      <p>Không cộng doanh thu hai bảng với nhau: mỗi đơn xuất hiện một lần ở mỗi góc nhìn. Lượt click Zalo hoặc gửi form không được tính thành đơn bán.</p>
    </div>
    <p className="inline-flex items-center gap-2 text-xs text-[#56635b]"><ChartNoAxesCombined size={16} aria-hidden="true" /> Báo cáo lấy từ đơn Completed trong cơ sở dữ liệu của website.</p>
  </main>;
}
