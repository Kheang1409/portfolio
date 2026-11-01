"use client";
import dynamic from "next/dynamic";
import Image from "next/image";
import { ArrowDown, ArrowUpRight, Cpu, Database, Network } from "lucide-react";
import type { Resume } from "@/lib/resume";
const ArcReactorScene = dynamic(
  () => import("@/components/visuals/ArcReactorScene"),
  { ssr: false },
);
export default function Hero({ resume }: { resume: Resume | null }) {
  const name = resume?.personals?.preferredName || resume?.personals?.legalName || "";
  const role = resume?.experiences?.[0]?.role || "";
  const tech = resume?.skills?.slice(0, 6) ?? [];
  return (
    <section id="home" className="armor-hero" aria-labelledby="hero-heading">
      <div className="hero-reactor">
        <ArcReactorScene />
      </div>
      <div className="hero-scan" aria-hidden="true" />
      <div className="hero-content">
        <div className="hero-copy">
          <p className="system-label">
            <span>SYS.00</span> // IDENTITY CORE <i className="online-dot" />{" "}
            ONLINE
          </p>
          <h1 id="hero-heading">
            {name}
          </h1>
          <p className="hero-role">{role}</p>
          <p className="hero-statement">
            {resume?.summary}
          </p>
          <div className="hero-actions">
            <a className="energy-button" href="#projects">
              EXPLORE SYSTEMS <ArrowUpRight />
            </a>
            <a className="ghost-button" href="/resume">
              VIEW RESUME
            </a>
            <button
              className="text-control"
              onClick={() =>
                document
                  .querySelector<HTMLButtonElement>(".assistant-launcher")
                  ?.click()
              }
            >
              <Cpu /> INITIALIZE AI
            </button>
          </div>
          <ul className="tech-rail" aria-label="Core technologies">
            {tech.map((t) => (
              <li key={t}>{t}</li>
            ))}
          </ul>
        </div>
        <div className="hero-portrait" aria-label="Portrait system">
          <div className="portrait-frame">
            <Image
              src="/images/kai-classic-stance.webp"
              alt="Professional profile portrait"
              fill
              priority
              sizes="(max-width: 768px) 80vw, 42vw"
              className="portrait-image"
            />
            <span className="portrait-scan" />
          </div>
          <span className="hud-tag tag-a">BIOMETRIC // VERIFIED</span>
          <span className="hud-tag tag-b">NANOSHELL // STANDBY</span>
          <span className="hud-tag tag-c">CORE // STABLE</span>
        </div>
      </div>
      <div className="quick-metrics">
        {tech.slice(0, 3).map((technology, index) => {
          const Icon = [Cpu, Network, Database][index];
          return <div key={technology}><Icon /><span>SKILL<strong>{technology}</strong></span></div>;
        })}
        <a href="#about">
          SYSTEM OVERVIEW <ArrowDown />
        </a>
      </div>
    </section>
  );
}
