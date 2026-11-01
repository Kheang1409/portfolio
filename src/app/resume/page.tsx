import { buildPageMetadata } from "@/lib/seo";
import { formatDateRange, getResume } from "@/lib/resume";
import ResumeActions from "./ResumeActions";

export const metadata = buildPageMetadata({ title: "Resume", description: "Professional resume", path: "/resume" });

export default async function ResumePage() {
  let resume = null;
  try { resume = await getResume(); } catch { resume = null; }
  const personals = resume?.personals;
  return <main id="main-content" className="resume-page"><article className="resume-dossier">
    <header className="resume-header"><div><p className="system-label"><span>DOSSIER.01</span> // ENGINEERING PROFILE</p>
      <h1>{personals?.preferredName || personals?.legalName}</h1>
      <address>{personals?.phone && <a href={`tel:${personals.phone}`}>{personals.phone}</a>}{personals?.email && <a href={`mailto:${personals.email}`}>{personals.email}</a>}{personals?.linkedin && <a href={personals.linkedin}>LinkedIn</a>}{personals?.github && <a href={personals.github}>GitHub</a>}</address>
      {personals?.workAuthorization && <p className="resume-authorization">{personals.workAuthorization}</p>}
    </div><ResumeActions markdownHref="/resume.pdf" /></header>
    {!resume && <p role="status">Resume information is not currently available.</p>}
    {resume?.summary && <section className="resume-section"><h2>Profile</h2><p>{resume.summary}</p></section>}
    {!!resume?.skills?.length && <section className="resume-section"><h2>Skills</h2><ul>{resume.skills.map((skill) => <li key={skill}>{skill}</li>)}</ul></section>}
    {!!resume?.experiences?.length && <section className="resume-section"><h2>Professional Experience</h2><div className="resume-timeline">{resume.experiences.map((item, index) => <article className="resume-role" key={`${item.company}-${index}`}><span className="resume-role-index">{item.role}</span><header><div><h3>{item.company}</h3></div><p>{formatDateRange(item.startDates, item.endDate)}<br />{item.location}</p></header><ul>{item.bulletPoints?.map((point) => <li key={point}>{point}</li>)}</ul></article>)}</div></section>}
    {!!resume?.projects?.length && <section className="resume-section"><h2>Projects</h2><div className="resume-project-grid">{resume.projects.map((item) => <article key={item.name}><h3>{item.name}</h3><p>{item.description}</p></article>)}</div></section>}
    {!!resume?.educations?.length && <section className="resume-section"><h2>Education</h2><div className="resume-education-grid">{resume.educations.map((item, index) => <article key={`${item.institution}-${index}`}><h3>{item.degree}</h3><p>{item.institution}</p><small>{formatDateRange(item.startDates, item.endDate)} {item.location}</small></article>)}</div></section>}
    {!!resume?.certifications?.length && <section className="resume-section"><h2>Certifications</h2>{resume.certifications.map((item) => <article key={`${item.title}-${item.issuer}`}><h3>{item.title}</h3><p>{item.issuer}</p></article>)}</section>}
  </article></main>;
}
