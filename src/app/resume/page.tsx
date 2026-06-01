import { buildPageMetadata } from "@/lib/seo";
import ResumeActions from "./ResumeActions";

export const metadata = buildPageMetadata({
  title: "Resume — Hang Kheang Taing",
  description:
    "Resume of Hang Kheang Taing — Software Engineer specializing in C#, .NET Core, ASP.NET Core, RESTful APIs, SQL optimization, cloud deployments, and microservices.",
  path: "/resume",
});

export default function ResumePage() {
  return (
    <main className="py-12 px-sm md:px-lg max-w-container mx-auto">
      <div className="bg-light-background dark:bg-dark-background p-lg rounded-lg border border-light-border dark:border-dark-border">
        <header className="flex flex-col md:flex-row md:items-center md:justify-between gap-md">
          <div>
            <h1 className="text-h2 font-bold">Hang Kheang Taing</h1>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary mt-xs">
              641-233-0129 | hangkheangtaing@gmail.com
            </p>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
              https://www.linkedin.com/in/hang-kheang-taing •
              https://github.com/Kheang1409 • https://kaitaing.netlify.app
            </p>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary mt-xs">
              Work Authorization: Authorized to work in the U.S. without current
              or future employer sponsorship
            </p>
          </div>

          <ResumeActions markdownHref="/resume.md" />
        </header>

        <section className="mt-lg space-y-md">
          <h2 className="text-h4 font-semibold">SUMMARY</h2>
          <p className="text-body text-light-text-secondary dark:text-dark-text-secondary leading-relaxed">
            Software Engineer with experience designing, developing, and
            maintaining scalable web applications using C#, .NET Core, and
            modern JavaScript frameworks. Strong background in building RESTful
            APIs, integrating third-party systems, and optimizing SQL Server and
            PostgreSQL databases. Experienced in writing clean, maintainable,
            and well-documented code while collaborating with cross-functional
            teams in Agile environments. Proven ability to debug, troubleshoot,
            and improve system performance in cloud-based architectures (Azure,
            AWS), delivering reliable and secure software solutions.
          </p>

          <h3 className="text-h5 font-semibold">CORE COMPETENCIES</h3>
          <p className="text-body text-light-text-secondary dark:text-dark-text-secondary">
            C# | .NET Core | ASP.NET Core | RESTful APIs | SQL Server | Entity
            Framework Core | Dapper | API Integrations | Debugging |
            Troubleshooting | CI/CD | Azure | AWS | React | Angular | JavaScript
            | TypeScript | HTML5 | CSS3 | Responsive Design | Agile/Scrum | Git
            | Microservices | Cloud Deployment
          </p>

          <h3 className="text-h5 font-semibold">TECHNICAL SKILLS</h3>
          <p className="text-body text-light-text-secondary dark:text-dark-text-secondary leading-relaxed">
            Backend & Architecture: C#, .NET Core, ASP.NET Core, RESTful API
            Development, Microservices, Event-Driven Architecture, Clean
            Architecture, CQRS, Dependency Injection, SOLID Principles,
            Asynchronous Programming (async/await). Frontend: React, Angular,
            Next.js, JavaScript, TypeScript, HTML5, CSS3, Responsive Design,
            SignalR. Databases: SQL Server, PostgreSQL, MongoDB, EF Core,
            Dapper. Cloud & DevOps: Azure, AWS, Docker, Kubernetes, CI/CD (Azure
            DevOps, GitHub Actions). Integration & Messaging: REST API
            Integrations, Kafka, Redis, SignalR. Security: OAuth2, JWT, RBAC.
            Tools & Practices: Git, Code Reviews, Unit & Integration Testing
            (xUnit, NUnit), Agile/Scrum, Debugging & Troubleshooting,
            Performance Tuning.
          </p>

          <h3 className="text-h4 font-semibold">PROFESSIONAL EXPERIENCE</h3>

          <article>
            <h4 className="font-semibold">
              Better & Best Inc. — Software Engineer
            </h4>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
              Apr 2026 – Present • Oregon, WI, USA
            </p>
            <ul className="list-disc ml-lg mt-xs space-y-1 text-body text-light-text-secondary dark:text-dark-text-secondary">
              <li>
                Design and develop backend services and internal applications
                supporting POS, billing, and order management systems.
              </li>
              <li>
                Build RESTful APIs enabling real-time data processing and system
                integration across operational workflows.
              </li>
              <li>
                Optimize database queries and schema design, improving response
                time and system reliability under daily transaction loads.
              </li>
              <li>
                Refactor legacy components into modular, scalable services to
                improve maintainability and extensibility.
              </li>
              <li>
                Troubleshoot and resolve production issues, ensuring high
                availability and system stability.
              </li>
              <li>
                Collaborate with stakeholders to translate business requirements
                into scalable technical solutions.
              </li>
            </ul>
          </article>

          <article>
            <h4 className="font-semibold">
              Sahakrinpheap Microfinance PLC — Software Engineer
            </h4>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
              Sep 2021 – May 2024 • Phnom Penh, Cambodia
            </p>
            <ul className="list-disc ml-lg mt-xs space-y-1 text-body text-light-text-secondary dark:text-dark-text-secondary">
              <li>
                Re-architected legacy banking systems into scalable
                microservices using C# and ASP.NET Core, developing RESTful APIs
                and reducing system crashes by 40% while improving
                maintainability.
              </li>
              <li>
                Built and maintained event-driven monitoring and alerting
                systems on Azure, improving incident response time by 50% and
                achieving 99% uptime.
              </li>
              <li>
                Optimized SQL Server and PostgreSQL databases by refining
                complex queries, stored procedures, and indexing strategies,
                reducing execution time from minutes to 2 seconds.
              </li>
              <li>
                Designed and developed backend services for payroll and
                attendance systems, automating workflows and reducing processing
                time by 7 days.
              </li>
              <li>
                Implemented secure authentication and authorization using
                OAuth2, JWT, and RBAC.
              </li>
            </ul>
          </article>

          <article>
            <h4 className="font-semibold">
              Pathmazing Inc. — Software Engineer
            </h4>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
              Mar 2021 – Sep 2021 • Phnom Penh, Cambodia
            </p>
            <ul className="list-disc ml-lg mt-xs space-y-1 text-body text-light-text-secondary dark:text-dark-text-secondary">
              <li>
                Designed and integrated secure RESTful APIs with third-party
                payment systems, improving transaction reliability by 30%.
              </li>
              <li>
                Developed and maintained backend services using C# and .NET,
                supporting ERP integrations and reducing manual operational
                workflows.
              </li>
              <li>
                Optimized SQL queries and database performance, improving system
                efficiency by 20%.
              </li>
            </ul>
          </article>

          <article>
            <h4 className="font-semibold">
              Anakut Digital Solution, Co. Ltd — Software Engineer
            </h4>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
              Oct 2020 – May 2021 • Phnom Penh, Cambodia
            </p>
            <ul className="list-disc ml-lg mt-xs space-y-1 text-body text-light-text-secondary dark:text-dark-text-secondary">
              <li>
                Developed scalable backend systems for POS and management
                platforms, reducing manual data entry errors by 99%.
              </li>
              <li>
                Built and maintained RESTful APIs for ERP-integrated systems,
                enabling real-time data tracking and reporting.
              </li>
            </ul>
          </article>

          <article>
            <h4 className="font-semibold">Arrow Dot — Software Engineer</h4>
            <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
              Oct 2019 – Oct 2020 • Phnom Penh, Cambodia
            </p>
            <ul className="list-disc ml-lg mt-xs space-y-1 text-body text-light-text-secondary dark:text-dark-text-secondary">
              <li>
                Developed and deployed web applications using CI/CD pipelines,
                Docker, and Kubernetes, improving deployment reliability and
                reducing manual release effort.
              </li>
            </ul>
          </article>

          <h3 className="text-h4 font-semibold mt-lg">PROJECTS</h3>
          <div className="space-y-sm">
            <div>
              <h4 className="font-semibold">AI-Powered Portfolio — Feb 2025</h4>
              <p className="text-body text-light-text-secondary dark:text-dark-text-secondary">
                Built and integrated AI-powered features using LLM APIs to
                enhance user interaction and dynamic content generation,
                increasing session duration by 35% for 150+ weekly users.
              </p>
            </div>
            <div>
              <h4 className="font-semibold">Angkor Milk Meal App — Sep 2023</h4>
              <p className="text-body text-light-text-secondary dark:text-dark-text-secondary">
                Collaborated with users to optimize SQL queries and reporting
                features, delivering faster data insights and improving
                usability for 500+ users.
              </p>
            </div>
          </div>

          <h3 className="text-h4 font-semibold mt-lg">EDUCATION & HONORS</h3>
          <div className="space-y-sm">
            <div>
              <h4 className="font-semibold">
                Maharishi International University — Master of Science in
                Computer Science
              </h4>
              <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
                May 2024 – Dec 2026 • Fairfield, IA, USA
              </p>
            </div>
            <div>
              <h4 className="font-semibold">
                Royal University of Phnom Penh — Bachelor of Science in Computer
                Science & Engineering
              </h4>
              <p className="text-small text-light-text-secondary dark:text-dark-text-secondary">
                Nov 2016 – Nov 2020 • Phnom Penh, Cambodia
              </p>
            </div>
            <div>
              <h4 className="font-semibold">Work Authorization</h4>
              <p className="text-body text-light-text-secondary dark:text-dark-text-secondary">
                Authorized to work in the U.S. without current or future
                employer sponsorship.
              </p>
            </div>
            <div>
              <h4 className="font-semibold">Honors</h4>
              <p className="text-body text-light-text-secondary dark:text-dark-text-secondary">
                Best Project & Team Effort of The Year – Khmer Care, Pathmazing
                Inc (06/2021)
              </p>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
