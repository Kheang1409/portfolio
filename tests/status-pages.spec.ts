import { expect, test } from "@playwright/test";

test("renders the illustrated not-found page for unknown routes", async ({ page }) => {
  const response = await page.goto("/this-page-does-not-exist");

  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { name: "This page wandered off." })).toBeVisible();
  await expect(page.getByRole("img", { name: "Illustration of Kai beside a 404 not found sign." })).toBeVisible();
  await expect(page.getByRole("link", { name: "Back to home" })).toHaveAttribute("href", "/");
  await expect(page.getByRole("navigation", { name: "Primary navigation" })).toBeVisible();
});

test("renders the maintenance page and marks it as non-indexable", async ({ page }) => {
  const response = await page.goto("/maintenance");

  expect(response?.status()).toBe(200);
  await expect(page.getByRole("heading", { name: "A little work behind the scenes." })).toBeVisible();
  await expect(page.getByRole("img", { name: "Illustration of Kai at a small maintenance work site." })).toBeVisible();
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", "noindex, nofollow");
});

test("shows the greeting illustration in the responsive home hero", async ({ page }) => {
  await page.goto("/");

  const greeting = page.getByRole("img", { name: "Kai waving hello." });
  await expect(greeting).toBeVisible();
  await expect.poll(() => greeting.evaluate((image: HTMLImageElement) => image.naturalWidth)).toBeGreaterThan(0);

  await page.setViewportSize({ width: 390, height: 844 });
  const widths = await page.evaluate(() => ({
    viewport: document.documentElement.clientWidth,
    document: document.documentElement.scrollWidth,
  }));
  expect(widths.document).toBe(widths.viewport);
});
