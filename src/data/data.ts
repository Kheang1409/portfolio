import projectsJson from "./projects.json";
import timelineJson from "./timeline.json";
import blogsJson from "./blogs.json";

export type Project = {
  title: string;
  description: string;
  image?: string | null;
};

export type TimelineItem = {
  type: string;
  role?: string;
  degree?: string;
  company?: string;
  university?: string;
  description?: string;
  start_date?: string;
  end_date?: string | null;
};

export type BlogPost = {
  id?: number;
  title: string;
  description?: string;
  link?: string;
  url?: string;
  image?: string | null;
  date?: string;
  author?: string;
  slug?: string;
  content?: string;
  tags?: string[];
};

export type NavItem = {
  id: string;
  label: string;
  href: string;
  section?: string;
};

export const projects: Project[] = projectsJson as unknown as Project[];
export const timelines: TimelineItem[] =
  timelineJson as unknown as TimelineItem[];
export const blogs: BlogPost[] = blogsJson as unknown as BlogPost[];

export const navItems: NavItem[] = [
  { id: "home", label: "Home", href: "/", section: "home" },
  { id: "about", label: "About", href: "/", section: "about" },
  { id: "portfolio", label: "Portfolio", href: "/", section: "portfolio" },
  { id: "timeline", label: "Timeline", href: "/", section: "timeline" },
  { id: "blog", label: "Blog", href: "/", section: "blog" },
  { id: "contact", label: "Contact", href: "/", section: "contact" },
];
