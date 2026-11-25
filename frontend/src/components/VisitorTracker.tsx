"use client";

import { useEffect } from "react";
import { buildVisitorPayload, trackVisit } from "@/lib/visitor-tracking";

export default function VisitorTracker() {
  useEffect(() => {
    const payload = buildVisitorPayload();
    void trackVisit(payload);
  }, []);

  return null;
}
