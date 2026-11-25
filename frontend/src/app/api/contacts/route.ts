import { forwardBackendRequest } from "@/lib/server/backend-proxy";

export function POST(request: Request) {
  return forwardBackendRequest(request, "/api/contacts", "Contact service");
}
