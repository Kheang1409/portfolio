"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import StatusPage from "@/components/StatusPage";

const HEALTH_CHECK_INTERVAL_MS = 30_000;

export default function ServiceAvailability({
  children,
}: {
  children: React.ReactNode;
}) {
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);
  const checkInProgress = useRef(false);

  const checkAvailability = useCallback(async () => {
    if (checkInProgress.current) return;
    checkInProgress.current = true;

    try {
      const response = await fetch("/api/health/live", { cache: "no-store" });
      setIsAvailable(response.ok);
    } catch {
      setIsAvailable(false);
    } finally {
      checkInProgress.current = false;
    }
  }, []);

  useEffect(() => {
    void checkAvailability();
    const intervalId = window.setInterval(
      () => void checkAvailability(),
      HEALTH_CHECK_INTERVAL_MS,
    );

    return () => window.clearInterval(intervalId);
  }, [checkAvailability]);

  if (isAvailable === false) {
    return (
      <StatusPage
        code="503"
        title="Service temporarily unavailable."
        description="The portfolio server is not responding right now. Please try again in a moment; this page will check again automatically."
        image="/images/error.jfif"
        imageAlt="Illustration of Kai beside a critical error warning."
        onRetry={() => void checkAvailability()}
        primaryLabel="Go to home"
      />
    );
  }

  return children;
}
