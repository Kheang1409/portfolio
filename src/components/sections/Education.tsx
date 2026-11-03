import { Award, GraduationCap } from "lucide-react";
import SectionShell from "@/components/hud/SectionShell";
import type { Resume } from "@/lib/resume";
import { formatDateRange } from "@/lib/resume";

export default function Education({ resume }: { resume: Resume | null }) {
  const education = resume?.educations ?? [];
  return (
    <SectionShell id="education" index="05" label="EDUCATION" title="Learning along the way." theme="theme-mark3" intro="Education and recognition.">
      <div className="archive-grid"><div>
        {education.map((item, index) => <article className="archive-card" key={`${item.institution}-${index}`}>
          <span className="archive-code">EDU // 0{index + 1}</span><GraduationCap /><div>
            <h3>{item.degree}</h3><p>{item.institution}</p><dl><dt>PERIOD</dt><dd>{formatDateRange(item.startDates, item.endDate)}</dd>{item.location && <><dt>LOCATION</dt><dd>{item.location}</dd></>}</dl>
          </div>
        </article>)}
      </div>
      {resume?.certifications?.length ? <aside className="honor-card">{resume.certifications.map((item) => <article key={`${item.title}-${item.issuer}`}><Award /><span>{item.date && new Date(item.date).getUTCFullYear()}</span><h3>{item.title}</h3><p>{item.issuer}</p></article>)}</aside> : null}
      </div>
    </SectionShell>
  );
}
