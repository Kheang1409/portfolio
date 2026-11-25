import { Calendar, MapPin } from "lucide-react";
import SectionShell from "@/components/hud/SectionShell";
import type { Resume } from "@/lib/resume";
import { formatDateRange } from "@/lib/resume";

export default function Experience({ resume }: { resume: Resume | null }) {
  const experiences = resume?.experiences ?? [];
  return (
    <SectionShell id="experience" index="03" label="EXPERIENCE" title="The work and the impact." theme="theme-mark46" intro="A selection of recent roles and contributions.">
      <div className="mission-timeline">
        {experiences.map((experience, index) => (
          <article className="mission-card" key={`${experience.company}-${index}`}>
            <div className="mission-index"><span>ROLE</span>{String(experiences.length - index).padStart(2, "0")}</div>
            <header><p>{experience.company}</p><h3>{experience.role}</h3><div>
              <span><Calendar /> {formatDateRange(experience.startDates, experience.endDate)}</span>
              {experience.location && <span><MapPin /> {experience.location}</span>}
            </div></header>
            <div className="mission-body"><label>EXPERIENCE</label><ul>
              {(experience.bulletPoints ?? []).map((point) => <li key={point}>{point}</li>)}
            </ul></div>
          </article>
        ))}
      </div>
    </SectionShell>
  );
}
