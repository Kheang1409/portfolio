"use client";

import {
  Code2,
  Server,
  Cloud,
  Database,
  Zap,
  GitBranch,
  Box,
  Boxes,
  Shield,
  Terminal,
} from "lucide-react";
import { motion } from "framer-motion";

const skillsData = [
  {
    category: "Backend & Architecture",
    icon: Server,
    skills: [
      { name: "C#", level: 90 },
      { name: ".NET", level: 90 },
      { name: "ASP.NET Core", level: 88 },
      { name: "Microservices", level: 85 },
    ],
  },
  {
    category: "Messaging & APIs",
    icon: Zap,
    skills: [
      { name: "REST APIs", level: 90 },
      { name: "Kafka", level: 80 },
      { name: "gRPC", level: 78 },
      { name: "SignalR", level: 75 },
    ],
  },
  {
    category: "Databases",
    icon: Database,
    skills: [
      { name: "SQL Server", level: 88 },
      { name: "PostgreSQL", level: 80 },
      { name: "MongoDB", level: 82 },
      { name: "Redis", level: 78 },
    ],
  },
  {
    category: "Cloud & DevOps",
    icon: Cloud,
    skills: [
      { name: "Docker", level: 85 },
      { name: "Kubernetes", level: 80 },
      { name: "AWS", level: 80 },
      { name: "Azure", level: 80 },
    ],
  },
  {
    category: "Frontend",
    icon: Code2,
    skills: [
      { name: "Angular", level: 82 },
      { name: "React", level: 78 },
      { name: "TypeScript", level: 80 },
      { name: "Tailwind CSS", level: 80 },
    ],
  },
  {
    category: "Testing & Security",
    icon: Shield,
    skills: [
      { name: "xUnit", level: 80 },
      { name: "NUnit", level: 78 },
      { name: "Swagger", level: 85 },
      { name: "JWT", level: 85 },
    ],
  },
];

const skillIcons: { [key: string]: React.ReactNode } = {
  // Frontend
  Angular: <Boxes className="w-4 h-4" />,
  React: <Code2 className="w-4 h-4" />,
  TypeScript: <Terminal className="w-4 h-4" />,
  "Tailwind CSS": <Zap className="w-4 h-4" />,

  // Backend & Architecture
  "C#": <Code2 className="w-4 h-4" />,
  ".NET": <Server className="w-4 h-4" />,
  "ASP.NET Core": <Server className="w-4 h-4" />,
  Microservices: <Server className="w-4 h-4" />,

  // Messaging & APIs
  "REST APIs": <Shield className="w-4 h-4" />,
  Kafka: <Zap className="w-4 h-4" />,
  gRPC: <Zap className="w-4 h-4" />,
  SignalR: <Zap className="w-4 h-4" />,

  // Databases
  "SQL Server": <Database className="w-4 h-4" />,
  PostgreSQL: <Database className="w-4 h-4" />,
  MongoDB: <Database className="w-4 h-4" />,
  Redis: <Database className="w-4 h-4" />,

  // Cloud & DevOps
  Docker: <Box className="w-4 h-4" />,
  Kubernetes: <Boxes className="w-4 h-4" />,
  Azure: <Cloud className="w-4 h-4" />,
  AWS: <Cloud className="w-4 h-4" />,
  "CI/CD": <Zap className="w-4 h-4" />,
  Git: <GitBranch className="w-4 h-4" />,

  // Testing & Security
  xUnit: <Shield className="w-4 h-4" />,
  NUnit: <Shield className="w-4 h-4" />,
  Swagger: <Shield className="w-4 h-4" />,
  JWT: <Shield className="w-4 h-4" />,
};

export default function Skills() {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <section
      id="skills"
      className="py-4xl md:py-[100px] bg-light-background dark:bg-dark-background"
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
            Skills & Expertise
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full" />
        </motion.div>

        <div className="grid md:grid-cols-2 gap-2xl">
          {skillsData.map((category, categoryIdx) => {
            const CategoryIcon = category.icon;
            return (
              <motion.div
                key={categoryIdx}
                initial="hidden"
                whileInView="visible"
                variants={fadeInVariants}
                transition={{ duration: 0.5, delay: categoryIdx * 0.1 }}
                viewport={{ once: true, margin: "-100px" }}
                className="p-lg rounded-lg bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border hover:border-light-primary/50 dark:hover:border-dark-primary/50 transition-all"
              >
                <div className="flex items-center gap-sm mb-lg">
                  <div className="p-2xs rounded-md bg-light-primary/10 dark:bg-dark-primary/10 text-light-primary dark:text-dark-primary">
                    <CategoryIcon className="w-5 h-5" />
                  </div>
                  <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
                    {category.category}
                  </h3>
                </div>

                <div className="space-y-md">
                  {category.skills.map((skill, skillIdx) => (
                    <motion.div
                      key={skillIdx}
                      initial="hidden"
                      whileInView="visible"
                      variants={fadeInVariants}
                      transition={{
                        duration: 0.4,
                        delay: categoryIdx * 0.1 + skillIdx * 0.05,
                      }}
                      viewport={{ once: true, margin: "-100px" }}
                    >
                      <div className="flex items-center justify-between mb-xs">
                        <div className="flex items-center gap-2xs">
                          <span className="text-light-accent dark:text-dark-accent">
                            {skillIcons[skill.name] || (
                              <Code2 className="w-4 h-4" />
                            )}
                          </span>
                          <span className="text-small font-medium text-light-text-primary dark:text-dark-text-primary">
                            {skill.name}
                          </span>
                        </div>
                        <span className="text-small text-light-text-secondary dark:text-dark-text-secondary">
                          {skill.level}%
                        </span>
                      </div>
                      <div className="w-full h-2 bg-light-border dark:bg-dark-border rounded-full overflow-hidden">
                        <motion.div
                          className="h-full bg-gradient-to-r from-light-primary to-light-accent dark:from-dark-primary dark:to-dark-accent rounded-full"
                          initial={{ width: "0%" }}
                          whileInView={{ width: `${skill.level}%` }}
                          transition={{
                            duration: 0.8,
                            ease: "easeOut",
                            delay: categoryIdx * 0.1 + skillIdx * 0.05,
                          }}
                          viewport={{ once: false, amount: 0.3 }}
                        />
                      </div>
                    </motion.div>
                  ))}
                </div>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
