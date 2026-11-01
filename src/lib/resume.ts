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
  const configuredUrl = process.env.NEXT_PUBLIC_BACKEND_API_URL?.trim();
  if (!configuredUrl) throw new Error("NEXT_PUBLIC_BACKEND_API_URL is required to load profile data");
  const baseUrl = configuredUrl.replace(/\/$/, "");
  const response = await fetch(`${baseUrl}/api/resumes/latest`, { next: { revalidate: 30 } });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error(`Resume request failed (${response.status})`);
  return (await response.json()) as Resume;
}

export function formatDateRange(start?: string, end?: string): string {
  const format = (value?: string) => value ? new Date(value).toLocaleDateString("en", { month: "short", year: "numeric", timeZone: "UTC" }) : "";
  return [format(start), format(end) || (start ? "Present" : "")].filter(Boolean).join(" – ");
}
