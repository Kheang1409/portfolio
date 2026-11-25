import SectionShell from "@/components/hud/SectionShell";
import type { Resume } from "@/lib/resume";

export default function Skills({ resume }: { resume: Resume | null }) {
  const skills = resume?.skills ?? [];

  return (
    <SectionShell id="skills" index="02" label="TECHNOLOGIES" title="Tools for building useful things." theme="theme-mark45" intro="A selection of technologies from my resume.">
      <ul className="skills-list" aria-label="Skills">
        {skills.map((skill) => <li key={skill}>{skill}</li>)}
      </ul>
    </SectionShell>
  );
}
