"use client";

import { useTheme } from "next-themes";
import { Moon, Sun, Menu, X } from "lucide-react";
import { useState, useEffect } from "react";
import Link from "next/link";

export default function Navigation() {
  const [mounted, setMounted] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const { theme, setTheme } = useTheme();

  useEffect(() => {
    setMounted(true);
  }, []);

  const navLinks = [
    { label: "About", href: "#about" },
    { label: "Skills", href: "#skills" },
    { label: "Experience", href: "#experience" },
    { label: "Projects", href: "#projects" },
    { label: "Contact", href: "#contact" },
  ];

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 bg-light-background/95 dark:bg-dark-background/95 backdrop-blur-sm border-b border-light-border dark:border-dark-border">
      <div className="max-w-container mx-auto px-sm md:px-lg h-20 flex items-center justify-between">
        <Link
          href="#"
          className="flex items-center gap-2 font-bold text-xl text-light-secondary dark:text-dark-secondary hover:text-light-primary dark:hover:text-dark-primary transition-colors"
        >
          <div className="w-8 h-8 bg-light-primary dark:bg-dark-primary rounded-md flex items-center justify-center text-white font-bold">
            K
          </div>
          <span className="hidden sm:inline">Kheang</span>
        </Link>

        <div className="hidden md:flex items-center gap-8">
          {navLinks.map((link) => (
            <Link
              key={link.label}
              href={link.href}
              className="text-light-text-secondary dark:text-dark-text-secondary hover:text-light-primary dark:hover:text-dark-primary text-sm font-medium transition-colors"
            >
              {link.label}
            </Link>
          ))}
        </div>

        <div className="flex items-center gap-4">
          {mounted && (
            <button
              onClick={() => setTheme(theme === "light" ? "dark" : "light")}
              className="p-2 rounded-md hover:bg-light-border dark:hover:bg-dark-border transition-colors"
              aria-label="Toggle theme"
            >
              {theme === "light" ? (
                <Moon className="w-5 h-5 text-light-text-secondary" />
              ) : (
                <Sun className="w-5 h-5 text-dark-text-secondary" />
              )}
            </button>
          )}

          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="md:hidden p-2 rounded-md hover:bg-light-border dark:hover:bg-dark-border"
            aria-label="Toggle menu"
          >
            {mobileOpen ? (
              <X className="w-5 h-5" />
            ) : (
              <Menu className="w-5 h-5" />
            )}
          </button>
        </div>
      </div>

      {mobileOpen && (
        <div className="md:hidden bg-light-surface dark:bg-dark-surface border-t border-light-border dark:border-dark-border">
          <div className="px-sm py-lg space-y-3 flex flex-col">
            {navLinks.map((link) => (
              <Link
                key={link.label}
                href={link.href}
                className="text-light-text-secondary dark:text-dark-text-secondary hover:text-light-primary dark:hover:text-dark-primary font-medium py-2"
                onClick={() => setMobileOpen(false)}
              >
                {link.label}
              </Link>
            ))}
          </div>
        </div>
      )}
    </nav>
  );
}
