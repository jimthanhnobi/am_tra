import { test } from "@playwright/test";
import { mkdir, writeFile } from "node:fs/promises";
import { resolve, join } from "node:path";

type Sample = { viewport: string; path: string; ttfbMs: number; lcpMs: number | null; cls: number; jsDecodedKiB: number; jsTransferredKiB: number; fontTransferredKiB: number; imageTransferredKiB: number; loadedMs: number };
const median = (values: number[]) => [...values].sort((a, b) => a - b)[Math.floor(values.length / 2)];

test("measure isolated production storefront lab timings", async ({ browser, request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== "true") throw new Error("Performance audit requires an isolated database.");
  const base = process.env.API_BASE_URL;
  if (!base || !/^http:\/\/127\.0\.0\.1:\d+\/api\/v1$/.test(base)) throw new Error("Set loopback test API_BASE_URL.");
  const catalog = await request.get(`${base}/public/products?page=1&pageSize=1`);
  const first = (await catalog.json()).items?.[0]?.slug as string | undefined;
  const routes = ["/", "/tim-kiem", "/lien-he", ...(first ? [`/san-pham/${first}`] : [])];
  const samples: Sample[] = [];
  for (const [viewport, width, height] of [["desktop", 1440, 900], ["mobile", 390, 844]] as const) {
    for (const path of routes) for (let run = 0; run < 3; run++) {
      const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: 1 });
      const page = await context.newPage();
      await page.addInitScript(() => {
        const state = { lcp: 0, cls: 0 };
        (window as typeof window & { __p11Perf?: typeof state }).__p11Perf = state;
        try { new PerformanceObserver((list) => { for (const entry of list.getEntries()) state.lcp = entry.startTime; }).observe({ type: "largest-contentful-paint", buffered: true }); } catch { /* unsupported browser */ }
        try { new PerformanceObserver((list) => { for (const entry of list.getEntries()) if (!(entry as PerformanceEntry & { hadRecentInput?: boolean }).hadRecentInput) state.cls += (entry as PerformanceEntry & { value: number }).value; }).observe({ type: "layout-shift", buffered: true }); } catch { /* unsupported browser */ }
      });
      const response = await page.goto(path, { waitUntil: "networkidle" });
      if (response?.status() !== 200) throw new Error(`${path} returned ${response?.status()}`);
      await page.waitForTimeout(200);
      const metrics = await page.evaluate(() => {
        const nav = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming;
        const js = performance.getEntriesByType("resource").filter((item) => item.name.includes(".js")) as PerformanceResourceTiming[];
        const resources = performance.getEntriesByType("resource") as PerformanceResourceTiming[];
        const fonts = resources.filter((item) => /\.(woff2?|ttf)(\?|$)/.test(item.name));
        const images = resources.filter((item) => /\.(avif|jpe?g|png|webp|svg)(\?|$)/.test(item.name) || item.name.includes("/_next/image?"));
        const state = (window as typeof window & { __p11Perf?: { lcp: number; cls: number } }).__p11Perf;
        return { ttfbMs: nav.responseStart - nav.requestStart, lcpMs: state?.lcp || null, cls: state?.cls || 0,
          jsDecodedKiB: js.reduce((total, item) => total + item.decodedBodySize, 0) / 1024,
          jsTransferredKiB: js.reduce((total, item) => total + item.transferSize, 0) / 1024,
          fontTransferredKiB: fonts.reduce((total, item) => total + item.transferSize, 0) / 1024,
          imageTransferredKiB: images.reduce((total, item) => total + item.transferSize, 0) / 1024,
          loadedMs: nav.loadEventEnd - nav.startTime };
      });
      samples.push({ viewport, path, ...metrics });
      await context.close();
    }
  }
  const apiMs: number[] = [];
  for (let index = 0; index < 10; index++) {
    const started = performance.now();
    const response = await fetch(`${base}/public/products?page=1&pageSize=12`, { cache: "no-store" });
    if (!response.ok) throw new Error(`API timing request returned ${response.status}`);
    await response.arrayBuffer();
    apiMs.push(performance.now() - started);
  }
  const summary = Object.entries(Object.groupBy(samples, (sample) => `${sample.viewport} ${sample.path}`)).map(([key, items]) => ({
    route: key,
    runs: items?.length ?? 0,
    ttfbMs: median(items!.map((item) => item.ttfbMs)),
    lcpMs: median(items!.map((item) => item.lcpMs ?? 0)),
    cls: median(items!.map((item) => item.cls)),
    jsDecodedKiB: median(items!.map((item) => item.jsDecodedKiB)),
    jsTransferredKiB: median(items!.map((item) => item.jsTransferredKiB)),
    fontTransferredKiB: median(items!.map((item) => item.fontTransferredKiB)),
    imageTransferredKiB: median(items!.map((item) => item.imageTransferredKiB)),
    loadedMs: median(items!.map((item) => item.loadedMs)),
  }));
  const report = { method: "Playwright Chromium production build, three fresh contexts per viewport and route, localhost; not Lighthouse or field data",
    browser: browser.version(), apiMedianMs: median(apiMs), apiMinMs: Math.min(...apiMs), apiMaxMs: Math.max(...apiMs), summary, samples };
  const directory = resolve(process.env.E2E_REPORT_DIR || "test-results");
  await mkdir(directory, { recursive: true });
  await writeFile(join(directory, "performance.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ apiMedianMs: report.apiMedianMs, summary }));
});
