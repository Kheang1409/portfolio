"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { Menu, X, ArrowUpRight } from "lucide-react";

const links = [
  ["About", "about"],
  ["Skills", "skills"],
  ["Experience", "experience"],
  ["Work", "projects"],
  ["Education", "education"],
  ["Contact", "contact"],
];

export default function Navigation() {
  const [open, setOpen] = useState(false);
  const [compact, setCompact] = useState(false);

  useEffect(() => {
    const update = () => setCompact(scrollY > 36);
    update();
    addEventListener("scroll", update, { passive: true });
    return () => removeEventListener("scroll", update);
  }, []);

  return (
    <header className={`command-nav${compact ? " is-compact" : ""}`}>
      <Link href="/#home" className="system-wordmark" aria-label="Kai home">
        <span className="wordmark-dot" aria-hidden="true" />
        Kai
      </Link>
      <nav
        className={`command-links${open ? " is-open" : ""}`}
        aria-label="Primary navigation"
      >
        {links.map(([label, id]) => (
          <Link key={id} href={`/#${id}`} onClick={() => setOpen(false)}>
            {label}
          </Link>
        ))}
      </nav>
      <div className="command-controls">
        <button
          className="ai-nav"
          onClick={() =>
            document
              .querySelector<HTMLButtonElement>(".assistant-launcher")
              ?.click()
          }
        >
          Ask Kai <ArrowUpRight size={15} />
        </button>
        <Link className="resume-nav" href="/resume">Resume</Link>
        <button
          className="menu-toggle"
          aria-label={open ? "Close menu" : "Open menu"}
          aria-expanded={open}
          onClick={() => setOpen(!open)}
        >
          {open ? <X /> : <Menu />}
        </button>
      </div>
    </header>
  );
}
