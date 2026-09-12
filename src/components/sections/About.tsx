import SectionShell from "@/components/hud/SectionShell";
import Image from "next/image";
const disciplines = [
  {
    n: "01",
    t: "SYSTEMS ENGINEERING",
    d: "Reliable services with clear boundaries, observability, and operational resilience.",
  },
  {
    n: "02",
    t: "BACKEND ARCHITECTURE",
    d: "High-performance APIs, data models, integrations, and event-driven workflows.",
  },
  {
    n: "03",
    t: "MODERNIZATION",
    d: "Practical migration from legacy systems toward scalable, maintainable platforms.",
  },
  {
    n: "04",
    t: "TECHNICAL LEADERSHIP",
    d: "Clear decisions, collaborative delivery, and ownership from design through production.",
  },
];
export default function About() {
  return (
    <SectionShell
      id="about"
      index="01"
      label="ENGINEERING PROFILE"
      title="Built to solve the hard parts."
      theme="theme-mark2"
      intro="I turn complex operational requirements into dependable software that teams can understand, evolve, and trust."
    >
      <div className="about-layout">
        <div className="profile-copy">
          <figure className="section-portrait section-portrait--prototype">
            <Image
              src="/images/kai-classic-stance.webp"
              alt="Hang Kheang Taing in professional attire"
              fill
              sizes="(max-width: 760px) 100vw, 40vw"
              className="section-portrait__image"
            />
            <figcaption>
              <span>PROFILE // VERIFIED</span>
              ENGINEERING PROFILE
            </figcaption>
          </figure>
          <p>
            Software Engineer experienced in scalable web applications using C#,
            .NET Core, and modern JavaScript frameworks. I build RESTful APIs,
            integrate third-party systems, optimize SQL Server and PostgreSQL,
            and improve performance across AWS and Azure environments.
          </p>
          <p>
            My work spans backend architecture and full-stack delivery, with an
            emphasis on clean code, secure systems, measurable outcomes, and
            calm execution in production.
          </p>
          <dl className="data-register">
            <div>
              <dt>ROLE</dt>
              <dd>SOFTWARE ENGINEER</dd>
            </div>
            <div>
              <dt>FOCUS</dt>
              <dd>BACKEND SYSTEMS</dd>
            </div>
            <div>
              <dt>MODE</dt>
              <dd>BUILD / OPTIMIZE / SCALE</dd>
            </div>
            <div>
              <dt>BASE</dt>
              <dd>IRVING, TEXAS</dd>
            </div>
          </dl>
        </div>
        <div className="discipline-grid">
          {disciplines.map((x) => (
            <article className="metal-panel" key={x.n}>
              <span>{x.n}</span>
              <h3>{x.t}</h3>
              <p>{x.d}</p>
            </article>
          ))}
        </div>
      </div>
    </SectionShell>
  );
}
