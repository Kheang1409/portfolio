"use client";
import Image from "next/image";
import { ArrowDown, ArrowRight, Cpu } from "lucide-react";
import type { Resume } from "@/lib/resume";

export default function Hero({ resume }: { resume: Resume | null }) {
  const name = resume?.personals?.preferredName || resume?.personals?.legalName || "";
  const role = resume?.experiences?.[0]?.role || "";
  const tech = resume?.skills?.slice(0, 6) ?? [];

  return (
    <section id="home" className="armor-hero greeting-section" aria-labelledby="hero-heading">
      <div className="hero-content">
        <div className="hero-copy">
          <p className="hero-eyebrow">A warm hello · Software engineering</p>
          <h1 id="hero-heading">{name || "Hello, I’m Kai."}</h1>
          {role && <p className="hero-role">{role}</p>}
          {resume?.summary && <p className="hero-statement">{resume.summary}</p>}
          <div className="hero-actions">
            <a className="energy-button" href="#projects">
              Explore my work <ArrowRight />
            </a>
            <a className="ghost-button" href="/resume">
              View resume
            </a>
            <button
              className="text-control"
              onClick={() =>
                document
                  .querySelector<HTMLButtonElement>(".assistant-launcher")
                  ?.click()
              }
            >
              <Cpu /> Ask me anything
            </button>
          </div>
          {tech.length > 0 && (
            <ul className="tech-rail" aria-label="Core technologies">
              {tech.map((technology) => <li key={technology}>{technology}</li>)}
            </ul>
          )}
        </div>
        <figure className="greeting-portrait">
          <Image
            src="/images/greeting.jfif"
            alt="Kai waving hello."
            fill
            priority
            sizes="(max-width: 760px) 76vw, 36vw"
            className="greeting-portrait__image"
          />
          <figcaption>Nice to meet you.</figcaption>
        </figure>
      </div>
      <div className="quick-metrics">
        <span className="hero-index">Portfolio · 2026</span>
        <a href="#about">
          Get to know me <ArrowDown />
        </a>
      </div>
    </section>
  );
}
