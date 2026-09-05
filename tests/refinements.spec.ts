import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
const reply = (text: string) =>
  [
    JSON.stringify({ type: "delta", messageId: "r1", text }),
    JSON.stringify({
      type: "completed",
      messageId: "r1",
      modelUsed: "test-model",
    }),
  ].join("\n") + "\n";
test.beforeEach(async ({ page }) => {
  await page.route("**/api/visits", (r) => r.fulfill({ json: {} }));
  await page.route("**/api/github/projects", (r) =>
    r.fulfill({ json: { projects: [] } }),
  );
});
test("navigation order and hero link remain clear on large and short displays", async ({
  page,
}) => {
  await page.goto("/");
  const ids = await page
    .locator("#primary-links a")
    .evaluateAll((es) => es.map((e) => e.getAttribute("href")?.split("#")[1]));
  const sectionIds = await page
    .locator("main>section[id]")
    .evaluateAll((es) => es.map((e) => e.id));
  expect(ids).toEqual(sectionIds.filter((id) => ids.includes(id)));
  for (const [width, height] of [
    [1920, 1080],
    [2560, 1080],
    [1440, 700],
    [1280, 720],
  ]) {
    await page.setViewportSize({ width, height });
    const actions = await page.locator(".hero-actions").boundingBox();
    const gateway = await page.locator(".scroll-gateway").boundingBox();
    expect(gateway!.y).toBeGreaterThan(actions!.y + actions!.height);
    await page.locator(".scroll-gateway").scrollIntoViewIfNeeded();
    expect(
      await page.locator(".scroll-gateway").evaluate((e) => {
        const r = e.getBoundingClientRect();
        return e.contains(
          document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2),
        );
      }),
    ).toBe(true);
  }
});
test("assistant suggestions, retry without duplicate history, completion, persistence and accessible layout", async ({
  page,
}) => {
  const payloads: any[] = [];
  await page.route("**/api/assistant", async (r) => {
    payloads.push(r.request().postDataJSON());
    if (payloads.length === 1)
      return r.fulfill({ status: 503, json: { message: "Unavailable" } });
    return r.fulfill({
      contentType: "application/x-ndjson",
      body: reply(
        "Kheang builds with **C# and .NET**. Explore [his skills](/#skills).",
      ),
    });
  });
  await page.goto("/");
  await page.getByRole("button", { name: "Open assistant" }).click();
  await page.getByRole("button", { name: "Skills & strengths" }).click();
  await expect(page.getByRole("dialog").getByRole("alert")).toBeVisible();
  await page.getByRole("button", { name: "Retry reply" }).click();
  await expect(
    page.getByRole("button", { name: "Copy reply", exact: true }),
  ).toBeVisible();
  await expect(page.locator(".kai-message--user")).toHaveCount(1);
  await expect(page.locator(".kai-message--bot")).toHaveCount(1);
  expect(payloads[1].history).toEqual([]);
  await page
    .getByPlaceholder("Ask about my work...")
    .fill("What about his experience?");
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Send message", exact: true })
    .click();
  await expect(page.locator(".kai-message--bot")).toHaveCount(2);
  expect(payloads[2].history).toHaveLength(2);
  const axe = await new AxeBuilder({ page })
    .include(".assistant-panel")
    .withTags(["wcag2a", "wcag2aa"])
    .analyze();
  expect(
    axe.violations.map((v) => ({
      id: v.id,
      nodes: v.nodes.map((n) => n.target),
    })),
  ).toEqual([]);
  await page.screenshot({ path: "tests/assistant-desktop.png" });
  await page.reload();
  await page.getByRole("button", { name: "Open assistant" }).click();
  await expect(page.locator(".kai-message--user")).toHaveCount(2);
  await page.getByRole("button", { name: "New conversation" }).click();
  await page.getByRole("button", { name: "Start new", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "Skills & strengths" }),
  ).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: "tests/assistant-mobile.png" });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBe(true);
  await page.keyboard.press("Escape");
  await expect(
    page.getByRole("button", { name: "Open assistant" }),
  ).toBeFocused();
});
test("stopping a pending response permits a new message without stale updates", async ({
  page,
}) => {
  let count = 0;
  await page.route("**/api/assistant", async (r) => {
    count++;
    if (count === 1) {
      await new Promise((resolve) => setTimeout(resolve, 1500));
      await r
        .fulfill({
          contentType: "application/x-ndjson",
          body: reply("Old response"),
        })
        .catch(() => {});
    } else
      await r.fulfill({
        contentType: "application/x-ndjson",
        body: reply("New response"),
      });
  });
  await page.goto("/");
  await page.getByRole("button", { name: "Open assistant" }).click();
  await page.getByRole("button", { name: "Career journey" }).click();
  await page.getByRole("button", { name: "Stop response" }).click();
  await page.getByPlaceholder("Ask about my work...").fill("New question");
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Send message" })
    .click();
  await expect(page.getByText("New response", { exact: true })).toBeVisible();
  await expect(page.getByText("Old response", { exact: true })).toHaveCount(0);
});
