// Browser smoke against a running storefront with at least two published products with cover images.
import assert from "node:assert/strict";
import { mkdir } from "node:fs/promises";
import { join, resolve } from "node:path";
import { chromium } from "@playwright/test";

const origin = process.env.E2E_WEB_BASE_URL || "http://localhost:3000";
const browser = await chromium.launch({ channel: process.env.E2E_CHROME_CHANNEL || "chrome", headless: true });
try {
  for (const [name, width, height] of [["desktop", 1440, 900], ["tablet", 850, 900], ["mobile", 390, 844], ["narrow", 320, 700]]) {
    const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: 1 });
    try {
      const response = await page.goto(origin, { waitUntil: "networkidle" });
      assert.equal(response?.status(), 200);
      const hero = page.locator(".sf-orbit-hero");
      await page.locator('.sf-orbit-hero[data-ready="true"]').waitFor();
      const items = hero.locator(".sf-orbit-item");
      const count = await items.count();
      assert.ok(count >= 2, `Orbit smoke needs two real product images; found ${count}`);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, `${name} horizontal overflow`);
      if (process.env.ORBIT_SCREENSHOT_DIR) {
        const output = resolve(process.env.ORBIT_SCREENSHOT_DIR);
        await mkdir(output, { recursive: true });
        await hero.screenshot({ path: join(output, `${name}.png`) });
      }

      const initial = await hero.locator("h1").innerText();
      await hero.getByRole("button", { name: "Sản phẩm tiếp theo" }).click();
      await page.waitForFunction((value) => document.querySelector(".sf-orbit-copy h1")?.textContent !== value, initial);
      await hero.getByRole("button", { name: "Sản phẩm trước" }).click();
      await page.waitForFunction((value) => document.querySelector(".sf-orbit-copy h1")?.textContent === value, initial);

      const stage = hero.locator(".sf-orbit-stage");
      const box = await stage.boundingBox();
      assert.ok(box);
      const x = box.x + box.width * (width <= 650 ? .53 : .55);
      const y = box.y + box.height * .54;
      await page.mouse.move(x, y);
      await page.mouse.down();
      await page.mouse.move(x - (width <= 650 ? 130 : 220), y, { steps: 12 });
      await page.mouse.up();
      await page.waitForFunction((value) => document.querySelector(".sf-orbit-copy h1")?.textContent !== value, initial);

      const slider = hero.getByRole("slider", { name: "Chọn sản phẩm nổi bật" });
      await slider.focus();
      await slider.press("Home");
      await page.waitForFunction((value) => document.querySelector(".sf-orbit-copy h1")?.textContent === value, initial);
      assert.equal(await slider.getAttribute("aria-valuetext"), initial);
      await slider.evaluate((element) => element.blur());
      console.log(`${name}: arrows, drag, keyboard slider, responsive width passed`);
    } finally { await page.close(); }
  }
} finally { await browser.close(); }
