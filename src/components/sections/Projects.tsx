"use client";

import { useEffect, useMemo, useState } from "react";
import { motion } from "framer-motion";
import {
  FeaturedProjectCard,
  OtherProjectCard,
  type Project,
} from "./Projects/ProjectCard";

async function fetchRepos(): Promise<Project[]> {
  const res = await fetch("/api/github/projects", { cache: "no-store" });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(text || `GitHub request failed (${res.status})`);
  }

  const payload = (await res.json()) as { projects?: Project[] };
  return payload.projects ?? [];
}

export default function Projects() {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const featuredSkeleton: Project[] = Array.from({ length: 2 }).map(() => ({
    title: "Loading...",
    description: "Fetching latest repositories...",
    tech: ["..."],
    github: "#",
    demo: "#",
    featured: true,
    stars: 0,
  }));

  const othersSkeleton: Project[] = Array.from({ length: 6 }).map(() => ({
    title: "Loading...",
    description: "Fetching repository details...",
    tech: ["..."],
    github: "#",
    demo: "#",
    featured: false,
    stars: 0,
  }));

  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        const repos = await fetchRepos();
        if (mounted) {
          setProjects(repos);
        }
      } catch (e) {
        if (mounted) setError("Unable to load GitHub projects right now.");
      } finally {
        if (mounted) setLoading(false);
      }
    }

    load();
    return () => {
      mounted = false;
    };
  }, []);

  const featured = useMemo(
    () => projects.filter((p) => p.featured),
    [projects]
  );
  const others = useMemo(() => projects.filter((p) => !p.featured), [projects]);
  const featuredDisplay = loading ? featuredSkeleton : featured;
  const othersDisplay = loading ? othersSkeleton : others;

  return (
    <section
      id="projects"
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
            Featured Projects
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full" />
        </motion.div>

        <div className="grid md:grid-cols-2 gap-lg mb-3xl">
          {featuredDisplay.map((project, idx) => (
            <FeaturedProjectCard key={idx} project={project} index={idx} />
          ))}
        </div>

        {error && (
          <div className="mb-lg text-red-600 dark:text-red-400">{error}</div>
        )}

        <motion.div
          initial="hidden"
          whileInView="visible"
          variants={fadeInVariants}
          transition={{ duration: 0.5, delay: 0.2 }}
          viewport={{ once: true, margin: "-100px" }}
          className="mb-xl"
        >
          <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary mb-lg">
            Other Projects
          </h3>
        </motion.div>

        <div className="grid md:grid-cols-3 gap-lg">
          {othersDisplay.map((project, idx) => (
            <OtherProjectCard key={idx} project={project} index={idx} />
          ))}
        </div>
      </div>
    </section>
  );
}
