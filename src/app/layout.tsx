import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";
import Navigation from "@/components/Navigation";
import Footer from "@/components/Footer";
import Assistant from "@/components/sections/Assistant";
import VisitorTracker from "@/components/VisitorTracker";
import { buildPageMetadata, siteConfig } from "@/lib/seo";

const inter = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-sans",
});

export const metadata: Metadata = {
  ...buildPageMetadata({
    title: siteConfig.fullTitle,
    description: siteConfig.description,
    path: "/",
  }),
  title: {
    default: siteConfig.fullTitle,
    template: "%s | Hang Kheang Taing",
  },
  verification: {
    google: process.env.GOOGLE_SITE_VERIFICATION,
  },
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
        className={`${inter.variable} font-sans transition-colors duration-300 bg-light-background dark:bg-dark-background text-light-text-primary dark:text-dark-text-primary`}
      >
        <Providers>
          <a
            href="#main-content"
            className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-[100] rounded-md bg-light-primary px-4 py-2 text-white"
          >
            Skip to main content
          </a>
          <VisitorTracker />
          <Navigation />
          {children}
          <Footer />
          <Assistant />
        </Providers>
      </body>
    </html>
  );
}
