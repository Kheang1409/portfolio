import styles from "./BlogItem.module.css";

type Props = {
  blog: any;
  index?: number;
};

export default function BlogItem({ blog, index }: Props) {
  const href = blog.link ?? blog.url ?? "#";

  const titleId = `blog-title-${blog.id ?? index}`;

  const content = (
    <div className={styles.cardContent}>
      <h4 id={titleId} className={styles.cardTitle}>
        {blog.title}
      </h4>
      {blog.image && (
        <img src={blog.image} alt={blog.title} className={styles.cardImage} />
      )}
      <p className={styles.cardDescription}>{blog.description}</p>
    </div>
  );

  return (
    <a
      href={href}
      className={`blog-card-link ${styles.cardLink}`}
      role="article"
      aria-labelledby={titleId}
      title={blog.title + (blog.description ? ` — ${blog.description}` : "")}
      target="_blank"
      rel="noopener noreferrer"
    >
      {content}
    </a>
  );
}
