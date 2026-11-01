import type { Metadata } from "next";

const siteUrl =
  process.env.NEXT_PUBLIC_SITE_URL ?? "https://kaitaing.netlify.app";

export const siteConfig = {
  name: "Professional Portfolio",
  fullTitle: "Professional Portfolio",
  description: "Professional portfolio and experience.",
  url: siteUrl,
  locale: "en_US",
  keywords: ["Portfolio"],
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
  image = "/images/kai/kai-og.webp",
}: PageMetadataInput): Metadata {
  const mergedKeywords = Array.from(
    new Set([...siteConfig.keywords, ...keywords]),
  );

  return {
    metadataBase: new URL(siteConfig.url),
    title: { default: title, template: "%s" },
    description,
    keywords: mergedKeywords,
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
    twitter: { card: "summary_large_image", title, description, images: [image] },
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
