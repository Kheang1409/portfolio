import type { ReactNode } from "react";

type Props = {
  id: string;
  index: string;
  label: string;
  title: string;
  theme: string;
  intro?: string;
  children: ReactNode;
};
export default function SectionShell({
  id,
  index,
  label,
  title,
  theme,
  intro,
  children,
}: Props) {
  return (
    <section
      id={id}
      className={`system-section ${theme}`}
      aria-labelledby={`${id}-heading`}
    >
      <div className="system-grid" aria-hidden="true" />
      <div className="section-shell">
        <header className="section-heading">
          <p className="system-label">
            <span>{index}</span> // {label}
          </p>
          <h2 id={`${id}-heading`}>{title}</h2>
          {intro && <p>{intro}</p>}
        </header>
        {children}
      </div>
    </section>
  );
}
