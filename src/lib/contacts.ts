import { requestJson } from "@/lib/api/client";
import type { ContactRequest, ContactResponse } from "@/lib/api/types";

export async function postContact(
  payload: ContactRequest,
): Promise<ContactResponse> {
  const { data } = await requestJson<ContactResponse>({
    path: "/api/contacts",
    body: payload,
    useAssistantProxy: false,
  });

  return data ?? { message: "Message sent successfully." };
}
