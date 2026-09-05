export type SceneVariant =
  "about" | "skills" | "experience" | "projects" | "education" | "contact";
export default function SectionScene({ variant }: { variant: SceneVariant }) {
  return (
    <div
      className={`section-atmosphere section-atmosphere--${variant}`}
      aria-hidden="true"
    >
      <span />
      <span />
      <span />
    </div>
  );
}
