"use client";
import { useState } from "react";
import { publicMutationUrl } from "@/lib/api/public-browser";

type Channel = "zalo" | "facebook" | "hotline";
export function ContactActions({ links }: Readonly<{ links: readonly { channel: Channel; href: string; label: string }[] }>) {
  const [consent, setConsent] = useState(false);
  function record(channel: Channel) {
    if (!consent) return;
    void fetch(publicMutationUrl("contact-events"), { method: "POST", keepalive: true,
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ clientEventId: crypto.randomUUID(), eventType: "contact_click",
        channel, productId: null, pagePath: window.location.pathname, consentState: true }),
    }).catch(() => {});
  }
  const classes = "inline-flex items-center justify-center rounded-full border border-[#183c31] px-5 py-3 text-sm font-medium transition hover:bg-[#183c31] hover:text-[#fbf8f1]";
  return <div><div className="flex flex-wrap gap-3">{links.map((link) => <a key={link.channel} className={classes} href={link.href} target={link.channel === "hotline" ? undefined : "_blank"} rel={link.channel === "hotline" ? undefined : "noopener noreferrer"} onClick={() => record(link.channel)}>{link.label}</a>)}</div><label className="mt-4 flex items-start gap-2 text-xs leading-5 text-[#66736c]"><input type="checkbox" checked={consent} onChange={(event) => setConsent(event.target.checked)} className="mt-0.5" />Cho phép ghi nhận lượt nhấn kênh liên hệ. Lượt nhấn không được tính là yêu cầu tư vấn.</label></div>;
}
