"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";

export type Customer = {
  id: string; name: string; phone: string; origin: string; acquisitionSource: string;
  hadPriorPurchase: boolean; tags: string[]; createdAtUtc: string;
};
export type CustomerPage = { items: Customer[]; totalItems: number; page: number; pageSize: number };

const sources = [
  ["unknown", "Chưa xác định"], ["legacy_base", "Khách hàng có sẵn"],
  ["google_organic", "Google / SEO"], ["zalo_oa", "Zalo OA"],
  ["zalo_personal", "Zalo cá nhân"], ["referral", "Được giới thiệu"],
  ["facebook", "Facebook"], ["direct", "Trực tiếp"], ["other", "Khác"],
];

export function CustomerManager({ data, canManage, search }: {
  data: CustomerPage; canManage: boolean; search: string;
}) {
  const router = useRouter();
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [source, setSource] = useState("unknown");
  const [origin, setOrigin] = useState("new");
  const [tags, setTags] = useState("end_user");
  const [history, setHistory] = useState(false);
  const [busy, setBusy] = useState(false);
  const [query, setQuery] = useState(search);
  const [selected, setSelected] = useState<Customer | null>(null);
  const [notes, setNotes] = useState("");
  const [channel, setChannel] = useState("zalo_personal");
  const [interactionKind, setInteractionKind] = useState("note");

  async function createCustomer(e: FormEvent) {
    e.preventDefault(); setBusy(true);
    try {
      await adminRequest("customers", { method: "POST", body: jsonBody({
        name, phone, origin, acquisitionSource: source, hadPriorPurchase: history,
        tags: tags.split(",").map(x => x.trim()).filter(Boolean),
      }) });
      toast.success("Đã tạo hồ sơ khách hàng.");
      setName(""); setPhone(""); router.refresh();
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function addInteraction(e: FormEvent) {
    e.preventDefault();
    if (!selected) return;
    setBusy(true);
    try {
      await adminRequest(`customers/${selected.id}/interactions`, {
        method: "POST", body: jsonBody({ channel, kind: interactionKind, notes }),
      });
      toast.success("Đã lưu lịch sử tư vấn."); setNotes("");
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  function searchCustomers(e: FormEvent) {
    e.preventDefault();
    router.push(`/admin/customers?search=${encodeURIComponent(query)}`);
  }

  const field = "w-full min-h-10 rounded-lg border border-[#ddd8cc] bg-white px-3 py-2 text-sm";
  return <main className="space-y-6">
    <header className="admin-page-header">
      <div><p className="admin-eyebrow">CRM / khách hàng</p>
        <h1 className="admin-title">Khách hàng</h1>
        <p className="admin-description">{data.totalItems} hồ sơ. Theo dõi khách sỉ, khách bán lại và khách mua sử dụng.</p>
      </div>
    </header>
    <form onSubmit={searchCustomers} className="flex flex-wrap gap-2">
      <input aria-label="Tìm tên hoặc số điện thoại" className={field + " sm:max-w-md"} value={query}
        onChange={e => setQuery(e.target.value)} placeholder="Tìm theo tên hoặc SĐT..." />
      <button type="submit" className="admin-primary-button rounded-lg px-5 py-2">Tìm kiếm</button>
    </form>
    {canManage && <form onSubmit={createCustomer} className="admin-panel grid gap-3 p-5 sm:grid-cols-2">
      <h2 className="text-lg font-semibold sm:col-span-2">Thêm khách hàng</h2>
      <label className="text-sm">Tên khách <input className={field} required minLength={2} maxLength={200} value={name} onChange={e => setName(e.target.value)} /></label>
      <label className="text-sm">Điện thoại <input className={field} required value={phone} onChange={e => setPhone(e.target.value)} placeholder="09xxxxxxxx" /></label>
      <label className="text-sm">Nguồn ban đầu <select className={field} value={source} onChange={e => setSource(e.target.value)}>
        {sources.map(([value, text]) => <option key={value} value={value}>{text}</option>)}
      </select></label>
      <label className="text-sm">Tệp khách <select className={field} value={origin} onChange={e => setOrigin(e.target.value)}>
        <option value="new">Khách được tạo mới</option><option value="legacy_import">Khách cũ nhập vào</option><option value="unknown">Chưa xác định</option>
      </select></label>
      <label className="text-sm sm:col-span-2">Nhãn, cách nhau dấu phẩy <input className={field}
        value={tags} onChange={e => setTags(e.target.value)} placeholder="wholesale, vip" /></label>
      <label className="flex items-center gap-2 text-sm sm:col-span-2"><input type="checkbox" checked={history} onChange={e => setHistory(e.target.checked)} />
        Đã xác nhận khách có lịch sử mua hàng trước đây
      </label>
      <button disabled={busy} type="submit" className="admin-primary-button rounded-lg px-5 py-2 sm:col-span-2">
        {busy ? "Đang lưu..." : "Tạo hồ sơ"}
      </button>
    </form>}
    <div className="admin-panel overflow-x-auto">
      <table className="w-full min-w-[650px] text-left text-sm">
        <thead><tr className="border-b border-[#ddd8cc]">
          <th className="px-4 py-3">Khách hàng</th><th className="px-4 py-3">Nguồn</th>
          <th className="px-4 py-3">Nhãn</th><th className="px-4 py-3">Lịch sử mua</th><th className="px-4 py-3">Thao tác</th>
        </tr></thead>
        <tbody>{data.items.map(c => <tr key={c.id} className="border-b border-[#eeeae1]">
          <td className="px-4 py-3"><strong>{c.name}</strong><p>{c.phone}</p></td>
          <td className="px-4 py-3">{c.acquisitionSource}</td>
          <td className="px-4 py-3">{c.tags.join(", ") || "—"}</td>
          <td className="px-4 py-3">{c.hadPriorPurchase ? "Đã xác nhận" : "Chưa rõ"}</td>
          <td className="px-4 py-3"><button className="admin-link" type="button" onClick={() => setSelected(c)}>Ghi chú tư vấn</button></td>
        </tr>)}</tbody>
      </table>
      {!data.items.length && <p className="p-5 text-sm">Không có khách phù hợp.</p>}
    </div>
    {canManage && selected && <form onSubmit={addInteraction} className="admin-panel space-y-3 p-5">
      <h2 className="font-semibold">Ghi chú tư vấn: {selected.name}</h2>
      <div className="flex flex-wrap gap-2">
        <select className={field + " sm:max-w-xs"} aria-label="Kênh" value={channel} onChange={e => setChannel(e.target.value)}>
          <option value="zalo_personal">Zalo cá nhân</option><option value="zalo_oa">Zalo OA</option>
          <option value="website">Website</option><option value="phone">Điện thoại</option>
          <option value="in_person">Trực tiếp</option><option value="other">Khác</option>
        </select>
        <select className={field + " sm:max-w-xs"} aria-label="Loại tương tác" value={interactionKind} onChange={e => setInteractionKind(e.target.value)}>
          <option value="note">Ghi chú</option><option value="inquiry">Hỏi mua</option>
          <option value="quote">Báo giá</option><option value="follow_up">Chăm sóc lại</option>
        </select>
      </div>
      <textarea className={field} rows={3} required maxLength={2000} value={notes}
        onChange={e => setNotes(e.target.value)} placeholder="Ghi nhu cầu, sản phẩm và kết quả tư vấn..." />
      <button disabled={busy} type="submit" className="admin-primary-button rounded-lg px-5 py-2">Lưu ghi chú</button>
    </form>}
  </main>;
}
