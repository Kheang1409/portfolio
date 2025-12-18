"use client";

import { Calendar, MapPin } from "lucide-react";
import { motion } from "framer-motion";

const experiences = [
  {
    company: "Sahakrinpheap Microfinance PLC",
    role: "Senior Software Engineer",
    duration: "Sep 2021 - May 2024",
    location: "Phnom Penh, Cambodia",
    description:
      "Architected and scaled backend systems for microfinance operations, mentoring junior developers, and delivering mission-critical financial solutions.",
    achievements: [
      "Automated biometric attendance tracking, accelerating payroll processing by 7 days",
      "Consolidated legacy banking plugins into single application, reducing system crashes by 40%",
      "Implemented real-time Telegram Bot notifications, improving incident response by 50%",
      "Redesigned SQL reporting pipelines, reducing query times from minutes to under 2 seconds",
      "Maintained 99% system uptime with OAuth2 and CORS security policies",
      "Mentored 2 junior developers, reducing onboarding time by 30%",
    ],
  },
  {
    company: "Pathmazing Inc",
    role: "Full-Stack Engineer",
    duration: "Mar 2021 - Sep 2021",
    location: "Phnom Penh, Cambodia",
    description:
      "Engineered Python REST APIs and custom ERP integrations, streamlining enterprise workflows and achieving high user satisfaction.",
    achievements: [
      "Developed Python REST APIs and custom Odoo ERP integrations, achieving 99% user satisfaction",
      "Integrated Microsoft Teams with Odoo, increasing meeting adoption by 35%",
      "Upgraded Odoo 13 to 14, improving page load times by 20% and optimizing 3 module workflows",
      "Connected Odoo with ABA PayWay, enhancing transaction reliability by 30% for 1,000+ monthly payments",
      "Launched Khmer Care fundraising platform, raising $70.8K and partnering with 30+ organizations",
      "Optimized Firearms Metrics Platform, reducing reporting time from 30 minutes to under 5 minutes",
    ],
  },
  {
    company: "Anakut Digital Solution, Co. Ltd",
    role: "Application Developer",
    duration: "Oct 2020 - May 2021",
    location: "Phnom Penh, Cambodia",
    description:
      "Developed enterprise management systems and integrated ERP solutions for hospitality, retail, and healthcare sectors.",
    achievements: [
      "Developed POS system with automated inventory and scale integration, eliminating 99% of manual entry errors",
      "Designed Hotel Management System with booking engine, halving check-in time and boosting efficiency by 30%",
      "Delivered Clinic Management System with scheduling and patient tracking, reducing wait times by 45%",
      "Configured Odoo ERP for e-commerce, enabling real-time order tracking and increasing retention by 15%",
    ],
  },
];

export default function Experience() {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <section
      id="experience"
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
            Experience
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full" />
        </motion.div>

        <div className="relative">
          <div className="absolute left-[6px] top-0 bottom-0 w-[2px] bg-light-primary/30 dark:bg-dark-primary/30 md:hidden" />

          <div className="space-y-lg">
            {experiences.map((exp, idx) => (
              <motion.div
                key={idx}
                initial="hidden"
                whileInView="visible"
                variants={fadeInVariants}
                transition={{ duration: 0.5, delay: idx * 0.1 }}
                viewport={{ once: true, margin: "-100px" }}
                className="relative pl-[32px] md:pl-0 pb-lg md:pb-xl md:border-b-2 md:border-light-primary/30 md:dark:border-dark-primary/30 last:border-0"
              >
                <div className="absolute left-0 top-0 w-3 h-3 rounded-full bg-light-primary dark:bg-dark-primary md:hidden" />

                <div className="grid md:grid-cols-3 gap-lg">
                  <div className="md:col-span-1">
                    <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary mb-xs">
                      {exp.role}
                    </h3>
                    <p className="text-small text-light-text-secondary dark:text-dark-text-secondary font-medium mb-md">
                      {exp.company}
                    </p>
                    <div className="space-y-xs">
                      <div className="flex items-center gap-xs text-small text-light-text-secondary dark:text-dark-text-secondary">
                        <Calendar className="w-4 h-4 flex-shrink-0" />
                        {exp.duration}
                      </div>
                      <div className="flex items-center gap-xs text-small text-light-text-secondary dark:text-dark-text-secondary">
                        <MapPin className="w-4 h-4 flex-shrink-0" />
                        {exp.location}
                      </div>
                    </div>
                  </div>

                  <div className="md:col-span-2">
                    <p className="text-body text-light-text-secondary dark:text-dark-text-secondary mb-md">
                      {exp.description}
                    </p>
                    <div className="space-y-2xs">
                      {exp.achievements.map((achievement, aIdx) => (
                        <div
                          key={aIdx}
                          className="flex gap-xs text-small text-light-text-secondary dark:text-dark-text-secondary"
                        >
                          <span className="text-light-primary dark:text-dark-primary flex-shrink-0 mt-1">
                            ✓
                          </span>
                          <span>{achievement}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
