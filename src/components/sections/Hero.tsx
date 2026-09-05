"use client";
import Image from "next/image";
import { ArrowDown, ArrowUpRight, Github, Linkedin } from "lucide-react";
import { PERSONAL_INFO } from "@/lib/constants";
export default function Hero() {
  return (
    <section id="home" className="khmer-hero" aria-labelledby="hero-heading">
      <Image
        src="/images/angkor-dawn.webp"
        alt="Angkor-inspired temple towers reflected in still water at dawn"
        fill
        priority
        sizes="100vw"
        className="hero-landscape"
      />
      <div className="hero-shade" />
      <div className="hero-particles" aria-hidden="true">
        {Array.from({ length: 8 }, (_, i) => (
          <i
            key={i}
            style={{
              left: `${18 + i * 10}%`,
              bottom: `${12 + (i % 3) * 12}%`,
              animationDelay: `${i * -1.7}s`,
            }}
          />
        ))}
      </div>
      <div className="hero-mist" aria-hidden="true" />
      <div className="hero-topline">
        <span>CAMBODIAN ROOTS. GLOBAL PERSPECTIVE.</span>
        <span>PERSONAL PORTFOLIO / 2026</span>
      </div>
      <div className="hero-copy">
        <p className="eyebrow">
          <span className="status-dot" /> SOFTWARE ENGINEER & CREATIVE THINKER
        </p>
        <h1 id="hero-heading">
          Rooted in heritage.
          <br /> <em>Building the future.</em>
        </h1>
        <div className="hero-intro">
          <span className="gold-rule" />
          <div>
            <p className="hero-name">I’m Hang Kheang Taing.</p>
            <p>
              Building meaningful digital experiences from Cambodian roots to
              the modern world.
            </p>
          </div>
        </div>
        <div className="hero-actions">
          <a className="button-gold" href="#projects">
            View my work <ArrowUpRight size={17} />
          </a>
          <a className="button-line" href="#journey">
            Explore my journey <ArrowUpRight size={17} />
          </a>
        </div>
      </div>
      <div className="hero-bottom">
        <a href="#journey" className="scroll-gateway">
          <span>
            <ArrowDown size={16} />
          </span>{" "}
          A JOURNEY WORTH BUILDING
        </a>
        <div className="hero-socials">
          <a href="#contact">
            Let’s connect <ArrowUpRight size={14} />
          </a>
          <a href={PERSONAL_INFO.GITHUB} aria-label="GitHub">
            <Github size={17} />
          </a>
          <a href={PERSONAL_INFO.LINKEDIN} aria-label="LinkedIn">
            <Linkedin size={17} />
          </a>
        </div>
      </div>
      <div className="landscape-caption">
        01 / ANGKOR AT FIRST LIGHT
        <span>HERITAGE IS WHERE THE STORY BEGINS.</span>
      </div>
    </section>
  );
}
