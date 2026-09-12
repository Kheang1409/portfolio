import { Calendar, MapPin } from "lucide-react";
import SectionShell from "@/components/hud/SectionShell";
const experiences = [
  {
    company: "Better & Best Inc.",
    role: "Software Engineer",
    duration: "Apr 2026 – Present",
    location: "Oregon, WI, USA",
    objective:
      "Backend services and internal applications for POS, billing, and order management.",
    impact: [
      "Build REST APIs for real-time operational workflows",
      "Optimize queries and schemas for reliability under transaction loads",
      "Refactor legacy components into modular services",
      "Resolve production issues and improve system stability",
    ],
    stack: ".NET / SQL / REST / CLOUD",
  },
  {
    company: "Sahakrinpheap Microfinance PLC",
    role: "Software Engineer",
    duration: "Sep 2021 – May 2024",
    location: "Phnom Penh, Cambodia",
    objective:
      "Modernized banking systems into scalable services with secure integrations.",
    impact: [
      "Reduced system crashes by 40% through microservice modernization",
      "Improved incident response by 50% and achieved 99% uptime",
      "Reduced complex database execution from minutes to 2 seconds",
      "Automated payroll workflows, reducing processing by 7 days",
    ],
    stack: "C# / ASP.NET CORE / AZURE / SQL",
  },
  {
    company: "Pathmazing Inc.",
    role: "Software Engineer",
    duration: "Mar 2021 – Sep 2021",
    location: "Phnom Penh, Cambodia",
    objective:
      "Secure payment integrations and ERP-connected backend services.",
    impact: [
      "Improved payment transaction reliability by 30%",
      "Reduced manual workflows through ERP integrations",
      "Improved database efficiency by 20%",
    ],
    stack: "C# / .NET / REST / SQL",
  },
  {
    company: "Anakut Digital Solution, Co. Ltd",
    role: "Software Engineer",
    duration: "Oct 2020 – May 2021",
    location: "Phnom Penh, Cambodia",
    objective:
      "Scalable POS and management platforms with real-time reporting.",
    impact: [
      "Reduced manual data-entry errors by 99%",
      "Built ERP-integrated APIs for live tracking and reporting",
    ],
    stack: ".NET / ERP / REST / DATA",
  },
  {
    company: "Arrow Dot",
    role: "Software Engineer",
    duration: "Oct 2019 – Oct 2020",
    location: "Phnom Penh, Cambodia",
    objective: "Reliable web delivery through modern deployment automation.",
    impact: [
      "Built CI/CD pipelines and containerized deployments",
      "Improved release reliability while reducing manual effort",
    ],
    stack: "CI/CD / DOCKER / KUBERNETES",
  },
];
export default function Experience() {
  return (
    <SectionShell
      id="experience"
      index="03"
      label="MISSION LOG"
      title="Systems shipped. Outcomes measured."
      theme="theme-mark46"
      intro="A tactical record of modernization, performance work, and production ownership."
    >
      <div className="mission-timeline">
        {experiences.map((e, i) => (
          <article className="mission-card" key={e.company}>
            <div className="mission-index">
              <span>MISSION</span>
              {String(experiences.length - i).padStart(2, "0")}
            </div>
            <header>
              <p>{e.company}</p>
              <h3>{e.role}</h3>
              <div>
                <span>
                  <Calendar /> {e.duration}
                </span>
                <span>
                  <MapPin /> {e.location}
                </span>
              </div>
            </header>
            <div className="mission-body">
              <label>SYSTEM OBJECTIVE</label>
              <p>{e.objective}</p>
              <label>IMPACT</label>
              <ul>
                {e.impact.map((x) => (
                  <li key={x}>{x}</li>
                ))}
              </ul>
              <label>STACK</label>
              <strong>{e.stack}</strong>
            </div>
          </article>
        ))}
      </div>
    </SectionShell>
  );
}
