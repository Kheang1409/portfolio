import SectionShell from "@/components/hud/SectionShell";
import Image from "next/image";
import type { Resume } from "@/lib/resume";

export default function Skills({ resume }: { resume: Resume | null }) {
  const skills = resume?.skills ?? [];
  return (
    <SectionShell id="skills" index="02" label="CAPABILITY MATRIX" title="A systems-first technology stack." theme="theme-mark45" intro="Skills and technologies from the latest resume.">
      <div className="system-visual system-visual--skills"><Image src="/images/kai/kai-og.webp" alt="" fill sizes="(max-width: 760px) 100vw, 42vw" className="system-visual__portrait" /><div className="system-visual__diagram" aria-hidden="true" /><div className="system-visual__readout"><span>CAPABILITIES</span><strong>{skills.length} SKILLS</strong></div></div>
      <ul className="flex flex-wrap gap-sm">{skills.map((skill) => <li key={skill} className="skill-cluster">{skill}</li>)}</ul>
    </SectionShell>
  );
}
