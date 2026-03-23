"use client";

import { useEffect, useRef, useState } from "react";
import { motion } from "framer-motion";
import { MessageSquare, X, Send, Trash2, Sparkles } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import type { Pluggable } from "unified";
import { streamAssistant } from "@/lib/assistants";
import type { AssistantContext, ConversationMessage } from "@/lib/types";

const STORAGE_KEY = "kai_assistant_history_v2";

type ChatMessage = {
  id: string;
  sender: "user" | "bot";
  text: string;
  status: "streaming" | "final" | "error";
};

function createId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export default function Assistant() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [lastModelUsed, setLastModelUsed] = useState<string | null>(null);
  const [lastLatencyMs, setLastLatencyMs] = useState<number | null>(null);
  const [lastTtftMs, setLastTtftMs] = useState<number | null>(null);
  const [retryMessage, setRetryMessage] = useState<string | null>(null);
  const [lastErrorText, setLastErrorText] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const streamAbortRef = useRef<AbortController | null>(null);
  const sessionIdRef = useRef<string>(
    typeof crypto !== "undefined" && "randomUUID" in crypto
      ? crypto.randomUUID()
      : `${Date.now()}-${Math.random().toString(16).slice(2)}`,
  );

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as unknown[];
        if (Array.isArray(parsed)) {
          const normalized = parsed
            .map((entry) => {
              if (!entry || typeof entry !== "object") {
                return null;
              }

              const e = entry as Record<string, unknown>;
              const sender =
                e.sender === "user"
                  ? "user"
                  : e.sender === "bot"
                    ? "bot"
                    : null;
              const text = typeof e.text === "string" ? e.text : "";
              if (!sender) {
                return null;
              }

              return {
                id: typeof e.id === "string" ? e.id : createId(),
                sender,
                text,
                status:
                  e.status === "streaming" ||
                  e.status === "error" ||
                  e.status === "final"
                    ? e.status
                    : "final",
              } satisfies ChatMessage;
            })
            .filter((x): x is ChatMessage => x !== null);

          setMessages(normalized);
        }
      }
    } catch (e) {}
  }, []);

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(messages.slice(-200)));
    } catch (e) {}
  }, [messages]);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages, open]);

  useEffect(() => {
    return () => {
      if (streamAbortRef.current) {
        streamAbortRef.current.abort();
      }
    };
  }, []);

  useEffect(() => {
    if (open && inputRef.current) {
      inputRef.current.focus();
    }
  }, [open]);

  async function send(overrideText?: string) {
    if (loading) return;

    const text = (overrideText ?? input).trim();
    if (!text) return;

    setRetryMessage(null);
    setLastErrorText(null);

    const userId = createId();
    const botId = createId();

    const userMsg: ChatMessage = {
      id: userId,
      sender: "user",
      text,
      status: "final",
    };
    const botMsg: ChatMessage = {
      id: botId,
      sender: "bot",
      text: "",
      status: "streaming",
    };

    const baseMessages = [...messages, userMsg];
    setMessages((m) => [...m, userMsg, botMsg]);
    if (!overrideText) {
      setInput("");
    }
    setLoading(true);

    const abortController = new AbortController();
    streamAbortRef.current = abortController;

    try {
      const history: ConversationMessage[] = baseMessages
        .filter((msg) => msg.text.trim().length > 0)
        .map((msg) => ({
          role:
            msg.sender === "user" ? ("user" as const) : ("assistant" as const),
          content: msg.text,
        }));

      const context: AssistantContext = {
        systemPersona: "Hang Kheang Taing portfolio assistant",
        metadata: {
          sessionId: sessionIdRef.current,
          uiSurface: "portfolio-assistant",
          locale:
            typeof navigator !== "undefined" ? navigator.language : "en-US",
        },
      };

      await streamAssistant(
        text,
        history,
        context,
        {
          onDelta: (event) => {
            setLastModelUsed(event.modelUsed ?? null);

            setMessages((current) =>
              current.map((message) =>
                message.id === botId
                  ? {
                      ...message,
                      text: message.text + (event.text ?? ""),
                      status: "streaming",
                    }
                  : message,
              ),
            );
          },
          onCompleted: (event) => {
            setLastModelUsed(event.modelUsed ?? null);
            setLastLatencyMs(event.latencyMs ?? null);
            setLastTtftMs(event.ttftMs ?? null);

            setMessages((current) =>
              current.map((message) =>
                message.id === botId
                  ? {
                      ...message,
                      text:
                        message.text.trim().length > 0
                          ? message.text
                          : "I couldn't generate a response.",
                      status: "final",
                    }
                  : message,
              ),
            );
          },
          onError: (event) => {
            const friendly =
              event.errorCode === "RATE_LIMITED"
                ? "Rate limit reached. Please wait and try again."
                : (event.errorMessage ??
                  "Something went wrong. Please try again.");

            setLastErrorText(friendly);
            setRetryMessage(text);
            setMessages((current) =>
              current.map((message) =>
                message.id === botId
                  ? {
                      ...message,
                      text:
                        message.text.trim().length > 0
                          ? message.text
                          : friendly,
                      status: "error",
                    }
                  : message,
              ),
            );
          },
        },
        abortController.signal,
      );
    } catch (err: unknown) {
      if ((err as { name?: string })?.name !== "AbortError") {
        setRetryMessage(text);
        const friendly = "Network error. Please retry.";
        setLastErrorText(friendly);
        setMessages((current) =>
          current.map((message) =>
            message.id === botId
              ? {
                  ...message,
                  text:
                    message.text.trim().length > 0 ? message.text : friendly,
                  status: "error",
                }
              : message,
          ),
        );
      }
    } finally {
      setLoading(false);
      streamAbortRef.current = null;
    }
  }

  function cancelStreaming() {
    if (streamAbortRef.current) {
      streamAbortRef.current.abort();
      streamAbortRef.current = null;
    }
    setLoading(false);
    setMessages((current) =>
      current.map((message) =>
        message.status === "streaming"
          ? {
              ...message,
              status: "final",
              text:
                message.text.trim().length > 0
                  ? message.text
                  : "Response cancelled.",
            }
          : message,
      ),
    );
  }

  function handleKey(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  }

  function clearHistory() {
    setMessages([]);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch (e) {}
  }

  return (
    <div className="fixed bottom-6 right-6 z-50">
      {!open ? (
        <button
          onClick={() => setOpen(true)}
          className="group relative flex items-center gap-3 rounded-full border border-light-border/80 bg-light-surface/95 px-lg py-sm text-light-text shadow-lg transition-all duration-200 hover:-translate-y-0.5 hover:shadow-xl dark:border-dark-border/80 dark:bg-dark-surface/90 dark:text-dark-text"
          aria-label="Open assistant"
        >
          <span className="absolute inset-0 rounded-full bg-gradient-to-r from-light-primary/25 via-transparent to-light-primary/15 opacity-80 transition-opacity duration-200 group-hover:opacity-100 dark:from-dark-primary/25 dark:to-dark-primary/10" />
          <span className="absolute inset-[2px] rounded-full bg-white/80 backdrop-blur dark:bg-dark-surface/90" />
          <span className="relative z-10 flex items-center gap-2 text-sm font-semibold">
            <motion.span
              initial={{ scale: 1 }}
              animate={{ scale: [1, 1.08, 1] }}
              transition={{
                repeat: Infinity,
                duration: 2.6,
                ease: "easeInOut",
              }}
              className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-light-primary via-light-primary to-light-primary/80 text-white shadow-md dark:from-dark-primary dark:via-dark-primary dark:to-dark-primary/80"
            >
              <MessageSquare className="h-4 w-4" />
            </motion.span>
            Ask Hang Kheang's Assistant
          </span>
        </button>
      ) : (
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="w-full max-w-[360px] md:max-w-[420px] overflow-hidden rounded-2xl border border-light-border/60 bg-light-surface/90 shadow-[0_20px_70px_-30px_rgba(0,0,0,0.45)] backdrop-blur dark:border-dark-border/60 dark:bg-dark-surface/80"
        >
          <div className="relative flex items-center justify-between px-md py-sm">
            <div className="flex items-center gap-2 font-semibold">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-light-primary via-light-primary to-light-accent text-white shadow-md dark:from-dark-primary dark:via-dark-primary dark:to-dark-accent">
                <Sparkles className="h-4 w-4" />
              </span>
              <div>
                <div className="leading-none">Hang Kheang's Assistant</div>
                <div className="text-[11px] font-normal text-light-text-secondary dark:text-dark-text-secondary">
                  {lastModelUsed
                    ? `Model: ${lastModelUsed}${lastTtftMs ? ` · TTFT ${Math.round(lastTtftMs)}ms` : ""}${lastLatencyMs ? ` · ${Math.round(lastLatencyMs)}ms` : ""}`
                    : "Online · Quick replies"}
                </div>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                className="rounded-md p-2 text-light-text-secondary transition hover:bg-light-border/60 hover:text-light-text dark:text-dark-text-secondary dark:hover:bg-dark-border/60 dark:hover:text-dark-text"
                onClick={clearHistory}
                aria-label="Clear history"
                title="Clear history"
              >
                <Trash2 className="w-4 h-4" />
              </button>
              <button
                className="rounded-md p-2 text-light-text-secondary transition hover:bg-light-border/60 hover:text-light-text dark:text-dark-text-secondary dark:hover:bg-dark-border/60 dark:hover:text-dark-text"
                onClick={() => setOpen(false)}
                aria-label="Close assistant"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
            <div className="pointer-events-none absolute inset-x-0 bottom-0 h-px bg-gradient-to-r from-transparent via-light-accent/60 to-transparent dark:via-dark-accent/50" />
          </div>

          <div
            ref={scrollRef}
            className="max-h-[55vh] space-y-sm overflow-y-auto px-md py-sm"
          >
            {messages.length === 0 && !loading && (
              <div className="rounded-lg border border-dashed border-light-border/80 bg-light-background/70 px-md py-lg text-center text-small text-light-text-secondary dark:border-dark-border/70 dark:bg-dark-background/60 dark:text-dark-text-secondary">
                Start a chat to get tailored help about this portfolio or your
                ideas.
              </div>
            )}

            {messages.map((m) => (
              <div
                key={m.id}
                className={`flex ${
                  m.sender === "user" ? "justify-end" : "justify-start"
                }`}
              >
                <div
                  className={`relative max-w-[85%] rounded-2xl border px-md py-sm text-small shadow-sm ${
                    m.sender === "user"
                      ? "bg-gradient-to-br from-light-primary via-light-primary to-light-accent text-white border-light-primary/60 dark:from-dark-primary dark:via-dark-primary dark:to-dark-accent dark:border-dark-primary/60"
                      : "bg-light-background/80 text-light-text-primary border-light-border/80 dark:bg-dark-background/80 dark:text-dark-text-primary dark:border-dark-border/80"
                  }`}
                >
                  {m.sender === "bot" ? (
                    <ReactMarkdown
                      className="prose prose-sm dark:prose-invert max-w-none"
                      remarkPlugins={[remarkGfm]}
                      rehypePlugins={[rehypeHighlight as Pluggable]}
                    >
                      {m.text || "..."}
                    </ReactMarkdown>
                  ) : (
                    <span className="whitespace-pre-wrap leading-relaxed">
                      {m.text}
                    </span>
                  )}
                  <span className="absolute -bottom-4 text-[10px] font-medium uppercase tracking-wide text-light-text-secondary dark:text-dark-text-secondary">
                    {m.sender === "user" ? "You" : "Assistant"}
                  </span>
                </div>
              </div>
            ))}

            {loading && (
              <div className="flex items-center gap-2 text-small text-light-text-secondary dark:text-dark-text-secondary">
                <span className="h-2 w-2 animate-pulse rounded-full bg-light-accent dark:bg-dark-accent" />
                is typing...
              </div>
            )}

            {!loading && lastErrorText && (
              <div className="rounded-lg border border-light-border/80 bg-light-background/70 px-sm py-sm text-small text-light-text-secondary dark:border-dark-border/80 dark:bg-dark-background/60 dark:text-dark-text-secondary">
                {lastErrorText}
              </div>
            )}

            {!loading && retryMessage && (
              <div className="flex items-center justify-between rounded-lg border border-light-border/80 bg-light-background/70 px-sm py-sm text-small dark:border-dark-border/80 dark:bg-dark-background/60">
                <span className="text-light-text-secondary dark:text-dark-text-secondary">
                  Request failed. Retry?
                </span>
                <button
                  onClick={() => send(retryMessage)}
                  className="rounded-md bg-light-primary px-sm py-1 text-xs font-semibold text-white hover:opacity-90 dark:bg-dark-primary"
                >
                  Retry
                </button>
              </div>
            )}
          </div>

          <div className="border-t border-light-border/70 bg-light-background/70 px-md py-sm backdrop-blur dark:border-dark-border/70 dark:bg-dark-background/60">
            <div className="flex items-center gap-3 rounded-xl border border-light-border/80 bg-white/70 px-sm py-1 shadow-inner focus-within:border-light-primary focus-within:ring-2 focus-within:ring-light-accent/50 dark:border-dark-border/80 dark:bg-dark-surface/60 dark:focus-within:border-dark-primary dark:focus-within:ring-dark-accent/40">
              <input
                ref={inputRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKey}
                placeholder="Ask me anything..."
                aria-label="Ask Kai's assistant"
                className="flex-1 bg-transparent px-sm py-sm text-small placeholder:text-light-text-secondary focus:outline-none dark:placeholder:text-dark-text-secondary"
              />
              <button
                onClick={() => {
                  if (loading) {
                    cancelStreaming();
                  } else {
                    void send();
                  }
                }}
                aria-label="Send message"
                className="flex items-center gap-2 sm:gap-1 flex-shrink-0 rounded-lg bg-gradient-to-br from-light-primary via-light-primary to-light-accent px-3 sm:px-md py-sm text-sm font-semibold text-white shadow-md transition hover:shadow-lg disabled:cursor-not-allowed disabled:opacity-60 dark:from-dark-primary dark:via-dark-primary dark:to-dark-accent"
              >
                {loading ? (
                  <X className="h-4 w-4" />
                ) : (
                  <Send className="h-4 w-4" />
                )}
                <span className="hidden sm:inline">
                  {loading ? "Stop" : "Send"}
                </span>
              </button>
            </div>
          </div>
        </motion.div>
      )}
    </div>
  );
}
