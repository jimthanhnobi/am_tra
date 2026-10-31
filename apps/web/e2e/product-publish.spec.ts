import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";

const mediaAssetId = process.env.E2E_MEDIA_ASSET_ID;
test.skip(!mediaAssetId, "Provide a MediaAsset fixture in the isolated test database.");

test("Admin publishes and archives a product with a cover asset", async ({ page, request }) => {
  if (process.env.E2E_ISOLATED_DATABASE !== "true")
    throw new Error("E2E product publishing requires a disposable test database.");
  const email = process.env.E2E_ADMIN_EMAIL;
  const password = process.env.E2E_ADMIN_PASSWORD;
  if (!email || !password) throw new Error("Set E2E Admin credentials for the isolated database.");
  await page.goto("/admin/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Mật khẩu").fill(password);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).toHaveURL(/\/admin$/);
  const fixtureResponse = await page.request.get("/api/admin/media?search=product-fixture.webp");
  expect(fixtureResponse.status()).toBe(200);
  const fixturePage = await fixtureResponse.json() as { items: { id: string; fileName: string }[] };
  const fixture = fixturePage.items.find((item) => item.id === mediaAssetId);
  if (!fixture) throw new Error("The isolated MediaAsset fixture is missing from Admin API.");

  const suffix = randomUUID().slice(0, 12);
  const categorySlug = `e2e-danh-muc-${suffix}`;
  const productSlug = `e2e-san-pham-${suffix}`;
  const renamedSlug = `e2e-doi-slug-${suffix}`;
  const title = `Sản phẩm kiểm thử ${suffix}`;
  await page.goto("/admin/categories");
  await page.getByRole("button", { name: "+ Thêm" }).click();
  await page.getByLabel("Tên *").fill(`Danh mục kiểm thử ${suffix}`);
  await page.getByLabel("Slug *").fill(categorySlug);
  await page.getByLabel("Mô tả").fill("Danh mục kiểm thử trong database cô lập.");
  await page.getByLabel("SEO title").fill(`Danh mục kiểm thử ${suffix}`);
  await page.getByLabel("Meta description").fill("Nội dung kiểm thử danh mục, không mô tả hàng hóa thật.");
  await page.getByRole("button", { name: "Lưu", exact: true }).click();
  await page.getByRole("button", { name: "Gửi duyệt" }).click();
  await page.getByRole("button", { name: "Xuất bản", exact: true }).click();
  await expect.poll(async () => (await request.get(`/danh-muc/${categorySlug}`)).status()).toBe(200);

  await page.goto("/admin/products/new");
  await page.getByLabel("Tên sản phẩm *").fill(title);
  await page.getByLabel("Slug *").fill(productSlug);
  await page.getByLabel("Loại sản phẩm *").fill("Test fixture");
  await page.getByLabel("Tóm tắt").fill("Sản phẩm kiểm thử trong database cô lập.");
  await page.getByLabel("Danh mục chính").selectOption({ label: `Danh mục kiểm thử ${suffix} (Published)` });
  await page.getByLabel("Mô tả sản phẩm").fill("Nội dung kiểm thử, không phải thông tin hàng hóa thật.");
  await page.getByLabel("SEO title").fill(title);
  await page.getByLabel("Meta description").fill("Trang sản phẩm kiểm thử luồng xuất bản.");
  await page.getByRole("button", { name: "Lưu bản nháp" }).click();
  await expect(page).toHaveURL(/\/admin\/products\/[0-9a-f-]+\/edit$/);
  const editUrl = page.url();
  await expect.poll(async () => (await request.get(`/san-pham/${productSlug}`)).status()).toBe(404);

  await page.getByRole("button", { name: "Chọn từ thư viện" }).first().click();
  await page.getByRole("button", { name: fixture.fileName }).click();
  await page.getByLabel("ALT text").fill("Ảnh kiểm thử");
  await page.getByRole("button", { name: "Lưu ALT" }).click();
  await expect(page.getByRole("button", { name: "Lưu ALT" })).toBeDisabled();
  await page.getByRole("button", { name: "Đặt cover" }).click();
  await expect(page.getByText("Cover", { exact: true })).toBeVisible();

  await page.getByRole("button", { name: "Gửi duyệt" }).click();
  await page.getByRole("button", { name: "Xuất bản", exact: true }).click();
  await expect.poll(async () => (await request.get(`/san-pham/${productSlug}`)).status()).toBe(200);
  await page.goto(`/san-pham/${productSlug}`);
  await expect(page.getByRole("heading", { name: title, level: 1 })).toBeVisible();
  await page.goto(`/tim-kiem?q=${suffix}`);
  await expect(page.getByRole("heading", { name: title }).getByRole("link")).toBeVisible();
  await expect(page.getByText(`1 kết quả cho “${suffix}”`)).toBeVisible();
  await page.reload();
  await expect(page.getByRole("heading", { name: title }).getByRole("link")).toBeVisible();
  await page.goto(`/danh-muc/${categorySlug}`);
  await expect(page.getByRole("heading", { name: title }).getByRole("link")).toBeVisible();

  await page.goto(editUrl);
  page.once("dialog", (dialog) => void dialog.accept());
  await page.getByLabel("Slug *").fill(renamedSlug);
  await page.getByRole("button", { name: "Lưu thay đổi" }).click();
  await expect.poll(async () => (await request.get(`/san-pham/${productSlug}`, { maxRedirects: 0 })).status()).toBe(301);
  await expect.poll(async () => (await request.get(`/san-pham/${renamedSlug}`)).status()).toBe(200);
  await page.goto(editUrl);
  const archive = page.getByRole("button", { name: "Lưu trữ", exact: true });
  await archive.click();
  await expect(page.getByRole("alertdialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("alertdialog")).toBeHidden();
  await expect(archive).toBeFocused();
  await archive.click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Lưu trữ" }).click();
  await expect.poll(async () => (await request.get(`/san-pham/${renamedSlug}`)).status()).toBe(404);
});
