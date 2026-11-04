"use client";
import Link from "next/link";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { adminRequest, jsonBody } from "@/lib/api/client";
import { getErrorMessage } from "@/lib/admin-format";

type CustomerInput = {
  name: string; phone: string; origin: string; acquisitionSource: string;
  hadPriorPurchase: boolean; tags: string[];
};
type ImportResult = {
  inputCount: number; newCount: number; existingCount: number; dryRun: boolean;
};

const field = "admin-field";

/** Excel's CSV/UTF-8 exports support quoted values, commas, and CRLF. */
function parseCsv(csv: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let cell = "";
  let quoted = false;
  const value = csv.replace(/^\uFEFF/, "");
  for (let i = 0; i < value.length; i++) {
    const c = value[i];
    if (c === '"') {
      if (quoted && value[i + 1] === '"') { cell += '"'; i++; }
      else quoted = !quoted;
    } else if (c === "," && !quoted) {
      row.push(cell.trim()); cell = "";
    } else if ((c === "\n" || c === "\r") && !quoted) {
      if (c === "\r" && value[i + 1] === "\n") i++;
      row.push(cell.trim()); cell = "";
      if (row.some(x => x.length > 0)) rows.push(row);
      row = [];
    } else {
      cell += c;
    }
  }
  if (quoted) throw new Error("CSV chứa dấu nháy không đóng.");
  row.push(cell.trim());
  if (row.some(x => x.length > 0)) rows.push(row);
  if (rows.length < 2) throw new Error("File CSV cần có dòng tiêu đề và ít nhất một khách.");
  return rows;
}

function parseCustomers(csv: string, acquisitionSource: string, tag: string): CustomerInput[] {
  const rows = parseCsv(csv);
  const headers = rows[0].map(x => x.toLowerCase().replace(/\s+/g, "_"));
  const nameIndex = headers.findIndex(x => ["name", "ten", "ho_ten", "ten_khach"].includes(x));
  const phoneIndex = headers.findIndex(x => ["phone", "sdt", "so_dien_thoai", "dien_thoai"].includes(x));
  if (nameIndex < 0 || phoneIndex < 0) throw new Error("CSV cần cột name và phone (hoặc ten và sdt).");
  const result = rows.slice(1).map((cols, idx) => {
    if (cols.length !== headers.length) throw new Error(`Dòng ${idx + 2} có số cột không hợp lệ.`);
    return {
      name: cols[nameIndex], phone: cols[phoneIndex],
      origin: "legacy_import", acquisitionSource,
      hadPriorPurchase: false, tags: tag ? [tag] : [],
    };
  });
  if (result.length > 1000) throw new Error("Mỗi lần chỉ nhập tối đa 1.000 khách.");
  return result;
}

export function CustomerImport() {
  const router = useRouter();
  const [csv, setCsv] = useState("");
  const [source, setSource] = useState("unknown");
  const [tag, setTag] = useState("");
  const [preview, setPreview] = useState<ImportResult | null>(null);
  const [busy, setBusy] = useState(false);

  async function onFile(file?: File) {
    if (!file) return;
    setPreview(null);
    if (file.size > 150_000) {
      toast.error("File CSV quá lớn, hãy chia nhỏ trước khi import."); return;
    }
    try { setCsv(await file.text()); }
    catch { toast.error("Không đọc được file CSV."); }
  }

  async function upload(dryRun: boolean) {
    setBusy(true);
    try {
      const items = parseCustomers(csv, source, tag);
      const result = await adminRequest<ImportResult>("customers/import", {
        method: "POST", body: jsonBody({ dryRun, items })
      });
      if (dryRun) {
        setPreview(result);
        toast.success("Đã kiểm tra dữ liệu, chưa ghi vào database.");
      } else {
        setPreview(null); setCsv("");
        toast.success(`Đã thêm ${result.newCount} khách, bỏ qua ${result.existingCount} khách trùng.`);
        router.push("/admin/customers"); router.refresh();
      }
    } catch(error) { toast.error(error instanceof Error && !(error as Error).message.includes("API")
      ? getErrorMessage(error) : String(error)); }
    finally { setBusy(false); }
  }

  return <main className="space-y-6">
    <header className="admin-page-header"><div>
      <p className="admin-eyebrow">CRM / nhập danh sách</p>
      <h1 className="admin-title">Import khách hàng cũ</h1>
      <p className="admin-description">Dành cho 400–500 khách đang có. Luôn kiểm tra trước, chỉ ghi dữ liệu sau khi xác nhận.</p>
    </div></header>
    <div className="admin-panel space-y-5 p-5 sm:p-7">
      <div className="border-b border-[#ddd8cc] pb-5"><h2 className="admin-card-heading">Danh sách khách hàng hiện có</h2>
        <p className="admin-field-note mt-2">Bước 1: tải CSV · Bước 2: kiểm tra dữ liệu · Bước 3: xác nhận import. Không ghi đè hồ sơ có sẵn.</p></div>
      <p className="text-sm">Xuất CSV UTF-8 từ Excel với cột <code>name,phone</code> hoặc <code>ten,sdt</code>.
        Mỗi số điện thoại chỉ được tạo một lần. Import không làm thay đổi hồ sơ khách đã tồn tại.</p>
      <label className="block text-sm font-medium">File CSV
        <input type="file" accept=".csv,text/csv" className="mt-2 block w-full text-sm" onChange={e => void onFile(e.target.files?.[0])} />
      </label>
      <label className="block text-sm">Nguồn khách ban đầu
        <select className={field} value={source} onChange={e => {setSource(e.target.value);setPreview(null);}}>
          <option value="unknown">Chưa xác định</option>
          <option value="legacy_base">Tệp khách có sẵn</option>
          <option value="referral">Được giới thiệu (có xác nhận)</option>
          <option value="zalo_personal">Zalo cá nhân (có xác nhận)</option>
        </select>
      </label>
      <label className="block text-sm">Gắn nhãn mặc định
        <select className={field} value={tag} onChange={e => {setTag(e.target.value);setPreview(null);}}>
          <option value="">Chưa phân nhóm</option>
          <option value="wholesale">Nhập sỉ</option>
          <option value="reseller">Bán lại</option>
          <option value="end_user">Mua sử dụng</option>
          <option value="vip">Khách VIP</option>
        </select>
      </label>
      <p className="text-xs text-[#56635b]">Việc có số điện thoại không đồng nghĩa đã đồng ý nhận tin tiếp thị. Lịch sử mua hàng chưa được xác minh sẽ để chưa rõ.</p>
      <div className="flex flex-wrap gap-3">
        <button type="button" disabled={busy || !csv} onClick={() => void upload(true)}
          className="admin-primary-button">1. Kiểm tra dữ liệu</button>
        <button type="button" disabled={busy || !preview || preview.dryRun !== true}
          onClick={() => void upload(false)} className="admin-secondary-button">
          2. Xác nhận ghi dữ liệu
        </button>
      </div>
      {preview && <div className="rounded-lg bg-[#e7eee8] p-4 text-sm">
        <p className="font-semibold">Xem trước: {preview.inputCount} khách</p>
        <p>Được thêm: {preview.newCount} · Đã tồn tại: {preview.existingCount}</p>
        <p>Chưa ghi database. Hãy xác nhận để hoàn tất.</p>
      </div>}
    </div>
    <Link href="/admin/customers" className="admin-link inline-flex min-h-11 items-center">Quay lại khách hàng</Link>
  </main>;
}
