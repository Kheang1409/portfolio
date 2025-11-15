import styles from "./TimeSection.module.css";
import { timelines as defaultTimelines, TimelineItem } from "../../data/data";

function formatYear(item: TimelineItem) {
  if (!item?.start_date) return "";
  const start = item.start_date.slice(0, 4);
  const end = item.end_date ? item.end_date.slice(0, 4) : null;
  return end ? `${start} - ${end}` : `${start} -  Pres.`;
}

function formatTitle(item: TimelineItem) {
  if (item.role) return item.role;
  if (item.degree) return item.degree;
  return item.type || "";
}

function formatSubtitle(item: TimelineItem) {
  return item.company ?? item.university ?? "";
}

export default function TimeSection({
  timelines,
}: {
  timelines?: TimelineItem[];
}) {
  const data =
    Array.isArray(timelines) && timelines.length ? timelines : defaultTimelines;

  return (
    <section id="timeline" className={styles.timeSection}>
      <div className={styles.center}>
        <div className={styles.mainTimeline}>
          {data.map((item, index) => (
            <div className={styles.timeline} key={index}>
              <a className={styles.timelineContent}>
                <span className={styles.timelineYear}>{formatYear(item)}</span>
                <div className={styles.content}>
                  <h3 className={styles.title}>{formatTitle(item)}</h3>
                  {formatSubtitle(item) ? (
                    <h4 className={styles.subtitle}>{formatSubtitle(item)}</h4>
                  ) : null}
                  <p className={styles.description}>
                    {item.description || "No description provided."}
                  </p>
                </div>
              </a>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
