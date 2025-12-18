"use client";

import { Github, ExternalLink, Code2, Star } from "lucide-react";
import Link from "next/link";
import { motion } from "framer-motion";

export type Project = {
  title: string;
  description: string;
  tech: string[];
  github: string;
  demo: string;
  featured: boolean;
  stars: number;
};

type FeaturedProjectCardProps = {
  project: Project;
  index: number;
};

export function FeaturedProjectCard({
  project,
  index,
}: FeaturedProjectCardProps) {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <motion.div
      initial="hidden"
      whileInView="visible"
      variants={fadeInVariants}
      transition={{ duration: 0.5, delay: index * 0.1 }}
      viewport={{ once: true, margin: "-100px" }}
      className="group rounded-lg overflow-hidden bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border hover:border-light-primary/50 dark:hover:border-dark-primary/50 hover:shadow-lg transition-all duration-300"
    >
      <div className="h-48 bg-light-primary/10 dark:bg-dark-primary/10 flex items-center justify-center group-hover:bg-light-primary/20 dark:group-hover:bg-dark-primary/20 transition-colors">
        <Code2 className="w-16 h-16 text-light-primary/30 dark:text-dark-primary/30" />
      </div>

      <div className="p-lg space-y-sm">
        <div className="flex items-start justify-between gap-sm">
          <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
            {project.title}
          </h3>
          {project.stars > 0 && (
            <span className="inline-flex items-center gap-1 text-small text-light-text-secondary dark:text-dark-text-secondary">
              <Star className="w-4 h-4 text-light-accent dark:text-dark-accent" />
              {project.stars}
            </span>
          )}
        </div>

        <p className="text-body text-light-text-secondary dark:text-dark-text-secondary">
          {project.description}
        </p>

        <div className="flex flex-wrap gap-2xs">
          {project.tech.map((tech, tIdx) => (
            <span
              key={tIdx}
              className="px-xs py-2xs rounded text-small font-medium text-light-primary dark:text-dark-primary bg-light-primary/10 dark:bg-dark-primary/10"
            >
              {tech}
            </span>
          ))}
        </div>

        <div className="flex gap-sm pt-sm">
          <Link
            href={project.github}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 flex items-center justify-center gap-2xs px-md py-2xs rounded-md bg-light-primary/10 dark:bg-dark-primary/10 text-light-primary dark:text-dark-primary hover:bg-light-primary/20 dark:hover:bg-dark-primary/20 transition-colors font-medium text-small"
          >
            <Github className="w-4 h-4" />
            Code
          </Link>
          <Link
            href={project.demo}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 flex items-center justify-center gap-2xs px-md py-2xs rounded-md bg-light-primary dark:bg-dark-primary text-white hover:shadow-md transition-all font-medium text-small"
          >
            <ExternalLink className="w-4 h-4" />
            Demo
          </Link>
        </div>
      </div>
    </motion.div>
  );
}

type OtherProjectCardProps = {
  project: Project;
  index: number;
};

export function OtherProjectCard({ project, index }: OtherProjectCardProps) {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  return (
    <motion.div
      initial="hidden"
      whileInView="visible"
      variants={fadeInVariants}
      transition={{ duration: 0.5, delay: index * 0.1 }}
      viewport={{ once: true, margin: "-100px" }}
      className="group rounded-lg bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border hover:border-light-primary/50 dark:hover:border-dark-primary/50 hover:shadow-lg transition-all duration-300 p-lg"
    >
      <div className="flex items-start justify-between gap-sm mb-md">
        <h4 className="text-h4 font-semibold text-light-text-primary dark:text-dark-text-primary">
          {project.title}
        </h4>
        {project.stars > 0 && (
          <span className="inline-flex items-center gap-1 text-small text-light-text-secondary dark:text-dark-text-secondary flex-shrink-0">
            <Star className="w-4 h-4 text-light-accent dark:text-dark-accent" />
            {project.stars}
          </span>
        )}
      </div>

      <p className="text-small text-light-text-secondary dark:text-dark-text-secondary mb-md line-clamp-2">
        {project.description}
      </p>

      <div className="flex flex-wrap gap-2xs mb-md">
        {project.tech.slice(0, 3).map((tech, tIdx) => (
          <span
            key={tIdx}
            className="px-xs py-2xs rounded text-2xs font-medium text-light-primary dark:text-dark-primary bg-light-primary/10 dark:bg-dark-primary/10"
          >
            {tech}
          </span>
        ))}
        {project.tech.length > 3 && (
          <span className="text-2xs text-light-text-secondary dark:text-dark-text-secondary">
            +{project.tech.length - 3}
          </span>
        )}
      </div>

      <div className="flex gap-md">
        <Link
          href={project.github}
          target="_blank"
          rel="noopener noreferrer"
          className="flex-1 flex items-center justify-center gap-1 px-md py-2xs rounded-md bg-light-primary/10 dark:bg-dark-primary/10 text-light-primary dark:text-dark-primary hover:bg-light-primary/20 dark:hover:bg-dark-primary/20 transition-colors font-medium text-small"
        >
          <Github className="w-4 h-4" />
          <span className="hidden sm:inline">Code</span>
        </Link>
        <Link
          href={project.demo}
          target="_blank"
          rel="noopener noreferrer"
          className="flex-1 flex items-center justify-center gap-1 px-md py-2xs rounded-md bg-light-primary dark:bg-dark-primary text-white hover:shadow-md transition-all font-medium text-small"
        >
          <ExternalLink className="w-4 h-4" />
          <span className="hidden sm:inline">Demo</span>
        </Link>
      </div>
    </motion.div>
  );
}
