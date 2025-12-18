"use client";

import { Github, Linkedin, Mail } from "lucide-react";
import Link from "next/link";

export default function Footer() {
  const currentYear = new Date().getFullYear();
  const socialLinks = [
    { icon: Github, href: "https://github.com", label: "GitHub" },
    { icon: Linkedin, href: "https://linkedin.com", label: "LinkedIn" },
    { icon: Mail, href: "mailto:your.email@example.com", label: "Email" },
  ];

  return (
    <footer className="bg-light-surface dark:bg-dark-surface border-t border-light-border dark:border-dark-border">
      <div className="max-w-container mx-auto px-sm md:px-lg py-3xl">
        <div className="flex flex-col md:flex-row items-center justify-between gap-lg">
          <div className="text-light-text-secondary dark:text-dark-text-secondary text-small">
            <p>&copy; {currentYear} All rights reserved.</p>
          </div>

          <div className="text-light-text-secondary dark:text-dark-text-secondary text-small font-medium">
            Designed & Built by Kheang
          </div>

          <div className="flex items-center gap-md">
            {socialLinks.map((link) => {
              const Icon = link.icon;
              return (
                <Link
                  key={link.label}
                  href={link.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="p-2 rounded-md bg-light-background dark:bg-dark-background hover:bg-light-primary hover:text-white dark:hover:bg-dark-primary text-light-text-secondary dark:text-dark-text-secondary transition-colors"
                  aria-label={link.label}
                >
                  <Icon className="w-5 h-5" />
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    </footer>
  );
}
