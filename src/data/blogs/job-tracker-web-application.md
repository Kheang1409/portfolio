---
title: "Job Tracker Web Application: A Modern .NET Microservices Project"
date: "2025-11-11"
slug: "job-tracker-web-application"
---

The **Job Tracker Web Application** is designed to help users manage job listings, applications, and status updates. This project was developed solo to explore microservices, Domain-Driven Design (DDD), CQRS (Command Query Responsibility Segregation), and the full software development lifecycle. It leverages modern .NET best practices for scalability, clean code, and maintainability.

---

## Key Features

- **Job Listings Management:** Create, read, update, and delete job postings.
- **Job Applications:** Users can apply for jobs and track their application status.
- **CQRS:** Commands (write operations) and queries (read operations) are clearly separated for improved scalability.
- **Email Notifications:** Automated emails notify users when job statuses change.
- **Microservices Architecture:** The application is modularized into microservices to enhance maintainability and scalability.
- **Clean Code & DDD:** Implements Domain-Driven Design principles for a maintainable and scalable architecture.

---

## Tech Stack

- **Backend:** .NET 6+ (C#)
- **Architecture:** Microservices, CQRS, Domain-Driven Design
- **Frameworks & Tools:**
  - MediatR: CQRS command and query handling
  - SendGrid / SMTP: Email notifications
  - Serilog: Structured logging
  - Moq / NUnit: Unit testing
  - Docker (optional): Containerization
  - CI/CD: GitHub Actions or GitLab CI pipelines
  - EF Core: Database ORM

---

## Setup

### Prerequisites

Before running the application, ensure you have:

- **.NET 6+ SDK** installed ([Download .NET](https://dotnet.microsoft.com/download))
- **Docker** (optional, for containerization)
- **SMTP/SendGrid account** for email notifications

---

### Installation Steps

1. **Clone the Repository**

```bash
git clone git@github.com:Kheang1409/job-tracker-api.git
cd job-tracker-api
```

2. **Install Dependencies**

```bash
dotnet restore
```

3. **Configure Environment Variables**

- Set up `.env` or `appsettings.json` with email credentials and database connection strings.

4. **Run the Application**

```bash
dotnet run --project JobTracker.Api
```

5. **Optional: Run with Docker**

```bash
docker-compose up --build
```

---

## What I Learned

Building this project taught me a wide range of technical and architectural skills:

- **Domain-Driven Design (DDD):** Learned how to model complex business domains using aggregates, entities, and value objects.
- **CQRS Implementation:** Understood the benefits of separating write operations (commands) from read operations (queries) to improve scalability and maintainability.
- **Microservices Architecture:** Gained practical experience decomposing a monolithic application into loosely coupled services.
- **Asynchronous Email Handling:** Integrated SendGrid/SMTP for real-time email notifications using background services.
- **Unit Testing & Mocks:** Learned to write effective tests for CQRS commands, queries, and services using Moq and NUnit.
- **Logging & Observability:** Implemented structured logging with Serilog for better monitoring and debugging.
- **Containerization & CI/CD:** Practiced containerizing the services with Docker and setting up continuous integration pipelines with GitHub Actions or GitLab CI.
- **Clean Code & Maintainability:** Applied SOLID principles and DDD to produce modular, maintainable, and testable code.

---

This project demonstrates modern .NET architecture principles while providing a functional job tracking platform. It serves as a practical example of scalable, maintainable, and testable enterprise-grade application design.

---

If you want, I can **also add a “Next Steps / Future Enhancements” section linked to the lessons learned**, so it not only shows what you learned but also what you plan to do next, making it more engaging for readers and potential employers. Do you want me to do that?

```

```
