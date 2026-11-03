import { expect, test, type Page, type Route } from "@playwright/test";

type JsonResponse = { status?: number; body: Record<string, unknown> };
type CheckoutSetup = {
  orderResponse?: (body: Record<string, unknown>) => JsonResponse;
  captureResponse?: (body: Record<string, unknown>) => JsonResponse;
  configHandler?: (route: Route) => Promise<void>;
};

async function openDonationCheckout(page: Page, setup: CheckoutSetup = {}) {
  await page.addInitScript(() => {
    type ButtonsOptions = {
      fundingSource: string;
      createOrder: () => Promise<string>;
      onApprove: (data: { orderID: string }) => Promise<void>;
      onCancel: () => void;
      onError: () => void;
      style?: { color?: string; shape?: string; label?: string };
    };
    const paypal = {
      FUNDING: { PAYPAL: "paypal" },
      Buttons: (options: ButtonsOptions) => ({
        render: async (container: HTMLElement) => {
          document.documentElement.dataset.paypalButtonColor = options.style?.color ?? "";
          const sources = options.fundingSource === "paypal" ? ["paypal"] : ["paypal", "paylater", "card"];
          for (const source of sources) {
            const checkout = document.createElement("button");
            checkout.type = "button";
            checkout.textContent = source === "paypal" ? "Donate with PayPal" : source === "paylater" ? "Pay Later" : "Debit or Credit Card";
            checkout.addEventListener("click", async () => {
              try {
                const orderID = await options.createOrder();
                await options.onApprove({ orderID });
              } catch {
                options.onError();
              }
            });
            container.append(checkout);
          }
          const cancel = document.createElement("button");
          cancel.type = "button";
          cancel.textContent = "Cancel PayPal checkout";
          cancel.addEventListener("click", options.onCancel);
          container.append(cancel);
        },
      }),
    };
    (window as Window & { paypal?: typeof paypal }).paypal = paypal;
  });

  await page.route("**/api/donations/config", async (route) => {
    if (setup.configHandler) return setup.configHandler(route);
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ clientId: "sandbox-public-client-id", currency: "USD", minimumDonation: 1, maximumDonation: 5000 }),
    });
  });
  await page.route("**/api/donations/leaderboard", (route) =>
    route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ entries: [], totalDonations: 0, totalAmount: 0 }) }),
  );
  await page.route("**/api/donations/orders", async (route) => {
    const body = route.request().postDataJSON() as Record<string, unknown>;
    const response = setup.orderResponse?.(body) ?? { body: { orderId: "ORDER-TEST" } };
    await fulfill(route, response);
  });
  await page.route("**/api/donations/capture", async (route) => {
    const body = route.request().postDataJSON() as Record<string, unknown>;
    const response = setup.captureResponse?.(body) ?? { body: { status: "COMPLETED" } };
    await fulfill(route, response);
  });

  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Support this work" })).toBeVisible();
}

async function fulfill(route: Route, response: JsonResponse) {
  await route.fulfill({
    status: response.status ?? 200,
    contentType: "application/json",
    body: JSON.stringify(response.body),
  });
}

test("shows checkout loading state until PayPal configuration loads", async ({ page }) => {
  let releaseConfig!: () => void;
  const configReady = new Promise<void>((resolve) => { releaseConfig = resolve; });
  await openDonationCheckout(page, {
    configHandler: async (route) => {
      await configReady;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ clientId: "sandbox-public-client-id", currency: "USD" }),
      });
    },
  });

  await expect(page.getByText("Loading secure checkout…")).toBeVisible();
  releaseConfig();
  await expect(page.getByRole("button", { name: "Donate with PayPal" })).toBeVisible();
});

test("provides clear preset amounts and keeps optional donor details private by default", async ({ page }) => {
  await openDonationCheckout(page);

  await expect(page.getByRole("button", { name: "USD 10.00" })).toHaveAttribute("aria-pressed", "true");
  await page.getByRole("button", { name: "USD 25.00" }).click();
  await expect(page.getByLabel("Or enter a custom amount")).toHaveValue("25");
  await expect(page.getByRole("button", { name: "USD 25.00" })).toHaveAttribute("aria-pressed", "true");

  await expect(page.getByText("Add optional details")).toBeVisible();
  await expect(page.getByLabel("Show my display name, amount, and message on the public supporter list. My email is never shown.")).not.toBeVisible();
  await page.getByText("Add optional details").click();
  await expect(page.getByRole("textbox", { name: "Display name" })).toBeVisible();
  await expect(page.getByLabel("Show my display name, amount, and message on the public supporter list. My email is never shown.")).not.toBeChecked();
});

test("renders one PayPal checkout button without separate Pay Later or card buttons", async ({ page }) => {
  await openDonationCheckout(page);

  const paymentButtons = page.locator("#paypal-donation-buttons button").filter({ hasText: "Donate with PayPal" });
  await expect(paymentButtons).toHaveCount(1);
  await expect(paymentButtons).toHaveText("Donate with PayPal");
  await expect(page.locator("html")).toHaveAttribute("data-paypal-button-color", "blue");
  await expect(page.locator("#paypal-donation-buttons").getByText("Pay Later", { exact: true })).toHaveCount(0);
  await expect(page.locator("#paypal-donation-buttons").getByText("Debit or Credit Card", { exact: true })).toHaveCount(0);
});

test("allows retry when donation configuration is temporarily unavailable", async ({ page }) => {
  let configRequests = 0;
  let allowConfigSuccess = false;
  await openDonationCheckout(page, {
    configHandler: async (route) => {
      configRequests += 1;
      if (!allowConfigSuccess) {
        await route.fulfill({ status: 503, contentType: "application/json", body: JSON.stringify({ message: "Unavailable" }) });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ clientId: "sandbox-public-client-id", currency: "USD", minimumDonation: 1, maximumDonation: 5000 }),
      });
    },
  });

  await expect(page.getByText("Checkout is temporarily unavailable.", { exact: true })).toBeVisible();
  allowConfigSuccess = true;
  await page.getByRole("button", { name: "Try again" }).click();
  await expect(page.getByRole("button", { name: "Donate with PayPal" })).toBeVisible();
  expect(configRequests).toBeGreaterThanOrEqual(2);
});

test("rejects amounts outside supported precision before creating an order", async ({ page }) => {
  let orderRequests = 0;
  await openDonationCheckout(page, {
    orderResponse: () => {
      orderRequests += 1;
      return { body: { orderId: "ORDER-TEST" } };
    },
  });

  await page.getByLabel("Or enter a custom amount").fill("10.001");
  await page.getByRole("button", { name: "Donate with PayPal" }).click();

  await expect(page.getByText("Enter an amount from 1 to 5000 USD, with up to two decimal places.")).toBeVisible();
  await expect(page.getByLabel("Or enter a custom amount")).toHaveAttribute("aria-invalid", "true");
  expect(orderRequests).toBe(0);
});

test("approves checkout and reports only backend-confirmed capture", async ({ page }) => {
  let captureOrderId: unknown;
  await openDonationCheckout(page, {
    captureResponse: (body) => {
      captureOrderId = body.orderId;
      return { body: { status: "COMPLETED" } };
    },
  });

  await page.getByRole("button", { name: "Donate with PayPal" }).click();

  await expect(page.getByText("Thank you for your support. Your payment was confirmed.")).toBeVisible();
  expect(captureOrderId).toBe("ORDER-TEST");
});

test("cancellation does not create or capture a donation", async ({ page }) => {
  let orderRequests = 0;
  let captureRequests = 0;
  await openDonationCheckout(page, {
    orderResponse: () => {
      orderRequests += 1;
      return { body: { orderId: "ORDER-TEST" } };
    },
    captureResponse: () => {
      captureRequests += 1;
      return { body: { status: "COMPLETED" } };
    },
  });

  await page.getByRole("button", { name: "Cancel PayPal checkout" }).click();

  await expect(page.getByText("Checkout was cancelled. No donation was recorded.")).toBeVisible();
  expect(orderRequests).toBe(0);
  expect(captureRequests).toBe(0);
});

test("capture failure can be retried with the same checkout attempt key", async ({ page }) => {
  const attemptKeys: unknown[] = [];
  let captureRequests = 0;
  await openDonationCheckout(page, {
    orderResponse: (body) => {
      attemptKeys.push(body.idempotencyKey);
      return { body: { orderId: "ORDER-TEST" } };
    },
    captureResponse: () => {
      captureRequests += 1;
      return captureRequests === 1
        ? { status: 503, body: { message: "PayPal has not confirmed this donation." } }
        : { body: { status: "COMPLETED" } };
    },
  });

  await page.getByRole("button", { name: "Donate with PayPal" }).click();
  await expect(page.getByText("PayPal has not confirmed this donation.")).toBeVisible();
  await page.getByRole("button", { name: "Donate with PayPal" }).click();

  await expect(page.getByText("Thank you for your support. Your payment was confirmed.")).toBeVisible();
  expect(attemptKeys).toHaveLength(2);
  expect(attemptKeys[0]).toBeTruthy();
  expect(attemptKeys[1]).toBe(attemptKeys[0]);
});

test("checkout refresh recovers the same persisted attempt key", async ({ page }) => {
  const attemptKeys: unknown[] = [];
  let captureRequests = 0;
  const setup: CheckoutSetup = {
    orderResponse: (body) => {
      attemptKeys.push(body.idempotencyKey);
      return { body: { orderId: "ORDER-TEST" } };
    },
    captureResponse: () => {
      captureRequests += 1;
      return captureRequests === 1
        ? { status: 503, body: { message: "Capture interrupted." } }
        : { body: { status: "COMPLETED" } };
    },
  };
  await openDonationCheckout(page, setup);
  await page.getByRole("button", { name: "Donate with PayPal" }).click();
  await expect(page.getByText("Capture interrupted.")).toBeVisible();

  await page.reload();
  await page.getByRole("button", { name: "Donate with PayPal" }).click();

  await expect(page.getByText("Thank you for your support. Your payment was confirmed.")).toBeVisible();
  expect(attemptKeys).toHaveLength(2);
  expect(attemptKeys[1]).toBe(attemptKeys[0]);
});

test("starting a new intentional donation rotates the checkout attempt key", async ({ page }) => {
  const attemptKeys: unknown[] = [];
  await openDonationCheckout(page, {
    orderResponse: (body) => {
      attemptKeys.push(body.idempotencyKey);
      return { body: { orderId: "ORDER-TEST" } };
    },
    captureResponse: () => ({ status: 503, body: { message: "Retry checkout." } }),
  });

  await page.getByRole("button", { name: "Donate with PayPal" }).click();
  await expect(page.getByRole("button", { name: "Start a new donation attempt" })).toBeVisible();
  await page.getByRole("button", { name: "Start a new donation attempt" }).click();
  await page.getByRole("button", { name: "Donate with PayPal" }).click();

  expect(attemptKeys).toHaveLength(2);
  expect(attemptKeys[1]).not.toBe(attemptKeys[0]);
});
