import Link from "next/link";
import { Github, Linkedin, Mail } from "lucide-react";
import { PERSONAL_INFO } from "@/lib/constants";
export default function Footer() {
  return (
    <footer className="campaign-footer">
      <div className="max-w-container mx-auto px-sm md:px-lg py-3xl">
        <div className="flex flex-col md:flex-row items-center justify-between gap-lg">
          <div>
            <p className="system-label">
              <span>KAI // SYSTEM</span> · INTERFACE STANDBY
            </p>
            <small>© {new Date().getFullYear()} Hang Kheang Taing</small>
          </div>
          <nav aria-label="Footer navigation" className="flex gap-md">
            <Link href="/#about">About</Link>
            <Link href="/#projects">Projects</Link>
            <Link href="/resume">Resume</Link>
            <Link href="/#contact">Contact</Link>
          </nav>
          <div className="flex gap-md">
            <a href={PERSONAL_INFO.GITHUB} aria-label="GitHub">
              <Github />
            </a>
            <a href={PERSONAL_INFO.LINKEDIN} aria-label="LinkedIn">
              <Linkedin />
            </a>
            <a href={`mailto:${PERSONAL_INFO.EMAIL}`} aria-label="Email">
              <Mail />
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}
