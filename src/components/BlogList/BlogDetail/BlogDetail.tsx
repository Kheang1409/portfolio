import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import type { Pluggable } from "unified";
import styles from "./BlogDetail.module.css";
import type { BlogPost } from "../../../data/data";

type Props = {
  post: BlogPost | (Partial<BlogPost> & { title?: string });
  content?: string;
  variant?: "preview" | "full";
  href?: string;
};

export default function BlogDetail({
  post,
  content,
  variant = "preview",
  href,
}: Props) {
  const date = post.date ?? null;
  const title = post.title ?? "Untitled";
  const description = post.description ?? "";

  if (variant === "preview") {
    const linkHref = href ?? post.link ?? post.url ?? "#";
    return (
      <a className={styles.previewCard} href={linkHref} role="article">
        <div className={styles.previewHeader}>
          {post.image && (
            <img src={post.image} alt={title} className={styles.thumb} />
          )}
          <div>
            <h4 className={styles.title}>{title}</h4>
            {date && <div className={styles.meta}>{date}</div>}
          </div>
        </div>
        {description && <p className={styles.description}>{description}</p>}
      </a>
    );
  }

  // full article
  return (
    <article className={styles.fullArticle}>
      <header className={styles.articleHeader}>
        <h1 className={styles.articleTitle}>{title}</h1>
        {date && <div className={styles.articleMeta}>{date}</div>}
      </header>
      <div className={styles.articleContent}>
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          rehypePlugins={[rehypeHighlight as Pluggable]}
        >
          {content ?? post.description ?? ""}
        </ReactMarkdown>
      </div>
    </article>
  );
}
