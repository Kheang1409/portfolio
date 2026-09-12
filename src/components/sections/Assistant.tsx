"use client";

import { useEffect, useRef, useState } from "react";
import {
  ArrowUpRight,
  Check,
  Copy,
  Plus,
  Send,
  Square,
  X,
  RotateCcw,
  Minus,
  Maximize2,
  Minimize2,
  Activity,
  Cpu,
  Radio,
  ShieldCheck,
} from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import type { Pluggable } from "unified";
import { streamAssistant } from "@/lib/assistants";
import { clearSessionId, getOrCreateSessionId } from "@/lib/session";
import type { AiMetadata, ConversationMessage } from "@/lib/api/types";

const STORAGE_KEY = "kai_assistant_history_v2";
const plugins = [rehypeHighlight as Pluggable];
type Message = {
  id: string;
  sender: "user" | "bot";
  text: string;
  status: "streaming" | "final" | "error" | "stopped";
  metadata?: AiMetadata;
};
type Retry = { text: string; userId: string; botId: string };
const id = () => crypto.randomUUID();

function Reply({ message }: { message: Message }) {
  const [copied, setCopied] = useState(false);
  const [copyError, setCopyError] = useState(false);
  useEffect(() => {
    if (!copied) return;
    const timer = setTimeout(() => setCopied(false), 2000);
    return () => clearTimeout(timer);
  }, [copied]);
  if (!message.text) return null;
  return (
    <article
      className={`kai-message kai-message--${message.sender}`}
      aria-label={message.sender === "user" ? "Your message" : "Kai's reply"}
    >
      <span className="kai-message-author">
        {message.sender === "user" ? "You" : "AI Core"}
        {message.status === "stopped" && " / Stopped"}
      </span>
      {message.sender === "bot" ? (
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          rehypePlugins={plugins}
          components={{
            a: ({ href, children }) => (
              <a
                href={href}
                target={href?.startsWith("http") ? "_blank" : undefined}
                rel="noopener noreferrer"
              >
                {children}
              </a>
            ),
          }}
        >
          {message.text}
        </ReactMarkdown>
      ) : (
        <p>{message.text}</p>
      )}
      {message.sender === "bot" && message.status !== "streaming" && (
        <div className="kai-reply-actions">
          <button
            type="button"
            aria-label={copied ? "Reply copied" : "Copy reply"}
            onClick={async () => {
              try {
                await navigator.clipboard.writeText(message.text);
                setCopied(true);
                setCopyError(false);
              } catch {
                setCopyError(true);
              }
            }}
          >
            {copied ? <Check size={13} /> : <Copy size={13} />}{" "}
            {copied ? "Copied" : "Copy"}
          </button>
          {copyError && <span role="status">Select the text to copy it.</span>}
          {message.metadata?.modelUsed && (
            <details>
              <summary>Response details</summary>
              <p>
                Model: {message.metadata.modelUsed}
                {message.metadata.cachedResponse ? " / Cached response" : ""}
                {message.metadata.fallbackUsed ? " / Fallback model" : ""}
              </p>
            </details>
          )}
        </div>
      )}
    </article>
  );
}

export default function Assistant() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState<Retry | null>(null);
  const [announcement, setAnnouncement] = useState("");
  const [confirmClear, setConfirmClear] = useState(false);
  const [panelMode, setPanelMode] = useState<
    "standard" | "minimized" | "maximized"
  >("standard");
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const launcherRef = useRef<HTMLButtonElement>(null);
  const logRef = useRef<HTMLDivElement>(null);
  const controllerRef = useRef<AbortController | null>(null);
  const stickToBottom = useRef(true);
  const sessionRef = useRef("");

  useEffect(() => {
    sessionRef.current = getOrCreateSessionId();
    try {
      const saved: unknown = JSON.parse(
        localStorage.getItem(STORAGE_KEY) || "[]",
      );
      if (Array.isArray(saved))
        setMessages(
          saved
            .filter(
              (m): m is Message =>
                !!m &&
                typeof m === "object" &&
                typeof m.id === "string" &&
                typeof m.text === "string" &&
                ["user", "bot"].includes(m.sender),
            )
            .slice(-100)
            .map((m) => ({
              ...m,
              status:
                m.status === "streaming"
                  ? "stopped"
                  : ["final", "error", "stopped"].includes(m.status)
                    ? m.status
                    : "final",
            })),
        );
    } catch {
      /* Storage is optional, including in private browsing. */
    }
    setReady(true);
    return () => controllerRef.current?.abort();
  }, []);
  useEffect(() => {
    if (ready) {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(messages.slice(-100)));
      } catch {
        /* Keep the in-memory conversation. */
      }
    }
  }, [messages, ready]);
  useEffect(() => {
    if (!open) return;
    if (panelMode !== "minimized") inputRef.current?.focus();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        if (panelMode === "maximized") setPanelMode("standard");
        else {
          setOpen(false);
          requestAnimationFrame(() => launcherRef.current?.focus());
        }
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, panelMode]);
  useEffect(() => {
    if (!open || panelMode !== "maximized") return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, [open, panelMode]);
  useEffect(() => {
    if (open && stickToBottom.current && logRef.current)
      logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [messages, loading, error, open]);
  useEffect(() => {
    const field = inputRef.current;
    if (field) {
      field.style.height = "auto";
      field.style.height = `${Math.min(field.scrollHeight, 120)}px`;
    }
  }, [input, open]);

  async function send(question = input, retrying?: Retry) {
    const text = question.trim();
    if (!text || text.length > 2000 || controllerRef.current) return;
    const controller = new AbortController();
    controllerRef.current = controller;
    const current = () =>
      controllerRef.current === controller && !controller.signal.aborted;
    const previous = retrying
      ? messages.filter(
          (m) => m.id !== retrying.userId && m.id !== retrying.botId,
        )
      : messages;
    const userId = id(),
      botId = id();
    setMessages([
      ...previous,
      { id: userId, sender: "user", text, status: "final" },
      { id: botId, sender: "bot", text: "", status: "streaming" },
    ]);
    setInput("");
    setLoading(true);
    setError(null);
    setRetry(null);
    setConfirmClear(false);
    stickToBottom.current = true;
    setAnnouncement("Kai is thinking.");
    let completed = false,
      failed = false,
      received = false;
    const fail = (code?: string) => {
      if (!current() || failed) return;
      failed = true;
      const friendly =
        code === "RATE_LIMITED" || code === "HTTP_429"
          ? "Kai is receiving too many requests. Please wait a moment, then retry."
          : "Kai couldn't finish this reply. Please retry, or explore the portfolio directly below.";
      setError(friendly);
      setRetry({ text, userId, botId });
      setAnnouncement(friendly);
      setMessages((all) =>
        all.map((m) => (m.id === botId ? { ...m, status: "error" } : m)),
      );
    };
    try {
      const history: ConversationMessage[] = previous
        .filter((m) => m.status === "final" && m.text.trim())
        .slice(-20)
        .map((m) => ({
          role: m.sender === "user" ? "user" : "assistant",
          content: m.text,
        }));
      await streamAssistant(
        {
          message: text,
          history,
          context: {
            systemPersona:
              "You are Kai, Hang Kheang Taing's portfolio assistant. Answer concisely using the supplied portfolio facts. Never invent projects, credentials, or contact details. Link visitors to relevant portfolio sections.",
            metadata: {
              sessionId: sessionRef.current,
              conversationId: sessionRef.current,
              uiSurface: "portfolio-assistant",
              locale: navigator.language,
            },
          },
        },
        {
          onDelta: (event) => {
            if (!current() || failed) return;
            received ||= !!event.text;
            setMessages((all) =>
              all.map((m) =>
                m.id === botId
                  ? {
                      ...m,
                      text: m.text + (event.text || ""),
                      metadata: {
                        ...m.metadata,
                        modelUsed: event.modelUsed ?? m.metadata?.modelUsed,
                      },
                    }
                  : m,
              ),
            );
          },
          onCompleted: (event) => {
            if (!current() || failed) return;
            if (!received) {
              fail();
              return;
            }
            completed = true;
            setMessages((all) =>
              all.map((m) =>
                m.id === botId
                  ? {
                      ...m,
                      status: "final",
                      metadata: { ...m.metadata, ...event },
                    }
                  : m,
              ),
            );
            setAnnouncement("Kai's reply is ready.");
          },
          onError: (event) => fail(event.errorCode),
        },
        { signal: controller.signal, timeoutMs: 45000, retries: 0 },
      );
      if (current() && !completed && !failed) fail();
    } catch {
      if (current()) fail();
    } finally {
      if (controllerRef.current === controller) {
        controllerRef.current = null;
        setLoading(false);
      }
    }
  }
  function stop() {
    controllerRef.current?.abort();
    controllerRef.current = null;
    setLoading(false);
    setMessages((all) =>
      all.map((m) =>
        m.status === "streaming" ? { ...m, status: "stopped" } : m,
      ),
    );
    setAnnouncement("Response stopped.");
    inputRef.current?.focus();
  }
  function clear() {
    stop();
    clearSessionId();
    sessionRef.current = getOrCreateSessionId();
    setMessages([]);
    setError(null);
    setRetry(null);
    setConfirmClear(false);
    setAnnouncement("New conversation started.");
    stickToBottom.current = true;
  }
  function close() {
    setOpen(false);
    requestAnimationFrame(() => launcherRef.current?.focus());
  }

  return (
    <div className="portfolio-assistant">
      {!open ? (
        <button
          ref={launcherRef}
          type="button"
          className="assistant-launcher"
          onClick={() => setOpen(true)}
          aria-label="Open assistant"
          aria-haspopup="dialog"
        >
          <span className="kai-launcher-reactor" aria-hidden="true">
            <i />
            <i />
            <span className="kai-launcher-icon">
              <Cpu size={17} />
            </span>
          </span>
        </button>
      ) : panelMode === "minimized" ? (
        <button
          type="button"
          className="assistant-minimized-core"
          onClick={() => setPanelMode("standard")}
          aria-label="Restore assistant"
        >
          <span className="kai-launcher-reactor" aria-hidden="true">
            <i />
            <i />
            <span className="kai-launcher-icon">
              <Cpu size={17} />
            </span>
          </span>
        </button>
      ) : (
        <section
          className={`assistant-panel assistant-panel--${panelMode}`}
          role="dialog"
          aria-label="Portfolio assistant"
          aria-describedby="kai-description"
        >
          <header className="kai-header">
            <span className="kai-avatar" aria-hidden="true">
              <span className="kai-avatar-rings" />
              <Cpu size={19} />
            </span>
            <div>
              <h2>
                KAI <span>// INTELLIGENCE CORE</span>
              </h2>
              <p id="kai-description">ENGINEERING KNOWLEDGE INTERFACE</p>
            </div>
            <div className="kai-header-actions">
              <button
                type="button"
                aria-label="New conversation"
                title="New conversation"
                disabled={!messages.length}
                onClick={() => setConfirmClear(!confirmClear)}
              >
                <Plus size={19} />
              </button>
              <button
                type="button"
                aria-label="Minimize assistant"
                title="Minimize"
                onClick={() => setPanelMode("minimized")}
              >
                <Minus size={18} />
              </button>
              <button
                type="button"
                aria-label={
                  panelMode === "maximized"
                    ? "Restore assistant size"
                    : "Maximize assistant"
                }
                title={panelMode === "maximized" ? "Restore size" : "Maximize"}
                onClick={() =>
                  setPanelMode(
                    panelMode === "maximized" ? "standard" : "maximized",
                  )
                }
              >
                {panelMode === "maximized" ? (
                  <Minimize2 size={17} />
                ) : (
                  <Maximize2 size={17} />
                )}
              </button>
              <button
                type="button"
                aria-label="Close assistant"
                onClick={close}
              >
                <X size={19} />
              </button>
            </div>
          </header>
          <div className="kai-telemetry" aria-label="Assistant system status">
            <span>
              <i /> CORE ONLINE
            </span>
            <span>
              <ShieldCheck /> CONTEXT LOCKED
            </span>
            <span>
              <Radio /> STREAM READY
            </span>
          </div>
          {confirmClear && (
            <div className="kai-clear">
              <p>
                Start fresh? This clears the saved conversation on this device.
              </p>
              <button onClick={clear}>Start new</button>
              <button onClick={() => setConfirmClear(false)}>Keep chat</button>
            </div>
          )}
          <div
            className="assistant-log"
            ref={logRef}
            role="region"
            aria-label="Conversation"
            tabIndex={0}
            onScroll={(e) => {
              const el = e.currentTarget;
              stickToBottom.current =
                el.scrollHeight - el.scrollTop - el.clientHeight < 70;
            }}
          >
            {!messages.length && (
              <div className="kai-welcome">
                <div className="kai-core-display" aria-hidden="true">
                  <span>
                    <Cpu />
                  </span>
                  <i />
                  <i />
                  <i />
                </div>
                <span className="eyebrow">
                  INTERFACE READY // AWAITING QUERY
                </span>
                <h3>
                  Access the
                  <br />
                  engineering <em>archive.</em>
                </h3>
                <p>
                  Ask KAI about Kheang’s systems, technical strengths,
                  experience, or current work. Responses are grounded in this
                  portfolio’s knowledge base.
                </p>
              </div>
            )}
            {messages.map((message) => (
              <Reply key={message.id} message={message} />
            ))}
            {loading && (
              <div className="kai-thinking">
                <Activity size={13} />
                <span />
                <span />
                <span />
                {messages[messages.length - 1]?.text
                  ? "Writing a reply"
                  : "Thinking about your question"}
              </div>
            )}
            {error && (
              <div className="kai-error" role="alert">
                <p>{error}</p>
                {retry && (
                  <button
                    type="button"
                    onClick={() => void send(retry.text, retry)}
                  >
                    <RotateCcw size={14} /> Retry reply
                  </button>
                )}
                <a href="/#projects" onClick={close}>
                  Explore projects <ArrowUpRight size={14} />
                </a>
                <a href="/#contact" onClick={close}>
                  Contact Kheang <ArrowUpRight size={14} />
                </a>
              </div>
            )}
          </div>
          <div className="sr-only" role="status" aria-live="polite">
            {announcement}
          </div>
          <form
            className="kai-composer"
            onSubmit={(e) => {
              e.preventDefault();
              void send();
            }}
          >
            <div className="kai-input-row">
              <textarea
                ref={inputRef}
                rows={1}
                value={input}
                maxLength={2000}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={(e) => {
                  if (
                    e.key === "Enter" &&
                    !e.shiftKey &&
                    !e.nativeEvent.isComposing
                  ) {
                    e.preventDefault();
                    void send();
                  }
                }}
                placeholder="Ask about my work..."
                aria-label="Ask Kai's assistant"
                aria-describedby="kai-input-hint"
              />
              <button
                type={loading ? "button" : "submit"}
                onClick={loading ? stop : undefined}
                aria-label={loading ? "Stop response" : "Send message"}
                disabled={!loading && !input.trim()}
              >
                {loading ? <Square size={15} /> : <Send size={16} />}
                <span>{loading ? "Stop" : "Send"}</span>
              </button>
            </div>
            <div className="kai-composer-note">
              <span id="kai-input-hint">
                {input.length > 1800
                  ? `${input.length}/2000 characters`
                  : "Enter to send · Shift + Enter for a new line"}
              </span>
            </div>
            <p className="kai-disclaimer">
              AI can make mistakes. <a href="/resume">View the resume</a> for
              the source details.
            </p>
          </form>
        </section>
      )}
    </div>
  );
}
