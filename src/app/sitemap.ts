import type { MetadataRoute } from "next";
import { siteConfig } from "@/lib/seo";

const staticRoutes = ["/"];

export default function sitemap(): MetadataRoute.Sitemap {
  return staticRoutes.map((path) => ({
    url: path === "/" ? siteConfig.url : `${siteConfig.url}${path}`,
    lastModified: new Date(),
    changeFrequency: "weekly",
    priority: path === "/" ? 1 : 0.8,
  }));
}
