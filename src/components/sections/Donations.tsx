"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  clearDonationAttemptId,
  createDonationAttemptId,
  getOrCreateDonationAttemptId,
} from "@/lib/donation-attempt";

type Donor = {
  name: string;
  amount: number;
  remark: string;
  paidAtUtc: string;
  rank: number;
};
type PayPalButtonsApi = {
  FUNDING: { PAYPAL: string };
  Buttons: (options: {
    fundingSource: string;
    createOrder: () => Promise<string>;
    onApprove: (data: { orderID: string }) => Promise<void>;
    onError: () => void;
    onCancel: () => void;
    style?: { layout?: string; color?: string; shape?: string; label?: string };
  }) => {
    render: (container: HTMLElement) => Promise<void>;
    close?: () => void;
  };
};
declare global {
  interface Window {
    paypal?: PayPalButtonsApi;
  }
}

export default function Donations() {
  const [amount, setAmount] = useState("10");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [remark, setRemark] = useState("");
  const [listed, setListed] = useState(false);
  const [status, setStatus] = useState("");
  const [leaders, setLeaders] = useState<Donor[]>([]);
  const [total, setTotal] = useState({ count: 0, amount: 0 });
  const [clientId, setClientId] = useState("");
  const [sdkCurrency, setSdkCurrency] = useState("USD");
  const [minimum, setMinimum] = useState(1);
  const [maximum, setMaximum] = useState(5000);
  const [receiptAvailable, setReceiptAvailable] = useState(false);
  const [sdkReady, setSdkReady] = useState(false);
  const [attemptStarted, setAttemptStarted] = useState(false);
  const [configRetry, setConfigRetry] = useState(0);
  const [configFailed, setConfigFailed] = useState(false);
  const [amountError, setAmountError] = useState("");
  const [leaderboardState, setLeaderboardState] = useState<
    "loading" | "ready" | "error"
  >("loading");
  const donationAttemptId = useRef<string | null>(null);
  const donationDetails = useRef({ amount, name, email, remark, listed });
  donationDetails.current = { amount, name, email, remark, listed };

  const refresh = useCallback(async () => {
    try {
      const response = await fetch("/api/donations/leaderboard", {
        cache: "no-store",
      });
      if (!response.ok)
        throw new Error("Supporter list is temporarily unavailable.");
      const data = await response.json();
      setLeaders(data.entries ?? []);
      setTotal({
        count: data.totalDonations ?? 0,
        amount: data.totalAmount ?? 0,
      });
      setLeaderboardState("ready");
    } catch {
      setLeaderboardState("error");
    }
  }, []);

  useEffect(() => {
    void refresh();
    let active = true;
    setConfigFailed(false);
    void fetch("/api/donations/config", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok)
          throw new Error(
            "Donation checkout is temporarily unavailable. Please try again later.",
          );
        return response.json();
      })
      .then((data) => {
        if (!active) return;
        if (!data?.clientId)
          throw new Error(
            "Donation checkout is temporarily unavailable. Please try again later.",
          );
        setClientId(data.clientId);
        setSdkCurrency(data.currency || "USD");
        setMinimum(data.minimumDonation ?? 1);
        setMaximum(data.maximumDonation ?? 5000);
        setReceiptAvailable(Boolean(data.receiptAvailable));
      })
      .catch((error: unknown) => {
        if (active) {
          setConfigFailed(true);
          setStatus(
            error instanceof Error
              ? error.message
              : "Donation checkout is temporarily unavailable. Please try again later.",
          );
        }
      });
    return () => {
      active = false;
    };
  }, [refresh, configRetry]);

  useEffect(() => {
    if (!clientId) return;
    if (window.paypal) {
      setSdkReady(true);
      return;
    }
    const script = document.createElement("script");
    script.src = `https://www.paypal.com/sdk/js?client-id=${encodeURIComponent(clientId)}&currency=${encodeURIComponent(sdkCurrency)}&intent=capture`;
    script.async = true;
    script.onload = () => setSdkReady(true);
    script.onerror = () =>
      setStatus("PayPal checkout could not load. Please try again later.");
    document.head.append(script);
    return () => {
      script.remove();
    };
  }, [clientId, sdkCurrency]);

  useEffect(() => {
    const root = document.getElementById("paypal-donation-buttons");
    if (!sdkReady || !window.paypal || !root) return;
    root.replaceChildren();
    const buttons = window.paypal.Buttons({
      fundingSource: window.paypal.FUNDING.PAYPAL,
      style: {
        layout: "vertical",
        color: "blue",
        shape: "rect",
        label: "paypal",
      },
      createOrder: async () => {
        setStatus("");
        const current = donationDetails.current;
        const parsedAmount = Number(current.amount);
        if (
          !Number.isFinite(parsedAmount) ||
          parsedAmount < minimum ||
          parsedAmount > maximum ||
          Math.round(parsedAmount * 100) !== parsedAmount * 100
        ) {
          const message = `Enter an amount from ${minimum} to ${maximum} ${sdkCurrency}, with up to two decimal places.`;
          setAmountError(message);
          throw new Error("Invalid donation amount.");
        }
        setAmountError("");
        let idempotencyKey: string;
        try {
          idempotencyKey =
            donationAttemptId.current ?? getOrCreateDonationAttemptId();
          donationAttemptId.current = idempotencyKey;
        } catch {
          const message =
            "Checkout could not save this donation attempt in this browser. Enable session storage and try again.";
          setStatus(message);
          throw new Error(message);
        }
        setAttemptStarted(true);
        const response = await fetch("/api/donations/orders", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            amount: parsedAmount,
            name: current.name,
            email: current.email,
            remark: current.remark,
            showOnLeaderboard: current.listed,
            idempotencyKey,
          }),
        });
        const data = await response.json().catch(() => ({}));
        if (!response.ok || typeof data.orderId !== "string" || !data.orderId) {
          const message =
            data.message ?? "Could not start donation. Please try again.";
          setStatus(message);
          throw new Error(message);
        }
        return data.orderId;
      },
      onApprove: async ({ orderID }) => {
        const response = await fetch("/api/donations/capture", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ orderId: orderID }),
        });
        const data = await response.json().catch(() => ({}));
        if (!response.ok || data.status !== "COMPLETED") {
          setStatus(
            data.message ??
              "PayPal has not confirmed this donation. Check your PayPal account before trying again.",
          );
          throw new Error("PayPal did not confirm the donation.");
        }
        setStatus("Thank you for your support. Your payment was confirmed.");
        setName("");
        setEmail("");
        setRemark("");
        setListed(false);
        try {
          clearDonationAttemptId();
          donationAttemptId.current = null;
          setAttemptStarted(false);
        } catch {
          setStatus(
            "Your payment was confirmed, but this browser could not reset the checkout attempt. Start a new donation attempt before donating again.",
          );
        }
        await refresh();
      },
      onCancel: () =>
        setStatus("Checkout was cancelled. No donation was recorded."),
      onError: () =>
        setStatus(
          (current) =>
            current ||
            "Checkout could not be completed. Check your PayPal account before trying again.",
        ),
    });
    void buttons
      .render(root)
      .catch(() => setStatus("PayPal checkout could not start."));
    return () => {
      void buttons.close?.();
      root.replaceChildren();
    };
  }, [sdkReady, refresh, minimum, maximum, sdkCurrency]);

  const presets = [
    ...new Set(
      [minimum, 10, 25, 50].filter(
        (value) => value >= minimum && value <= maximum,
      ),
    ),
  ];
  const inputClass =
    "w-full rounded-lg border border-light-border dark:border-dark-border bg-light-background dark:bg-dark-background px-4 py-3 text-light-text-primary dark:text-dark-text-primary placeholder:text-light-text-secondary dark:placeholder:text-dark-text-secondary focus:outline-none focus:ring-2 focus:ring-light-primary dark:focus:ring-dark-primary";
  const cardClass =
    "rounded-2xl border border-light-border dark:border-dark-border bg-light-surface dark:bg-dark-surface p-5 shadow-lg md:p-7";

  return (
    <section
      id="donate"
      className="system-section py-3xl"
      aria-labelledby="donate-heading"
    >
      <div className="relative z-[1] mx-auto max-w-container px-sm md:px-lg">
        <header className="mb-xl max-w-3xl">
          <p className="system-label mb-sm">COMMUNITY // SUPPORT</p>
          <h2
            id="donate-heading"
            className="text-h2 font-bold text-light-text-primary dark:text-dark-text-primary"
          >
            Support this work
          </h2>
          <p className="mt-sm text-body text-light-text-secondary dark:text-dark-text-secondary">
            Contributions are optional and help support ongoing portfolio
            projects. Choose an amount; you can review and approve your payment
            securely with PayPal.
          </p>
        </header>

        <div className="grid items-start gap-lg lg:grid-cols-[minmax(0,1.15fr)_minmax(300px,0.85fr)]">
          <div className={cardClass}>
            <div className="mb-lg flex items-start justify-between gap-md">
              <div>
                <p className="text-small font-semibold text-light-text-primary dark:text-dark-text-primary">
                  Choose your contribution
                </p>
                <p className="mt-1 text-sm text-light-text-secondary dark:text-dark-text-secondary">
                  No account on this site is required.
                </p>
              </div>
              <span className="rounded-full border border-light-border dark:border-dark-border px-3 py-1 text-xs font-medium text-light-text-secondary dark:text-dark-text-secondary">
                Optional
              </span>
            </div>

            <fieldset>
              <legend className="mb-sm text-sm font-medium text-light-text-primary dark:text-dark-text-primary">
                Select an amount ({sdkCurrency})
              </legend>
              <div className="grid grid-cols-2 gap-sm sm:grid-cols-4">
                {presets.map((preset) => (
                  <button
                    key={preset}
                    type="button"
                    aria-pressed={Number(amount) === preset}
                    onClick={() => {
                      setAmount(String(preset));
                      setAmountError("");
                      setStatus("");
                    }}
                    className={`min-h-11 rounded-lg border px-sm py-2 font-semibold transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-light-primary dark:focus-visible:outline-dark-primary ${Number(amount) === preset ? "border-light-primary bg-light-primary/10 text-light-text-primary dark:border-dark-primary dark:bg-dark-primary/10 dark:text-dark-text-primary" : "border-light-border dark:border-dark-border text-light-text-secondary hover:border-light-primary dark:hover:border-dark-primary"}`}
                  >
                    {sdkCurrency} {preset.toFixed(2)}
                  </button>
                ))}
              </div>
            </fieldset>

            <div className="mt-md">
              <label
                className="mb-xs block text-sm font-medium text-light-text-primary dark:text-dark-text-primary"
                htmlFor="donation-amount"
              >
                Or enter a custom amount
              </label>
              <div className="relative">
                <input
                  id="donation-amount"
                  type="number"
                  min={minimum}
                  max={maximum}
                  step="0.01"
                  inputMode="decimal"
                  value={amount}
                  aria-invalid={Boolean(amountError)}
                  aria-describedby={
                    amountError
                      ? "donation-amount-hint donation-amount-error"
                      : "donation-amount-hint"
                  }
                  onChange={(event) => {
                    setAmount(event.target.value);
                    setAmountError("");
                  }}
                  className={`${inputClass} pr-24`}
                />
                <span className="pointer-events-none absolute inset-y-0 right-4 flex items-center text-sm text-light-text-secondary dark:text-dark-text-secondary">
                  {sdkCurrency}
                </span>
              </div>
              <p
                id="donation-amount-hint"
                className="mt-xs text-xs text-light-text-secondary dark:text-dark-text-secondary"
              >
                From {minimum} to {maximum} {sdkCurrency}; up to two decimal
                places.
              </p>
              {amountError && (
                <p
                  id="donation-amount-error"
                  className="mt-xs text-sm text-red-600 dark:text-red-400"
                >
                  {amountError}
                </p>
              )}
            </div>

            <details className="mt-lg rounded-lg border border-light-border dark:border-dark-border">
              <summary className="cursor-pointer px-4 py-3 text-sm font-medium text-light-text-primary dark:text-dark-text-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-light-primary dark:focus-visible:outline-dark-primary">
                Add optional details
              </summary>
              <div className="space-y-md border-t border-light-border px-4 py-4 dark:border-dark-border">
                <div>
                  <label
                    className="mb-xs block text-sm font-medium text-light-text-primary dark:text-dark-text-primary"
                    htmlFor="donor-name"
                  >
                    Display name
                  </label>
                  <input
                    id="donor-name"
                    maxLength={80}
                    value={name}
                    onChange={(event) => setName(event.target.value)}
                    placeholder="Anonymous"
                    className={inputClass}
                  />
                </div>
                {receiptAvailable && (
                  <div>
                    <label
                      className="mb-xs block text-sm font-medium text-light-text-primary dark:text-dark-text-primary"
                      htmlFor="donor-email"
                    >
                      Email for a receipt (optional)
                    </label>
                    <input
                      id="donor-email"
                      type="email"
                      maxLength={254}
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                      autoComplete="email"
                      className={inputClass}
                    />
                    <p className="mt-xs text-xs text-light-text-secondary dark:text-dark-text-secondary">
                      Used only for a receipt attempt. Never shown on the public
                      list.
                    </p>
                  </div>
                )}
                <div>
                  <label
                    className="mb-xs block text-sm font-medium text-light-text-primary dark:text-dark-text-primary"
                    htmlFor="donor-remark"
                  >
                    Message (optional)
                  </label>
                  <textarea
                    id="donor-remark"
                    maxLength={240}
                    rows={3}
                    value={remark}
                    onChange={(event) => setRemark(event.target.value)}
                    placeholder="A short message"
                    className={`${inputClass} resize-y`}
                  />
                </div>
                <label className="flex cursor-pointer items-start gap-sm text-sm text-light-text-secondary dark:text-dark-text-secondary">
                  <input
                    type="checkbox"
                    checked={listed}
                    onChange={(event) => setListed(event.target.checked)}
                    className="mt-1 h-4 w-4 accent-light-primary dark:accent-dark-primary"
                  />
                  <span>
                    Show my display name, amount, and message on the public
                    supporter list. My email is never shown.
                  </span>
                </label>
              </div>
            </details>

            <div className="mt-md min-h-[58px]" aria-label="Checkout options">
              {clientId ? (
                <div
                  id="paypal-donation-buttons"
                  aria-label="PayPal and eligible card payment options"
                />
              ) : (
                <div className="flex min-h-[52px] items-center justify-between gap-md rounded-lg border border-light-border dark:border-dark-border px-4 py-3">
                  <p
                    role="status"
                    className="text-sm text-light-text-secondary dark:text-dark-text-secondary"
                  >
                    {configFailed
                      ? "Checkout is temporarily unavailable."
                      : "Loading secure checkout…"}
                  </p>
                  {configFailed && (
                    <button
                      type="button"
                      className="text-sm font-semibold text-light-primary underline dark:text-dark-primary"
                      onClick={() => {
                        setStatus("");
                        setConfigRetry((count) => count + 1);
                      }}
                    >
                      Try again
                    </button>
                  )}
                </div>
              )}
            </div>

            {status && !configFailed && (
              <p
                role="status"
                aria-live="polite"
                className="mt-sm text-sm text-light-text-secondary dark:text-dark-text-secondary"
              >
                {status}
              </p>
            )}
            {attemptStarted && (
              <button
                type="button"
                className="mt-sm text-sm font-medium text-light-primary underline dark:text-dark-primary"
                onClick={() => {
                  try {
                    donationAttemptId.current = createDonationAttemptId();
                    setAttemptStarted(false);
                    setStatus("A new donation attempt is ready.");
                  } catch {
                    setStatus(
                      "Could not save a new donation attempt. Enable session storage and try again.",
                    );
                  }
                }}
              >
                Start a new donation attempt
              </button>
            )}
          </div>

          <aside className={cardClass} aria-labelledby="supporters-heading">
            <p className="system-label mb-sm">COMMUNITY // SUPPORTERS</p>
            <h3
              id="supporters-heading"
              className="text-xl font-semibold text-light-text-primary dark:text-dark-text-primary"
            >
              A little goes a long way
            </h3>
            <p className="mt-sm text-sm leading-relaxed text-light-text-secondary dark:text-dark-text-secondary">
              Thank you to everyone who chooses to support this work. Public
              recognition is always optional.
            </p>
            <div className="my-lg grid grid-cols-2 gap-sm">
              <div className="rounded-lg border border-light-border dark:border-dark-border p-4">
                <p className="text-xs text-light-text-secondary dark:text-dark-text-secondary">
                  Supporters
                </p>
                <p className="mt-1 text-2xl font-semibold text-light-text-primary dark:text-dark-text-primary">
                  {total.count}
                </p>
              </div>
              <div className="rounded-lg border border-light-border dark:border-dark-border p-4">
                <p className="text-xs text-light-text-secondary dark:text-dark-text-secondary">
                  Total raised
                </p>
                <p className="mt-1 text-xl font-semibold text-light-text-primary dark:text-dark-text-primary">
                  {sdkCurrency} {total.amount.toFixed(2)}
                </p>
              </div>
            </div>
            <div className="border-t border-light-border pt-lg dark:border-dark-border">
              <h4 className="mb-md text-sm font-semibold text-light-text-primary dark:text-dark-text-primary">
                Public supporters
              </h4>
              {leaderboardState === "loading" && (
                <p
                  role="status"
                  className="text-sm text-light-text-secondary dark:text-dark-text-secondary"
                >
                  Loading supporters…
                </p>
              )}
              {leaderboardState === "error" && (
                <p
                  role="status"
                  className="text-sm text-light-text-secondary dark:text-dark-text-secondary"
                >
                  The supporter list is temporarily unavailable.
                </p>
              )}
              {leaderboardState === "ready" && leaders.length === 0 && (
                <p className="rounded-lg bg-light-background/60 p-4 text-sm text-light-text-secondary dark:bg-dark-background/60 dark:text-dark-text-secondary">
                  No public supporters yet. You can contribute privately.
                </p>
              )}
              {leaderboardState === "ready" && leaders.length > 0 && (
                <ol
                  className="max-h-96 space-y-sm overflow-y-auto"
                  aria-label="Public supporters"
                >
                  {leaders.map((donor) => (
                    <li
                      key={`${donor.rank}-${donor.name}-${donor.paidAtUtc}`}
                      className="rounded-lg border border-light-border dark:border-dark-border p-4"
                    >
                      <div className="flex items-start justify-between gap-sm">
                        <strong className="break-words text-sm text-light-text-primary dark:text-dark-text-primary">
                          #{donor.rank} {donor.name}
                        </strong>
                        <strong className="shrink-0 text-sm text-light-text-primary dark:text-dark-text-primary">
                          {donor.amount.toFixed(2)} {sdkCurrency}
                        </strong>
                      </div>
                      <time
                        dateTime={donor.paidAtUtc}
                        className="mt-1 block text-xs text-light-text-secondary dark:text-dark-text-secondary"
                      >
                        {new Date(donor.paidAtUtc).toLocaleDateString()}
                      </time>
                      {donor.remark && (
                        <p className="mt-xs break-words text-sm text-light-text-secondary dark:text-dark-text-secondary">
                          {donor.remark}
                        </p>
                      )}
                    </li>
                  ))}
                </ol>
              )}
            </div>
          </aside>
        </div>
      </div>
    </section>
  );
}
