"use client";

import { Calendar, MapPin } from "lucide-react";
import { motion } from "framer-motion";

const experiences = [
  {
    company: "Sahakrinpheap Microfinance PLC",
    role: "Software Engineer",
    duration: "Sep 2021 - May 2024",
    location: "Phnom Penh, Cambodia",
    description:
      "Re-architected legacy banking modules into full-stack microservices using C#/.NET Core backend services with React/Angular frontends for reliable financial operations.",
    achievements: [
      "Cut system crashes by 40% and reduced maintenance by 20+ hours/month through microservice re-architecture",
      "Built event-driven monitoring and alerting services on Azure, improving incident response by 50% and sustaining 99% uptime",
      "Optimized SQL Server and PostgreSQL reporting pipelines from minutes to 2 seconds",
      "Designed and automated payroll and attendance backend services, reducing processing time by 7 days",
      "Implemented OAuth2, JWT, and RBAC with zero unauthorized access incidents",
      "Mentored engineers and led code reviews, reducing onboarding time by 30% and lowering defect rates",
    ],
  },
  {
    company: "Pathmazing Inc",
    role: "Software Engineer",
    duration: "Mar 2021 - Sep 2021",
    location: "Phnom Penh, Cambodia",
    description:
      "Designed secure payment APIs and ERP integrations with C# .NET backend and React frontend for higher reliability and performance.",
    achievements: [
      "Increased transaction reliability by 30% through secure payment API integration",
      "Developed and scaled backend services and ERP integrations to reduce manual workflows",
      "Optimized database queries and upgraded core components, improving performance by 20%",
      "Improved reporting pipelines from 30 minutes to 5 minutes",
      "Contributed backend support to fundraising platform enabling $70.8K raised across 30+ organizations",
    ],
  },
  {
    company: "Anakut Digital Solution, Co. Ltd",
    role: "Software Engineer",
    duration: "Oct 2020 - May 2021",
    location: "Phnom Penh, Cambodia",
    description:
      "Built scalable backend systems for POS and management platforms with ERP integration and real-time communication features.",
    achievements: [
      "Eliminated 99% of manual data entry errors by delivering scalable backend systems",
      "Delivered ERP-integrated backend services enabling real-time tracking and operational reporting",
      "Designed booking and scheduling services that reduced processing time by 50%",
      "Implemented SignalR real-time communication features, reducing user wait times by 45%",
      "Applied modular architecture and design patterns to improve maintainability and scalability",
    ],
  },
  {
    company: "Arrow Dot",
    role: "Software Engineer",
    duration: "Oct 2019 - Oct 2020",
    location: "Phnom Penh, Cambodia",
    description:
      "Developed and deployed web applications with CI/CD pipelines, Docker containers, and Kubernetes to improve release reliability.",
    achievements: [
      "Improved deployment reliability and reduced manual release effort with automated CI/CD",
      "Enhanced backend services and database performance, increasing overall system efficiency by about 30%",
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
