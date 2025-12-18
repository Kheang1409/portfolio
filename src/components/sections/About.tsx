"use client";

import { motion } from "framer-motion";
import Image from "next/image";
import ResumeModal from "@/components/ui/ResumeModal";

export default function About() {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <section
      id="about"
      className="py-4xl md:py-[100px] bg-light-surface dark:bg-dark-surface"
    >
      <div className="max-w-container mx-auto px-sm md:px-lg">
        <motion.div
          initial="hidden"
          whileInView="visible"
          variants={fadeInVariants}
          transition={{ duration: 0.5 }}
          viewport={{ once: true, margin: "-100px" }}
          className="mb-3xl"
        >
          <h2 className="text-h2 font-bold text-light-text-primary dark:text-dark-text-primary mb-sm">
            About
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full" />
        </motion.div>

        <div className="grid md:grid-cols-2 gap-3xl items-center">
          <motion.div
            initial="hidden"
            whileInView="visible"
            variants={fadeInVariants}
            transition={{ duration: 0.6, delay: 0.1 }}
            viewport={{ once: true, margin: "-100px" }}
            className="space-y-md"
          >
            <p className="text-body text-light-text-secondary dark:text-dark-text-secondary leading-relaxed">
              I&apos;m passionate about staying up-to-date with the latest
              technologies and trends, ensuring my skills remain relevant and
              effective. With a strong foundation in computer science and a
              drive to continuously learn, I&apos;m committed to delivering
              high-quality solutions that meet the evolving needs of businesses
              and organizations.
            </p>

            <div className="pt-md">
              <div className="flex flex-wrap gap-sm">
                <a
                  href="#contact"
                  className="px-xl py-sm rounded-md bg-light-primary dark:bg-dark-primary text-white font-semibold hover:shadow-lg transform hover:scale-105 transition-all duration-300"
                >
                  Contact
                </a>
                <ResumeModal triggerClassName="px-xl py-sm rounded-md border-2 border-light-primary dark:border-dark-primary text-light-primary dark:text-dark-primary font-semibold hover:bg-light-primary/10 dark:hover:bg-dark-primary/10 transition-colors flex items-center gap-2 min-h-[44px]" />
              </div>
            </div>
          </motion.div>

          <motion.div
            initial="hidden"
            whileInView="visible"
            variants={fadeInVariants}
            transition={{ duration: 0.6, delay: 0.2 }}
            viewport={{ once: true, margin: "-100px" }}
            className="flex justify-center md:justify-end"
          >
            <div className="relative w-56 h-56 md:w-64 md:h-64 rounded-full bg-light-primary/10 dark:bg-dark-primary/10 border-4 border-light-primary/20 dark:border-dark-primary/20 shadow-[0_8px_30px_rgba(11,92,255,0.08)] overflow-hidden">
              <Image
                src="/avatar.png"
                alt="Hang Kheang Taing profile"
                fill
                sizes="(max-width: 768px) 224px, 256px"
                className="object-cover"
                priority
              />
            </div>
          </motion.div>
        </div>
      </div>
    </section>
  );
}
