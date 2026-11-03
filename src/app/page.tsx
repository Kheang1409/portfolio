import type { Metadata } from "next";
import dynamicImport from "next/dynamic";
import Hero from "@/components/sections/Hero";
import About from "@/components/sections/About";
import { buildPageMetadata, siteConfig } from "@/lib/seo";
import { getResume } from "@/lib/resume";

const Skills = dynamicImport(() => import("@/components/sections/Skills"));
const Experience = dynamicImport(() => import("@/components/sections/Experience"));
const Projects = dynamicImport(() => import("@/components/sections/Projects"));
const Education = dynamicImport(() => import("@/components/sections/Education"));
const AICore = dynamicImport(() => import("@/components/sections/AICore"));
const Contact = dynamicImport(() => import("@/components/sections/Contact"));
const Donations = dynamicImport(() => import("@/components/sections/Donations"));

export const metadata: Metadata = buildPageMetadata({ title: siteConfig.fullTitle, description: siteConfig.description, path: "/" });
export const dynamic = "force-dynamic";

export default async function Home() {
  let resume = null;
  let profileLoadFailed = false;
  try {
    resume = await getResume();
  } catch (error) {
    console.error("Unable to load portfolio profile data from the backend.", error);
    profileLoadFailed = true;
  }

  return (
    <main
      id="main-content"
      className="campaign-main"
      aria-label="Portfolio content"
    >
      {profileLoadFailed && (
        <p className="profile-data-notice" role="status">
          Profile information is temporarily unavailable. Please try again later.
        </p>
      )}
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
