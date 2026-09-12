import SectionShell from "@/components/hud/SectionShell";
import Image from "next/image";
const groups = [
  {
    id: "CORE",
    title: "CORE LANGUAGES",
    items: ["C#", "TypeScript", "JavaScript", "Python", "SQL"],
  },
  {
    id: "API",
    title: "BACKEND",
    items: [".NET", "ASP.NET Core", "REST APIs", "Microservices", "CQRS"],
  },
  {
    id: "UI",
    title: "FRONTEND",
    items: ["React", "Next.js", "Angular", "Responsive UI"],
  },
  {
    id: "DATA",
    title: "DATA",
    items: ["SQL Server", "PostgreSQL", "MongoDB", "Redis"],
  },
  {
    id: "OPS",
    title: "CLOUD / PLATFORM",
    items: ["AWS", "Azure", "Docker", "Kubernetes", "CI/CD"],
  },
  {
    id: "ARCH",
    title: "ARCHITECTURE",
    items: [
      "Distributed Systems",
      "Event-Driven",
      "Caching",
      "API Design",
      "Performance",
    ],
  },
];

const CENTER = 500;
const INNER_RADIUS = 170;
const OUTER_RADIUS = 455;

function polar(radius: number, angle: number) {
  const radians = (angle * Math.PI) / 180;
  return {
    x: CENTER + radius * Math.cos(radians),
    y: CENTER + radius * Math.sin(radians),
  };
}

function sectorPath(index: number) {
  const startAngle = -119 + index * 60;
  const endAngle = startAngle + 58;
  const outerStart = polar(OUTER_RADIUS, startAngle);
  const outerEnd = polar(OUTER_RADIUS, endAngle);
  const innerEnd = polar(INNER_RADIUS, endAngle);
  const innerStart = polar(INNER_RADIUS, startAngle);
  return [
    `M ${outerStart.x} ${outerStart.y}`,
    `A ${OUTER_RADIUS} ${OUTER_RADIUS} 0 0 1 ${outerEnd.x} ${outerEnd.y}`,
    `L ${innerEnd.x} ${innerEnd.y}`,
    `A ${INNER_RADIUS} ${INNER_RADIUS} 0 0 0 ${innerStart.x} ${innerStart.y}`,
    "Z",
  ].join(" ");
}

function labelPosition(index: number) {
  const angle = -90 + index * 60;
  const point = polar(325, angle);
  return { x: point.x - 125, y: point.y - 82 };
}
export default function Skills() {
  return (
    <SectionShell
      id="skills"
      index="02"
      label="CAPABILITY MATRIX"
      title="A systems-first technology stack."
      theme="theme-mark45"
      intro="Capabilities organized by how they work together—not by arbitrary proficiency scores."
    >
      <div className="system-visual system-visual--skills">
        <Image
          src="/images/kai/kai-og.webp"
          alt="Hang Kheang Taing in an engineering command center"
          fill
          sizes="(max-width: 760px) 100vw, 42vw"
          className="system-visual__portrait"
        />
        <div className="system-visual__diagram" aria-hidden="true" />
        <div className="system-visual__readout">
          <span>ARCHITECTURE CORE // ACTIVE</span>
          <strong>CAPABILITY TELEMETRY</strong>
        </div>
      </div>
      <div
        className="skill-matrix"
        aria-label="Connected engineering capability groups"
      >
        <div className="matrix-core" aria-hidden="true">
          <span>KT</span>
          <small>
            ENGINEERING
            <br />
            CORE
          </small>
          <i>06 SYSTEMS</i>
        </div>
        <svg
          className="skill-donut"
          viewBox="0 0 1000 1000"
          role="img"
          aria-label="Six connected engineering capability sectors"
        >
          {groups.map((group, index) => {
            const label = labelPosition(index);
            return (
              <g
                className={`skill-donut__sector skill-donut__sector--${index + 1}`}
                key={group.id}
              >
                <path d={sectorPath(index)}>
                  <title>{group.title}</title>
                </path>
                <foreignObject x={label.x} y={label.y} width="250" height="164">
                  <article className="skill-donut__content">
                    <span>
                      {group.id} // 0{index + 1}
                    </span>
                    <h3>{group.title}</h3>
                    <ul>
                      {group.items.map((item) => (
                        <li key={item}>{item}</li>
                      ))}
                    </ul>
                  </article>
                </foreignObject>
              </g>
            );
          })}
        </svg>
        {groups.map((g, i) => (
          <article
            key={g.id}
            className={`skill-cluster skill-cluster--fallback cluster-${i + 1}`}
          >
            <span className="cluster-id">{g.id}</span>
            <h3>{g.title}</h3>
            <ul>
              {g.items.map((x) => (
                <li key={x}>{x}</li>
              ))}
            </ul>
          </article>
        ))}
      </div>
    </SectionShell>
  );
}
