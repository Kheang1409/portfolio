"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { Menu, X, Cpu } from "lucide-react";
import ThemeSelector from "@/components/hud/ThemeSelector";

const links = [
  ["01", "ABOUT", "about"],
  ["02", "STACK", "skills"],
  ["03", "EXPERIENCE", "experience"],
  ["04", "PROJECTS", "projects"],
  ["05", "EDUCATION", "education"],
  ["06", "CONTACT", "contact"],
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
      <Link href="/#home" className="system-wordmark">
        <span className="reactor-mark">
          <i />
        </span>
        <span>
          KAI // SYSTEM<small>ENGINEERING INTERFACE</small>
        </span>
      </Link>
      <nav
        className={`command-links${open ? " is-open" : ""}`}
        aria-label="Primary navigation"
      >
        {links.map(([n, label, id]) => (
          <Link key={id} href={`/#${id}`} onClick={() => setOpen(false)}>
            <small>{n}</small>
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
          <Cpu size={15} /> AI CORE
        </button>
        <ThemeSelector />
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
