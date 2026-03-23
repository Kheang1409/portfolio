"use client";

import { Award } from "lucide-react";
import { motion } from "framer-motion";

const education = [
  {
    school: "Maharishi International University",
    degree: "Master of Science",
    year: "May 2024 - Dec 2026",
    detail: "Major: Computer Science",
    highlights: [
      "Graduate studies focused on advanced computer science concepts",
      "Applied software engineering for scalable systems",
      "Cross-functional collaboration and research-driven implementation",
    ],
  },
  {
    school: "Royal University of Phnom Penh",
    degree: "Bachelor of Science",
    year: "Nov 2016 - Nov 2020",
    detail: "Major: Computer Science & Engineering",
    highlights: [
      "Strong foundation in software engineering and systems design",
      "Database and web engineering fundamentals",
      "Computer science and engineering core curriculum",
    ],
  },
];

const certifications = [
  {
    name: "Best Project & Team Effort of The Year",
    issuer: "Khmer Care, Pathmazing Inc",
    year: "06/2021",
    credential: "Honor",
  },
];

export default function Education() {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <section
      id="education"
      aria-labelledby="education-heading"
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
          <h2
            id="education-heading"
            className="text-h2 font-bold text-light-text-primary dark:text-dark-text-primary mb-sm"
          >
            Education & Honors
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full" />
        </motion.div>

        <div className="grid md:grid-cols-2 gap-3xl">
          <motion.div
            initial="hidden"
            whileInView="visible"
            variants={fadeInVariants}
            transition={{ duration: 0.5, delay: 0.1 }}
            viewport={{ once: true, margin: "-100px" }}
          >
            <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary mb-lg">
              Education
            </h3>

            <div className="space-y-lg">
              {education.map((edu, idx) => (
                <div
                  key={idx}
                  className="p-lg rounded-lg bg-light-background dark:bg-dark-background border border-light-border dark:border-dark-border"
                >
                  <div className="flex items-start justify-between mb-md">
                    <div>
                      <h4 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
                        {edu.degree}
                      </h4>
                      <p className="text-body text-light-text-secondary dark:text-dark-text-secondary mt-xs">
                        {edu.school}
                      </p>
                    </div>
                    <span className="text-small font-medium text-light-accent dark:text-dark-accent bg-light-accent/10 dark:bg-dark-accent/10 px-md py-xs rounded-md whitespace-nowrap">
                      {edu.year}
                    </span>
                  </div>

                  <p className="text-small text-light-text-secondary dark:text-dark-text-secondary mb-md">
                    <span className="font-semibold">{edu.detail}</span>
                  </p>

                  <div className="flex flex-wrap gap-2xs">
                    {edu.highlights.map((highlight, hIdx) => (
                      <span
                        key={hIdx}
                        className="px-xs py-2xs rounded text-small font-medium text-light-primary dark:text-dark-primary bg-light-primary/10 dark:bg-dark-primary/10"
                      >
                        {highlight}
                      </span>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </motion.div>

          <motion.div
            initial="hidden"
            whileInView="visible"
            variants={fadeInVariants}
            transition={{ duration: 0.5, delay: 0.15 }}
            viewport={{ once: true, margin: "-100px" }}
          >
            <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary mb-lg">
              Honors
            </h3>

            <div className="space-y-sm">
              {certifications.map((cert, idx) => (
                <motion.div
                  key={idx}
                  initial="hidden"
                  whileInView="visible"
                  variants={fadeInVariants}
                  transition={{
                    duration: 0.4,
                    delay: 0.15 + idx * 0.05,
                  }}
                  viewport={{ once: true, margin: "-100px" }}
                  className="p-md rounded-md bg-light-background dark:bg-dark-background border border-light-border dark:border-dark-border hover:border-light-primary/50 dark:hover:border-dark-primary/50 transition-all group cursor-pointer"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2xs mb-xs">
                        <Award className="w-4 h-4 text-light-primary dark:text-dark-primary flex-shrink-0" />
                        <h4 className="font-semibold text-light-text-primary dark:text-dark-text-primary">
                          {cert.name}
                        </h4>
                      </div>
                      <p className="text-small text-light-text-secondary dark:text-dark-text-secondary mb-xs">
                        {cert.issuer}
                      </p>
                    </div>
                    <span className="text-small font-medium text-light-text-secondary dark:text-dark-text-secondary whitespace-nowrap ml-md">
                      {cert.year}
                    </span>
                  </div>
                  <span className="inline-block text-small font-medium text-light-primary dark:text-dark-primary mt-sm group-hover:translate-x-1 transition-transform">
                    {cert.credential} →
                  </span>
                </motion.div>
              ))}
            </div>
          </motion.div>
        </div>
      </div>
    </section>
  );
}
