"use client";
import { ArrowUpRight, BrainCircuit, Radio, ShieldCheck } from "lucide-react";
import SectionShell from "@/components/hud/SectionShell";
export default function AICore() {
  return (
    <SectionShell
      id="ai-core"
      index="06"
      label="PORTFOLIO ASSISTANT"
      title="An easier way to explore my work."
      theme="theme-ai"
      intro="Ask focused questions about experience, technical strengths, projects, and availability."
    >
      <div className="ai-showcase">
        <div className="ai-core-visual" aria-hidden="true">
          <span>
            <BrainCircuit />
          </span>
          <i />
          <i />
          <i />
        </div>
        <div>
          <p className="system-label">
            <span>PORTFOLIO ASSISTANT</span>
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
            Chat with Kai <ArrowUpRight />
          </button>
        </div>
      </div>
    </SectionShell>
  );
}
