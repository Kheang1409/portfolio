"use client";

import { ArrowRight } from "lucide-react";
import Link from "next/link";
import { motion } from "framer-motion";
import ResumeModal from "@/components/ui/ResumeModal";

type BadgeProps = {
  variants: any;
};

export function Badge({ variants }: BadgeProps) {
  return (
    <motion.div variants={variants} className="mb-lg">
      <span className="inline-block px-md py-2xs rounded-full bg-light-primary/10 dark:bg-dark-primary/10 text-light-primary dark:text-dark-primary text-small font-medium">
        <span className="mr-2 inline-block h-2 w-2 rounded-full bg-emerald-400 shadow-[0_0_12px_rgba(52,211,153,0.9)]" />
        Available for new opportunities
      </span>
    </motion.div>
  );
}

type HeadingProps = {
  variants: any;
};

export function Heading({ variants }: HeadingProps) {
  return (
    <motion.h1
      id="hero-heading"
      variants={variants}
      className="text-[clamp(2.75rem,6vw,5.4rem)] font-bold tracking-[-0.055em] text-light-text-primary dark:text-dark-text-primary mb-md leading-[0.98]"
    >
      I build systems
      <br />
      <span className="hero-gradient-text">that scale.</span>
    </motion.h1>
  );
}

type TaglineProps = {
  variants: any;
};

export function Tagline({ variants }: TaglineProps) {
  return (
    <motion.p
      variants={variants}
      className="text-lg md:text-xl text-light-text-secondary dark:text-dark-text-secondary mb-xl max-w-2xl"
    >
      I&apos;m Hang Kheang Taing, a software engineer turning complex ideas into
      resilient products with .NET, React, microservices, and cloud-native
      architecture.
    </motion.p>
  );
}

type CTAButtonsProps = {
  variants: any;
};

export function CTAButtons({ variants }: CTAButtonsProps) {
  return (
    <motion.div
      variants={variants}
      className="flex flex-col sm:flex-row gap-md items-center md:items-start justify-center md:justify-start"
    >
      <Link
        href="#projects"
        aria-label="Jump to featured projects"
        className="px-xl py-sm rounded-md bg-light-primary dark:bg-dark-primary text-white font-semibold hover:shadow-lg transform hover:scale-105 transition-all duration-300 flex items-center gap-2 min-h-[44px]"
      >
        View Projects
        <ArrowRight className="w-5 h-5" />
      </Link>

      <ResumeModal />
    </motion.div>
  );
}

export function ScrollIndicator() {
  return (
    <motion.div
      animate={{ y: [0, 10, 0] }}
      transition={{ duration: 2, repeat: Infinity }}
      className="mt-2xl hidden md:block text-light-text-secondary dark:text-dark-text-secondary"
    >
      <p className="text-small font-medium mb-2">Scroll to explore</p>
      <svg
        className="w-6 h-6 mx-auto"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M19 14l-7 7m0 0l-7-7m7 7V3"
        />
      </svg>
    </motion.div>
  );
}
