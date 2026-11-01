import SectionShell from "@/components/hud/SectionShell";
import Image from "next/image";
import type { Resume } from "@/lib/resume";
export default function About({ resume }: { resume: Resume | null }) {
  return (
    <SectionShell
      id="about"
      index="01"
      label="ENGINEERING PROFILE"
      title="Built to solve the hard parts."
      theme="theme-mark2"
      intro={resume?.summary ?? "Professional profile is not available."}
    >
      <div className="about-layout">
        <div className="profile-copy">
          <figure className="section-portrait section-portrait--prototype">
            <Image
              src="/images/kai-classic-stance.webp"
              alt="Professional profile portrait"
              fill
              sizes="(max-width: 760px) 100vw, 40vw"
              className="section-portrait__image"
            />
            <figcaption>
              <span>PROFILE // VERIFIED</span>
              ENGINEERING PROFILE
            </figcaption>
          </figure>
          {resume?.summary && <p>{resume.summary}</p>}
          <dl className="data-register">
            <div>
              <dt>ROLE</dt>
              <dd>{resume?.experiences?.[0]?.role ?? ""}</dd>
            </div>
            <div>
              <dt>FOCUS</dt>
              <dd>{resume?.projects?.[0]?.name ?? ""}</dd>
            </div>
            <div>
              <dt>MODE</dt>
              <dd>{resume?.skills?.slice(0, 3).join(" / ") ?? ""}</dd>
            </div>
            <div>
              <dt>BASE</dt>
              <dd>{resume?.experiences?.[0]?.location ?? ""}</dd>
            </div>
          </dl>
        </div>
        <div className="discipline-grid">
          {(resume?.projects ?? []).slice(0, 4).map((project, index) => (
            <article className="metal-panel" key={project.name}>
              <span>{String(index + 1).padStart(2, "0")}</span>
              <h3>{project.name}</h3>
              <p>{project.description}</p>
            </article>
          ))}
        </div>
      </div>
    </SectionShell>
  );
}
