import { expect, test } from "@playwright/test";

test("presents a responsive resume with navigation and print actions", async ({
  page,
}) => {
  const response = await page.goto("/resume");

  expect(response?.status()).toBe(200);
  await expect(page.locator(".resume-dossier")).toBeVisible();
  await expect(
    page.getByRole("heading", { level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole("link", { name: /download pdf/i })).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Print resume" }),
  ).toBeVisible();
  await expect(page.locator(".resume-project-tech").first()).toBeVisible();
  await expect(page.locator(".resume-education-icon").first()).toBeVisible();
  await expect(page.locator(".resume-certification-icon").first()).toBeVisible();
  await expect(page.locator(".resume-role").first()).toBeVisible();
  await expect(page.locator(".resume-role-label").first()).toContainText(
    "ROLE 01",
  );
  await expect(
    page.locator(".resume-certification-date").first(),
  ).not.toContainText("T00:00:00");

  const projectTag = page.locator(".resume-project-grid li").first();
  await expect(projectTag).toBeVisible();
  await expect
    .poll(() =>
      projectTag.evaluate((element) => getComputedStyle(element).borderTopWidth),
    )
    .toBe("1px");

  const sectionNav = page.getByRole("navigation", { name: "On this page" });
  if (await sectionNav.isVisible()) {
    const profileLink = sectionNav.getByRole("link", { name: "Profile" });
    await profileLink.click();
    await expect(page).toHaveURL(/#profile$/);
  }

  await page.setViewportSize({ width: 390, height: 844 });
  const widths = await page.evaluate(() => ({
    viewport: document.documentElement.clientWidth,
    document: document.documentElement.scrollWidth,
  }));
  expect(widths.document).toBe(widths.viewport);

  await page.emulateMedia({ media: "print" });
  await expect(page.locator(".resume-actions")).toBeHidden();
});
