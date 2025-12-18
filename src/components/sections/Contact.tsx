"use client";

import {
  Mail,
  Loader2,
  CheckCircle2,
  Linkedin,
  Github,
  Phone,
} from "lucide-react";
import { motion } from "framer-motion";
import { postContact } from "@/lib/contacts";
import type { ContactRequest } from "@/lib/types";
import { useState, FormEvent } from "react";

export default function Contact() {
  const [formState, setFormState] = useState({
    name: "",
    email: "",
    message: "",
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitStatus, setSubmitStatus] = useState<
    "idle" | "loading" | "success" | "error"
  >("idle");

  const fadeInVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: { opacity: 1, y: 0 },
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormState((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setIsSubmitting(true);
    setSubmitStatus("loading");

    const payload: ContactRequest = {
      name: formState.name.trim(),
      email: formState.email.trim(),
      message: formState.message.trim(),
    };

    try {
      await postContact(payload);
      setSubmitStatus("success");
      setFormState({ name: "", email: "", message: "" });
      setTimeout(() => setSubmitStatus("idle"), 3000);
    } catch (error) {
      setSubmitStatus("error");
      setTimeout(() => setSubmitStatus("idle"), 3000);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section
      id="contact"
      className="py-4xl md:py-[100px] bg-light-background dark:bg-dark-background"
    >
      <div className="max-w-container mx-auto px-sm md:px-lg">
        <motion.div
          initial="hidden"
          whileInView="visible"
          variants={fadeInVariants}
          transition={{ duration: 0.5 }}
          viewport={{ once: true, margin: "-100px" }}
          className="mb-3xl text-center"
        >
          <h2 className="text-h2 font-bold text-light-text-primary dark:text-dark-text-primary mb-sm">
            Contact
          </h2>
          <div className="w-12 h-1 bg-light-primary dark:bg-dark-primary rounded-full mx-auto mb-md" />
          <p className="text-lg text-light-text-secondary dark:text-dark-text-secondary max-w-2xl mx-auto">
            Send a message to collaborate or say hello.
          </p>
        </motion.div>

        <div className="grid md:grid-cols-2 gap-3xl max-w-4xl mx-auto">
          <motion.div
            initial="hidden"
            whileInView="visible"
            variants={fadeInVariants}
            transition={{ duration: 0.5, delay: 0.1 }}
            viewport={{ once: true, margin: "-100px" }}
            className="space-y-lg"
          >
            <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
              Contact details
            </h3>

            {[
              {
                icon: Mail,
                label: "Email",
                value: "hangkheangtaing@gmail.com",
                href: "mailto:hangkheangtaing@gmail.com",
              },
              {
                icon: Phone,
                label: "Phone",
                value: "+1 641 233 0129",
                href: "tel:+16412330129",
              },
              {
                icon: Linkedin,
                label: "LinkedIn",
                value: "linkedin.com/in/hang-kheang-taing",
                href: "https://www.linkedin.com/in/hang-kheang-taing",
              },
              {
                icon: Github,
                label: "GitHub",
                value: "github.com/Kheang1409",
                href: "https://github.com/Kheang1409",
              },
            ].map((item, idx) => {
              const Icon = item.icon;
              return (
                <motion.a
                  key={idx}
                  href={item.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  initial="hidden"
                  whileInView="visible"
                  variants={fadeInVariants}
                  transition={{ duration: 0.4, delay: 0.1 + idx * 0.05 }}
                  viewport={{ once: true, margin: "-100px" }}
                  className="flex items-start gap-md p-md rounded-lg bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border hover:border-light-primary/50 dark:hover:border-dark-primary/50 transition-all group"
                >
                  <div className="p-2xs rounded-md bg-light-primary/10 dark:bg-dark-primary/10 text-light-primary dark:text-dark-primary group-hover:bg-light-primary/20 dark:group-hover:bg-dark-primary/20 transition-colors flex-shrink-0">
                    <Icon className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-small font-medium text-light-text-secondary dark:text-dark-text-secondary mb-xs">
                      {item.label}
                    </p>
                    <p className="text-body font-semibold text-light-text-primary dark:text-dark-text-primary group-hover:text-light-primary dark:group-hover:text-dark-primary transition-colors">
                      {item.value}
                    </p>
                  </div>
                </motion.a>
              );
            })}
          </motion.div>

          <motion.form
            onSubmit={handleSubmit}
            initial="hidden"
            whileInView="visible"
            variants={fadeInVariants}
            transition={{ duration: 0.5, delay: 0.15 }}
            viewport={{ once: true, margin: "-100px" }}
            className="space-y-md"
          >
            <h3 className="text-h3 font-semibold text-light-text-primary dark:text-dark-text-primary">
              Send a message
            </h3>

            <motion.div
              initial="hidden"
              whileInView="visible"
              variants={fadeInVariants}
              transition={{ duration: 0.4, delay: 0.2 }}
              viewport={{ once: true, margin: "-100px" }}
            >
              <label className="block text-small font-medium text-light-text-primary dark:text-dark-text-primary mb-xs">
                Name
              </label>
              <input
                type="text"
                name="name"
                value={formState.name}
                onChange={handleChange}
                required
                className="w-full px-md py-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border text-light-text-primary dark:text-dark-text-primary focus:outline-none focus:border-light-primary dark:focus:border-dark-primary focus:ring-2 focus:ring-light-primary/20 dark:focus:ring-dark-primary/20 transition-all"
                placeholder="Your name"
              />
            </motion.div>

            <motion.div
              initial="hidden"
              whileInView="visible"
              variants={fadeInVariants}
              transition={{ duration: 0.4, delay: 0.25 }}
              viewport={{ once: true, margin: "-100px" }}
            >
              <label className="block text-small font-medium text-light-text-primary dark:text-dark-text-primary mb-xs">
                Email
              </label>
              <input
                type="email"
                name="email"
                value={formState.email}
                onChange={handleChange}
                required
                className="w-full px-md py-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border text-light-text-primary dark:text-dark-text-primary focus:outline-none focus:border-light-primary dark:focus:border-dark-primary focus:ring-2 focus:ring-light-primary/20 dark:focus:ring-dark-primary/20 transition-all"
                placeholder="hangkheangtaing@gmail.com"
              />
            </motion.div>

            <motion.div
              initial="hidden"
              whileInView="visible"
              variants={fadeInVariants}
              transition={{ duration: 0.4, delay: 0.3 }}
              viewport={{ once: true, margin: "-100px" }}
            >
              <label className="block text-small font-medium text-light-text-primary dark:text-dark-text-primary mb-xs">
                Message
              </label>
              <textarea
                name="message"
                value={formState.message}
                onChange={handleChange}
                required
                rows={4}
                className="w-full px-md py-sm rounded-md bg-light-surface dark:bg-dark-surface border border-light-border dark:border-dark-border text-light-text-primary dark:text-dark-text-primary focus:outline-none focus:border-light-primary dark:focus:border-dark-primary focus:ring-2 focus:ring-light-primary/20 dark:focus:ring-dark-primary/20 transition-all resize-none"
                placeholder="Your message here..."
              />
            </motion.div>

            <motion.button
              initial="hidden"
              whileInView="visible"
              variants={fadeInVariants}
              transition={{ duration: 0.4, delay: 0.35 }}
              viewport={{ once: true, margin: "-100px" }}
              type="submit"
              disabled={isSubmitting || submitStatus === "success"}
              className="w-full px-md py-sm rounded-md bg-light-primary dark:bg-dark-primary text-white font-semibold hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transform hover:scale-105 active:scale-95 transition-all duration-300 flex items-center justify-center gap-2 min-h-[44px]"
            >
              {submitStatus === "loading" && (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  Sending...
                </>
              )}
              {submitStatus === "success" && (
                <>
                  <CheckCircle2 className="w-5 h-5" />
                  Message Sent!
                </>
              )}
              {submitStatus === "idle" && "Send message"}
            </motion.button>

            {submitStatus === "success" && (
              <p className="text-small text-green-600 dark:text-green-400 text-center">
                Thanks for your message! I&apos;ll get back to you soon.
              </p>
            )}
            {submitStatus === "error" && (
              <p className="text-small text-red-600 dark:text-red-400 text-center">
                Something went wrong. Please try again.
              </p>
            )}
          </motion.form>
        </div>
      </div>
    </section>
  );
}
