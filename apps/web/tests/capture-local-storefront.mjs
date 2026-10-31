import { chromium } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import { resolve, join } from "node:path";

const origin = process.env.STOREFRONT_URL || "http://localhost:3000";
const directory = resolve(process.env.STOREFRONT_CAPTURE_DIR || "../../artifacts/prompt9a");
const routes = [["home", "/"], ["blog", "/kien-thuc"], ["contact", "/lien-he"], ["about", "/gioi-thieu"]];
if (process.env.STOREFRONT_VISUAL_ARTICLE_SLUG) routes.push(["article", `/kien-thuc/${process.env.STOREFRONT_VISUAL_ARTICLE_SLUG}`]);
const viewports = [["desktop", 1440, 900], ["tablet", 768, 1024], ["mobile", 390, 844]];

await mkdir(directory, { recursive: true });
const browser = await chromium.launch({ channel: "chrome", headless: true });
try {
  for (const [label, width, height] of viewports) {
    const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
    for (const [name, path] of routes) {
      const response = await page.goto(new URL(path, origin).href, { waitUntil: "networkidle" });
      if (response?.status() !== 200) throw new Error(`${name} ${label}: HTTP ${response?.status()}`);
      if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error(`${name} ${label}: horizontal overflow`);
      await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
      await page.screenshot({ path: join(directory, `${name}-${label}.png`), fullPage: true });
      const brokenImages = await page.locator("img").evaluateAll((images) => images.filter((image) => image.complete && image.naturalWidth === 0).length);
      if (brokenImages) throw new Error(`${name} ${label}: ${brokenImages} broken images`);
      process.stdout.write(`${name} ${label}: HTTP 200, no overflow or broken images\n`);
    }
    await page.close();
  }
} finally {
  await browser.close();
}
