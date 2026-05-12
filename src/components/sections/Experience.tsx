"use client";

import { Calendar, MapPin } from "lucide-react";
import { motion } from "framer-motion";

const experiences = [
  {
    company: "Better & Best Inc.",
    role: "Software Engineer",
    duration: "Apr 2026 - Present",
    location: "Oregon, WI, USA",
    description:
      "Designing and developing backend services and internal applications supporting POS, billing, and order management systems.",
    achievements: [
      "Build RESTful APIs enabling real-time data processing and system integration across operational workflows",
      "Optimize database queries and schema design to improve response time and system reliability under daily transaction loads",
      "Refactor legacy components into modular, scalable services to improve maintainability and extensibility",
      "Troubleshoot and resolve production issues to ensure high availability and system stability",
      "Collaborate with stakeholders to translate business requirements into scalable technical solutions",
    ],
  },
  {
    company: "Sahakrinpheap Microfinance PLC",
    role: "Software Engineer",
    duration: "Sep 2021 - May 2024",
    location: "Phnom Penh, Cambodia",
    description:
      "Re-architected legacy banking systems into scalable microservices using C# and ASP.NET Core, developing RESTful APIs and improving maintainability.",
    achievements: [
      "Re-architected legacy banking systems into scalable microservices using C# and ASP.NET Core, developing RESTful APIs and reducing system crashes by 40% while improving maintainability",
      "Built and maintained event-driven monitoring and alerting systems on Azure, improving incident response time by 50% and achieving 99% uptime",
      "Optimized SQL Server and PostgreSQL databases by refining complex queries, stored procedures, and indexing strategies, reducing execution time from minutes to 2 seconds",
      "Designed and developed backend services for payroll and attendance systems, automating workflows and reducing processing time by 7 days",
      "Implemented secure authentication and authorization using OAuth2, JWT, and RBAC",
    ],
  },
  {
    company: "Pathmazing Inc.",
    role: "Software Engineer",
    duration: "Mar 2021 - Sep 2021",
    location: "Phnom Penh, Cambodia",
    description:
      "Designed and integrated secure RESTful APIs with third-party payment systems, improving transaction reliability.",
    achievements: [
      "Designed and integrated secure RESTful APIs with third-party payment systems, improving transaction reliability by 30%",
      "Developed and maintained backend services using C# and .NET, supporting ERP integrations and reducing manual operational workflows",
      "Optimized SQL queries and database performance, improving system efficiency by 20%",
    ],
  },
  {
    company: "Anakut Digital Solution, Co. Ltd",
    role: "Software Engineer",
    duration: "Oct 2020 - May 2021",
    location: "Phnom Penh, Cambodia",
    description:
      "Developed scalable backend systems for POS and management platforms, reducing manual data entry errors.",
    achievements: [
      "Developed scalable backend systems for POS and management platforms, reducing manual data entry errors by 99%",
      "Built and maintained RESTful APIs for ERP-integrated systems, enabling real-time data tracking and reporting",
    ],
  },
  {
    company: "Arrow Dot",
    role: "Software Engineer",
    duration: "Oct 2019 - Oct 2020",
    location: "Phnom Penh, Cambodia",
    description:
      "Developed and deployed web applications using CI/CD pipelines, Docker, and Kubernetes to improve release reliability.",
    achievements: [
      "Developed and deployed web applications using CI/CD pipelines, Docker, and Kubernetes, improving deployment reliability and reducing manual release effort",
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
      aria-labelledby="experience-heading"
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
            id="experience-heading"
            className="text-h2 font-bold text-light-text-primary dark:text-dark-text-primary mb-sm"
          >
            Experience
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full" />
        </motion.div>

        <div className="relative">
          <div className="absolute left-[6px] top-0 bottom-0 w-[2px] bg-light-primary/30 dark:bg-dark-primary/30 md:hidden" />

          <div className="space-y-lg">
            {experiences.map((exp, idx) => (
              <motion.article
                key={`${exp.company}-${exp.role}`}
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
              </motion.article>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
