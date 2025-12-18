import { NextResponse } from "next/server";

const GITHUB_USER = "Kheang1409";
const API_VERSION = "2022-11-28";

interface Repo {
  name: string;
  description: string | null;
  html_url: string;
  homepage: string | null;
  stargazers_count: number;
  language: string | null;
  topics?: string[];
  languages_url: string;
  fork: boolean;
  pushed_at: string;
}

interface Project {
  title: string;
  description: string;
  tech: string[];
  github: string;
  demo: string;
  featured: boolean;
  stars: number;
}

const baseHeaders: HeadersInit = {
  Accept: "application/vnd.github+json",
  "X-GitHub-Api-Version": API_VERSION,
  "User-Agent": process.env.GITHUB_USER_AGENT ?? "kai-portfolio-site",
};

if (process.env.GITHUB_TOKEN) {
  baseHeaders.Authorization = `Bearer ${process.env.GITHUB_TOKEN}`;
}

async function fetchProjects(): Promise<Project[]> {
  const res = await fetch(
    `https://api.github.com/users/${GITHUB_USER}/repos?per_page=20&sort=updated`,
    {
      headers: baseHeaders,
      // Cache at the edge to ease rate limits, still refreshed hourly.
      next: { revalidate: 3600 },
    }
  );

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(text || `GitHub request failed (${res.status})`);
  }

  const data = (await res.json()) as Repo[];

  const filtered = data
    .filter((repo) => !repo.fork)
    .sort((a, b) =>
      b.stargazers_count === a.stargazers_count
        ? new Date(b.pushed_at).getTime() - new Date(a.pushed_at).getTime()
        : b.stargazers_count - a.stargazers_count
    );

  const languageLists = await Promise.all(
    filtered.map(async (repo) => {
      try {
        const langRes = await fetch(repo.languages_url, {
          headers: baseHeaders,
        });
        if (!langRes.ok) throw new Error("lang fetch failed");
        const langData = (await langRes.json()) as Record<string, number>;
        return Object.keys(langData);
      } catch {
        return repo.language ? [repo.language] : [];
      }
    })
  );

  const dotnetFiltered = filtered
    .map((repo, idx) => ({ repo, langs: languageLists[idx] }))
    .filter(({ repo, langs }) => {
      const combined = [...langs, repo.language ?? ""]
        .filter(Boolean)
        .map((l) => l.toLowerCase());
      return combined.some(
        (l) =>
          l.includes("c#") ||
          l.includes("csharp") ||
          l.includes(".net") ||
          l.includes("javascript") ||
          l.includes("typescript") ||
          l.includes("js") ||
          l.includes("ts")
      );
    })
    .slice(0, 8);

  return dotnetFiltered.map(({ repo, langs }, idx) => ({
    title: repo.name,
    description:
      repo.description ??
      "No description provided. Pulling details from GitHub.",
    tech: langs.length
      ? langs.slice(0, 6)
      : repo.language
      ? [repo.language]
      : [],
    github: repo.html_url,
    demo:
      repo.homepage && repo.homepage.trim() !== ""
        ? repo.homepage
        : repo.html_url,
    featured: idx < 2,
    stars: repo.stargazers_count,
  }));
}

export async function GET() {
  try {
    const projects = await fetchProjects();
    return NextResponse.json(
      { projects },
      {
        status: 200,
        headers: {
          "Cache-Control": "s-maxage=3600, stale-while-revalidate=1800",
        },
      }
    );
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Unable to fetch GitHub data";
    return NextResponse.json(
      { error: message },
      {
        status: 503,
        headers: { "Cache-Control": "no-store" },
      }
    );
  }
}
