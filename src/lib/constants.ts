/**
 * Portfolio Customization Utilities
 * Common functions and data structures for easy customization
 */

// ============================================================================
// THEME COLORS - Edit here to change portfolio colors globally
// ============================================================================

export const COLORS = {
  LIGHT: {
    PRIMARY: "#334155", // Graphite
    SECONDARY: "#0F172A", // Deep Navy
    BACKGROUND: "#F8FAFC", // Light Gray
    SURFACE: "#FFFFFF", // White
    TEXT_PRIMARY: "#020617", // Almost Black
    TEXT_SECONDARY: "#475569", // Gray
    ACCENT: "#64748B", // Slate
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
  NAME: "Hang Kheang Taing",
  TITLE: "Software Engineer",
  TAGLINE:
    "Software Engineer building scalable backend and frontend systems for finance, ERP, and high-traffic platforms.",
  SHORT_BIO:
    "Expert in C#, .NET Core, ASP.NET Core, React/Angular, microservices, and cloud-native solutions on AWS and Azure.",
  EMAIL: "hangkheangtaing@gmail.com",
  PHONE: "+1 (641) 233-0129",
  LOCATION: "Lrving, TX, USA",
  GITHUB: "https://www.github.com/Kheang1409",
  LINKEDIN: "https://www.linkedin.com/in/hang-kheang-taing/",
  WEBSITE: "https://kaitaing.netlify.app/",
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

// Note: Components define their own data structures locally (Skills.tsx, Experience.tsx, etc.)
// This keeps data coupled with presentation and simplifies refactoring

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
 * Get initials from name (e.g., "John Doe" -> "JD")
 */
export function getInitials(name: string): string {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase();
}

// ============================================================================
// EXPORT ALL FOR EASY IMPORT
// ============================================================================

export default {
  COLORS,
  PERSONAL_INFO,
  CORE_VALUES,
  SOCIAL_LINKS,
  ANIMATION,
  SPACING,
  BREAKPOINTS,
  TYPOGRAPHY,
  getInitials,
};
