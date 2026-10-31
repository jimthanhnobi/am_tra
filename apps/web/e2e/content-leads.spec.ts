import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

test.beforeEach(async ({ page }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== "true")
    throw new Error("Set E2E_ISOLATED_DATABASE=true only after confirming a disposable test database.");
  if (!adminEmail || !adminPassword)
    throw new Error("Set E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD for a disposable Development admin account.");
  await page.goto("/admin/login");
  await page.getByLabel("Email").fill(adminEmail!);
  await page.getByLabel("Mật khẩu").fill(adminPassword!);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).toHaveURL(/\/admin$/);
});

test("article moves from draft to public HTML, then archive removes it", async ({ page, request }) => {
  const slug = `e2e-bai-viet-${randomUUID().slice(0, 12)}`;
  const title = `Bài viết kiểm thử ${slug}`;
  const publicPath = `/kien-thuc/${slug}`;
  page.on("dialog", (dialog) => void dialog.accept());

  await page.goto("/admin/articles/new");
  await page.getByLabel("Tiêu đề").fill(title);
  await page.getByLabel("Slug").fill(slug);
  await page.getByLabel("Tóm tắt").fill("Nội dung kiểm thử trình biên tập, không phải thông tin sản phẩm.");
  await page.locator(".article-editor [contenteditable='true']").fill("Nội dung kiểm thử từ trình duyệt.");
  await page.getByLabel("SEO title").fill(title);
  await page.getByLabel("Meta description").fill("Bài viết kiểm thử luồng xuất bản CMS.");
  await page.getByRole("button", { name: "Lưu bài viết" }).click();
  await expect(page).toHaveURL(/\/admin\/articles\/[0-9a-f-]+\/edit$/);
  const editUrl = page.url();

  await expect.poll(async () => (await request.get(publicPath)).status()).toBe(404);
  await page.getByRole("button", { name: "Preview đã lưu" }).click();
  await expect(page.getByText("Preview HTML đã lưu")).toBeVisible();
  await expect(page.locator(".catalog-prose").getByText("Nội dung kiểm thử từ trình duyệt.")).toBeVisible();
  await page.getByRole("button", { name: "Gửi duyệt" }).click();
  await page.getByRole("button", { name: "Publish", exact: true }).click();
  await expect.poll(async () => (await request.get(publicPath)).status()).toBe(200);
  const publicResponse = await page.goto(publicPath);
  expect(publicResponse?.status()).toBe(200);
  await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
  const jsonLd = await page.locator("script[type='application/ld+json']").first().textContent();
  expect(jsonLd).toContain('"@type":"Article"');
  await expect(page.locator("body")).not.toContainText("contentJson");

  await page.goto(editUrl);
  await page.getByRole("button", { name: "Archive" }).click();
  await expect.poll(async () => (await request.get(publicPath)).status()).toBe(404);
});

test("contact form creates one consented lead that Admin can manage", async ({ page }) => {
  const name = `E2E kiểm thử ${randomUUID().slice(0, 8)}`;
  await page.goto("/lien-he");
  await page.getByLabel("Tên của bạn").fill(name);
  await page.getByLabel("Email").fill("browser-test@example.invalid");
  await page.getByLabel("Nội dung").fill("Yêu cầu thử nghiệm biểu mẫu từ trình duyệt.");
  await expect(page.getByRole("button", { name: "Gửi yêu cầu" })).toBeDisabled();
  await page.getByLabel(/Tôi đồng ý gửi thông tin liên hệ/).check();
  await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
  await expect(page.getByRole("status")).toContainText("đã được ghi nhận");

  await page.goto("/admin/leads");
  await expect(page.getByRole("heading", { name, level: 2 })).toBeVisible();
  await page.getByRole("combobox", { name: `Trạng thái lead của ${name}` }).selectOption("Closed");
  await expect(page.getByRole("combobox", { name: `Trạng thái lead của ${name}` })).toHaveValue("Closed");
});
