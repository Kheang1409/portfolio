import Link from "next/link";
import { getResume } from "@/lib/resume";

export default async function Footer() {
  let name = "";
  try {
    const resume = await getResume();
    name = resume?.personals?.preferredName || resume?.personals?.legalName || "";
  } catch {
    name = "";
  }
  return <footer className="campaign-footer"><div className="max-w-container mx-auto px-sm md:px-lg py-3xl"><div className="flex flex-col md:flex-row items-center justify-between gap-lg"><div><p className="system-label"><span>PORTFOLIO</span> · INTERFACE</p><small>© {new Date().getFullYear()} {name}</small></div><nav aria-label="Footer navigation" className="flex gap-md"><Link href="/#about">About</Link><Link href="/#projects">Projects</Link><Link href="/resume">Resume</Link><Link href="/#donate">Support</Link><Link href="/#contact">Contact</Link></nav></div></div></footer>;
}
