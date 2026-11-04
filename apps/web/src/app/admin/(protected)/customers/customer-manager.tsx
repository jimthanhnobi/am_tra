"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
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
  const [interactions, setInteractions] = useState<{ id: string; channel: string; kind: string; notes: string; occurredAtUtc: string }[]>([]);

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
      toast.success("Đã lưu lịch sử tư vấn.");
      const updated = await adminRequest<typeof interactions>(`customers/${selected.id}/interactions`);
      setInteractions(updated); setNotes("");
    } catch (error) { toast.error(getErrorMessage(error)); }
    finally { setBusy(false); }
  }

  async function openCustomer(customer: Customer) {
    setSelected(customer);
    setInteractions([]);
    try {
      setInteractions(await adminRequest<typeof interactions>(`customers/${customer.id}/interactions`));
    } catch (error) { toast.error(getErrorMessage(error)); }
  }

  function searchCustomers(e: FormEvent) {
    e.preventDefault();
    router.push(`/admin/customers?search=${encodeURIComponent(query)}`);
  }

  const field = "admin-field";
  return <main className="space-y-6">
    <header className="admin-page-header">
      <div><p className="admin-eyebrow">CRM / khách hàng</p>
        <h1 className="admin-title">Khách hàng</h1>
        <p className="admin-description">{data.totalItems} hồ sơ. Theo dõi khách sỉ, khách bán lại và khách mua sử dụng.</p>
      </div>
      {canManage && <Link href="/admin/customers/import" className="admin-secondary-button">Import CSV khách cũ</Link>}
    </header>
    <form onSubmit={searchCustomers} className="admin-panel flex flex-wrap gap-3 p-4">
      <input aria-label="Tìm tên hoặc số điện thoại" className={field + " sm:max-w-md"} value={query}
        onChange={e => setQuery(e.target.value)} placeholder="Tìm theo tên hoặc SĐT..." />
      <button type="submit" className="admin-primary-button rounded-lg px-5 py-2">Tìm kiếm</button>
    </form>
    {canManage && <form onSubmit={createCustomer} className="admin-panel grid gap-5 p-5 sm:grid-cols-2 sm:p-7">
      <div className="sm:col-span-2"><h2 className="admin-card-heading">Tạo hồ sơ khách hàng</h2><p className="admin-field-note mt-2">Chỉ điền nguồn đã biết; nếu chưa xác minh, giữ trạng thái “Chưa xác định”.</p></div>
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
      <table className="admin-data-table min-w-[700px]">
        <thead><tr className="border-b border-[#ddd8cc]">
          <th className="px-4 py-3">Khách hàng</th><th className="px-4 py-3">Nguồn</th>
          <th className="px-4 py-3">Nhãn</th><th className="px-4 py-3">Lịch sử mua</th><th className="px-4 py-3">Thao tác</th>
        </tr></thead>
        <tbody>{data.items.map(c => <tr key={c.id} className="border-b border-[#eeeae1]">
          <td className="px-4 py-3"><strong>{c.name}</strong><p>{c.phone}</p></td>
          <td className="px-4 py-3">{sources.find(([code]) => code === c.acquisitionSource)?.[1] ?? c.acquisitionSource}</td>
          <td className="px-4 py-3">{c.tags.length ? <span className="flex flex-wrap gap-1">{c.tags.map(tag => <span className="admin-tag" key={tag}>{tag}</span>)}</span> : "—"}</td>
          <td className="px-4 py-3"><span className="admin-status">{c.hadPriorPurchase ? "Đã xác nhận" : "Chưa rõ"}</span></td>
          <td className="px-4 py-3"><button className="admin-link" type="button" onClick={() => void openCustomer(c)}>Lịch sử tư vấn</button></td>
        </tr>)}</tbody>
      </table>
      {!data.items.length && <p className="p-5 text-sm">Không có khách phù hợp.</p>}
    </div>
    {data.totalItems > data.pageSize && <nav className="flex items-center justify-between text-sm" aria-label="Phân trang khách hàng">
      <span>Trang {data.page}/{Math.ceil(data.totalItems / data.pageSize)}</span>
      <div className="flex gap-3">
        {data.page > 1 && <Link className="admin-link" href={`/admin/customers?page=${data.page - 1}&search=${encodeURIComponent(search)}`}>Trước</Link>}
        {data.page * data.pageSize < data.totalItems && <Link className="admin-link" href={`/admin/customers?page=${data.page + 1}&search=${encodeURIComponent(search)}`}>Tiếp</Link>}
      </div>
    </nav>}
    {canManage && selected && <form onSubmit={addInteraction} className="admin-panel space-y-3 p-5">
      <div className="flex flex-wrap items-center justify-between gap-3"><div><p className="admin-eyebrow">Hồ sơ tư vấn</p><h2 className="admin-card-heading mt-1">{selected.name}</h2><p className="admin-field-note mt-1">{selected.phone}</p></div><button type="button" className="admin-secondary-button" onClick={() => setSelected(null)}>Đóng</button></div>
      <h3 className="text-sm font-semibold text-[#233b32]">Thêm trao đổi mới</h3>
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
      <button disabled={busy} type="submit" className="admin-primary-button">Lưu ghi chú</button>
      <div className="border-t border-[#ddd8cc] pt-5"><h3 className="admin-card-heading">Lịch sử trao đổi</h3>
        {interactions.length ? <ol className="mt-4 space-y-4">{interactions.map(item => <li key={item.id} className="border-l-2 border-[#b79b6c] bg-[#f7f4ec] p-4">
          <p className="text-xs font-semibold text-[#925b3d]">{item.kind} · {item.channel} · {new Date(item.occurredAtUtc).toLocaleString("vi-VN")}</p>
          <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-7 text-[#233b32]">{item.notes}</p>
        </li>)}</ol> : <p className="mt-3 text-sm text-[#56635b]">Chưa có ghi chú tư vấn.</p>}
      </div>
    </form>}
  </main>;
}
