"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import {
  FeaturedProjectCard,
  OtherProjectCard,
  type Project,
} from "./Projects/ProjectCard";

const resumeProjects = [
  {
    title: "AI-Powered Portfolio",
    timeframe: "Feb 2025",
    context: "Personal",
    summary:
      "Built and integrated AI-powered features using LLM APIs for dynamic content and richer user interaction, improving session duration by 35% for 150+ weekly users.",
  },
  {
    title: "Angkor Milk Meal App",
    timeframe: "Sep 2023",
    context: "AngkorMilk (Freelance)",
    summary:
      "Collaborated with 500+ users to optimize reporting queries and deliver faster operational insights.",
  },
];

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
              Selected systems
            </h2>
            <div className="section-level-divider w-full h-2 bg-light-primary dark:bg-dark-primary" />
          </div>
        </motion.div>

        <div className="projects-group-heading">
          <span>01</span>
          <div>
            <h3>Selected case studies</h3>
            <p>Products with measurable business and user impact.</p>
          </div>
        </div>
        <div className="project-case-studies grid md:grid-cols-2 gap-lg mb-3xl">
          {resumeProjects.map((project, idx) => (
            <motion.article
              key={project.title}
              initial={false}
              whileInView="visible"
              variants={fadeInVariants}
              transition={{ duration: 0.45, delay: idx * 0.08 }}
              viewport={{ once: true, margin: "-100px" }}
              className="project-case-study p-lg rounded-lg bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border"
            >
              <div className="flex items-center justify-between gap-md mb-sm">
                <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
                  {project.title}
                </h3>
                <span className="text-small font-medium text-light-accent dark:text-dark-accent whitespace-nowrap">
                  {project.timeframe}
                </span>
              </div>
              <p className="text-small font-medium text-light-text-secondary dark:text-dark-text-secondary mb-sm">
                {project.context}
              </p>
              <p className="text-body text-light-text-secondary dark:text-dark-text-secondary leading-relaxed">
                {project.summary}
              </p>
            </motion.article>
          ))}
        </div>

        <div className="projects-group-heading">
          <span>02</span>
          <div>
            <h3>Featured builds</h3>
            <p>Highlighted repositories from my current engineering work.</p>
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
            <a href="https://github.com/Kheang1409">View projects on GitHub</a>
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
            <h3>More repositories</h3>
            <p>Additional experiments, services, and open-source work.</p>
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
