"use client";

import { Download, X } from "lucide-react";
import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";

type ResumeModalProps = {
  trigger?: React.ReactNode;
  triggerClassName?: string;
};

export default function ResumeModal({
  trigger,
  triggerClassName = "px-xl py-sm rounded-md border-2 border-light-primary dark:border-dark-primary text-light-primary dark:text-dark-primary font-semibold hover:bg-light-primary/10 dark:hover:bg-dark-primary/10 transition-colors flex items-center gap-2 min-h-[44px]",
}: ResumeModalProps) {
  const [isOpen, setIsOpen] = useState(false);

  const handleDownload = () => {
    const link = document.createElement("a");
    link.href = "/resume.pdf";
    link.download = "Hang_Kheang_Taing_Resume.pdf";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <>
      <button onClick={() => setIsOpen(true)} className={triggerClassName}>
        {trigger || (
          <>
            <Download className="w-5 h-5" />
            Resume
          </>
        )}
      </button>

      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => setIsOpen(false)}
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-sm"
          >
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              transition={{ duration: 0.3 }}
              onClick={(e) => e.stopPropagation()}
              className="relative max-h-[90vh] w-full max-w-3xl overflow-hidden rounded-lg bg-light-background dark:bg-dark-background shadow-2xl"
            >
              <div className="sticky top-0 z-10 flex items-center justify-between border-b border-light-border dark:border-dark-border bg-light-surface dark:bg-dark-surface px-lg py-md">
                <h2 className="text-lg font-semibold text-light-text-primary dark:text-dark-text-primary">
                  Resume Preview
                </h2>
                <div className="flex items-center gap-md">
                  <button
                    onClick={handleDownload}
                    className="flex items-center gap-2 px-md py-sm rounded-md bg-light-primary dark:bg-dark-primary text-white font-semibold hover:shadow-lg transition-all duration-300"
                  >
                    <Download className="w-4 h-4" />
                    <span className="hidden sm:inline">Download</span>
                  </button>
                  <button
                    onClick={() => setIsOpen(false)}
                    className="p-2 rounded-md hover:bg-light-border dark:hover:bg-dark-border transition-colors"
                    aria-label="Close modal"
                  >
                    <X className="w-5 h-5" />
                  </button>
                </div>
              </div>

              <div className="overflow-y-auto p-lg">
                <iframe
                  src="/resume.pdf#toolbar=0"
                  className="h-[70vh] w-full rounded-md border border-light-border dark:border-dark-border"
                  title="Resume Preview"
                />
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  );
}
