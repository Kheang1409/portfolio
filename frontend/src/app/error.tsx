"use client";

import { useEffect } from "react";
import StatusPage from "@/components/StatusPage";

export default function Error({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  retry: () => void;
}) {
  useEffect(() => {
    console.error("A page failed to render.", error);
  }, [error]);

  return (
    <StatusPage
      code="500"
      title="Something went wrong."
      description="The page hit an unexpected problem. Your information has not been shared here; please try again."
      image="/images/error.jfif"
      imageAlt="Illustration of Kai beside a critical error warning."
      onRetry={retry}
      primaryLabel="Go to home"
    />
  );
}
