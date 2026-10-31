import { expect, test } from "@playwright/test";

test.beforeEach(() => {
  if (process.env.E2E_ISOLATED_DATABASE !== "true")
    throw new Error("Security E2E requires a disposable test database.");
});

async function login(page: import("@playwright/test").Page, email: string, password: string) {
  await page.goto("/admin/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Mật khẩu").fill(password);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).toHaveURL(/\/admin$/);
}

test("anonymous is 401; Admin logout ends the session", async ({ page }) => {
  expect((await page.request.get("/api/admin/leads")).status()).toBe(401);
  const email = process.env.E2E_ADMIN_EMAIL;
  const password = process.env.E2E_ADMIN_PASSWORD;
  if (!email || !password) throw new Error("Set isolated Admin credentials.");
  await login(page, email, password);
  const cookies = await page.context().cookies();
  expect(cookies.filter((cookie) => cookie.name.endsWith("TeaPremium.Admin")))
    .toEqual(expect.arrayContaining([expect.objectContaining({ httpOnly: true })]));
  await page.locator("header details > summary").click();
  await page.getByRole("button", { name: "Đăng xuất" }).click();
  await expect(page).toHaveURL(/\/admin\/login$/);
  expect((await page.request.get("/api/admin/leads")).status()).toBe(401);
});

test("Editor receives 403 for Leads and cannot see private lead data", async ({ page }) => {
  const email = process.env.E2E_EDITOR_EMAIL;
  const password = process.env.E2E_ADMIN_PASSWORD;
  if (!email || !password) throw new Error("Set isolated Editor credentials.");
  await login(page, email, password);
  expect((await page.request.get("/api/admin/leads")).status()).toBe(403);
  await page.goto("/admin/leads");
  await expect(page.getByText("Leads.Read")).toBeVisible();
  await expect(page.getByText("browser-test@example.invalid")).toHaveCount(0);
});
