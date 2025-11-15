import { useRef, useEffect } from "react";
import Card from "../Card";
import styles from "./Portfolio.module.css";
import { projects as defaultProjects, Project } from "../../data/data";

type Props = {
  projects?: Project[];
};

export default function Portfolio({ projects }: Props) {
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
  }, [projects]);

  return (
    <section id="portfolio" className={`container ${styles.fullSection}`}>
      <div className={styles.center}>
        <div className={styles.intro}>
          <h2 className={styles.title}>Portfolio</h2>
          <p className={styles.lead}>
            As a driven software developer, I've worked on various projects that
            demonstrate my skills and expertise. From .NET development to
            full-stack solutions, I've delivered scalable and efficient projects
            that meet the needs of businesses and organizations.
          </p>
          <div
            ref={rowRef}
            className={`${styles.projectRow} show-horizontal-scrollbar`}
            tabIndex={0}
            aria-label="Projects carousel"
          >
            {(projects ?? defaultProjects).map((p: Project, i: number) => (
              <Card
                key={i}
                front={
                  <>
                    <h4>{p.title}</h4>
                    {p.image && (
                      <img
                        src={p.image}
                        alt={p.title}
                        style={{
                          height: 120,
                          objectFit: "cover",
                          borderRadius: 6,
                        }}
                      />
                    )}
                  </>
                }
                back={<p>{p.description}</p>}
              />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
