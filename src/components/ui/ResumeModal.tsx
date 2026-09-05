"use client";
import { Download, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
type Props = { trigger?: React.ReactNode; triggerClassName?: string };
export default function ResumeModal({
  trigger,
  triggerClassName = "button-line",
}: Props) {
  const [open, setOpen] = useState(false);
  const dialog = useRef<HTMLDialogElement>(null);
  const button = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    if (!open) return;
    dialog.current?.showModal();
    const triggerButton = button.current;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
      triggerButton?.focus();
    };
  }, [open]);
  return (
    <>
      <button
        ref={button}
        type="button"
        onClick={() => setOpen(true)}
        className={triggerClassName}
        aria-haspopup="dialog"
        aria-expanded={open}
      >
        {trigger || (
          <>
            <Download size={18} /> Resume
          </>
        )}
      </button>
      {open &&
        createPortal(
          <dialog
            ref={dialog}
            className="resume-dialog"
            aria-labelledby="resume-preview-title"
            onCancel={() => setOpen(false)}
            onClick={(e) => {
              if (e.target === e.currentTarget) setOpen(false);
            }}
          >
            <div className="resume-dialog-header">
              <h2 id="resume-preview-title">Resume preview</h2>
              <div>
                <a
                  className="button-gold"
                  href="/resume.pdf"
                  download="Hang_Kheang_Taing_Resume.pdf"
                >
                  <Download size={16} /> Download
                </a>
                <button aria-label="Close modal" onClick={() => setOpen(false)}>
                  <X />
                </button>
              </div>
            </div>
            <iframe title="Resume preview" src="/resume.pdf#toolbar=0" />
            <a className="button-line" href="/resume">
              Read accessible resume
            </a>
          </dialog>,
          document.body,
        )}
    </>
  );
}
