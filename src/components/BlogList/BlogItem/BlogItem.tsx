import styles from "./BlogItem.module.css";
import type { BlogPost } from "../../../data/data";
import BlogDetail from "../BlogDetail/BlogDetail";

type Props = {
  blog: BlogPost;
};

export default function BlogItem({ blog }: Props) {
  const href = blog.link ?? blog.url ?? "#";

  return (
    <div className={styles.cardLink}>
      <BlogDetail post={blog} variant="preview" href={href} />
    </div>
  );
}
