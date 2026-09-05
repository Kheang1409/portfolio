"use client";
import { useEffect, useState } from "react";
import { useTheme } from "next-themes";
import { Menu, X, Sun, Moon, ArrowUpRight } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
const links = [
  ["Journey", "journey"],
  ["About", "about"],
  ["Skills", "skills"],
  ["Work", "projects"],
  ["Culture", "culture"],
  ["Experience", "experience"],
  ["Education", "education"],
];
export default function Navigation() {
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);
  const [active, setActive] = useState("");
  const { resolvedTheme, setTheme } = useTheme();
  const path = usePathname();
  useEffect(() => setMounted(true), []);
  useEffect(() => {
    if (!open) return;
    const escape = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setOpen(false);
        document.getElementById("menu-toggle")?.focus();
      }
    };
    window.addEventListener("keydown", escape);
    return () => window.removeEventListener("keydown", escape);
  }, [open]);
  useEffect(() => {
    let frame = 0;
    const update = () => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        const ids = ["home", ...links.map(([, id]) => id), "contact"];
        let current = "home";
        for (const id of ids) {
          const section = document.getElementById(id);
          if (section && section.getBoundingClientRect().top <= 160)
            current = id;
        }
        setActive(current);
      });
    };
    update();
    window.addEventListener("scroll", update, { passive: true });
    return () => {
      cancelAnimationFrame(frame);
      window.removeEventListener("scroll", update);
    };
  }, [path]);
  return (
    <header className="site-header">
      <Link
        href="/#home"
        className="wordmark"
        aria-label="Hang Kheang Taing home"
      >
        <span className="monogram">kt.</span>
        <span>
          KHEANG TAING<small>SOFTWARE ENGINEER</small>
        </span>
      </Link>
      <nav
        className={open ? "primary-links is-open" : "primary-links"}
        id="primary-links"
        aria-label="Primary"
      >
        {links.map(([label, id]) => (
          <Link
            key={id}
            href={`/#${id}`}
            onClick={() => setOpen(false)}
            aria-current={active === id ? "location" : undefined}
          >
            {label}
          </Link>
        ))}
        <Link
          href="/#contact"
          className="nav-contact"
          aria-current={active === "contact" ? "location" : undefined}
          onClick={() => setOpen(false)}
        >
          Let’s talk <ArrowUpRight size={14} />
        </Link>
      </nav>
      <div className="nav-controls">
        {mounted && (
          <button
            aria-label="Toggle theme"
            onClick={() =>
              setTheme(resolvedTheme === "dark" ? "light" : "dark")
            }
          >
            {resolvedTheme === "dark" ? <Sun size={17} /> : <Moon size={17} />}
          </button>
        )}
        <button
          id="menu-toggle"
          className="menu-toggle"
          aria-label={open ? "Close menu" : "Open menu"}
          aria-expanded={open}
          aria-controls="primary-links"
          onClick={() => setOpen(!open)}
        >
          {open ? <X /> : <Menu />}
        </button>
      </div>
    </header>
  );
}
