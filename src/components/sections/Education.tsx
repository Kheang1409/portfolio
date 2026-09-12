import { Award, GraduationCap } from "lucide-react";
import SectionShell from "@/components/hud/SectionShell";
const education = [
  {
    school: "Maharishi International University",
    degree: "Master of Science in Computer Science",
    year: "May 2024 – Dec 2026",
    location: "Fairfield, IA, USA",
    focus:
      "Advanced computer science, scalable systems, and applied software engineering.",
  },
  {
    school: "Royal University of Phnom Penh",
    degree: "Bachelor of Science in Computer Science & Engineering",
    year: "Nov 2016 – Nov 2020",
    location: "Phnom Penh, Cambodia",
    focus:
      "Software engineering, databases, web systems, and computer engineering foundations.",
  },
];
export default function Education() {
  return (
    <SectionShell
      id="education"
      index="05"
      label="EDUCATION DATABASE"
      title="Engineering foundations."
      theme="theme-mark3"
      intro="Formal computer science study reinforced by years of production engineering."
    >
      <div className="archive-grid">
        <div>
          {education.map((e, i) => (
            <article className="archive-card" key={e.school}>
              <span className="archive-code">EDU // 0{i + 1}</span>
              <GraduationCap />
              <div>
                <h3>{e.degree}</h3>
                <p>{e.school}</p>
                <dl>
                  <dt>PERIOD</dt>
                  <dd>{e.year}</dd>
                  <dt>LOCATION</dt>
                  <dd>{e.location}</dd>
                  <dt>FOCUS</dt>
                  <dd>{e.focus}</dd>
                </dl>
              </div>
            </article>
          ))}
        </div>
        <aside className="honor-card">
          <Award />
          <span>HONOR // 2021</span>
          <h3>Best Project & Team Effort of the Year</h3>
          <p>Khmer Care, Pathmazing Inc.</p>
          <small>Recognition awarded 06/2021</small>
        </aside>
      </div>
    </SectionShell>
  );
}
