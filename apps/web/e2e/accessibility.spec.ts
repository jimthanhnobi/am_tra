import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import { writeFile } from "node:fs/promises";

test("WCAG 2.2 A/AA automated audit of public flows", async ({ page, request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== "true")
    throw new Error("Accessibility E2E requires a disposable test database.");
  const api = process.env.API_BASE_URL;
  if (!api || !/^http:\/\/127\.0\.0\.1:\d+\/api\/v1$/.test(api)) throw new Error("Set loopback test API_BASE_URL.");
  const [products, articles, categories] = await Promise.all([
    request.get(`${api}/public/products?page=1&pageSize=1`).then((response) => response.json()),
    request.get(`${api}/public/articles?page=1&pageSize=1`).then((response) => response.json()),
    request.get(`${api}/public/categories`).then((response) => response.json()),
  ]);
  const publicPaths = ["/", "/tim-kiem", "/lien-he"];
  if (products.items?.[0]?.slug) publicPaths.push(`/san-pham/${products.items[0].slug}`);
  if (articles.items?.[0]?.slug) publicPaths.push(`/kien-thuc/${articles.items[0].slug}`);
  if (categories[0]?.slug) publicPaths.push(`/danh-muc/${categories[0].slug}`);
  const findings: { viewport: string; path: string; rule: string; impact: string | null; targets: string[] }[] = [];
  for (const [viewport, width, height] of [["desktop", 1440, 900], ["mobile", 390, 844]] as const) {
    await page.setViewportSize({ width, height });
    if (viewport === "mobile") {
      await page.goto("/");
      await page.keyboard.press("Tab");
      await expect(page.getByRole("link", { name: "Đến nội dung chính" })).toBeFocused();
      await page.keyboard.press("Enter");
      await expect(page).toHaveURL(/#noi-dung$/);
      await page.locator(".sf-mobile-nav summary").focus();
      await page.keyboard.press("Enter");
      await expect(page.locator(".sf-mobile-panel")).toBeVisible();
      await page.keyboard.press("Enter");
      await expect(page.locator(".sf-mobile-panel")).toBeHidden();
      await page.emulateMedia({ reducedMotion: "reduce" });
      expect(await page.evaluate(() => getComputedStyle(document.documentElement).scrollBehavior)).toBe("auto");
      await page.emulateMedia({ reducedMotion: "no-preference" });
    }
    for (const path of publicPaths) {
      await page.goto(path);
      await expect(page.locator("h1")).toHaveCount(1);
      const result = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]).analyze();
      findings.push(...result.violations.map((item) => ({ viewport, path, rule: item.id, impact: item.impact ?? null,
        targets: item.nodes.slice(0, 3).map((node) => String(node.target)) })));
    }
    await page.goto("/lien-he");
    await page.locator("#tu-van").getByRole("button", { name: "Gửi yêu cầu" }).click();
    await expect(page.locator("#lead-name-error")).toBeVisible();
    const invalidForm = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"]).analyze();
    findings.push(...invalidForm.violations.map((item) => ({ viewport, path: "/lien-he (validation)", rule: item.id, impact: item.impact ?? null,
      targets: item.nodes.slice(0, 3).map((node) => String(node.target)) })));
  }
  await writeFile("test-results/axe-findings.json", JSON.stringify(findings, null, 2));
  expect(findings).toHaveLength(0);
});
