import { expect, test, type Page } from "@playwright/test";

async function mockHealthyBackend(page: Page) {
  await page.route("**/api/health/live", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ status: "healthy" }),
    }),
  );
}

test("renders the illustrated not-found page for unknown routes", async ({ page }) => {
  await mockHealthyBackend(page);
  const response = await page.goto("/this-page-does-not-exist");

  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { name: "This page wandered off." })).toBeVisible();
  await expect(page.getByRole("img", { name: "Illustration of Kai beside a 404 not found sign." })).toBeVisible();
  await expect(page.getByRole("link", { name: "Back to home" })).toHaveAttribute("href", "/");
  await expect(page.getByRole("navigation", { name: "Primary navigation" })).toBeVisible();
});

test("renders the maintenance page and marks it as non-indexable", async ({ page }) => {
  await mockHealthyBackend(page);
  const response = await page.goto("/maintenance");

  expect(response?.status()).toBe(200);
  await expect(page.getByRole("heading", { name: "A little work behind the scenes." })).toBeVisible();
  await expect(page.getByRole("img", { name: "Illustration of Kai at a small maintenance work site." })).toBeVisible();
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", "noindex, nofollow");
});

test("shows the service status page when the backend is down and recovers on retry", async ({
  page,
}) => {
  let healthChecks = 0;
  await page.route("**/api/health/live", (route) => {
    healthChecks += 1;
    return route.fulfill({
      status: healthChecks === 1 ? 503 : 200,
      contentType: "application/json",
      body: JSON.stringify({
        status: healthChecks === 1 ? "unavailable" : "healthy",
      }),
    });
  });

  await page.goto("/");
  await expect.poll(() => healthChecks).toBeGreaterThan(0);

  await expect(
    page.getByRole("heading", { name: "Service temporarily unavailable." }),
  ).toBeVisible();
  await expect(
    page.getByRole("img", {
      name: "Illustration of Kai beside a critical error warning.",
    }),
  ).toBeVisible();

  await page.getByRole("button", { name: "Try again" }).click();
  await expect(page.getByRole("img", { name: "Kai waving hello." })).toBeVisible();
  expect(healthChecks).toBeGreaterThanOrEqual(2);
});

test("shows the greeting illustration in the responsive home hero", async ({ page }) => {
  await mockHealthyBackend(page);
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
