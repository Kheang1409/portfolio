import type { ContactRequest, ContactResponse } from "./types";

export async function postContact(
  payload: ContactRequest
): Promise<ContactResponse> {
  const envBackend = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  const fallbackDefault = "http://localhost:5000";

  const backend =
    envBackend && envBackend.trim() !== "" ? envBackend : fallbackDefault;
  const url = backend
    ? `${backend.replace(/\/$/, "")}/api/contacts`
    : "/api/contacts`";

  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => null);
    throw new Error(text ?? `Request failed with status ${res.status}`);
  }

  const data = (await res.json().catch(() => null)) as ContactResponse | null;
  return data ?? { message: "Message sent successfully." };
}
