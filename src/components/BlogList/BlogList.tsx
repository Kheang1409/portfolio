import React, { useRef, useEffect, useState } from "react";
import styles from "./BlogList.module.css";
import BlogItem from "./BlogItem/BlogItem";
import { blogs as defaultBlogs, BlogPost } from "../../data/data";

type Props = {
  blogs?: BlogPost[];
};

export default function BlogList({ blogs }: Props) {
  const rowRef = useRef<HTMLDivElement | null>(null);
  const isDownRef = useRef(false);
  const startXRef = useRef(0);
  const scrollLeftRef = useRef(0);

  useEffect(() => {
    const el = rowRef.current;
    if (!el) return;
    el.scrollLeft = 0;

    const wheelHandler = (e: WheelEvent) => {
      if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
        e.preventDefault();
        el.scrollLeft -= e.deltaY * 1.2;
      }
    };

    const pointerDownHandler = (e: PointerEvent) => {
      isDownRef.current = true;
      startXRef.current =
        (e as PointerEvent & { pageX: number }).pageX - el.offsetLeft;
      scrollLeftRef.current = el.scrollLeft;
      try {
        (e.target as Element).setPointerCapture?.(
          (e as PointerEvent).pointerId
        );
      } catch (err) {}
    };

    const pointerMoveHandler = (e: PointerEvent) => {
      if (!isDownRef.current) return;
      e.preventDefault();
      const x = (e as PointerEvent & { pageX: number }).pageX - el.offsetLeft;
      const walk = (x - startXRef.current) * 1;
      el.scrollLeft = scrollLeftRef.current - walk;
    };

    const pointerUpHandler = (e: PointerEvent) => {
      isDownRef.current = false;
      try {
        (e.target as Element).releasePointerCapture?.(
          (e as PointerEvent).pointerId
        );
      } catch (err) {}
    };

    const pointerLeaveHandler = () => {
      isDownRef.current = false;
    };

    el.addEventListener("wheel", wheelHandler, { passive: false });
    el.addEventListener("pointerdown", pointerDownHandler, { passive: false });
    el.addEventListener("pointermove", pointerMoveHandler, { passive: false });
    el.addEventListener("pointerup", pointerUpHandler);
    el.addEventListener("pointerleave", pointerLeaveHandler);

    return () => {
      el.removeEventListener("wheel", wheelHandler as EventListener);
      el.removeEventListener(
        "pointerdown",
        pointerDownHandler as EventListener
      );
      el.removeEventListener(
        "pointermove",
        pointerMoveHandler as EventListener
      );
      el.removeEventListener("pointerup", pointerUpHandler as EventListener);
      el.removeEventListener(
        "pointerleave",
        pointerLeaveHandler as EventListener
      );
    };
  }, [blogs]);

  function handleWheel(e: React.WheelEvent) {
    const el = rowRef.current;
    if (!el) return;
    if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
      el.scrollLeft -= e.deltaY * 1.2;
      e.preventDefault();
    }
  }

  return (
    <section id="blog" className={`container ${styles.fullSection}`}>
      <div className={styles.center}>
        <div className={styles.intro}>
          <h2 className={styles.title}>Blog</h2>
          <p className={styles.lead}>
            I write about development, projects and lessons learned. Here are
            some recent posts.
          </p>

          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <div
              ref={rowRef}
              className={`${styles.projectRow} show-horizontal-scrollbar`}
              tabIndex={0}
              aria-label="Blog posts carousel"
              onWheel={handleWheel}
            >
              {(blogs ?? defaultBlogs).map((b: BlogPost) => (
                <BlogItem key={b.id} blog={b} />
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
