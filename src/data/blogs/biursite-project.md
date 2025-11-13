---
title: "BiUrSite: A Web Application for Sharing Ideas and Seeking Advice"
date: "2025-11-11"
author: "Kai"
tags:
  [
    "Web Development",
    "Full-Stack",
    "Next.js",
    "Angular",
    ".NET",
    "Docker",
    "AWS",
  ]
---

In today's digital world, having a safe and engaging space to share ideas, thoughts, and challenges is more important than ever. **BiUrSite** is a web application I developed to allow users to express themselves, post anonymously or through verified accounts, and receive advice from a supportive community.

---

## Core Features

### Anonymous Users

- Can browse all posts without registering.
- Limited to viewing only—no interaction allowed.

### Verified Users

- Can upload posts and comment on existing posts.
- Report issues, provide feedback, and suggest improvements.
- Must verify email to ensure authenticity.

### Admin Dashboard

- View total posts, active users, and reports.
- Ban users based on behavior.
- Publish announcements visible to all users.

### Additional Features

- **Email Verification:** Ensures that users are real and active.
- **Ratings & Feedback:** Users can rate posts and provide feedback on advice given.

---

## Technical Stack

**Backend**

- .NET API (latest version)
- MSSQL Database
- Redis Distributed Cache for performance optimization
- SMTP Email Service
- xUnit for unit testing
- Role-based & policy-based authentication and authorization

**Frontend**

- Angular framework
- Initial data via `data.json`
- Admin portal built with Razor pages and API

**Deployment & DevOps**

- Docker containerization for backend, frontend, and SQL Server
- Kubernetes orchestration for cloud deployment
- AWS / Azure for production hosting
- Configuration management via `appsettings.json` (backend) and `environment.variable.ts` (frontend)

---

## Project Phases

### Phase 1: Initial Setup

- Design project structure
- Build working API with OpenAPI documentation
- Version the API for future changes
- Implement basic CRUD operations
- Write unit tests for each module

### Phase 2: Authentication & Authorization

- Implement AuthN and AuthZ
- Update unit tests to reflect new authentication logic

### Phase 3: Security Enhancements

- Fix potential vulnerabilities: DDOS, XSS, CSRF attacks

### Phase 4: Frontend Implementation

- Build frontend pages using static `data.json`
- Implement user interface for posts, comments, and feedback

### Phase 5: Integration

- Connect backend API with frontend
- Test API calls and dynamic data handling

### Phase 6: Deployment

- Containerize services using Docker
- Deploy using Kubernetes on AWS/Azure

---

## Docker Setup

**Services:**

- `sql_server_container`: SQL Server container storing application data
- `backend`: .NET API backend
- `frontend`: Angular frontend communicating with backend

**Run the Application:**

```bash
docker-compose up --build
```
