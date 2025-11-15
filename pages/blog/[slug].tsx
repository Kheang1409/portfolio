import { GetStaticPaths, GetStaticProps } from "next";
import Head from "next/head";
import BlogDetail from "../../src/components/BlogList/BlogDetail/BlogDetail";
import type { BlogPost } from "../../src/data/data";

// Load blog index once at module load to avoid repeated requires during build
let externalBlogs: BlogPost[] = [];
try {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  externalBlogs = require("../../src/data/blogs.json") as BlogPost[];
} catch (err) {
  externalBlogs = [];
}

export default function PostPage({
  post,
  content,
}: {
  post: BlogPost;
  content?: string;
}) {
  const slug =
    post.slug ?? (post.link ?? post.url ?? "").split("/").filter(Boolean).pop();
  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? "";
  const canonical = siteUrl
    ? `${siteUrl.replace(/\/$/, "")}/blog/${slug}`
    : `/blog/${slug}`;

  return (
    <>
      <Head>
        <title>{post.title}</title>
        <meta name="description" content={post.description ?? ""} />
        <meta property="og:type" content="article" />
        <meta property="og:title" content={post.title} />
        <meta property="og:description" content={post.description ?? ""} />
        {post.image && <meta property="og:image" content={post.image} />}
        <meta property="og:url" content={canonical} />
      </Head>
      <main className="container" style={{ padding: "48px 20px" }}>
        <BlogDetail post={post ?? {}} content={content} variant="full" />
      </main>
    </>
  );
}

export const getStaticPaths: GetStaticPaths = async () => {
  // Return no pre-rendered slug pages to speed up the build.
  // Pages will be generated on-demand when first requested (blocking).
  return {
    paths: [],
    fallback: "blocking",
  };
};

export const getStaticProps: GetStaticProps = async ({ params }) => {
  const slug = params?.slug as string;
  const match = externalBlogs.find((p) => {
    const s =
      p.slug ?? (p.link ?? p.url ?? "").split("/").filter(Boolean).pop();
    return s === slug;
  });

  if (!match) {
    return { notFound: true };
  }

  return {
    props: {
      post: match,
      content: match.content ?? match.description ?? "",
    },
    // Revalidate this page after 60 seconds so updates can appear without redeploy
    revalidate: 60,
  };
};
