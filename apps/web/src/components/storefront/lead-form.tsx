"use client";

import { useState, type FormEvent } from "react";
import { publicMutationUrl } from "@/lib/api/public-browser";

type Field = "name" | "contact" | "message" | "consent";
const fieldClass = "mt-2 min-h-12 w-full border border-[#bcb5a7] bg-white px-4 py-3 text-base text-[#202722] outline-none focus:border-[#925b3d]";

export function LeadForm({ initialMessage = "" }: Readonly<{ initialMessage?: string }>) {
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState(initialMessage);
  const [website, setWebsite] = useState("");
  const [consent, setConsent] = useState(false);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [resultIsError, setResultIsError] = useState(false);
  const [errors, setErrors] = useState<Partial<Record<Field, string>>>({});

  async function submit(event: FormEvent) {
    event.preventDefault();
    const next: Partial<Record<Field, string>> = {};
    if (name.trim().length < 2) next.name = "Vui lòng nhập tên có ít nhất 2 ký tự.";
    if (!phone.trim() && !email.trim()) next.contact = "Cần số điện thoại hoặc email để phản hồi.";
    else if (email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) next.contact = "Email chưa đúng định dạng.";
    if (message.trim().length < 5) next.message = "Vui lòng nhập nội dung có ít nhất 5 ký tự.";
    if (!consent) next.consent = "Vui lòng đồng ý để gửi yêu cầu tư vấn.";
    setErrors(next);
    if (Object.keys(next).length) return;

    setBusy(true); setResult(null);
    try {
      const query = new URLSearchParams(window.location.search);
      const utm = Object.fromEntries(["source", "medium", "campaign", "term", "content"]
        .map((key) => [key, query.get(`utm_${key}`)] as const).filter((pair): pair is readonly [string, string] => !!pair[1]));
      const response = await fetch(publicMutationUrl("leads"), { method: "POST", headers: { "content-type": "application/json" },
        body: JSON.stringify({ name: name.trim(), phone: phone.trim() || null, email: email.trim() || null,
          message: message.trim(), productId: null, sourcePage: window.location.pathname,
          channel: "web_form", utm, consent, website }),
      });
      if (!response.ok) {
        setResultIsError(true);
        setResult(response.status === 429 ? "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau." : "Không thể gửi yêu cầu. Vui lòng kiểm tra thông tin hoặc thử lại sau.");
        return;
      }
      setResultIsError(false);
      setResult("Yêu cầu đã được ghi nhận. Cảm ơn bạn đã liên hệ.");
      setName(""); setPhone(""); setEmail(""); setMessage(""); setConsent(false);
    } catch { setResultIsError(true); setResult("Không thể kết nối lúc này. Vui lòng thử lại sau."); }
    finally { setBusy(false); }
  }

  return <form onSubmit={(event) => void submit(event)} noValidate className="sf-card grid gap-5 p-7 sm:p-9">
    <div><p className="sf-kicker">Gửi lời nhắn</p><h2 className="mt-3 font-serif text-3xl text-[#233b32]">Yêu cầu tư vấn</h2><p className="mt-3 text-sm leading-7 text-[#56635b]">Thông tin bạn gửi chỉ dùng để phản hồi yêu cầu này.</p></div>
    <label className="block text-sm font-medium text-[#233b32]" htmlFor="lead-name">Tên của bạn <span aria-hidden="true">*</span><input id="lead-name" className={fieldClass} value={name} onChange={(event) => { setName(event.target.value); setErrors((current) => ({ ...current, name: undefined })); }} maxLength={200} autoComplete="name" aria-invalid={Boolean(errors.name)} aria-describedby={errors.name ? "lead-name-error" : undefined} /></label>
    {errors.name && <p id="lead-name-error" className="-mt-4 text-sm text-[#9b332b]">{errors.name}</p>}
    <div className="grid gap-5 sm:grid-cols-2"><label className="block text-sm font-medium text-[#233b32]" htmlFor="lead-phone">Số điện thoại<input id="lead-phone" className={fieldClass} value={phone} onChange={(event) => { setPhone(event.target.value); setErrors((current) => ({ ...current, contact: undefined })); }} maxLength={30} autoComplete="tel" inputMode="tel" aria-invalid={Boolean(errors.contact)} /></label><label className="block text-sm font-medium text-[#233b32]" htmlFor="lead-email">Email<input id="lead-email" className={fieldClass} type="email" value={email} onChange={(event) => { setEmail(event.target.value); setErrors((current) => ({ ...current, contact: undefined })); }} maxLength={254} autoComplete="email" aria-invalid={Boolean(errors.contact)} /></label></div>
    <p className="-mt-3 text-xs leading-5 text-[#56635b]">Điền ít nhất một kênh để chúng tôi có thể phản hồi.</p>
    {errors.contact && <p role="alert" className="-mt-3 text-sm text-[#9b332b]">{errors.contact}</p>}
    <label className="block text-sm font-medium text-[#233b32]" htmlFor="lead-message">Nội dung <span aria-hidden="true">*</span><textarea id="lead-message" className={fieldClass} rows={5} value={message} onChange={(event) => { setMessage(event.target.value); setErrors((current) => ({ ...current, message: undefined })); }} maxLength={4000} aria-invalid={Boolean(errors.message)} aria-describedby={errors.message ? "lead-message-error" : undefined} /></label>
    {errors.message && <p id="lead-message-error" className="-mt-4 text-sm text-[#9b332b]">{errors.message}</p>}
    <label className="absolute -left-[10000px]" aria-hidden="true">Website<input tabIndex={-1} autoComplete="off" value={website} onChange={(event) => setWebsite(event.target.value)} /></label>
    <label className="flex items-start gap-3 text-sm leading-6 text-[#233b32]"><input type="checkbox" className="mt-1 size-4 shrink-0 accent-[#233b32]" checked={consent} onChange={(event) => { setConsent(event.target.checked); setErrors((current) => ({ ...current, consent: undefined })); }} aria-invalid={Boolean(errors.consent)} /><span>Tôi đồng ý gửi thông tin liên hệ để website phản hồi yêu cầu tư vấn này.</span></label>
    {errors.consent && <p role="alert" className="-mt-3 text-sm text-[#9b332b]">{errors.consent}</p>}
    <div className="flex flex-wrap items-center gap-4"><button type="submit" disabled={busy} className="sf-button disabled:cursor-wait disabled:opacity-60">{busy ? "Đang gửi…" : "Gửi yêu cầu"}</button><span className="text-xs text-[#56635b]">* Thông tin cần nhập</span></div>
    {result && <p role={resultIsError ? "alert" : "status"} className="border-l-[3px] border-[#b79b6c] bg-[#f7f4ec] p-4 text-sm text-[#233b32]">{result}</p>}
  </form>;
}
