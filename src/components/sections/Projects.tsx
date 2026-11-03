"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import {
  FeaturedProjectCard,
  OtherProjectCard,
  type Project,
} from "./Projects/ProjectCard";
import type { Resume } from "@/lib/resume";

async function fetchRepos(): Promise<Project[]> {
  const res = await fetch("/api/github/projects", { cache: "no-store" });

  if (!res.ok) {
    const payload = (await res.json().catch(() => null)) as
      | { error?: string }
      | null;
    throw new Error(
      payload?.error || `GitHub request failed (${res.status})`,
    );
  }

  const payload = (await res.json()) as { projects?: Project[] };
  return payload.projects ?? [];
}

export default function Projects({ resume }: { resume: Resume | null }) {
  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  const [filter, setFilter] = useState("All work");
  const categoryMatchers: Record<string, RegExp> = {
    "Web Applications": /^(javascript|typescript|html|css)$/i,
    "Mobile Applications": /^(dart|kotlin|swift|objective-c)$/i,
    "UI/UX": /^(figma|sketch|design)$/i,
    "3D/Creative": /^(glsl|hlsl|shaderlab|three\.js)$/i,
    "Backend/API": /^(c#|csharp|python|java|go|rust|php|\.net)$/i,
  };
  const matches = (p: Project) =>
    filter === "All work" ||
    filter === "Personal Projects" ||
    p.tech.some((t) => categoryMatchers[filter]?.test(t));
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        const repos = await fetchRepos();
        if (mounted) {
          setProjects(repos);
        }
      } catch (e) {
        if (mounted) {
          setError(
            e instanceof Error
              ? e.message
              : "Unable to load GitHub projects right now.",
          );
        }
      } finally {
        if (mounted) setLoading(false);
      }
    }

    load();
    return () => {
      mounted = false;
    };
  }, []);

  const featured = projects.filter((p) => p.featured && matches(p));
  const others = projects.filter((p) => !p.featured && matches(p));
  const featuredDisplay = featured;
  const othersDisplay = others;

  return (
    <section
      id="projects"
      aria-labelledby="projects-heading"
      className="webgl-section system-section theme-mark50"
    >
      <div className="section-shell relative z-10 max-w-container mx-auto px-sm md:px-lg">
        <motion.div
          initial={false}
          whileInView="visible"
          variants={fadeInVariants}
          transition={{ duration: 0.5 }}
          viewport={{ once: true, margin: "-100px" }}
          className="mb-3xl"
        >
          <div className="section-title-lockup">
            <h2
              id="projects-heading"
              className="text-h2 font-bold text-light-text-primary dark:text-dark-text-primary mb-sm"
            >
              Selected work
            </h2>
            <div className="section-level-divider w-full h-2 bg-light-primary dark:bg-dark-primary" />
          </div>
        </motion.div>

        {!!resume?.projects?.length && <><div className="projects-group-heading">
          <span>01</span>
          <div>
            <h3>Case studies</h3>
            <p>Selected projects and the problems behind them.</p>
          </div>
        </div>
        <div className="project-case-studies grid md:grid-cols-2 gap-lg mb-3xl">
          {resume.projects.map((project, idx) => (
            <motion.article
              key={project.name}
              initial={false}
              whileInView="visible"
              variants={fadeInVariants}
              transition={{ duration: 0.45, delay: idx * 0.08 }}
              viewport={{ once: true, margin: "-100px" }}
              className="project-case-study p-lg rounded-lg bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border"
            >
              <div className="flex items-center justify-between gap-md mb-sm">
                <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
                  {project.name}
                </h3>
              </div>
              <p className="text-body text-light-text-secondary dark:text-dark-text-secondary leading-relaxed">
                {project.description}
              </p>
            </motion.article>
          ))}
        </div></>}

        <div className="projects-group-heading">
          <span>02</span>
          <div>
            <h3>Featured projects</h3>
            <p>A few projects from my GitHub profile.</p>
          </div>
        </div>
        <div
          className="project-filters"
          role="group"
          aria-label="Filter repositories"
        >
          {[
            "All work",
            ...Object.keys(categoryMatchers),
            "Personal Projects",
          ].map((category) => (
            <button
              key={category}
              aria-pressed={filter === category}
              onClick={() => setFilter(category)}
            >
              {category}
            </button>
          ))}
        </div>
        {!loading && !error && !featured.length && !others.length && (
          <p role="status">No repositories in this category yet.</p>
        )}
        <div className="project-featured-grid grid lg:grid-cols-2 gap-lg mb-3xl">
          {featuredDisplay.map((project, idx) => (
            <FeaturedProjectCard key={idx} project={project} index={idx} />
          ))}
        </div>

        {error && (
          <div
            aria-live="polite"
            className="mb-lg text-red-600 dark:text-red-400"
          >
            {error}{" "}
            <a href="/api/github/projects">View project feed</a>
          </div>
        )}

        <motion.div
          initial={false}
          whileInView="visible"
          variants={fadeInVariants}
          transition={{ duration: 0.5, delay: 0.2 }}
          viewport={{ once: true, margin: "-100px" }}
          className="projects-group-heading mb-xl"
        >
          <span>03</span>
          <div>
            <h3>More projects</h3>
            <p>Additional experiments and open-source work.</p>
          </div>
        </motion.div>

        <div className="project-repository-grid grid md:grid-cols-2 xl:grid-cols-3 gap-lg">
          {othersDisplay.map((project, idx) => (
            <OtherProjectCard key={idx} project={project} index={idx} />
          ))}
        </div>
      </div>
    </section>
  );
}
