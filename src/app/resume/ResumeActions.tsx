"use client";

type ResumeActionsProps = {
  markdownHref: string;
};

export default function ResumeActions({ markdownHref }: ResumeActionsProps) {
  return (
    <div className="resume-actions">
      <a href="/">Back to portfolio</a>
      <a
        href={markdownHref}
        download
        className="px-md py-sm rounded-md bg-light-primary dark:bg-dark-primary text-white font-semibold"
      >
        Download PDF
      </a>
      <button
        type="button"
        onClick={() => window.print()}
        className="px-md py-sm rounded-md border border-light-border dark:border-dark-border"
      >
        Print
      </button>
    </div>
  );
}
