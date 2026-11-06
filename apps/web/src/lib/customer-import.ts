export type CustomerImportInput = {
  name: string; phone: string; origin: string; acquisitionSource: string;
  hadPriorPurchase: boolean; tags: string[]; notes: string | null; importError: string | null;
};

function fold(value: string) {
  return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[đĐ]/g, "d").toLowerCase().trim();
}

export function parseCustomers(csv: string, acquisitionSource: string, tag: string): CustomerImportInput[] {
  const rows: string[][] = [];
  let row: string[] = [], cell = "", quoted = false;
  const value = csv.replace(/^\uFEFF/, "");
  for (let i = 0; i < value.length; i++) {
    const c = value[i];
    if (c === '"') {
      if (quoted && value[i + 1] === '"') { cell += '"'; i++; }
      else quoted = !quoted;
    } else if (c === "," && !quoted) { row.push(cell.trim()); cell = ""; }
    else if ((c === "\n" || c === "\r") && !quoted) {
      if (c === "\r" && value[i + 1] === "\n") i++;
      row.push(cell.trim()); cell = ""; rows.push(row); row = [];
    } else cell += c;
  }
  if (quoted) throw new Error("CSV chứa dấu nháy không đóng.");
  row.push(cell.trim());
  if (row.some(Boolean)) rows.push(row);
  if (rows.length < 2) throw new Error("CSV cần có tiêu đề và ít nhất một khách.");
  if (rows.length > 1001) throw new Error("Mỗi lần chỉ nhập tối đa 1.000 dòng.");
  const headers = rows[0].map(x => fold(x).replace(/\s+/g, "_"));
  const column = (...names: string[]) => headers.findIndex(x => names.includes(x));
  const nameIndex = column("name", "ten", "ho_ten", "ten_khach", "ten_khach_hang");
  const phoneIndex = column("phone", "sdt", "so_dien_thoai", "dien_thoai");
  const tagIndex = column("tags", "nhan", "nhom");
  const historyIndex = column("had_prior_purchase", "da_mua");
  const sourceIndex = column("acquisition_source", "nguon");
  const notesIndex = column("notes", "ghi_chu");
  if (nameIndex < 0 || phoneIndex < 0) throw new Error("CSV cần cột Tên và Số điện thoại (hoặc name,phone).");
  return rows.slice(1).map(cols => {
    const history = fold(cols[historyIndex] ?? "");
    const validHistory = ["", "true", "false", "1", "0", "co", "khong", "yes", "no"].includes(history);
    return {
      name: cols[nameIndex] ?? "", phone: cols[phoneIndex] ?? "", origin: "legacy_import",
      acquisitionSource: cols[sourceIndex]?.trim() || acquisitionSource,
      hadPriorPurchase: ["true", "1", "co", "yes"].includes(history),
      tags: (cols[tagIndex]?.trim() || tag).split(/[;|,]/).map(x => x.trim()).filter(Boolean),
      notes: cols[notesIndex]?.trim() || null,
      importError: cols.length !== headers.length ? "Số cột không khớp tiêu đề." :
        !validHistory ? "Cột đã mua cần Có/Không, true/false hoặc 1/0." : null,
    };
  });
}
