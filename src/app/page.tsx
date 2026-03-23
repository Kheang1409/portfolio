import type { Metadata } from "next";
import dynamic from "next/dynamic";
import Hero from "@/components/sections/Hero";
import About from "@/components/sections/About";
import { buildPageMetadata, buildPersonJsonLd, siteConfig } from "@/lib/seo";

const Skills = dynamic(() => import("@/components/sections/Skills"));
const Experience = dynamic(() => import("@/components/sections/Experience"));
const Projects = dynamic(() => import("@/components/sections/Projects"));
const Education = dynamic(() => import("@/components/sections/Education"));
const Contact = dynamic(() => import("@/components/sections/Contact"));

export const metadata: Metadata = buildPageMetadata({
  title: siteConfig.fullTitle,
  description:
    "Hang Kheang Taing is a Software Engineer specializing in C#, .NET Core, ASP.NET Core, React, microservices, and cloud-native systems on Azure and AWS.",
  path: "/",
  keywords: [
    "Hang Kheang Taing portfolio",
    "Hang Kheang Taing software engineer",
    "full stack .NET engineer",
    "cloud-native software engineer",
  ],
});

export default function Home() {
  const personJsonLd = buildPersonJsonLd();

  return (
    <main id="main-content" aria-label="Hang Kheang Taing portfolio content">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(personJsonLd) }}
      />
      <Hero />
      <About />
      <Skills />
      <Experience />
      <Projects />
      <Education />
      <Contact />
    </main>
  );
}
