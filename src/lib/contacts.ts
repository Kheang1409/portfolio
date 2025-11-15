import { ContactRequest, ContactResponse } from "./types";

export async function postContact(
  payload: ContactRequest
): Promise<ContactResponse> {
  const envBackend = process.env.BACKEND_API_URL;
  const fallbackDefault = "http://localhost:5000";

  const backend =
    envBackend && envBackend.trim() !== "" ? envBackend : fallbackDefault;

  const url = backend
    ? `${backend.replace(/\/$/, "")}/api/contacts`
    : "/api/contacts";

  try {
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
  } catch (err: unknown) {
    if (err instanceof Error) {
      throw new Error(`Network error: ${err.message}`);
    }
    throw new Error("Network error: failed to send contact message.");
  }
}
