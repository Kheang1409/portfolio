# Professional Software Engineer Portfolio

A production-ready, fully responsive portfolio website built with **Next.js 14**, **TypeScript**, **Tailwind CSS**, and **Framer Motion**. Features seamless dark/light mode switching, smooth animations, and WCAG accessibility compliance.

## ✨ Features

- **🌗 Dark/Light Mode**: Smooth theme switching with persistence
- **📱 Fully Responsive**: Mobile-first design optimized for all devices
- **✨ Smooth Animations**: Framer Motion for delightful micro-interactions
- **📄 Resume Preview**: Interactive modal with resume preview and download functionality
- **♿ Accessibility**: WCAG contrast compliance, focus states, semantic HTML
- **⚡ Performance**: Optimized for Core Web Vitals
- **🎨 Professional Design**: Clean, modern UI with 12px spacing system
- **📦 Production Ready**: Pre-configured with best practices

## 🎯 Sections

- **Hero** - Eye-catching introduction with CTA buttons
- **About** - Professional summary & core values
- **Skills** - Categorized tech stack with progress indicators
- **Experience** - Timeline with companies, roles & achievements
- **Projects** - Featured and other projects with tech stacks
- **Education** - Degrees, GPA, and certifications
- **Contact** - Contact form and social links
- **Footer** - Minimal footer with social icons

## 🛠 Tech Stack

- **Framework**: Next.js 14
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **Animations**: Framer Motion
- **Icons**: Lucide React
- **Theme**: next-themes

## 🚀 Quick Start

### Prerequisites

- Node.js 18+ and npm
- OR: Docker & Docker Compose

### Installation & Development

#### Option 1: Local Node.js

```bash
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000)

#### Option 2: Docker (Production Build)

From project root:
```bash
docker-compose up -d
```

Access at [http://localhost:3000](http://localhost:3000)

#### Option 3: Docker (Development with Hot-Reload)

From project root:
```bash
docker-compose -f docker-compose.dev.yml up
```

Auto-reloads on source code changes

## 📜 Scripts

- `dev`: Starts Next.js in development mode
- `build`: Builds the production application
- `start`: Runs the production build
- `lint`: Runs ESLint via `next lint`
- `type-check`: Strict TypeScript check

## 📁 Project Structure

```
frontend/
├── src/
│   ├── app/
│   │   ├── globals.css       # Global styles and animations
│   │   ├── layout.tsx        # Root layout with providers
│   │   ├── page.tsx          # Home page
│   │   ├── providers.tsx     # Theme provider setup
│   │   └── api/              # API routes
│   │
│   ├── components/
│   │   ├── Navigation.tsx    # Navbar with theme toggle
│   │   ├── Footer.tsx        # Footer component
│   │   ├── ui/               # Reusable UI components
│   │   │   └── ResumeModal.tsx
│   │   │
│   │   └── sections/         # Page sections
│   │       ├── Hero.tsx      # Hero section
│   │       ├── Hero/         # Hero sub-components
│   │       ├── About.tsx
│   │       ├── Skills.tsx
│   │       ├── Experience.tsx
│   │       ├── Projects.tsx  # Projects section
│   │       ├── Projects/     # Project card components
│   │       ├── Education.tsx
│   │       ├── Contact.tsx
│   │       └── Assistant.tsx
│   │
│   └── lib/                  # Utility functions & constants
│
├── public/                   # Static assets (resume.pdf, avatar.png)
├── tailwind.config.ts        # Tailwind configuration
├── tsconfig.json            # TypeScript configuration
└── package.json
```

## 🎨 Color System

### Light Theme
- Primary: `#2563EB`, Secondary: `#0F172A`, Background: `#F8FAFC`

### Dark Theme
- Primary: `#3B82F6`, Secondary: `#E5E7EB`, Background: `#020617`

## 📱 Responsive Breakpoints

- Mobile: < 640px
- Tablet: 640px - 1024px
- Desktop: > 1024px

## 🎯 Customization

### Update Personal Information

1. **Hero Section** - Edit name and tagline in `src/components/sections/Hero/HeroComponents.tsx`
2. **About Section** - Modify content in `src/components/sections/About.tsx`
3. **Skills** - Update `skillsData` in `src/components/sections/Skills.tsx`
4. **Experience** - Modify `experiences` array in `src/components/sections/Experience.tsx`
5. **Projects** - Update via GitHub API or modify data source
6. **Education** - Modify `education` and `certifications` in `src/components/sections/Education.tsx`
7. **Contact** - Update links in `src/components/sections/Contact.tsx`

### Modify Colors

Edit `tailwind.config.ts` to customize the color palette

### Add Resume

Place your `resume.pdf` file in the `public/` directory. The ResumeModal will automatically detect it for preview and download.

## 📄 Resume Modal Feature

The Resume Modal provides:
- **Preview**: View PDF directly in an iframe
- **Download**: One-click download with custom filename
- **Responsive**: Works seamlessly on all devices
- **Modal Backdrop**: Click outside to close

Usage:
```tsx
import ResumeModal from '@/components/ui/ResumeModal';

<ResumeModal />
```

## 📦 Build & Deploy

### Build for Production

```bash
npm run build
npm start
```

### Deploy to Vercel

```bash
npm install -g vercel
vercel
```

### Deploy to Netlify

Connect your GitHub repo and set:
- Build command: `npm run build`
- Publish directory: `.next`

## ✅ Code Quality

- **No Comments**: Clean, self-documenting code
- **Separation of Concerns**: Modular component structure
- **Reusable Components**: Small, focused UI components
- **TypeScript**: Full type safety throughout
- **Responsive**: Mobile-first design approach

## ♿ Accessibility

- WCAG AA contrast compliance
- Semantic HTML structure
- Keyboard navigation support
- Focus visible states
- ARIA labels where needed

## 📄 License

Open source under the MIT License.

---

**Made with ❤️ for Software Engineers**
