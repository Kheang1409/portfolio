import fs from "fs";
import path from "path";
import { GetStaticProps } from "next";
import BlogList from "../../src/components/BlogList/BlogList";
import matter from "gray-matter";
import { BlogPost } from "../../src/data/data";

export default function BlogIndex({ posts }: { posts: BlogPost[] }) {
  return (
    <main>
      <BlogList blogs={posts} />
    </main>
  );
}

export const getStaticProps: GetStaticProps = async () => {
  const postsDir = path.join(process.cwd(), "src", "data", "blogs");
  let localPosts: BlogPost[] = [];

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

  let external: BlogPost[] = [];
  try {
    external = require("../../src/data/blogs.json") as BlogPost[];
  } catch (err) {
    external = [];
  }

  const localSlugs = new Set(
    localPosts.map((p) => (p.link ?? "").replace("/blog/", ""))
  );

  const merged: BlogPost[] = [];

  for (const p of localPosts) merged.push(p);

  for (const e of external) {
    const href = (e as BlogPost).link ?? (e as BlogPost).url ?? "";
    const lastSeg = href.split("/").filter(Boolean).pop() || href;
    if (localSlugs.has(lastSeg)) continue;
    merged.push({
      id: e.id,
      title: e.title,
      description: e.description,
      link: href,
      date: e.date,
      author: e.author,
      image: e.image,
      slug: (e as BlogPost).slug,
      content: (e as BlogPost).content,
      tags: (e as BlogPost).tags,
    });
  }

  merged.sort((a, b) => {
    const ta = a.date ? new Date(a.date).getTime() : 0;
    const tb = b.date ? new Date(b.date).getTime() : 0;
    return tb - ta;
  });

  // Normalize values to avoid returning `undefined` in static props
  const normalized = merged.map((p) => ({
    id: p.id ?? null,
    title: p.title,
    description: p.description ?? null,
    link: p.link ?? p.url ?? null,
    slug: p.slug ?? null,
    date: p.date ?? null,
    author: p.author ?? null,
    image: p.image ?? null,
    content: p.content ?? null,
    tags: p.tags ?? null,
  })) as unknown as BlogPost[];

  return {
    props: {
      posts: normalized,
    },
  };
};
