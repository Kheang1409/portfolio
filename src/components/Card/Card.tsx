import React, { PropsWithChildren, useState, KeyboardEvent } from "react";
import styles from "./Card.module.css";

type CardProps = PropsWithChildren<{
  front?: React.ReactNode;
  back?: React.ReactNode;
}>;

export default function Card({ children, front, back }: CardProps) {
  const [flipped, setFlipped] = useState(false);

  const hasFlip = Boolean(front) || Boolean(back);

  function toggleFlip() {
    if (!hasFlip) return;
    setFlipped((v) => !v);
  }

  function onKey(e: KeyboardEvent) {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      toggleFlip();
    }
  }

  if (!hasFlip) {
    return <div className={styles.card}>{children}</div>;
  }

  return (
    <div
      className={`${styles.card} ${flipped ? styles.isFlipped : ""}`}
      onClick={toggleFlip}
      onKeyDown={onKey}
      role="button"
      tabIndex={0}
      aria-pressed={flipped}
    >
      <div className={styles.inner}>
        <div className={`${styles.face} ${styles.front}`}>{front}</div>
        <div className={`${styles.face} ${styles.back}`}>{back}</div>
      </div>
    </div>
  );
}
