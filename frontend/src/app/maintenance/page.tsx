import type { Metadata } from "next";
import StatusPage from "@/components/StatusPage";

export const metadata: Metadata = {
  title: "Under maintenance",
  description: "The portfolio is being updated. Please check back soon.",
  robots: { index: false, follow: false },
};

export default function MaintenancePage() {
  return (
    <StatusPage
      code="A SHORT PAUSE"
      title="A little work behind the scenes."
      description="The portfolio is being refreshed. Please check back soon, or head home to keep exploring."
      image="/images/under%20maintance.jfif"
      imageAlt="Illustration of Kai at a small maintenance work site."
      primaryLabel="Visit the portfolio"
    />
  );
}
