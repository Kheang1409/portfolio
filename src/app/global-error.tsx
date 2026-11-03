"use client";

import { useEffect } from "react";
import StatusPage from "@/components/StatusPage";
import "./globals.css";

export default function GlobalError({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  retry: () => void;
}) {
  useEffect(() => {
    console.error("A root application error occurred.", error);
  }, [error]);

  return (
    <html lang="en">
      <body>
        <title>Something went wrong | Kai</title>
        <StatusPage
          code="500"
          title="Something went wrong."
          description="The site hit an unexpected problem. Please try again or return to the home page."
          image="/images/error.jfif"
          imageAlt="Illustration of Kai beside a critical error warning."
          onRetry={retry}
          primaryLabel="Go to home"
        />
      </body>
    </html>
  );
}
