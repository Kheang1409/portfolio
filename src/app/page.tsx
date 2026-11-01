import type { Metadata } from "next";
import dynamic from "next/dynamic";
import Hero from "@/components/sections/Hero";
import About from "@/components/sections/About";
import { buildPageMetadata, siteConfig } from "@/lib/seo";
import { getResume } from "@/lib/resume";

const Skills = dynamic(() => import("@/components/sections/Skills"));
const Experience = dynamic(() => import("@/components/sections/Experience"));
const Projects = dynamic(() => import("@/components/sections/Projects"));
const Education = dynamic(() => import("@/components/sections/Education"));
const AICore = dynamic(() => import("@/components/sections/AICore"));
const Contact = dynamic(() => import("@/components/sections/Contact"));
const Donations = dynamic(() => import("@/components/sections/Donations"));

export const metadata: Metadata = buildPageMetadata({ title: siteConfig.fullTitle, description: siteConfig.description, path: "/" });

export default async function Home() {
  let resume = null;
  try { resume = await getResume(); } catch { resume = null; }

  return (
    <main
      id="main-content"
      className="campaign-main"
      aria-label="Portfolio content"
    >
      <Hero resume={resume} />
      <About resume={resume} />
      <Skills resume={resume} />
      <Experience resume={resume} />
      <Projects resume={resume} />
      <Education resume={resume} />
      <AICore />
      <Donations />
      <Contact resume={resume} />
    </main>
  );
}
