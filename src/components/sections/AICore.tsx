"use client";
import { ArrowUpRight, BrainCircuit, Radio, ShieldCheck } from "lucide-react";
import Image from "next/image";
import SectionShell from "@/components/hud/SectionShell";
export default function AICore() {
  return (
    <SectionShell
      id="ai-core"
      index="06"
      label="INTELLIGENCE SYSTEM"
      title="A guided interface to my work."
      theme="theme-ai"
      intro="Ask focused questions about experience, technical strengths, projects, and availability."
    >
      <div className="ai-showcase">
        <div className="ai-core-visual" aria-hidden="true">
          <Image
            src="/images/backgrounds/software-core.webp"
            alt=""
            fill
            sizes="(max-width: 760px) 90vw, 40vw"
            className="ai-core-visual__image"
          />
          <span>
            <BrainCircuit />
          </span>
          <i />
          <i />
          <i />
        </div>
        <div>
          <p className="system-label">
            <span>AI CORE</span> // PORTFOLIO KNOWLEDGE
          </p>
          <h3>Contextual. Conversational. Grounded.</h3>
          <p>
            The assistant streams responses from the portfolio knowledge system
            while keeping conversation history in your browser session.
          </p>
          <ul>
            <li>
              <Radio /> Streaming responses
            </li>
            <li>
              <ShieldCheck /> Source-grounded portfolio context
            </li>
            <li>
              <BrainCircuit /> Career and project navigation
            </li>
          </ul>
          <button
            className="energy-button"
            onClick={() =>
              document
                .querySelector<HTMLButtonElement>(".assistant-launcher")
                ?.click()
            }
          >
            OPEN AI CORE <ArrowUpRight />
          </button>
        </div>
      </div>
    </SectionShell>
  );
}
