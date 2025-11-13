import type { NextApiRequest, NextApiResponse } from "next";

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse
) {
  if (req.method !== "POST") {
    res.setHeader("Allow", "POST");
    return res.status(405).end("Method Not Allowed");
  }

  const base = process.env.BACKEND_API_URL || "http://localhost:5000";
  const target = `${base.replace(/\/$/, "")}/api/assistants/ask`;

  try {
    const forwarded = await fetch(target, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(req.body ?? {}),
    });

    const text = await forwarded.text();
    try {
      const json = JSON.parse(text);
      return res.status(forwarded.status).json(json);
    } catch (e) {
      res.status(forwarded.status).setHeader("Content-Type", "text/plain");
      return res.status(forwarded.status).send(text);
    }
  } catch (err: any) {
    console.error("Proxy error forwarding assistant request:", err);
    return res.status(502).json({ message: `Proxy error: ${String(err)}` });
  }
}
