import fs from "fs";
import path from "path";
import { GetStaticProps } from "next";
import BlogList from "../../src/components/BlogList/BlogList";
import matter from "gray-matter";

type Blog = {
  id?: number;
  title: string;
  description?: string;
  link: string;
  date?: string;
};

export default function BlogIndex({ posts }: { posts: Blog[] }) {
  return (
    <main>
      <BlogList blogs={posts} />
    </main>
  );
}

export const getStaticProps: GetStaticProps = async () => {
  const postsDir = path.join(process.cwd(), "src", "data", "blogs");
  let localPosts: Blog[] = [];

  try {
    const files = fs.readdirSync(postsDir);
    for (const file of files) {
      if (!file.endsWith(".md")) continue;
      const full = path.join(postsDir, file);
      const raw = fs.readFileSync(full, "utf8");
      const { data, content } = matter(raw);
      const slug = data.slug ?? file.replace(/\.md$/, "");
      const description = data.description ?? (content.split("\n\n")[0] || "");
      localPosts.push({
        title: data.title || slug,
        description,
        link: `/blog/${slug}`,
        date: data.date,
      });
    }
  } catch (err) {
    localPosts = [];
  }

  let external: Blog[] = [];
  try {
    external = require("../../src/data/blogs.json");
  } catch (err) {
    external = [];
  }

  const localSlugs = new Set(
    localPosts.map((p) => p.link.replace("/blog/", ""))
  );

  const merged: Blog[] = [];

  for (const p of localPosts) merged.push(p);

  for (const e of external) {
    const href = (e as any).link ?? (e as any).url ?? "";
    const lastSeg = href.split("/").filter(Boolean).pop() || href;
    if (localSlugs.has(lastSeg)) continue;
    merged.push({
      id: e.id,
      title: e.title,
      description: e.description,
      link: href,
      date: e.date,
    });
  }

  return {
    props: {
      posts: merged,
    },
  };
};
