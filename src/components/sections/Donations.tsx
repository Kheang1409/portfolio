"use client";

import { useCallback, useEffect, useRef, useState } from "react";

type Donor = { name: string; amount: number; remark: string; paidAtUtc: string; rank: number };
type PayPalButtonsApi = {
  Buttons: (options: {
    createOrder: () => Promise<string>;
    onApprove: (data: { orderID: string }) => Promise<void>;
    onError: () => void;
    style?: { layout?: string; color?: string; shape?: string; label?: string };
  }) => { render: (container: HTMLElement) => Promise<void>; close?: () => void };
};
declare global { interface Window { paypal?: PayPalButtonsApi } }

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
  const [sdkEnvironment, setSdkEnvironment] = useState("sandbox");
  const [minimum, setMinimum] = useState(1);
  const [maximum, setMaximum] = useState(5000);
  const [receiptAvailable, setReceiptAvailable] = useState(false);
  const [sdkReady, setSdkReady] = useState(false);
  const donationDetails = useRef({ amount, name, email, remark, listed });
  donationDetails.current = { amount, name, email, remark, listed };

  const refresh = useCallback(async () => {
    try {
      const response = await fetch("/api/donations/leaderboard", { cache: "no-store" });
      if (!response.ok) return;
      const data = await response.json();
      setLeaders(data.entries ?? []);
      setTotal({ count: data.totalDonations ?? 0, amount: data.totalAmount ?? 0 });
    } catch { /* public list remains unavailable until backend connection recovers */ }
  }, []);

  useEffect(() => {
    void refresh();
    void fetch("/api/donations/config").then((r) => r.ok ? r.json() : null)
      .then((data) => { if (data?.clientId) { setClientId(data.clientId); setSdkCurrency(data.currency || "USD"); setSdkEnvironment(data.environment || "sandbox"); setMinimum(data.minimumDonation ?? 1); setMaximum(data.maximumDonation ?? 5000); setReceiptAvailable(Boolean(data.receiptAvailable)); } })
      .catch(() => setStatus("Donations are not configured yet."));
  }, [refresh]);

  useEffect(() => {
    if (!clientId) return;
    if (window.paypal) { setSdkReady(true); return; }
    const script = document.createElement("script");
    script.src = `https://www.paypal.com/sdk/js?client-id=${encodeURIComponent(clientId)}&currency=${encodeURIComponent(sdkCurrency)}&intent=capture`;
    script.async = true;
    script.onload = () => setSdkReady(true);
    script.onerror = () => setStatus("PayPal checkout could not load. Please try again later.");
    document.head.append(script);
    return () => { script.remove(); };
  }, [clientId, sdkCurrency, sdkEnvironment]);

  useEffect(() => {
    const root = document.getElementById("paypal-donation-buttons");
    if (!sdkReady || !window.paypal || !root) return;
    root.replaceChildren();
    const buttons = window.paypal.Buttons({
      style: { layout: "vertical", color: "blue", shape: "rect", label: "paypal" },
      createOrder: async () => {
        setStatus("");
        const current = donationDetails.current;
        const idempotencyKey = crypto.randomUUID();
        const response = await fetch("/api/donations/orders", {
          method: "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ amount: Number(current.amount), name: current.name, email: current.email, remark: current.remark, showOnLeaderboard: current.listed, idempotencyKey }),
        });
        const data = await response.json();
        if (!response.ok) throw new Error(data.message ?? "Could not start donation.");
        return data.orderId;
      },
      onApprove: async ({ orderID }) => {
        const response = await fetch("/api/donations/capture", {
          method: "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ orderId: orderID }),
        });
        if (!response.ok) throw new Error("PayPal could not confirm the donation.");
        setStatus("Thank you for your support. Your payment was confirmed.");
        setName(""); setEmail(""); setRemark(""); setListed(false);
        await refresh();
      },
      onError: () => setStatus("Donation was not completed. You have not been charged unless PayPal confirms the payment."),
    });
    void buttons.render(root).catch(() => setStatus("PayPal checkout could not start."));
    return () => { void buttons.close?.(); root.replaceChildren(); };
  }, [sdkReady, refresh]);

  return <section id="donate" className="system-section py-3xl" aria-labelledby="donate-heading">
    <div className="max-w-container mx-auto px-sm md:px-lg grid lg:grid-cols-2 gap-3xl">
      <div className="metal-panel p-lg rounded-lg">
        <p className="system-label">COMMUNITY // SUPPORT</p>
        <h2 id="donate-heading" className="text-h2 font-bold mb-md">Support this work</h2>
        <p className="text-light-text-secondary dark:text-dark-text-secondary mb-lg">Donations are optional. Pay securely through PayPal. Your card details are entered on PayPal and never reach this site.</p>
        <label className="block mb-xs" htmlFor="donation-amount">Donation amount ({sdkCurrency})</label>
        <input id="donation-amount" type="number" min={minimum} max={maximum} step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} className="w-full p-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border mb-md" />
        <label className="block mb-xs" htmlFor="donor-name">Display name</label>
        <input id="donor-name" maxLength={80} value={name} onChange={(e) => setName(e.target.value)} placeholder="Anonymous" className="w-full p-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border mb-md" />
        {receiptAvailable && <><label className="block mb-xs" htmlFor="donor-email">Email for a thank-you receipt (optional)</label>
        <input id="donor-email" type="email" maxLength={254} value={email} onChange={(e) => setEmail(e.target.value)} className="w-full p-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border mb-xs" />
        <p className="text-sm text-light-text-secondary dark:text-dark-text-secondary mb-md">Used only for the receipt attempt. Never shown publicly.</p></>}
        <label className="block mb-xs" htmlFor="donor-remark">Remark (optional)</label>
        <textarea id="donor-remark" maxLength={240} rows={3} value={remark} onChange={(e) => setRemark(e.target.value)} placeholder="A short message" className="w-full p-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border mb-md" />
        <label className="flex items-start gap-sm mb-lg"><input type="checkbox" checked={listed} onChange={(e) => setListed(e.target.checked)} /><span>Show my name, donation amount, and remark on the public supporter list. My email will never be shown.</span></label>
        {clientId ? <div id="paypal-donation-buttons" aria-label="PayPal and eligible card payment options" /> : <p role="status">{status || "Donation checkout is not configured."}</p>}
        {status && <p role="status" aria-live="polite" className="mt-md">{status}</p>}
      </div>
      <div className="metal-panel p-lg rounded-lg">
        <p className="system-label">COMMUNITY // LEADERBOARD</p>
        <h2 className="text-h2 font-bold mb-md">Supporters</h2>
        <p className="mb-lg">{total.count} donations · {sdkCurrency} {total.amount.toFixed(2)} raised</p>
        {leaders.length ? <ol className="space-y-md">{leaders.map((donor) => <li key={`${donor.rank}-${donor.name}-${donor.paidAtUtc}`} className="border-b border-light-border dark:border-dark-border pb-md"><div className="flex justify-between gap-md"><strong>#{donor.rank} {donor.name}</strong><strong>{donor.amount.toFixed(2)} {sdkCurrency}</strong></div><time dateTime={donor.paidAtUtc} className="text-sm text-light-text-secondary dark:text-dark-text-secondary">{new Date(donor.paidAtUtc).toLocaleDateString()}</time>{donor.remark && <p className="text-light-text-secondary dark:text-dark-text-secondary mt-xs">{donor.remark}</p>}</li>)}</ol> : <p>No public supporters yet.</p>}
      </div>
    </div>
  </section>;
}
