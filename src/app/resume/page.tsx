import { buildPageMetadata } from "@/lib/seo";
import {
  resumeEducation,
  resumeExperience,
  resumeProjects,
  resumeSkills,
  resumeSummary,
} from "@/config/resume";
import ResumeActions from "./ResumeActions";

export const metadata = buildPageMetadata({
  title: "Resume — Hang Kheang Taing",
  description:
    "Resume of Hang Kheang Taing — Software Engineer specializing in enterprise application modernization, C#/.NET, distributed systems, cloud architecture, and production reliability.",
  path: "/resume",
});

export default function ResumePage() {
  return (
    <main id="main-content" className="resume-page">
      <article className="resume-dossier">
        <header className="resume-header">
          <div>
            <p className="system-label">
              <span>DOSSIER.01</span> // ENGINEERING PROFILE
            </p>
            <h1>Hang Kheang Taing</h1>
            <p className="resume-title">SOFTWARE ENGINEER</p>
            <address>
              <a href="tel:+16412330129">641-233-0129</a>
              <a href="mailto:hangkheangtaing@gmail.com">
                hangkheangtaing@gmail.com
              </a>
              <a href="https://www.linkedin.com/in/hang-kheang-taing">
                LinkedIn
              </a>
              <a href="https://github.com/Kheang1409">GitHub</a>
            </address>
            <p className="resume-authorization">
              Authorized to work in the U.S. without current or future employer
              sponsorship
            </p>
          </div>
          <ResumeActions markdownHref="/resume.pdf" />
        </header>

        <section className="resume-section" aria-labelledby="resume-summary">
          <h2 id="resume-summary">
            <span>01</span> Profile
          </h2>
          <p>{resumeSummary}</p>
        </section>

        <section className="resume-section" aria-labelledby="resume-skills">
          <h2 id="resume-skills">
            <span>02</span> Technical Systems
          </h2>
          <dl className="resume-skill-grid">
            {resumeSkills.map(([label, value]) => (
              <div key={label}>
                <dt>{label}</dt>
                <dd>{value}</dd>
              </div>
            ))}
          </dl>
        </section>

        <section className="resume-section" aria-labelledby="resume-experience">
          <h2 id="resume-experience">
            <span>03</span> Professional Experience
          </h2>
          <div className="resume-timeline">
            {resumeExperience.map((experience, index) => (
              <article className="resume-role" key={experience.company}>
                <span className="resume-role-index">
                  MISSION {String(index + 1).padStart(2, "0")}
                </span>
                <header>
                  <div>
                    <h3>{experience.role}</h3>
                    <p>{experience.company}</p>
                  </div>
                  <p>
                    {experience.dates}
                    <br />
                    {experience.location}
                  </p>
                </header>
                <ul>
                  {experience.achievements.map((achievement) => (
                    <li key={achievement}>{achievement}</li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </section>

        <section className="resume-section" aria-labelledby="resume-projects">
          <h2 id="resume-projects">
            <span>04</span> Selected Projects
          </h2>
          <div className="resume-project-grid">
            {resumeProjects.map((project) => (
              <article key={project.name}>
                <header>
                  <h3>{project.name}</h3>
                  <span>{project.date}</span>
                </header>
                <ul>
                  {project.achievements.map((achievement) => (
                    <li key={achievement}>{achievement}</li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </section>

        <section className="resume-section" aria-labelledby="resume-education">
          <h2 id="resume-education">
            <span>05</span> Education & Honors
          </h2>
          <div className="resume-education-grid">
            {resumeEducation.map((item) => (
              <article key={item.school}>
                <h3>{item.degree}</h3>
                <p>{item.school}</p>
                <small>
                  {item.dates} · {item.location}
                </small>
              </article>
            ))}
            <article>
              <h3>Best Project & Team Effort of The Year</h3>
              <p>Khmer Care, Pathmazing Inc.</p>
              <small>June 2021</small>
            </article>
          </div>
        </section>
      </article>
    </main>
  );
}
