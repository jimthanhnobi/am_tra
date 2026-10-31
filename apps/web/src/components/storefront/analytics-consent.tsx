"use client";
import { useSyncExternalStore } from "react";
import Script from "next/script";

function getChoice(): "yes" | "no" | null {
  const value = window.localStorage.getItem("tea-analytics-consent");
  return value === "yes" || value === "no" ? value : null;
}
function subscribe(onChange: () => void) {
  window.addEventListener("storage", onChange);
  window.addEventListener("tea-analytics-choice", onChange);
  return () => { window.removeEventListener("storage", onChange); window.removeEventListener("tea-analytics-choice", onChange); };
}

export function AnalyticsConsent({ measurementId }: Readonly<{ measurementId: string }>) {
  const choice = useSyncExternalStore(subscribe, getChoice, () => null);
  function decide(value: "yes" | "no") {
    window.localStorage.setItem("tea-analytics-consent", value);
    if (value === "no" && choice === "yes") window.location.reload();
    else window.dispatchEvent(new Event("tea-analytics-choice"));
  }
  return <>
    {choice === "yes" && <><Script src={`https://www.googletagmanager.com/gtag/js?id=${measurementId}`} strategy="afterInteractive" /><Script id="ga4-config" strategy="afterInteractive">{`window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments)}gtag('js',new Date());gtag('config','${measurementId}');`}</Script></>}
    {choice === null && <div role="region" aria-label="Tùy chọn phân tích" className="fixed bottom-3 left-3 right-3 z-50 max-w-md rounded-xl border border-[#d8d1c3] bg-white p-4 shadow-lg sm:right-auto"><p className="text-sm leading-6">Cho phép Google Analytics 4 đo lượt xem trang? Website không lưu lượt xem trang vào cơ sở dữ liệu.</p><div className="mt-3 flex gap-3"><button type="button" className="rounded bg-[#183c31] px-4 py-2 text-xs text-white" onClick={() => decide("yes")}>Đồng ý</button><button type="button" className="rounded border px-4 py-2 text-xs" onClick={() => decide("no")}>Từ chối</button></div></div>}
    {choice === "yes" && <button type="button" className="sr-only focus:not-sr-only focus:fixed focus:bottom-3 focus:left-3 focus:z-50 focus:bg-white focus:p-3" onClick={() => decide("no")}>Tắt phân tích</button>}
  </>;
}
