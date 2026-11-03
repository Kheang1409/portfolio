"use client";

import { ArrowLeft, Download, Printer } from "lucide-react";

type ResumeActionsProps = {
  markdownHref: string;
};

export default function ResumeActions({ markdownHref }: ResumeActionsProps) {
  return (
    <div className="resume-actions">
      <a href="/" className="resume-back-link">
        <ArrowLeft aria-hidden="true" />
        <span>Portfolio</span>
      </a>
      <a
        href={markdownHref}
        download
        className="resume-download"
      >
        <Download aria-hidden="true" />
        <span>Download PDF</span>
      </a>
      <button
        type="button"
        onClick={() => window.print()}
        aria-label="Print resume"
        title="Print resume"
      >
        <Printer aria-hidden="true" />
        <span>Print</span>
      </button>
    </div>
  );
}
