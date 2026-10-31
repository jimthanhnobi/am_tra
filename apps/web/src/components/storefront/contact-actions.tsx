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
  const classes = "sf-button sf-button-outline";
  return <div><div className="flex flex-wrap gap-3">{links.map((link) => <a key={link.channel} className={classes} href={link.href} target={link.channel === "hotline" ? undefined : "_blank"} rel={link.channel === "hotline" ? undefined : "noopener noreferrer"} onClick={() => record(link.channel)}>{link.label}</a>)}</div><label className="mt-5 flex max-w-xl items-start gap-3 text-xs leading-6 text-current opacity-80"><input type="checkbox" checked={consent} onChange={(event) => setConsent(event.target.checked)} className="mt-1 size-4 shrink-0 accent-[#925b3d]" />Cho phép ghi nhận lượt nhấn kênh liên hệ. Lượt nhấn không được tính là yêu cầu tư vấn.</label></div>;
}
