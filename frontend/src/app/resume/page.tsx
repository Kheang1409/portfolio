import type { ReactNode } from "react";
import {
  ArrowUpRight,
  Award,
  CalendarDays,
  Code2,
  Github,
  GraduationCap,
  Linkedin,
  Mail,
  MapPin,
  Phone,
} from "lucide-react";
import { buildPageMetadata } from "@/lib/seo";
import { formatDateRange, getResume } from "@/lib/resume";
import ResumeActions from "./ResumeActions";

export const metadata = buildPageMetadata({
  title: "Resume",
  description: "Professional resume",
  path: "/resume",
});
export const dynamic = "force-dynamic";

function ResumeSection({
  id,
  number,
  title,
  children,
}: {
  id: string;
  number: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="resume-section" id={id} aria-labelledby={`${id}-title`}>
      <div className="resume-section-heading">
        <span aria-hidden="true">{number}</span>
        <h2 id={`${id}-title`}>{title}</h2>
      </div>
      {children}
    </section>
  );
}

export default async function ResumePage() {
  let resume = null;
  let profileLoadFailed = false;
  try {
    resume = await getResume();
  } catch (error) {
    console.error("Unable to load resume data from the backend.", error);
    profileLoadFailed = true;
  }

  const personals = resume?.personals;
  const name = personals?.preferredName || personals?.legalName;
  const sections: { id: string; label: string }[] = [];
  if (resume?.summary) sections.push({ id: "profile", label: "Profile" });
  if (resume?.skills?.length) sections.push({ id: "skills", label: "Skills" });
  if (resume?.experiences?.length)
    sections.push({ id: "experience", label: "Experience" });
  if (resume?.projects?.length)
    sections.push({ id: "projects", label: "Projects" });
  if (resume?.educations?.length)
    sections.push({ id: "education", label: "Education" });
  if (resume?.certifications?.length)
    sections.push({ id: "certifications", label: "Certifications" });
  const sectionNumber = (id: string) =>
    String(sections.findIndex((section) => section.id === id) + 1).padStart(
      2,
      "0",
    );
  const formatCredentialDate = (value?: string) => {
    if (!value) return "";
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? value
      : date.toLocaleDateString("en", {
          month: "long",
          year: "numeric",
          timeZone: "UTC",
        });
  };

  return (
    <main id="main-content" className="resume-page">
      <article className="resume-dossier">
        <div className="resume-topline">
          <p>Resume <span aria-hidden="true">/</span> Professional profile</p>
          <ResumeActions markdownHref="/resume.pdf" />
        </div>

        <header className="resume-header">
          <div className="resume-identity">
            <p className="resume-eyebrow">Software engineering</p>
            <h1>{name || "Professional profile"}</h1>
            <p className="resume-intro">
              A concise overview of experience, technical strengths, and selected
              work.
            </p>
            <address className="resume-contact-list">
              {personals?.email && (
                <a href={`mailto:${personals.email}`}>
                  <Mail aria-hidden="true" />
                  <span>{personals.email}</span>
                </a>
              )}
              {personals?.phone && (
                <a href={`tel:${personals.phone}`}>
                  <Phone aria-hidden="true" />
                  <span>{personals.phone}</span>
                </a>
              )}
              {personals?.linkedin && (
                <a href={personals.linkedin} target="_blank" rel="noreferrer">
                  <Linkedin aria-hidden="true" />
                  <span>LinkedIn</span>
                  <ArrowUpRight aria-hidden="true" />
                </a>
              )}
              {personals?.github && (
                <a href={personals.github} target="_blank" rel="noreferrer">
                  <Github aria-hidden="true" />
                  <span>GitHub</span>
                  <ArrowUpRight aria-hidden="true" />
                </a>
              )}
            </address>
            {personals?.workAuthorization && (
              <p className="resume-authorization">
                {personals.workAuthorization}
              </p>
            )}
          </div>
        </header>

        {profileLoadFailed && (
          <p className="resume-status" role="status">
            Resume information is temporarily unavailable. Please try again
            later.
          </p>
        )}
        {!resume && !profileLoadFailed && (
          <p className="resume-status" role="status">
            Resume information has not been added yet.
          </p>
        )}

        {sections.length > 1 && (
          <nav className="resume-section-nav" aria-label="On this page">
            <span>On this page</span>
            {sections.map((section) => (
              <a key={section.id} href={`#${section.id}`}>
                {section.label}
              </a>
            ))}
          </nav>
        )}

        {resume?.summary && (
          <ResumeSection id="profile" number={sectionNumber("profile")} title="Profile">
            <p className="resume-summary">{resume.summary}</p>
          </ResumeSection>
        )}

        {!!resume?.skills?.length && (
          <ResumeSection
            id="skills"
            number={sectionNumber("skills")}
            title="Skills"
          >
            <ul className="resume-skill-list" aria-label="Technical skills">
              {resume.skills.map((skill) => (
                <li key={skill}>{skill}</li>
              ))}
            </ul>
          </ResumeSection>
        )}

        {!!resume?.experiences?.length && (
          <ResumeSection
            id="experience"
            number={sectionNumber("experience")}
            title="Experience"
          >
            <div className="resume-timeline">
              {resume.experiences.map((item, index) => (
                <article
                  className="resume-role"
                  key={`${item.company}-${index}`}
                >
                  <header className="resume-role-header">
                    <div className="resume-role-title">
                      <span className="resume-role-label">
                        ROLE {String(index + 1).padStart(2, "0")}
                      </span>
                      <h3>{item.role}</h3>
                      <p>{item.company}</p>
                    </div>
                    <div className="resume-role-meta">
                      <span className="resume-role-dates">
                        {formatDateRange(item.startDates, item.endDate)}
                      </span>
                      {item.location && (
                        <span>
                          <MapPin aria-hidden="true" />
                          {item.location}
                        </span>
                      )}
                    </div>
                  </header>
                  {!!item.bulletPoints?.length && (
                    <ul>
                      {item.bulletPoints.map((point) => (
                        <li key={point}>{point}</li>
                      ))}
                    </ul>
                  )}
                </article>
              ))}
            </div>
          </ResumeSection>
        )}

        {!!resume?.projects?.length && (
          <ResumeSection
            id="projects"
            number={sectionNumber("projects")}
            title="Selected projects"
          >
            <div className="resume-project-grid">
              {resume.projects.map((item, index) => (
                <article key={item.name}>
                  <div className="resume-project-heading">
                    <span className="resume-project-index">
                      {String(index + 1).padStart(2, "0")}
                    </span>
                    <Code2 aria-hidden="true" />
                  </div>
                  <h3>{item.name}</h3>
                  <p>{item.description}</p>
                  {!!item.skills?.length && (
                    <div className="resume-project-tech">
                      <span>Built with</span>
                      <ul aria-label={`${item.name} technologies`}>
                        {item.skills.map((skill) => (
                          <li key={skill}>{skill}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </article>
              ))}
            </div>
          </ResumeSection>
        )}

        {!!resume?.educations?.length && (
          <ResumeSection
            id="education"
            number={sectionNumber("education")}
            title="Education"
          >
            <div className="resume-education-grid">
              {resume.educations.map((item, index) => (
                <article key={`${item.institution}-${index}`}>
                  <div className="resume-education-heading">
                    <span className="resume-education-icon">
                      <GraduationCap aria-hidden="true" />
                    </span>
                    <div>
                      <h3>{item.degree}</h3>
                      <p>{item.institution}</p>
                    </div>
                  </div>
                  <div className="resume-education-meta">
                    <span>
                      <CalendarDays aria-hidden="true" />
                      {formatDateRange(item.startDates, item.endDate)}
                    </span>
                    {item.location && (
                      <span>
                        <MapPin aria-hidden="true" />
                        {item.location}
                      </span>
                    )}
                  </div>
                </article>
              ))}
            </div>
          </ResumeSection>
        )}

        {!!resume?.certifications?.length && (
          <ResumeSection
            id="certifications"
            number={sectionNumber("certifications")}
            title="Certifications"
          >
            <div className="resume-certification-list">
              {resume.certifications.map((item) => (
                <article key={`${item.title}-${item.issuer}`}>
                  <span className="resume-certification-icon">
                    <Award aria-hidden="true" />
                  </span>
                  <div className="resume-certification-copy">
                    <h3>{item.title}</h3>
                    <p>{item.issuer}</p>
                  </div>
                  {item.date && (
                    <span className="resume-certification-date">
                      {formatCredentialDate(item.date)}
                    </span>
                  )}
                </article>
              ))}
            </div>
          </ResumeSection>
        )}
      </article>
    </main>
  );
}
