/**
 * Portfolio Customization Utilities
 * Common functions and data structures for easy customization
 */

// ============================================================================
// THEME COLORS - Edit here to change portfolio colors globally
// ============================================================================

export const COLORS = {
  LIGHT: {
    PRIMARY: "#2563EB", // Professional Blue
    SECONDARY: "#0F172A", // Deep Navy
    BACKGROUND: "#F8FAFC", // Light Gray
    SURFACE: "#FFFFFF", // White
    TEXT_PRIMARY: "#020617", // Almost Black
    TEXT_SECONDARY: "#475569", // Gray
    ACCENT: "#38BDF8", // Soft Cyan
    BORDER: "#E2E8F0", // Light Border
  },
  DARK: {
    PRIMARY: "#3B82F6", // Bright Blue
    SECONDARY: "#E5E7EB", // Light Gray
    BACKGROUND: "#020617", // Almost Black
    SURFACE: "#0F172A", // Dark Navy
    TEXT_PRIMARY: "#F8FAFC", // Almost White
    TEXT_SECONDARY: "#94A3B8", // Light Gray
    ACCENT: "#22D3EE", // Bright Cyan
    BORDER: "#1E293B", // Dark Border
  },
};

// ============================================================================
// PERSONAL INFORMATION
// ============================================================================

export const PERSONAL_INFO = {
  NAME: "Kai",
  TITLE: "Software Engineer",
  TAGLINE:
    "I craft elegant solutions to complex problems using cutting-edge technologies.",
  SHORT_BIO:
    "Full-stack software engineer with 5+ years of experience building scalable applications.",
  EMAIL: "hello@example.com",
  PHONE: "+1 (555) 123-4567",
  LOCATION: "San Francisco, CA",
  GITHUB: "https://github.com/yourprofile",
  LINKEDIN: "https://linkedin.com/in/yourprofile",
  WEBSITE: "https://yourwebsite.com",
};

// ============================================================================
// CORE VALUES (About Section)
// ============================================================================

export const CORE_VALUES = [
  {
    title: "Problem Solving",
    description: "Breaking down complex issues into elegant solutions",
  },
  {
    title: "Clean Code",
    description: "Writing maintainable, well-documented code",
  },
  {
    title: "Continuous Learning",
    description: "Always evolving with the latest technologies",
  },
  {
    title: "User Focus",
    description: "Designing with user experience in mind",
  },
];

// ============================================================================
// SKILLS DATA STRUCTURE
// ============================================================================

export interface Skill {
  name: string;
  level: number; // 0-100
}

export interface SkillCategory {
  category: string;
  icon: string; // Lucide icon name or similar
  skills: Skill[];
}

export const SKILLS_EXAMPLE: SkillCategory[] = [
  {
    category: "Frontend",
    icon: "Code2",
    skills: [
      { name: "React", level: 90 },
      { name: "TypeScript", level: 88 },
      { name: "Next.js", level: 85 },
      { name: "Tailwind CSS", level: 92 },
    ],
  },
  {
    category: "Backend",
    icon: "Server",
    skills: [
      { name: "Node.js", level: 87 },
      { name: "C#", level: 80 },
      { name: ".NET", level: 82 },
      { name: "SQL", level: 85 },
    ],
  },
];

// ============================================================================
// EXPERIENCE DATA STRUCTURE
// ============================================================================

export interface Experience {
  company: string;
  role: string;
  duration: string;
  location: string;
  description: string;
  achievements: string[];
}

export const EXPERIENCE_EXAMPLE: Experience[] = [
  {
    company: "Tech Corp",
    role: "Senior Full-Stack Engineer",
    duration: "2022 - Present",
    location: "San Francisco, CA",
    description: "Leading development of microservices architecture...",
    achievements: [
      "Architected cloud-native solutions using Azure",
      "Reduced API response time by 90%",
      "Mentored 5 junior developers",
    ],
  },
];

// ============================================================================
// PROJECTS DATA STRUCTURE
// ============================================================================

export interface Project {
  title: string;
  description: string;
  tech: string[];
  github: string;
  demo: string;
  featured: boolean;
}

export const PROJECTS_EXAMPLE: Project[] = [
  {
    title: "AI Assistant Platform",
    description:
      "Full-stack web application for AI-powered customer support...",
    tech: ["React", "Node.js", "MongoDB", "Azure"],
    github: "https://github.com/yourprofile/project",
    demo: "https://example.com",
    featured: true,
  },
];

// ============================================================================
// EDUCATION DATA STRUCTURE
// ============================================================================

export interface Education {
  school: string;
  degree: string;
  year: string;
  gpa: string;
  highlights: string[];
}

export interface Certification {
  name: string;
  issuer: string;
  year: string;
  credential: string;
}

export const EDUCATION_EXAMPLE: Education[] = [
  {
    school: "University of Technology",
    degree: "B.S. Computer Science",
    year: "2019",
    gpa: "3.8 / 4.0",
    highlights: ["Dean's List", "Full Scholarship", "Computer Science Award"],
  },
];

export const CERTIFICATIONS_EXAMPLE: Certification[] = [
  {
    name: "AWS Certified Solutions Architect",
    issuer: "Amazon Web Services",
    year: "2023",
    credential: "View Credential",
  },
];

// ============================================================================
// SOCIAL MEDIA LINKS
// ============================================================================

export interface SocialLink {
  icon: string; // Lucide icon name
  href: string;
  label: string;
}

export const SOCIAL_LINKS: SocialLink[] = [
  {
    icon: "Github",
    href: PERSONAL_INFO.GITHUB,
    label: "GitHub",
  },
  {
    icon: "Linkedin",
    href: PERSONAL_INFO.LINKEDIN,
    label: "LinkedIn",
  },
  {
    icon: "Mail",
    href: `mailto:${PERSONAL_INFO.EMAIL}`,
    label: "Email",
  },
];

// ============================================================================
// ANIMATION SETTINGS
// ============================================================================

export const ANIMATION = {
  FADE_IN_DURATION: 0.5,
  STAGGER_DELAY: 0.2,
  TRANSITION_DURATION: 0.3,
  SCROLL_MARGIN: "-100px",
};

// ============================================================================
// SPACING SYSTEM (8px base)
// ============================================================================

export const SPACING = {
  XS: "4px", // 2xs
  SM: "8px", // xs
  MD: "16px", // sm
  LG: "24px", // md
  XL: "32px", // lg
  "2XL": "48px", // xl
  "3XL": "64px", // 2xl
  "4XL": "80px", // 3xl
};

// ============================================================================
// BREAKPOINTS
// ============================================================================

export const BREAKPOINTS = {
  MOBILE: 640, // < 640px
  TABLET: 1024, // 640px - 1024px
  DESKTOP: 1280, // > 1280px
};

// ============================================================================
// TYPOGRAPHY
// ============================================================================

export const TYPOGRAPHY = {
  FONT_PRIMARY: "Inter, SF Pro, Roboto, sans-serif",
  FONT_MONO: "JetBrains Mono, monospace",
  H1: {
    SIZE: "48px",
    WEIGHT: "700",
    LINE_HEIGHT: "1.2",
  },
  H2: {
    SIZE: "32px",
    WEIGHT: "600",
    LINE_HEIGHT: "1.3",
  },
  H3: {
    SIZE: "22px",
    WEIGHT: "500",
    LINE_HEIGHT: "1.4",
  },
  BODY: {
    SIZE: "16px",
    WEIGHT: "400",
    LINE_HEIGHT: "1.6",
  },
  SMALL: {
    SIZE: "14px",
    WEIGHT: "400",
    LINE_HEIGHT: "1.5",
  },
};

// ============================================================================
// HELPFUL FUNCTIONS
// ============================================================================

/**
 * Format date range (e.g., "2022 - Present")
 */
export function formatDateRange(start: string, end: string): string {
  return `${start} - ${end}`;
}

/**
 * Calculate years of experience
 */
export function calculateYearsExperience(startYear: number): number {
  return new Date().getFullYear() - startYear;
}

/**
 * Get initials from name
 */
export function getInitials(name: string): string {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase();
}

/**
 * Format skill level to percentage text
 */
export function formatSkillLevel(level: number): string {
  if (level >= 90) return "Expert";
  if (level >= 75) return "Proficient";
  if (level >= 60) return "Intermediate";
  return "Learning";
}

// ============================================================================
// EXPORT ALL FOR EASY IMPORT
// ============================================================================

export default {
  COLORS,
  PERSONAL_INFO,
  CORE_VALUES,
  SKILLS_EXAMPLE,
  EXPERIENCE_EXAMPLE,
  PROJECTS_EXAMPLE,
  EDUCATION_EXAMPLE,
  CERTIFICATIONS_EXAMPLE,
  SOCIAL_LINKS,
  ANIMATION,
  SPACING,
  BREAKPOINTS,
  TYPOGRAPHY,
  formatDateRange,
  calculateYearsExperience,
  getInitials,
  formatSkillLevel,
};
