"use client";
import Image from "next/image";
import { motion, useReducedMotion } from "framer-motion";
import {
  Sprout,
  Waves,
  Fingerprint,
  ArrowUpRight,
  Landmark,
  GraduationCap,
  Code2,
  Globe2,
} from "lucide-react";
import { journey, culture, farming, values } from "@/lib/story";
import type { ReactNode } from "react";
function Reveal({
  children,
  className = "",
}: {
  children: ReactNode;
  className?: string;
}) {
  const reduced = useReducedMotion();
  return (
    <motion.div
      className={className}
      initial={false}
      whileInView={reduced ? {} : { y: [16, 0], opacity: [0.65, 1] }}
      viewport={{ once: true, amount: 0.1 }}
      transition={{ duration: 0.65 }}
    >
      {children}
    </motion.div>
  );
}
export function Journey() {
  const icons = [Sprout, GraduationCap, Code2, Globe2];
  return (
    <section
      id="journey"
      className="story-section journey-section"
      aria-labelledby="journey-title"
    >
      <div className="section-heading">
        <p className="eyebrow">01 / THE JOURNEY</p>
        <h2 id="journey-title">
          Every path starts
          <br />
          <em>somewhere.</em>
        </h2>
        <p>
          From Cambodia to new horizons. A story of learning, building, and
          carrying home with me.
        </p>
      </div>
      <Reveal className="journey-grid">
        {journey.map((step, i) => {
          const Icon = icons[i];
          return (
            <article key={step.title}>
              <span className="journey-node">
                <Icon size={21} />
              </span>
              <p className="eyebrow">{step.place}</p>
              <h3>{step.title}</h3>
              <p>{step.text}</p>
            </article>
          );
        })}
      </Reveal>
    </section>
  );
}
export function Culture() {
  const icons = [Sprout, Waves, Fingerprint];
  return (
    <section
      id="culture"
      className="story-section culture-section"
      aria-labelledby="culture-title"
    >
      <div className="section-heading">
        <p className="eyebrow">BEYOND THE SCREEN / CAMBODIA</p>
        <h2 id="culture-title">
          The place behind
          <br />
          <em>the perspective.</em>
        </h2>
        <p>
          Not just a visual inspiration. A reminder to approach work with
          patience, care, and a sense of connection.
        </p>
      </div>
      <Reveal className="culture-grid">
        {culture.map((item, i) => {
          const Icon = icons[i];
          return (
            <article key={item.title}>
              <div
                className={`culture-art culture-art--${i}`}
                aria-hidden="true"
              >
                <Image
                  src={`/images/culture-${i}.webp`}
                  alt=""
                  fill
                  sizes="(max-width: 767px) 88vw, 30vw"
                  className="culture-photo"
                />
                <Icon size={24} strokeWidth={1} />
                <span>0{i + 1}</span>
              </div>
              <p className="eyebrow">{item.label}</p>
              <h3>{item.title}</h3>
              <p>{item.text}</p>
            </article>
          );
        })}
      </Reveal>
    </section>
  );
}
export function Farming() {
  return (
    <section
      id="farming"
      className="story-section farming-section"
      aria-labelledby="farming-title"
    >
      <div className="section-heading">
        <p className="eyebrow">A REFLECTION / GROWTH TAKES CARE</p>
        <h2 id="farming-title">
          Lessons from
          <br />
          <em>the rice field.</em>
        </h2>
        <p>
          A farming-inspired philosophy for building software. Prepare
          carefully. Grow steadily. Create something worth sharing.
        </p>
      </div>
      <Reveal className="farming-grid">
        {farming.map(([n, title, value, text]) => (
          <article key={n}>
            <span className="chapter-number">{n}</span>
            <p className="eyebrow">{value}</p>
            <h3>{title}</h3>
            <p>{text}</p>
          </article>
        ))}
      </Reveal>
    </section>
  );
}
export function Values() {
  return (
    <section
      id="values"
      className="story-section values-section"
      aria-labelledby="values-title"
    >
      <p className="eyebrow">BUILT TO LAST</p>
      <h2 id="values-title">
        Good engineering has <em>good foundations.</em>
      </h2>
      <Reveal className="values-grid">
        {values.map(([part, title, text], i) => (
          <article key={part}>
            <Landmark size={24} strokeWidth={1} />
            <span className="eyebrow">
              0{i + 1} / {part}
            </span>
            <h3>{title}</h3>
            <p>{text}</p>
          </article>
        ))}
      </Reveal>
    </section>
  );
}
export function Showcase() {
  return (
    <section
      id="featured-project"
      className="story-section showcase"
      aria-labelledby="showcase-title"
    >
      <div>
        <p className="eyebrow">FEATURED STORY / PERSONAL PROJECT · 2025</p>
        <h2 id="showcase-title">
          A portfolio that
          <br />
          <em>starts a conversation.</em>
        </h2>
        <p>
          AI-Powered Portfolio brings engineering work and an interactive
          assistant into one personal experience.
        </p>
        <div className="showcase-metrics">
          <div>
            <strong>35%</strong>
            <span>longer sessions</span>
          </div>
          <div>
            <strong>150+</strong>
            <span>weekly users</span>
          </div>
        </div>
        <p className="metric-note">
          Results documented in my existing project summary.
        </p>
        <a href="#projects" className="button-line">
          Explore the projects <ArrowUpRight size={17} />
        </a>
      </div>
      <Reveal className="showcase-device">
        <div className="device-bar">
          <i />
          <i />
          <i />
          <span>kai / portfolio assistant</span>
        </div>
        <div className="device-content">
          <span className="monogram">kt.</span>
          <h3>
            Meaningful work.
            <br />
            <em>Human connection.</em>
          </h3>
          <p>Ask about my skills, experience, or projects.</p>
          <a href="#contact" className="device-input">
            Let’s build something together <ArrowUpRight size={16} />
          </a>
          <div className="device-code">
            <span>const perspective = &#123;</span>
            <br />
            &nbsp; roots: <b>"Cambodia"</b>,<br />
            &nbsp; focus: <b>"useful software"</b>
            <br />
            <span>&#125;;</span>
          </div>
        </div>
      </Reveal>
      <div className="showcase-process">
        {[
          ["The problem", "Make a technical portfolio easier to explore."],
          ["The process", "Connect portfolio content with LLM APIs."],
          ["The solution", "An assistant alongside projects and experience."],
          ["The result", "Richer interaction and longer sessions."],
        ].map(([title, text]) => (
          <div key={title}>
            <h3>{title}</h3>
            <p>{text}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
