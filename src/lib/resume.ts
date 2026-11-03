import { getBackendBaseUrl } from "@/lib/server/backend";

export type Resume = {
  personals?: { legalName?: string; preferredName?: string; phone?: string; email?: string; linkedin?: string; github?: string; workAuthorization?: string };
  summary?: string;
  skills?: string[];
  experiences?: { company: string; role: string; location?: string; startDates?: string; endDate?: string; bulletPoints?: string[] }[];
  projects?: { name: string; description: string; skills?: string[] }[];
  educations?: { institution: string; degree: string; location?: string; startDates?: string; endDate?: string }[];
  certifications?: { title: string; issuer: string; date?: string }[];
};

export async function getResume(): Promise<Resume | null> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/resumes/latest`, {
    cache: "no-store",
    signal: AbortSignal.timeout(10_000),
  });
  if (response.status === 404) return null;
  if (!response.ok) {
    throw new Error(`Resume request failed (${response.status}).`);
  }
  return (await response.json()) as Resume;
}

export function formatDateRange(start?: string, end?: string): string {
  const format = (value?: string) => value ? new Date(value).toLocaleDateString("en", { month: "short", year: "numeric", timeZone: "UTC" }) : "";
  return [format(start), format(end) || (start ? "Present" : "")].filter(Boolean).join(" – ");
}
