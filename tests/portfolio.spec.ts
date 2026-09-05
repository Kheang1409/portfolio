import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
const repos = [
  {
    title: "Web experience",
    description: "A responsive application.",
    tech: ["TypeScript"],
    github: "https://github.com/Kheang1409/web",
    demo: "",
    featured: true,
    stars: 2,
  },
  {
    title: "API service",
    description: "A backend service.",
    tech: ["C#"],
    github: "https://github.com/Kheang1409/api",
    demo: "",
    featured: false,
    stars: 1,
  },
];
test.beforeEach(async ({ page }) => {
  await page.route("**/api/github/projects", (r) =>
    r.fulfill({ json: { projects: repos } }),
  );
  await page.route("**/api/visitors**", (r) => r.fulfill({ json: {} }));
  await page.route("**/api/visits", (r) => r.fulfill({ json: {} }));
});
test("desktop: sections, filters, images, keyboard, accessibility", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await page.setViewportSize({ width: 1440, height: 960 });
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "Rooted in heritage",
  );
  for (const id of [
    "home",
    "journey",
    "about",
    "skills",
    "projects",
    "featured-project",
    "culture",
    "farming",
    "values",
    "experience",
    "education",
    "contact",
  ])
    await expect(page.locator("#" + id)).toHaveCount(1);
  await page.screenshot({ path: "tests/desktop.png" });
  await page.getByRole("button", { name: "Backend/API", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "API service" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Web experience" }),
  ).toHaveCount(0);
  await page.getByRole("button", { name: "Resume", exact: true }).click();
  await expect(
    page.getByRole("dialog", { name: "Resume preview" }),
  ).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(
    page.getByRole("dialog", { name: "Resume preview" }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("button", { name: "Resume", exact: true }),
  ).toBeFocused();
  await page.getByRole("button", { name: "Open assistant" }).click();
  await expect(page.getByPlaceholder("Ask about my work...")).toBeFocused();
  await page.keyboard.press("Escape");
  for (const img of await page.locator("main img").all()) {
    await img.scrollIntoViewIfNeeded();
    await expect(img).toHaveJSProperty("complete", true);
    expect(
      await img.evaluate((e: HTMLImageElement) => e.naturalWidth),
    ).toBeGreaterThan(0);
  }
  const a11y = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa"])
    .analyze();
  expect(
    a11y.violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => n.target),
    })),
  ).toEqual([]);
  expect(errors).toEqual([]);
});
test("mobile: no overflow, menu, reduced motion, theme, resume route", async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await page.screenshot({ path: "tests/mobile.png" });
  await page.getByRole("button", { name: "Open menu" }).click();
  await page
    .getByRole("navigation", { name: "Primary", exact: true })
    .getByRole("link", { name: "Work", exact: true })
    .click();
  await expect(page.getByRole("button", { name: "Open menu" })).toBeVisible();
  for (const section of await page.locator("main>section").all()) {
    await section.scrollIntoViewIfNeeded();
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
    ).toBe(true);
  }
  expect(
    await page
      .locator(".hero-landscape")
      .evaluate((e) => getComputedStyle(e).animationName),
  ).toBe("none");
  await page.getByRole("button", { name: "Toggle theme" }).click();
  await expect(page.locator("html")).toHaveClass("light");
  await page.goto("/resume");
  await expect(page.locator("#main-content")).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Download Markdown" }),
  ).toHaveAttribute("href", "/resume.md");
});
test("contact: validation, failure persists, retry, success", async ({
  page,
}) => {
  let calls = 0;
  await page.route("**/api/contacts", (r) => {
    calls++;
    return r.fulfill({
      status: calls <= 3 ? 400 : 200,
      json: calls <= 3 ? { message: "Unavailable" } : { message: "Sent" },
    });
  });
  await page.goto("/");
  await page.getByRole("button", { name: "Send message", exact: true }).click();
  expect(calls).toBe(0);
  await page.getByLabel("Name", { exact: true }).fill("Portfolio test");
  await page
    .getByRole("textbox", { name: "Email", exact: true })
    .fill("test@example.com");
  await page
    .getByLabel("Message", { exact: true })
    .fill("Mocked browser test.");
  await page.getByRole("button", { name: "Send message", exact: true }).click();
  await expect(
    page.getByText("Something went wrong. Please try again."),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Send message", exact: true }),
  ).toBeEnabled();
  await page.route("**/api/contacts", (r) =>
    r.fulfill({ json: { message: "Sent" } }),
  );
  await page.getByRole("button", { name: "Send message", exact: true }).click();
  await expect(
    page.getByText("Thanks for your message! I'll get back to you soon."),
  ).toBeVisible();
  await expect(page.getByLabel("Name", { exact: true })).toHaveValue("");
});

test("small screens, tablet and light theme accessibility", async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/");
  for (const width of [320, 768, 1024]) {
    await page.setViewportSize({ width, height: 900 });
    for (const section of await page.locator("main > section").all()) {
      await section.scrollIntoViewIfNeeded();
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= innerWidth,
        ),
      ).toBe(true);
    }
  }
  await page.getByRole("button", { name: "Toggle theme" }).click();
  await expect(page.locator("html")).toHaveClass("light");
  const result = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa"])
    .analyze();
  expect(
    result.violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => n.target),
    })),
  ).toEqual([]);
});
