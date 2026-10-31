"use client";
import { useState, type FormEvent } from "react";
import { publicMutationUrl } from "@/lib/api/public-browser";

export function LeadForm() {
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [website, setWebsite] = useState("");
  const [consent, setConsent] = useState(false);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<"success" | "error" | null>(null);
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setResult(null);
    try {
      const query = new URLSearchParams(window.location.search);
      const utm = Object.fromEntries(["source", "medium", "campaign", "term", "content"]
        .map((key) => [key, query.get(`utm_${key}`)] as const).filter((pair): pair is readonly [string, string] => !!pair[1]));
      const response = await fetch(publicMutationUrl("leads"), { method: "POST", headers: { "content-type": "application/json" },
        body: JSON.stringify({ name, phone: phone || null, email: email || null, message,
          productId: null, sourcePage: window.location.pathname, channel: "web_form", utm, consent, website }),
      });
      if (!response.ok) throw new Error("Không thể gửi yêu cầu. Vui lòng kiểm tra thông tin hoặc thử lại sau.");
      setResult("success"); setName(""); setPhone(""); setEmail(""); setMessage(""); setConsent(false);
    } catch { setResult("error"); }
    finally { setBusy(false); }
  }
  const field = "mt-1 w-full rounded border border-[#d8d1c3] bg-white px-3 py-2 text-sm";
  return <form onSubmit={(event) => void submit(event)} className="mt-9 grid gap-4 rounded border border-[#d8d1c3] bg-[#fbf8f1] p-6 sm:p-8"><h2 className="font-serif text-2xl">Gửi yêu cầu tư vấn</h2><p className="text-sm leading-6 text-[#66736c]">Thông tin bạn gửi chỉ dùng để phản hồi yêu cầu này. Trang không có chức năng đặt hàng.</p><label className="text-sm">Tên của bạn<input className={field} value={name} onChange={(event) => setName(event.target.value)} required minLength={2} maxLength={200} autoComplete="name" /></label><div className="grid gap-4 sm:grid-cols-2"><label className="text-sm">Số điện thoại<input className={field} value={phone} onChange={(event) => setPhone(event.target.value)} maxLength={30} autoComplete="tel" /></label><label className="text-sm">Email<input className={field} type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={254} autoComplete="email" /></label></div><p className="text-xs text-[#66736c]">Cần ít nhất số điện thoại hoặc email để phản hồi.</p><label className="text-sm">Nội dung<textarea className={field} rows={5} value={message} onChange={(event) => setMessage(event.target.value)} required minLength={5} maxLength={4000} /></label><label className="absolute -left-[10000px]" aria-hidden="true">Website<input tabIndex={-1} autoComplete="off" value={website} onChange={(event) => setWebsite(event.target.value)} /></label><label className="flex items-start gap-3 text-sm leading-6"><input type="checkbox" className="mt-1" checked={consent} onChange={(event) => setConsent(event.target.checked)} required /><span>Tôi đồng ý gửi thông tin liên hệ để website phản hồi yêu cầu tư vấn này.</span></label><button type="submit" disabled={busy || !consent} className="w-fit rounded-full bg-[#183c31] px-6 py-3 text-sm text-white disabled:opacity-50">{busy ? "Đang gửi…" : "Gửi yêu cầu"}</button>{result === "success" && <p role="status" className="text-sm text-[#183c31]">Yêu cầu đã được ghi nhận.</p>}{result === "error" && <p role="alert" className="text-sm text-[#a33d32]">Không thể gửi yêu cầu. Vui lòng kiểm tra thông tin hoặc thử lại sau.</p>}</form>;
}
