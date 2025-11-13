---
title: "Portfolio Overview"
date: "2025-11-11"
slug: "portfolio-overview"
author: "Kai"
tags:
  - portfolio
  - kaiassistant
  - nextjs
  - dotnet
  - architecture
---

This post summarizes the structure and setup of my portfolio repository, including the backend API (`KaiAssistant`), the original Angular frontend, and the new Next.js frontend. It includes notes on architecture, migration, and how the blog system is implemented.

## Project Overview

### Backend: KaiAssistant

The backend is an ASP.NET Core Web API structured with Domain-Driven Design (DDD). The projects are organized as follows:

- `KaiAssistant.Domain` — domain entities and models
- `KaiAssistant.Application` — commands, queries, and handlers
- `KaiAssistant.Infrastructure` — persistence, DI, and external integrations
- `KaiAssistant.API` — HTTP endpoints, middleware, and assistant endpoints

### Frontend Migration: Angular → Next.js

The original frontend was Angular-based. I migrated to a `frontent` Next.js + TypeScript project for:

- Component-first architecture and faster iteration
- Simplified tooling and reduced bundle size
- Improved SSR/SSG support for blog posts

Key migration points:

- Routes were mapped from Angular to Next.js pages; dynamic routes like `/blog/[slug]` use static generation.
- Global styles moved to `src/styles`; components use CSS Modules.
- Local markdown posts are read at build time. `blogs.json` now indexes both internal and external posts.
- Carousel and scroll interactions were reimplemented with scroll-snap and pointer events.

### Frontend (Next.js)

The current frontend is component-based, DRY, and uses the blog system reading markdown posts from `src/data/blogs/*.md` combined with `blogs.json` for external entries.

## Key Features

- **KaiAssistant API**

  - Modular DDD structure
  - Health, assistant, and contact endpoints
  - Gemini API integration (requires `GEMINI_API_KEY`)
  - SMTP email support
  - Docker-ready with `docker-compose`

- **Frontend**
  - Blog index merging local markdown and external posts
  - BlogItem component with CSS Modules
  - Dynamic pages `/blog/[slug]` built statically at compile time

## Running the Project

### Backend (KaiAssistant)

Requirements: .NET 9 SDK, Gemini API key (optional), SMTP credentials (if sending email).

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project KaiAssistant.API
```

Docker option:

```bash
docker build -t kaiassistant-api .
docker run -p 5000:5000 \
  -e GEMINI_API_KEY=your-key \
  -e SMTP_SENDER_EMAIL=... \
  kaiassistant-api
```

### Frontend (Next.js)

```bash
cd frontend
npm install
npm run dev
```

For production:

```bash
npm run build
npm start
```

Site: `http://localhost:3000`.

## Blog System Notes

- Local markdown posts in `src/data/blogs/*.md` use frontmatter (title, date, slug).
- `blogs.json` indexes external/internal posts. Local posts override external if slugs match.
- BlogItem links open in a new tab by default.

## Suggested Improvements

- Add pagination and date sorting on `/blog`
- Include SEO meta tags and Open Graph data on posts
- Add author profile and tags for better navigation
- Expand tests for blog pages
- Add retry/backoff for Gemini API calls and rate limit handling

## Summary

The repository combines a DDD-based backend with a lean Next.js frontend. Blog posts are easy to add: create a markdown file with frontmatter and rebuild. The setup supports scalable development and clear project organization.
