"use client";

import { useRef, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ArrowUpRight, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";

export type ProductOption = { id: string; name: string; sku: string | null };
export type OrderDto = {
  id: string; orderCode: string; customerId: string; customerName: string;
  closingChannel: string; acquisitionSource: string; isReturning: boolean | null;
  status: string; totalAmount: number; createdAtUtc: string;
  items?: { productName: string; quantity: number }[];
};
export type OrderPage = { items: OrderDto[]; totalItems: number };

type OrderLine = {
  key: number; productId: string; quantity: string; unitPrice: string; unitCost: string;
};

const channels = [
  ["zalo_personal", "Zalo cá nhân"], ["zalo_oa", "Zalo OA"],
  ["phone", "Điện thoại"], ["in_person", "Trực tiếp"],
  ["website", "Website"], ["other", "Khác"],
] as const;
const sources: Record<string, string> = {
  legacy_base: "Tệp khách có sẵn", google_organic: "Google / SEO", zalo_oa: "Zalo OA",
  zalo_personal: "Zalo cá nhân", referral: "Giới thiệu", facebook: "Facebook",
  direct: "Trực tiếp", unknown: "Chưa xác định", other: "Khác",
};
const statuses: Record<string, string> = {
  Confirmed: "Đã xác nhận", Completed: "Đã hoàn tất", Cancelled: "Đã hủy",
};
const channelLabel = (code: string) => channels.find(([value]) => value === code)?.[1] ?? code;
const money = (value: number) => value.toLocaleString("vi-VN", { maximumFractionDigits: 0 }) + " ₫";
const field = "admin-field";

export function OrdersManager({ orders, products, canManage }: Readonly<{
  orders: OrderPage; products: ProductOption[]; canManage: boolean;
}>) {
  const router = useRouter();
  const nextKey = useRef(2);
  const [phone, setPhone] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [lines, setLines] = useState<OrderLine[]>([
    { key: 1, productId: products[0]?.id || "", quantity: "1", unitPrice: "", unitCost: "" },
  ]);
  const [channel, setChannel] = useState("zalo_personal");
  const [campaignCode, setCampaignCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [requestId, setRequestId] = useState(() => crypto.randomUUID());
  const total = lines.reduce((sum, line) => sum + (Number(line.quantity) || 0) * (Number(line.unitPrice) || 0), 0);

  function changeLine(key: number, fieldName: keyof Omit<OrderLine, "key">, value: string) {
    setLines((current) => current.map((line) => line.key === key ? { ...line, [fieldName]: value } : line));
  }

  async function findCustomer() {
    setBusy(true);
    try {
      const data = await adminRequest<{ items: { id: string; name: string; phone: string }[] }>(
        `customers?pageSize=50&search=${encodeURIComponent(phone)}`
      );
      const normalize = (value: string) => {
        const digits = value.replace(/\D/g, "");
        return digits.startsWith("84") ? "0" + digits.slice(2) : digits;
      };
      const matched = data.items.find((customer) => normalize(customer.phone) === normalize(phone));
      if (!matched) {
        setCustomerId(""); setCustomerName("");
        toast.error("Chưa tìm thấy khách. Hãy tạo hồ sơ trong mục Khách hàng trước.");
        return;
      }
      setCustomerId(matched.id); setCustomerName(matched.name);
      toast.success(`Đã chọn ${matched.name}`);
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!customerId) { toast.error("Cần chọn khách hàng trước."); return; }
    if (lines.some((line) => !line.productId || Number(line.quantity) < 1 || line.unitPrice.trim() === "")) {
      toast.error("Kiểm tra sản phẩm, số lượng và đơn giá."); return;
    }
    setBusy(true);
    try {
      await adminRequest("orders", { method: "POST", body: jsonBody({
        customerId, requestId, leadId: null, closingChannel: channel,
        campaignCode: campaignCode.trim() || null,
        items: lines.map((line) => ({
          productId: line.productId,
          quantity: Number(line.quantity),
          unitPrice: Number(line.unitPrice),
          unitCost: line.unitCost.trim() === "" ? null : Number(line.unitCost),
        })),
      }) });
      toast.success("Đã ghi nhận đơn xác nhận.");
      setRequestId(crypto.randomUUID());
      setLines([{ key: nextKey.current++, productId: products[0]?.id || "", quantity: "1", unitPrice: "", unitCost: "" }]);
      setCampaignCode(""); router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function transition(id: string, action: "complete" | "cancel") {
    setBusy(true);
    try {
      await adminRequest(`orders/${id}/${action}`, { method: "POST" });
      toast.success(action === "complete" ? "Đơn đã hoàn tất." : "Đơn đã hủy.");
      router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  function orderActions(order: OrderDto) {
    if (!canManage || order.status !== "Confirmed") return null;
    return <div className="mt-3 flex flex-wrap items-center gap-2">
      <ConfirmDialog trigger={<button type="button" disabled={busy} className="admin-secondary-button">Hoàn tất</button>}
        title="Xác nhận hoàn tất đơn?" description="Đơn sẽ được tính vào báo cáo doanh thu đơn hoàn tất."
        confirmLabel="Hoàn tất đơn" onConfirm={() => transition(order.id, "complete")} />
      <ConfirmDialog trigger={<button type="button" disabled={busy} className="admin-secondary-button">Hủy đơn</button>}
        title="Hủy đơn hàng?" description="Đơn đã hủy không được tính vào doanh thu hoàn tất."
        confirmLabel="Hủy đơn" danger onConfirm={() => transition(order.id, "cancel")} />
    </div>;
  }

  return <main className="space-y-7">
    <header className="admin-page-header">
      <div><p className="admin-eyebrow">Bán hàng / Nội bộ</p><h1 className="admin-title">Đơn hàng &amp; chốt tư vấn</h1>
        <p className="admin-description">{orders.totalItems} đơn đã được ghi nhận. Chốt qua OA, Zalo cá nhân hay điện thoại đều quản lý tại đây.</p>
      </div>
    </header>

    {canManage && <form onSubmit={(event) => void create(event)} className="admin-panel space-y-6 p-5 sm:p-7">
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-[#ddd8cc] pb-5">
        <div><p className="admin-eyebrow">Tạo giao dịch</p><h2 className="admin-card-heading mt-2">Đơn mới sau tư vấn</h2>
          <p className="admin-field-note mt-2">Đơn nội bộ, không tạo trang thanh toán công khai.</p></div>
        <span className="admin-tag">Đơn xác nhận</span>
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,.9fr)_minmax(0,1.1fr)]">
        <div className="space-y-4">
          <h3 className="text-sm font-semibold text-[#233b32]">01 / Khách hàng &amp; kênh</h3>
          <label className="block text-sm font-medium">Số điện thoại khách
            <div className="mt-2 flex flex-wrap gap-2"><input className={field + " min-w-0 flex-1"} required value={phone}
              onChange={(event) => { setPhone(event.target.value); setCustomerId(""); setCustomerName(""); }}
              placeholder="09xxxxxxxx" inputMode="tel" />
              <button type="button" disabled={busy || !phone.trim()} className="admin-secondary-button" onClick={() => void findCustomer()}><Search size={16} aria-hidden="true" /> Tìm</button>
            </div>
          </label>
          <p role="status" className="admin-field-note">{customerName ? `Đã chọn khách: ${customerName}` : "Chưa chọn khách — tạo hồ sơ nếu khách chưa có trong CRM."}</p>
          <label className="block text-sm font-medium">Nơi chốt đơn
            <select className={field + " mt-2"} value={channel} onChange={(event) => setChannel(event.target.value)}>
              {channels.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="block text-sm font-medium">Mã chiến dịch (nếu có)
            <input className={field + " mt-2"} maxLength={100} value={campaignCode}
              onChange={(event) => setCampaignCode(event.target.value)} placeholder="Ví dụ: hang-moi-thang-11" />
          </label>
        </div>
        <div className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h3 className="text-sm font-semibold text-[#233b32]">02 / Trà cụ trong đơn</h3>
            <button type="button" disabled={lines.length >= 50 || !products.length} className="admin-link inline-flex items-center gap-1 text-sm"
              onClick={() => setLines((current) => [...current, { key: nextKey.current++, productId: products[0]?.id || "", quantity: "1", unitPrice: "", unitCost: "" }])}>
              <Plus size={16} aria-hidden="true" /> Thêm dòng
            </button>
          </div>
          {lines.map((line, index) => <div key={line.key} className="rounded-lg border border-[#ddd8cc] bg-[#f9f6ef] p-4">
            <div className="mb-3 flex items-center justify-between gap-3"><p className="text-xs font-semibold uppercase tracking-[.1em] text-[#925b3d]">Sản phẩm {index + 1}</p>
              {lines.length > 1 && <button type="button" aria-label={`Xóa dòng sản phẩm ${index + 1}`} className="inline-flex size-10 items-center justify-center text-[#805b4b] hover:text-[#9b332b]" onClick={() => setLines((current) => current.filter((item) => item.key !== line.key))}><Trash2 size={17} aria-hidden="true" /></button>}
            </div>
            <label className="block text-sm font-medium">Sản phẩm đã công bố
              <select required className={field + " mt-2"} value={line.productId}
                onChange={(event) => changeLine(line.key, "productId", event.target.value)}>
                {products.map((product) => <option key={product.id} value={product.id}>{product.name}{product.sku ? ` · ${product.sku}` : ""}</option>)}
              </select>
            </label>
            <div className="mt-3 grid gap-3 sm:grid-cols-3">
              <label className="text-xs font-medium">Số lượng
                <input className={field + " mt-1"} type="number" min={1} max={100000} step={1} required value={line.quantity}
                  onChange={(event) => changeLine(line.key, "quantity", event.target.value)} />
              </label>
              <label className="text-xs font-medium">Đơn giá (VNĐ)
                <input className={field + " mt-1"} type="number" min={0} step={1} required value={line.unitPrice}
                  onChange={(event) => changeLine(line.key, "unitPrice", event.target.value)} />
              </label>
              <label className="text-xs font-medium">Giá vốn (nếu biết)
                <input className={field + " mt-1"} type="number" min={0} step={1} value={line.unitCost}
                  onChange={(event) => changeLine(line.key, "unitCost", event.target.value)} />
              </label>
            </div>
          </div>)}
          {!products.length && <p className="text-sm text-[#9b332b]">Chưa có trà cụ Published để tạo đơn. Hãy công bố một sản phẩm trước.</p>}
          <p className="admin-field-note">Danh sách chọn hiện lấy tối đa 50 sản phẩm Published; giá chỉ lưu nội bộ.</p>
        </div>
      </div>
      <div className="flex flex-wrap items-end justify-between gap-4 border-t border-[#ddd8cc] pt-5">
        <div><p className="text-xs font-semibold uppercase tracking-[.1em] text-[#56635b]">Tổng tiền dự kiến</p>
          <p className="mt-1 font-serif text-3xl tracking-[-.035em] text-[#233b32]">{money(total)}</p></div>
        <button disabled={busy || !customerId || !products.length || lines.length < 1} type="submit" className="admin-primary-button">
          {busy ? "Đang ghi nhận..." : "Lưu đơn xác nhận"} <ArrowUpRight size={16} aria-hidden="true" />
        </button>
      </div>
    </form>}

    <section className="admin-panel overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#ddd8cc] p-5 sm:px-7">
        <div><p className="admin-eyebrow">Lịch sử giao dịch</p><h2 className="admin-card-heading mt-1">Đơn đã tạo</h2></div>
        <span className="admin-tag">{orders.totalItems} đơn</span>
      </div>
      {orders.items.length ? <>
        <div className="grid gap-3 p-4 md:hidden">
          {orders.items.map((order) => <article key={order.id} className="rounded-lg border border-[#ddd8cc] bg-[#fffefa] p-4">
            <div className="flex items-start justify-between gap-3"><div className="min-w-0"><p className="text-sm font-semibold text-[#233b32]">{order.orderCode}</p>
              <p className="mt-1 text-sm">{order.customerName}</p></div><span className="admin-status" data-state={order.status}>{statuses[order.status] ?? order.status}</span></div>
            <p className="mt-3 text-xs text-[#56635b]">{channelLabel(order.closingChannel)} · {sources[order.acquisitionSource] ?? order.acquisitionSource}</p>
            <p className="mt-3 font-serif text-2xl text-[#233b32]">{money(order.totalAmount)}</p>{orderActions(order)}
          </article>)}
        </div>
        <div className="hidden overflow-x-auto md:block"><table className="admin-data-table">
          <thead><tr><th>Đơn hàng</th><th>Khách hàng</th><th>Kênh chốt / Nguồn</th><th>Giá trị</th><th>Trạng thái &amp; thao tác</th></tr></thead>
          <tbody>{orders.items.map((order) => <tr key={order.id}>
            <td><p className="font-semibold text-[#233b32]">{order.orderCode}</p><p className="mt-1 text-xs text-[#56635b]">{order.items?.length ?? 0} dòng hàng</p></td>
            <td><p className="font-medium">{order.customerName}</p><p className="mt-1 text-xs text-[#56635b]">{order.isReturning === true ? "Khách quay lại" : order.isReturning === false ? "Mua lần đầu" : "Chưa rõ lịch sử mua"}</p></td>
            <td><p>{channelLabel(order.closingChannel)}</p><p className="mt-1 text-xs text-[#56635b]">{sources[order.acquisitionSource] ?? order.acquisitionSource}</p></td>
            <td className="font-semibold">{money(order.totalAmount)}</td>
            <td><span className="admin-status" data-state={order.status}>{statuses[order.status] ?? order.status}</span>{orderActions(order)}</td>
          </tr>)}</tbody>
        </table></div>
      </> : <p className="admin-table-empty">Chưa có đơn được ghi nhận. Tạo đơn đầu tiên sau khi xác nhận nhu cầu của khách.</p>}
    </section>
  </main>;
}
