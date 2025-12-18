# Components Structure

## 📦 Component Organization

```
components/
├── Navigation.tsx      # Top navbar with theme toggle
├── Footer.tsx         # Footer with social links
└── sections/          # Page sections
    ├── Hero.tsx       # Landing hero section
    ├── About.tsx      # About me section
    ├── Skills.tsx     # Skills & expertise section
    ├── Experience.tsx # Work experience timeline
    ├── Projects.tsx   # Projects showcase
    ├── Education.tsx  # Education & certifications
    └── Contact.tsx    # Contact form section
```

## 🎨 Component Features

### Navigation

- Fixed top navbar with brand logo
- Theme toggle (light/dark mode)
- Mobile responsive hamburger menu
- Smooth scroll navigation links
- Sticky positioning

**Props**: None (uses hooks internally)

**Features**:

- Auto-hides menu on link click
- Theme state with next-themes
- Accessibility-ready with aria-labels

---

### Footer

- Copyright year (auto-updated)
- Brand attribution
- Social media links (GitHub, LinkedIn, Email)
- Responsive flex layout

**Props**: None

**Features**:

- External link handling
- Icon buttons with hover effects
- Bottom section with all social icons

---

### Hero

- Large animated heading
- Tagline with CTA buttons
- Scroll-down indicator animation
- Framer Motion entrance animations

**Props**: None

**Features**:

- View Projects CTA
- Download CV button
- Staggered animation on mount
- Continuous scroll indicator animation

**Customize**:

- Update name and title in h1
- Modify tagline text
- Change CTA button links

---

### About

- Professional summary
- Core values grid
- Avatar placeholder
- Scroll-triggered animations

**Props**: None

**Features**:

- 4 core value cards
- Professional description
- Responsive 2-column grid
- Avatar section with emoji placeholder

**Customize**:

- Update biography text
- Modify core values
- Replace avatar placeholder with image

---

### Skills

- 4 skill categories (Frontend, Backend, DevOps, Tools)
- Progress indicators for each skill
- Category icons
- Animated progress bars

**Props**: None

**Data Structure**:

```typescript
interface SkillCategory {
  category: string;
  icon: React.ComponentType;
  skills: Array<{
    name: string;
    level: number; // 0-100
  }>;
}
```

**Customize**:

- Add/remove categories
- Adjust skill levels
- Add new skills to any category
- Change category icons

---

### Experience

- Timeline layout (vertical on mobile, horizontal on desktop)
- Company, role, duration, location
- Achievement bullet points
- Responsive grid

**Props**: None

**Data Structure**:

```typescript
interface Experience {
  company: string;
  role: string;
  duration: string;
  location: string;
  description: string;
  achievements: string[];
}
```

**Features**:

- Timeline dots and connectors
- Responsive layout
- Achievement icons (✓)
- Location and date badges

---

### Projects

- Featured projects grid (2 columns)
- Additional projects list (3 columns)
- Tech stack tags
- GitHub and Demo buttons
- Placeholder project images

**Props**: None

**Data Structure**:

```typescript
interface Project {
  title: string;
  description: string;
  tech: string[];
  github: string;
  demo: string;
  featured: boolean;
}
```

**Features**:

- 2-column featured grid
- 3-column other projects grid
- Tech badges
- External links to GitHub and demos
- Hover effects on cards

---

### Education

- Education details with GPA
- Certifications grid
- Award icons
- Responsive 2-column layout

**Props**: None

**Data Structures**:

```typescript
interface Education {
  school: string;
  degree: string;
  year: string;
  gpa: string;
  highlights: string[];
}

interface Certification {
  name: string;
  issuer: string;
  year: string;
  credential: string;
}
```

**Features**:

- Education highlights
- Year and GPA display
- Certification list with hover effects
- Credential links

---

### Contact

- Contact information cards (email, LinkedIn, GitHub)
- Contact form (name, email, message)
- Form validation and loading states
- Success/error messages
- Responsive 2-column layout

**Props**: None

**Features**:

- Form state management
- Loading spinner
- Success message
- Error handling
- 1500ms simulated submission (replace with actual API)

**Customize**:

- Update contact info links
- Modify form submission endpoint
- Add form fields as needed

---

## 🎯 Animation Patterns

All components use consistent animation patterns:

### Fade In

```typescript
const fadeInVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0 },
};
```

### Stagger Container

```typescript
const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.2,
      delayChildren: 0.1,
    },
  },
};
```

### Scroll Trigger

```typescript
whileInView="visible"
viewport={{ once: true, margin: "-100px" }}
```

---

## 🎨 Styling System

All components use Tailwind CSS with custom colors:

### Light Theme Colors

```
bg-light-background    #F8FAFC
bg-light-surface       #FFFFFF
text-light-text-primary     #020617
text-light-text-secondary   #475569
text-light-primary     #2563EB
text-light-accent      #38BDF8
```

### Dark Theme Colors

```
bg-dark-background     #020617
bg-dark-surface        #0F172A
text-dark-text-primary      #F8FAFC
text-dark-text-secondary    #94A3B8
text-dark-primary      #3B82F6
text-dark-accent       #22D3EE
```

---

## 📱 Responsive Breakpoints

```
Mobile:    < 640px   (single column)
Tablet:    640-1024px (2 columns)
Desktop:   > 1024px   (3+ columns)
```

All components use Tailwind's responsive prefixes:

- `md:` for tablet and up
- `lg:` for desktop and up

---

## ♿ Accessibility Features

- Semantic HTML (`<section>`, `<h1>`, `<button>`)
- ARIA labels on buttons and links
- Focus visible states
- Keyboard navigation support
- Color contrast compliance (WCAG AA)
- Motion preferences respected

---

## 🔧 Component Props & Configuration

### Adding New Section

1. Create new file in `sections/` folder
2. Export as default component
3. Import in `src/app/page.tsx`
4. Add to main layout

Example:

```typescript
// src/components/sections/NewSection.tsx
export default function NewSection() {
  return (
    <section id="new-section" className="py-4xl md:py-[100px]">
      {/* Your content */}
    </section>
  );
}
```

---

## 📊 Data Flow

```
layout.tsx
    ↓
page.tsx (imports all sections)
    ↓
Hero → About → Skills → Experience → Projects → Education → Contact
    ↓
Components (with Framer Motion)
    ↓
DOM (rendered with Tailwind CSS)
```

---

## 🚀 Performance Optimization

- Components use `next/image` for images (when added)
- No unnecessary re-renders (static data)
- Framer Motion optimized for performance
- CSS-in-JS kept minimal (Tailwind only)
- Lazy component loading with dynamic imports (optional)

---

## 💡 Tips

1. **Reuse animations**: Copy `fadeInVariants` pattern to new sections
2. **Consistent spacing**: Use spacing values from tailwind.config.ts
3. **Color consistency**: Always use color classes, never hex values
4. **Component modularity**: Keep sections independent
5. **Testing**: Test components in isolation before adding to page

---

For more details, see the main [README.md](../README.md) and [IMPLEMENTATION_GUIDE.md](../IMPLEMENTATION_GUIDE.md).
