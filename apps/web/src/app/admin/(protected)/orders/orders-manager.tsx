"use client";
import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";

export type ProductOption = { id: string; name: string; sku: string | null };
export type OrderDto = {
  id: string; orderCode: string; customerId: string; customerName: string;
  closingChannel: string; acquisitionSource: string; isReturning: boolean;
  status: string; totalAmount: number; createdAtUtc: string;
};
export type OrderPage = { items: OrderDto[]; totalItems: number };
const channels = [
  ["zalo_personal", "Zalo cá nhân"], ["zalo_oa", "Zalo OA"],
  ["phone", "Điện thoại"], ["in_person", "Trực tiếp"],
  ["website", "Website"], ["other", "Khác"]
];
const field = "min-h-10 w-full rounded-lg border border-[#ddd8cc] bg-white px-3 py-2 text-sm";
const money = (value: number) => value.toLocaleString("vi-VN", { maximumFractionDigits: 0 }) + " ₫";

export function OrdersManager({ orders, products, canManage }: {
  orders: OrderPage; products: ProductOption[]; canManage: boolean;
}) {
  const router = useRouter();
  const [phone, setPhone] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [productId, setProductId] = useState(products[0]?.id || "");
  const [quantity, setQuantity] = useState("1");
  const [unitPrice, setUnitPrice] = useState("");
  const [unitCost, setUnitCost] = useState("");
  const [channel, setChannel] = useState("zalo_personal");
  const [campaignCode, setCampaignCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [requestId, setRequestId] = useState(() => crypto.randomUUID());

  async function findCustomer() {
    setBusy(true);
    try {
      const data = await adminRequest<{items:{id:string;name:string;phone:string}[]}>(
        `customers?pageSize=50&search=${encodeURIComponent(phone)}`
      );
      const digits = (s: string) => s.replace(/\D/g, "");
      const target = digits(phone);
      const matched = data.items.find(c => digits(c.phone) === target || (
        target.startsWith("84") && digits(c.phone).startsWith("0") &&
        target.slice(2) === digits(c.phone).slice(1)
      ));
      if (!matched) { setCustomerId(""); setCustomerName(""); toast.error("Chưa tìm thấy số điện thoại. Hãy tạo khách trước."); return; }
      setCustomerId(matched.id); setCustomerName(matched.name);
      toast.success("Đã chọn khách " + matched.name);
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function create(e: FormEvent) {
    e.preventDefault();
    if (!customerId) { toast.error("Cần tìm và chọn khách hàng trước."); return; }
    setBusy(true);
    try {
      await adminRequest("orders", {method: "POST", body: jsonBody({
        customerId, requestId, leadId: null, closingChannel: channel,
        campaignCode: campaignCode.trim() || null,
        items: [{ productId, quantity: Number(quantity), unitPrice: Number(unitPrice),
          unitCost: unitCost.trim() === "" ? null : Number(unitCost) }]
      })});
      toast.success("Đã tạo đơn xác nhận.");
      setRequestId(crypto.randomUUID()); router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function transition(id: string, action: "complete" | "cancel") {
    const warning = action === "complete" ? "Xác nhận đơn đã hoàn tất?" : "Xác nhận hủy đơn?";
    if (!window.confirm(warning)) return;
    setBusy(true);
    try {
      await adminRequest(`orders/${id}/${action}`, {method: "POST"});
      toast.success("Đã cập nhật đơn hàng."); router.refresh();
    } catch(error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  return <main className="space-y-6">
    <header className="admin-page-header"><div>
      <p className="admin-eyebrow">Sales / nội bộ</p>
      <h1 className="admin-title">Đơn hàng</h1>
      <p className="admin-description">Nhập đơn chốt qua Zalo, điện thoại hoặc tư vấn. Không tạo checkout công khai.</p>
    </div></header>
    {canManage && <form onSubmit={create} className="admin-panel grid gap-3 p-5 sm:grid-cols-2">
      <h2 className="text-lg font-semibold sm:col-span-2">Tạo đơn xác nhận</h2>
      <label className="text-sm sm:col-span-2">Tìm số điện thoại khách
        <div className="flex flex-wrap gap-2">
          <input className={field + " sm:max-w-xs"} required value={phone}
            onChange={e => { setPhone(e.target.value); setCustomerId(""); setCustomerName(""); }}
            placeholder="09xxxxxxxx" />
          <button type="button" disabled={busy} onClick={() => void findCustomer()}
            className="rounded-lg border border-[#233b32] px-4 py-2">Tìm khách</button>
        </div>
        <span className="mt-1 block text-[#56635b]">{customerName ? "Đã chọn: " + customerName : "Chưa chọn khách"}</span>
      </label>
      <label className="text-sm">Sản phẩm (đã Published)
        <select required className={field} value={productId} onChange={e => setProductId(e.target.value)}>
          {products.map(p => <option key={p.id} value={p.id}>{p.name} {p.sku ? `(${p.sku})` : ""}</option>)}
        </select>
      </label>
      <label className="text-sm">Kênh chốt
        <select className={field} value={channel} onChange={e => setChannel(e.target.value)}>
          {channels.map(([v,l]) => <option key={v} value={v}>{l}</option>)}
        </select>
      </label>
      <label className="text-sm">Số lượng
        <input required type="number" min={1} max={100000} step={1} className={field}
          value={quantity} onChange={e => setQuantity(e.target.value)} /></label>
      <label className="text-sm">Đơn giá (VNĐ, nội bộ)
        <input required type="number" min={0} step={1} className={field}
          value={unitPrice} onChange={e => setUnitPrice(e.target.value)} /></label>
      <label className="text-sm">Giá vốn (nếu biết)
        <input type="number" min={0} step={1} className={field}
          value={unitCost} onChange={e => setUnitCost(e.target.value)} /></label>
      <label className="text-sm">Mã chiến dịch (nếu có)
        <input maxLength={100} className={field} value={campaignCode}
          onChange={e => setCampaignCode(e.target.value)} placeholder="hang-moi-thang-11" /></label>
      <button disabled={busy || !customerId || !productId} type="submit"
        className="admin-primary-button rounded-lg px-5 py-2 sm:col-span-2">
        {busy ? "Đang xử lý..." : "Tạo đơn hàng"}
      </button>
    </form>}
    <div className="admin-panel overflow-x-auto">
      <table className="w-full min-w-[650px] text-left text-sm">
        <thead><tr className="border-b border-[#ddd8cc]">
          <th className="px-4 py-3">Đơn</th><th className="px-4 py-3">Khách</th>
          <th className="px-4 py-3">Kênh / nguồn</th><th className="px-4 py-3">Giá trị</th>
          <th className="px-4 py-3">Trạng thái</th>
        </tr></thead>
        <tbody>{orders.items.map(o => <tr key={o.id} className="border-b border-[#eeeae1]">
          <td className="px-4 py-3 font-medium">{o.orderCode}</td>
          <td className="px-4 py-3">{o.customerName}<p className="text-xs text-[#56635b]">{o.isReturning ? "Khách quay lại" : "Chưa xác minh mua trước"}</p></td>
          <td className="px-4 py-3">{o.closingChannel}<p className="text-xs text-[#56635b]">{o.acquisitionSource}</p></td>
          <td className="px-4 py-3">{money(o.totalAmount)}</td>
          <td className="px-4 py-3">{o.status}
            {canManage && o.status === "Confirmed" && <div className="mt-2 flex gap-2">
              <button disabled={busy} type="button" className="admin-link" onClick={() => void transition(o.id, "complete")}>Hoàn tất</button>
              <button disabled={busy} type="button" className="admin-link" onClick={() => void transition(o.id, "cancel")}>Hủy</button>
            </div>}
          </td>
        </tr>)}</tbody>
      </table>
      {!orders.items.length && <p className="p-5 text-sm">Chưa có đơn hàng.</p>}
    </div>
  </main>;
}
