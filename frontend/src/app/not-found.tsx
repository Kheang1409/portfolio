import type { Metadata } from "next";
import StatusPage from "@/components/StatusPage";

export const metadata: Metadata = {
  title: "Page not found",
  description: "The page you are looking for could not be found.",
};

export default function NotFound() {
  return (
    <StatusPage
      code="404"
      title="This page wandered off."
      description="The link may be out of date, or the page may have moved. Let’s get you back on track."
      image="/images/404%20not%20found.jfif"
      imageAlt="Illustration of Kai beside a 404 not found sign."
      primaryLabel="Back to home"
    />
  );
}
