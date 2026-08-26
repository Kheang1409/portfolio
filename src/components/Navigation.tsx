"use client";

import { useTheme } from "next-themes";
import { Briefcase, Folder, Home, Mail, Menu, Moon, Sun, User, Wrench, X } from "lucide-react";
import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";

const navLinks = [
  { label: "Home", short: "HOME", href: "#home", icon: Home },
  { label: "About", short: "BIO", href: "#about", icon: User },
  { label: "Skills", short: "GEAR", href: "#skills", icon: Wrench },
  { label: "Experience", short: "LOG", href: "#experience", icon: Briefcase },
  { label: "Projects", short: "MAP", href: "#projects", icon: Folder },
  { label: "Contact", short: "COMMS", href: "#contact", icon: Mail },
];

export default function Navigation() {
  const [mounted, setMounted] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [activeSection, setActiveSection] = useState("home");
  const { theme, setTheme } = useTheme();
  useEffect(() => setMounted(true), []);

  useEffect(() => {
    const sectionIds = navLinks.map((link) => link.href.slice(1));
    const sections = sectionIds
      .map((id) => document.getElementById(id))
      .filter((section): section is HTMLElement => Boolean(section));
    if (!sections.length) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => b.intersectionRatio - a.intersectionRatio);
        if (visible[0]?.target.id) setActiveSection(visible[0].target.id);
      },
      { rootMargin: "-20% 0px -55% 0px", threshold: [0, 0.1, 0.25, 0.5] },
    );
    sections.forEach((section) => observer.observe(section));
    return () => observer.disconnect();
  }, []);


  return (
    <>
      <nav aria-label="Primary" className="campaign-rail">
        <Link href="/" className="campaign-brand" aria-label="Hang Kheang Taing homepage">
          <span className="campaign-brand__avatar"><Image src="/avatar-pixel-idle.gif" alt="" width={54} height={54} unoptimized /></span>
          <span className="campaign-brand__name">KAI</span>
        </Link>

        <div className="campaign-links">
          {navLinks.map(({ label, short, href, icon: Icon }) => {
            const isActive = activeSection === href.slice(1);
            return (
            <Link key={label} href={href} className={`campaign-link${isActive ? " campaign-link--active" : ""}`} aria-label={label} aria-current={isActive ? "location" : undefined} title={label}>
              <Icon aria-hidden="true" />
              <span>{short}</span>
            </Link>
          )})}
        </div>

        <div className="campaign-rail__bottom">
          <span className="campaign-online"><i />LV. 25</span>
          {mounted && <button type="button" className="campaign-theme" onClick={() => setTheme(theme === "light" ? "dark" : "light")} aria-label="Toggle theme">{theme === "light" ? <Moon /> : <Sun />}</button>}
        </div>
      </nav>

      <header className="campaign-mobile-bar">
        <Link href="/" className="campaign-mobile-brand"><Image src="/avatar-pixel-idle.gif" alt="" width={42} height={42} unoptimized /><strong>KAI.EXE</strong></Link>
        <div className="flex items-center gap-2">
          {mounted && <button type="button" onClick={() => setTheme(theme === "light" ? "dark" : "light")} aria-label="Toggle theme">{theme === "light" ? <Moon /> : <Sun />}</button>}
          <button type="button" onClick={() => setMobileOpen(!mobileOpen)} aria-expanded={mobileOpen} aria-controls="mobile-primary-nav" aria-label="Toggle menu">{mobileOpen ? <X /> : <Menu />}</button>
        </div>
      </header>

      {mobileOpen && <nav id="mobile-primary-nav" className="campaign-mobile-menu" aria-label="Mobile primary">
        {navLinks.map(({ label, href, icon: Icon }, index) => {
          const isActive = activeSection === href.slice(1);
          return <Link key={label} href={href} className={isActive ? "campaign-mobile-link--active" : ""} aria-current={isActive ? "location" : undefined} onClick={() => setMobileOpen(false)}><span>0{index}</span><Icon />{label}</Link>;
        })}
      </nav>}
    </>
  );
}
