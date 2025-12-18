import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "./providers";
import Navigation from "@/components/Navigation";
import Footer from "@/components/Footer";
import Assistant from "@/components/sections/Assistant";

export const metadata: Metadata = {
  title: "Portfolio - Software Engineer",
  description:
    "Professional portfolio showcasing projects, skills, and experience",
  icons: {
    icon: "/favicon.ico",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body
        suppressHydrationWarning
        className="transition-colors duration-300 bg-light-background dark:bg-dark-background text-light-text-primary dark:text-dark-text-primary"
      >
        <Providers>
          <Navigation />
          {children}
          <Footer />
          <Assistant />
        </Providers>
      </body>
    </html>
  );
}
