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
        👋 Welcome to my portfolio
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
      variants={variants}
      className="text-h1 font-bold text-light-text-primary dark:text-dark-text-primary mb-md leading-tight"
    >
      Hi, I&apos;m{" "}
      <span className="text-light-primary dark:text-dark-primary">
        Hang Kheang Taing
      </span>
      <br />
      Software Engineer
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
      As a driven software developer with a passion for innovation, I leverage
      technology to drive business growth and improvement. With expertise in
      .NET, full-stack development, and database management, I deliver scalable
      solutions that enhance operational efficiency. Currently pursuing an MS in
      Computer Science at Maharishi International University, I&apos;m committed
      to lifelong learning and staying ahead of industry trends.
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
      className="mt-3xl text-light-text-secondary dark:text-dark-text-secondary"
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
