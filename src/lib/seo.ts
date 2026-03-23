import type { Metadata } from "next";

const siteUrl =
  process.env.NEXT_PUBLIC_SITE_URL ?? "https://kaitaing.netlify.app";

export const siteConfig = {
  name: "Hang Kheang Taing",
  fullTitle: "Hang Kheang Taing | Software Engineer",
  description:
    "Software Engineer portfolio of Hang Kheang Taing: C#, .NET Core, ASP.NET Core, React, cloud-native architecture, and production-grade systems.",
  url: siteUrl,
  locale: "en_US",
  keywords: [
    "Hang Kheang Taing",
    "Software Engineer",
    "C# developer",
    ".NET Core",
    "ASP.NET Core",
    "React developer",
    "Portfolio",
    "Phnom Penh software engineer",
  ],
  social: {
    github: "https://github.com/Kheang1409",
    linkedin: "https://www.linkedin.com/in/hang-kheang-taing/",
  },
};

type PageMetadataInput = {
  title: string;
  description: string;
  path?: string;
  keywords?: string[];
  image?: string;
};

export function buildPageMetadata({
  title,
  description,
  path = "/",
  keywords = [],
  image = "/opengraph-image",
}: PageMetadataInput): Metadata {
  const mergedKeywords = Array.from(
    new Set([...siteConfig.keywords, ...keywords]),
  );

  return {
    metadataBase: new URL(siteConfig.url),
    title,
    description,
    keywords: mergedKeywords,
    authors: [{ name: siteConfig.name, url: siteConfig.url }],
    creator: siteConfig.name,
    publisher: siteConfig.name,
    alternates: {
      canonical: path,
    },
    openGraph: {
      type: "website",
      locale: siteConfig.locale,
      url: path,
      siteName: siteConfig.fullTitle,
      title,
      description,
      images: [
        {
          url: image,
          width: 1200,
          height: 630,
          alt: siteConfig.name,
        },
      ],
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: [image],
      creator: "@Kheang1409",
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
  };
}

export function buildPersonJsonLd() {
  return {
    "@context": "https://schema.org",
    "@type": "Person",
    name: siteConfig.name,
    url: siteConfig.url,
    jobTitle: "Software Engineer",
    description: siteConfig.description,
    image: `${siteConfig.url}/avatar.png`,
    sameAs: [siteConfig.social.github, siteConfig.social.linkedin],
    knowsAbout: [
      "C#",
      ".NET Core",
      "ASP.NET Core",
      "React",
      "Microservices",
      "Cloud Architecture",
    ],
  };
}
