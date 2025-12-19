import type { AssistantRequest } from "./types";

export type AssistantMsg = { sender: "user" | "bot"; text: string };

export async function askAssistant(message: string): Promise<string> {
  const payload: AssistantRequest = { message };
  const envBackend = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  const fallbackDefault = "http://localhost:5000";

  const backend =
    envBackend && envBackend.trim() !== "" ? envBackend : fallbackDefault;
  const url = backend
    ? `${backend.replace(/\/$/, "")}/api/assistants/ask`
    : "/api/assistants/ask";

  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => null);
    throw new Error(text ?? `Request failed with status ${res.status}`);
  }

  const text = await res.text();
  return text;
}
