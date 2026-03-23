import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "./providers";
import Navigation from "@/components/Navigation";
import Footer from "@/components/Footer";
import Assistant from "@/components/sections/Assistant";
import VisitorTracker from "@/components/VisitorTracker";

const siteUrl =
  process.env.NEXT_PUBLIC_SITE_URL ?? "https://kaitaing.netlify.app";
const siteName = "Hang Kheang Taing | Software Engineer";
const description =
  "Software Engineer portfolio of Hang Kheang Taing: C#, .NET Core, ASP.NET Core, React, cloud-native architecture, and production-grade systems.";

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: siteName,
    template: "%s | Hang Kheang Taing",
  },
  description,
  keywords: [
    "Hang Kheang Taing",
    "Software Engineer",
    "C# developer",
    ".NET Core",
    "ASP.NET Core",
    "React",
    "Portfolio",
  ],
  authors: [{ name: "Hang Kheang Taing", url: siteUrl }],
  creator: "Hang Kheang Taing",
  publisher: "Hang Kheang Taing",
  alternates: {
    canonical: "/",
  },
  openGraph: {
    type: "website",
    url: siteUrl,
    siteName,
    title: siteName,
    description,
    images: [
      {
        url: "/avatar.png",
        width: 512,
        height: 512,
        alt: "Hang Kheang Taing",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: siteName,
    description,
    images: ["/avatar.png"],
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
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
        className="transition-colors duration-300 bg-light-background dark:bg-dark-background text-light-text-primary dark:text-dark-text-primary"
      >
        <Providers>
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
