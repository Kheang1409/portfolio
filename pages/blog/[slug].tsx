import fs from "fs";
import path from "path";
import { GetStaticProps, GetStaticPaths } from "next";
import matter from "gray-matter";
import ReactMarkdown from "react-markdown";
import rehypeHighlight from "rehype-highlight";
import remarkGfm from "remark-gfm";

export default function PostPage({ frontmatter, content }: any) {
  return (
    <main className="container" style={{ padding: "48px 20px" }}>
      <article>
        <h1>{frontmatter?.title}</h1>
        <p>{frontmatter?.date}</p>
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          rehypePlugins={[rehypeHighlight as any]}
        >
          {content}
        </ReactMarkdown>
      </article>
    </main>
  );
}

export const getStaticPaths: GetStaticPaths = async () => {
  const postsDir = path.join(process.cwd(), "src", "data", "blogs");
  const paths: { params: { slug: string } }[] = [];

  try {
    const files = fs.readdirSync(postsDir);
    for (const file of files) {
      if (!file.endsWith(".md")) continue;
      const raw = fs.readFileSync(path.join(postsDir, file), "utf8");
      const { data } = matter(raw);
      const slug = data.slug ?? file.replace(/\.md$/, "");
      paths.push({ params: { slug } });
    }
  } catch (err) {}

  return {
    paths,
    fallback: false,
  };
};

export const getStaticProps: GetStaticProps = async ({ params }) => {
  const slug = params?.slug as string;
  const postsDir = path.join(process.cwd(), "src", "data", "blogs");
  try {
    const files = fs.readdirSync(postsDir);
    for (const file of files) {
      if (!file.endsWith(".md")) continue;
      const full = path.join(postsDir, file);
      const raw = fs.readFileSync(full, "utf8");
      const { data, content } = matter(raw);
      const fileSlug = data.slug ?? file.replace(/\.md$/, "");
      if (fileSlug === slug) {
        return {
          props: {
            frontmatter: data,
            content,
          },
        };
      }
    }
  } catch (err) {}

  return {
    notFound: true,
  };
};
