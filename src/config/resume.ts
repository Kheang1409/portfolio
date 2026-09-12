export type ResumeExperience = {
  company: string;
  location: string;
  role: string;
  dates: string;
  achievements: string[];
};

export const resumeSummary =
  "Software Engineer experienced in designing, maintaining, modernizing, and optimizing enterprise applications using C#, .NET, ASP.NET Core, SQL, Azure, and AWS. Experienced in supporting business-critical legacy systems while incrementally modernizing monolithic applications into scalable, cloud-native and microservices-based architectures. Proven track record of reducing system crashes by 40%, accelerating database workloads from minutes to seconds, and improving incident response by 50%. Strong background in backend engineering, legacy modernization, distributed systems, database optimization, and production reliability.";

export const resumeSkills = [
  [
    "Languages & Frameworks",
    "C#, .NET, .NET Core, ASP.NET Core, JavaScript, TypeScript",
  ],
  [
    "Backend & APIs",
    "RESTful APIs, Microservices, Distributed Systems, API Versioning, CQRS, Dependency Injection",
  ],
  [
    "Frontend",
    "Angular, React, Next.js, HTML5, CSS3, Bootstrap, Material UI, Tailwind CSS",
  ],
  [
    "Databases & Data Access",
    "SQL Server, PostgreSQL, MongoDB, T-SQL, Entity Framework Core, Dapper, Query Optimization, Indexing",
  ],
  [
    "Cloud & DevOps",
    "AWS (Lambda, EC2, S3, RDS), Azure (Functions, App Services), Docker, Kubernetes, CI/CD, GitHub Actions, Azure DevOps",
  ],
  [
    "Architecture",
    "MVC, MVVM, Clean Architecture, DDD, SOLID, Design Patterns, Retry & Circuit Breaker Patterns",
  ],
  [
    "Modernization & Integration",
    "Legacy Modernization, Monolith-to-Microservices, Incremental Refactoring, Kafka, Redis, SignalR",
  ],
  [
    "Security & Quality",
    "OAuth 2.0, JWT, RBAC, Unit Testing, Integration Testing, Production Troubleshooting",
  ],
  [
    "AI",
    "LLM API Integration, Retrieval-Augmented Generation (RAG), AI-Powered Applications",
  ],
] as const;

export const resumeExperience: ResumeExperience[] = [
  {
    company: "Better & Best Inc.",
    location: "Oregon, WI, USA",
    role: "Software Engineer",
    dates: "Apr 2026 – Present",
    achievements: [
      "Engineer and maintain backend services for POS, billing, and order-processing systems using C#, ASP.NET Core, AWS, and SQL, supporting reliable business-critical transaction workflows.",
      "Modernize legacy application components through incremental refactoring and service decomposition, improving maintainability and scalability while preserving existing business operations.",
      "Design RESTful APIs and AWS Lambda functions using C# and Dapper to integrate existing workflows with cloud-based services and real-time data processing.",
      "Optimize SQL queries, indexes, schemas, and data-access patterns to reduce latency and improve stability during peak transaction periods.",
      "Diagnose production bottlenecks across application, API, and database layers and implement performance and resiliency improvements.",
      "Collaborate with cross-functional stakeholders to deliver scalable solutions using AWS, Docker, and Kubernetes.",
    ],
  },
  {
    company: "Sahakrinpheap Microfinance PLC",
    location: "Phnom Penh, Cambodia",
    role: "Software Engineer",
    dates: "Sep 2021 – May 2024",
    achievements: [
      "Modernized legacy banking applications by decomposing monolithic components into C# and ASP.NET Core microservices, reducing system crashes by 40%.",
      "Designed event-driven monitoring and alerting services on Azure, improving incident response by 50% and contributing to 99% system uptime.",
      "Optimized SQL Server and PostgreSQL workloads through query tuning, indexing, and stored procedure optimization, reducing critical queries from minutes to seconds.",
      "Developed payroll and attendance systems that automated business workflows and reduced monthly manual processing time by 7 days.",
      "Built RESTful APIs and backend services supporting banking operations, internal applications, and third-party integrations while implementing OAuth 2.0, JWT, and RBAC security.",
      "Led Agile delivery and mentored junior engineers through code reviews and technical guidance, improving release cycles by 15% and reducing onboarding time by 30%.",
    ],
  },
  {
    company: "Pathmazing Inc.",
    location: "Phnom Penh, Cambodia",
    role: "Software Engineer",
    dates: "Mar 2021 – Sep 2021",
    achievements: [
      "Designed secure RESTful APIs using C#/.NET to integrate enterprise applications with third-party payment services, improving transaction reliability by 30%.",
      "Developed backend services for ERP systems and workflow automation while supporting production workloads using Azure and Kubernetes.",
      "Optimized SQL queries, stored procedures, and reporting pipelines, reducing execution time from approximately 30 minutes to a few minutes.",
    ],
  },
  {
    company: "Anakut Digital Solution, Co. Ltd",
    location: "Phnom Penh, Cambodia",
    role: "Software Engineer",
    dates: "Oct 2020 – May 2021",
    achievements: [
      "Developed C#/.NET backend services for POS and ERP platforms, automating workflows and reducing manual data-entry errors by 99%.",
      "Built RESTful APIs for system integration and real-time reporting and implemented SignalR features that improved application responsiveness by 45%.",
      "Applied Clean Architecture and SOLID principles while optimizing database and data-access workflows for maintainability and performance.",
    ],
  },
  {
    company: "Arrow Dot",
    location: "Phnom Penh, Cambodia",
    role: "Software Engineer",
    dates: "Oct 2019 – Oct 2020",
    achievements: [
      "Developed web applications and RESTful backend APIs using C#, .NET, JavaScript, and SQL for business-facing systems.",
      "Optimized database queries and application data-access patterns, improving database performance by 30%.",
      "Built CI/CD workflows using Git, Docker, and Kubernetes and participated in code reviews and production troubleshooting.",
    ],
  },
];

export const resumeProjects = [
  {
    name: "AI-Powered Portfolio",
    date: "Feb 2025",
    achievements: [
      "Developed a C#/.NET AI portfolio platform with RESTful APIs, MongoDB, and LLM integration, including assistant, contact, visitor tracking, resume, and health endpoints.",
      "Built a Retrieval-Augmented Generation (RAG) pipeline using keyword-first retrieval with semantic embedding fallback and bounded context to provide grounded responses while controlling AI API usage.",
      "Implemented streaming responses, dependency injection, health checks, and automated testing to improve responsiveness, maintainability, and reliability.",
    ],
  },
  {
    name: "Angkor Milk Meal App",
    date: "Sep 2023",
    achievements: [
      "Optimized SQL queries and reporting features for a meal-management application serving 500+ users, improving performance and access to operational data.",
    ],
  },
] as const;

export const resumeEducation = [
  {
    degree: "Master of Science in Computer Science",
    school: "Maharishi International University",
    location: "Fairfield, IA, USA",
    dates: "May 2024 – Dec 2026",
  },
  {
    degree: "Bachelor of Science in Computer Science & Engineering",
    school: "Royal University of Phnom Penh",
    location: "Phnom Penh, Cambodia",
    dates: "Nov 2016 – Nov 2020",
  },
] as const;
