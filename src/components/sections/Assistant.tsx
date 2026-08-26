"use client";

import { memo, useEffect, useMemo, useRef, useState } from "react";
import { motion } from "framer-motion";
import {
  X,
  Send,
  Trash2,
  RotateCcw,
  Brain,
  Database,
  Timer,
  Bot,
} from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import Image from "next/image";
import type { Pluggable } from "unified";
import { ApiError } from "@/lib/api/client";
import { streamAssistant } from "@/lib/assistants";
import { getOrCreateSessionId } from "@/lib/session";
import type {
  AiMetadata,
  AssistantContext,
  ConversationMessage,
} from "@/lib/api/types";

const STORAGE_KEY = "kai_assistant_history_v2";
const REQUEST_TIMEOUT_MS = 45_000;

type ChatMessage = {
  id: string;
  sender: "user" | "bot";
  text: string;
  status: "streaming" | "final" | "error";
  metadata?: AiMetadata;
};

type RetryPayload = {
  text: string;
};

type MetadataPillProps = {
  icon: React.ReactNode;
  label: string;
};

const markdownRehypePlugins = [rehypeHighlight as Pluggable];

const MetadataPill = memo(function MetadataPill({
  icon,
  label,
}: MetadataPillProps) {
  return (
    <span className="assistant-meta inline-flex items-center gap-1 rounded-full border border-light-border/70 bg-light-background/80 px-2 py-0.5 text-[10px] font-medium text-light-text-secondary dark:border-dark-border/70 dark:bg-dark-background/80 dark:text-dark-text-secondary">
      {icon}
      {label}
    </span>
  );
});

type MessageBubbleProps = {
  message: ChatMessage;
};

const MessageBubble = memo(function MessageBubble({
  message,
}: MessageBubbleProps) {
  const metadataLabels = useMemo(() => {
    if (message.sender !== "bot" || !message.metadata) {
      return [] as Array<{ key: string; icon: React.ReactNode; label: string }>;
    }

    const labels: Array<{ key: string; icon: React.ReactNode; label: string }> =
      [];

    if (message.metadata.modelUsed) {
      labels.push({
        key: "model",
        icon: <Bot className="h-3 w-3" />,
        label: message.metadata.modelUsed,
      });
    }

    if (typeof message.metadata.latencyMs === "number") {
      labels.push({
        key: "latency",
        icon: <Timer className="h-3 w-3" />,
        label: `${Math.round(message.metadata.latencyMs)}ms`,
      });
    }

    if (message.metadata.fallbackUsed) {
      labels.push({
        key: "fallback",
        icon: <RotateCcw className="h-3 w-3" />,
        label: "Fallback",
      });
    }

    if (message.metadata.contextEnhanced) {
      labels.push({
        key: "context",
        icon: <Brain className="h-3 w-3" />,
        label: "Context-enhanced",
      });
    }

    if (message.metadata.cachedResponse) {
      labels.push({
        key: "cached",
        icon: <Database className="h-3 w-3" />,
        label: "Cached response",
      });
    }

    return labels;
  }, [message]);

  return (
    <div
      className={`flex ${message.sender === "user" ? "justify-end" : "justify-start"}`}
    >
      <div
        className={`assistant-bubble relative max-w-[90%] rounded-2xl border px-md py-sm text-small shadow-sm ${
          message.sender === "user"
            ? "bg-gradient-to-br from-light-primary via-light-primary to-light-accent text-white border-light-primary/60 dark:from-dark-primary dark:via-dark-primary dark:to-dark-accent dark:border-dark-primary/60"
            : "bg-light-background/80 text-light-text-primary border-light-border/80 dark:bg-dark-background/80 dark:text-dark-text-primary dark:border-dark-border/80"
        }`}
      >
        <div className="mb-1 text-[10px] font-semibold uppercase tracking-wide opacity-80">
          {message.sender === "user" ? "You" : "Assistant"}
        </div>

        {message.sender === "bot" ? (
          <div className="space-y-2">
            <ReactMarkdown
              className="prose prose-sm dark:prose-invert max-w-none"
              remarkPlugins={[remarkGfm]}
              rehypePlugins={markdownRehypePlugins}
            >
              {message.text || "..."}
            </ReactMarkdown>

            {metadataLabels.length > 0 && (
              <div className="flex flex-wrap gap-1">
                {metadataLabels.map((item) => (
                  <MetadataPill
                    key={item.key}
                    icon={item.icon}
                    label={item.label}
                  />
                ))}
              </div>
            )}
          </div>
        ) : (
          <span className="whitespace-pre-wrap leading-relaxed">
            {message.text}
          </span>
        )}
      </div>
    </div>
  );
});

function useDebouncedValue(value: string, delayMs: number): string {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timerId = setTimeout(() => {
      setDebounced(value);
    }, delayMs);

    return () => clearTimeout(timerId);
  }, [value, delayMs]);

  return debounced;
}

function createId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function toFriendlyErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (
      error.code === "TIMEOUT" ||
      error.message.toLowerCase().includes("timeout")
    ) {
      return "Request timed out. Try again or shorten your prompt.";
    }

    if (error.status === 429 || error.code === "RATE_LIMITED") {
      return "Rate limit reached. Please wait and try again.";
    }

    if (error.status >= 500) {
      return "Assistant service is temporarily unavailable. Please retry.";
    }

    return error.message || "Request failed.";
  }

  if (error instanceof DOMException && error.name === "AbortError") {
    return "Request cancelled.";
  }

  return "Network error. Please retry.";
}

export default function Assistant() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const debouncedInput = useDebouncedValue(input, 150);
  const [loading, setLoading] = useState(false);
  const [lastModelUsed, setLastModelUsed] = useState<string | null>(null);
  const [lastLatencyMs, setLastLatencyMs] = useState<number | null>(null);
  const [lastTtftMs, setLastTtftMs] = useState<number | null>(null);
  const [lastFallbackUsed, setLastFallbackUsed] = useState<boolean | null>(
    null,
  );
  const [lastContextEnhanced, setLastContextEnhanced] = useState<
    boolean | null
  >(null);
  const [lastCachedResponse, setLastCachedResponse] = useState<boolean | null>(
    null,
  );
  const [retryPayload, setRetryPayload] = useState<RetryPayload | null>(null);
  const [lastErrorText, setLastErrorText] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const requestAbortRef = useRef<AbortController | null>(null);
  const sessionIdRef = useRef<string>("");

  // Initialize persistent session ID on mount
  useEffect(() => {
    sessionIdRef.current = getOrCreateSessionId();
  }, []);

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
      if (requestAbortRef.current) {
        requestAbortRef.current.abort();
      }
    };
  }, []);

  useEffect(() => {
    if (open && inputRef.current) {
      inputRef.current.focus();
    }
  }, [open]);

  async function send(override?: RetryPayload) {
    if (loading) return;

    const text = (override?.text ?? input).trim();
    if (!text) return;

    setRetryPayload(null);
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
    if (!override) {
      setInput("");
    }
    setLoading(true);

    const abortController = new AbortController();
    requestAbortRef.current = abortController;

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
          conversationId: sessionIdRef.current,
          locale:
            typeof navigator !== "undefined" ? navigator.language : "en-US",
        },
      };

      let completed = false;

      await streamAssistant(
        { message: text, history, context },
        {
          onDelta: (event) => {
            setLastModelUsed(event.modelUsed ?? null);
            setLastFallbackUsed(event.fallbackUsed ?? null);
            setLastContextEnhanced(event.contextEnhanced ?? null);
            setLastCachedResponse(event.cachedResponse ?? null);

            setMessages((current) =>
              current.map((message) =>
                message.id === botId
                  ? {
                      ...message,
                      text: message.text + (event.text ?? ""),
                      metadata: {
                        ...message.metadata,
                        modelUsed:
                          event.modelUsed ?? message.metadata?.modelUsed,
                        fallbackUsed:
                          event.fallbackUsed ?? message.metadata?.fallbackUsed,
                        contextEnhanced:
                          event.contextEnhanced ??
                          message.metadata?.contextEnhanced,
                        cachedResponse:
                          event.cachedResponse ??
                          message.metadata?.cachedResponse,
                      },
                      status: "streaming",
                    }
                  : message,
              ),
            );
          },
          onCompleted: (event) => {
            completed = true;
            setLastModelUsed(event.modelUsed ?? null);
            setLastLatencyMs(event.latencyMs ?? null);
            setLastTtftMs(event.ttftMs ?? null);
            setLastFallbackUsed(event.fallbackUsed ?? null);
            setLastContextEnhanced(event.contextEnhanced ?? null);
            setLastCachedResponse(event.cachedResponse ?? null);

            setMessages((current) =>
              current.map((message) =>
                message.id === botId
                  ? {
                      ...message,
                      metadata: {
                        ...message.metadata,
                        modelUsed:
                          event.modelUsed ?? message.metadata?.modelUsed,
                        fallbackUsed:
                          event.fallbackUsed ?? message.metadata?.fallbackUsed,
                        latencyMs:
                          event.latencyMs ?? message.metadata?.latencyMs,
                        ttftMs: event.ttftMs ?? message.metadata?.ttftMs,
                        contextEnhanced:
                          event.contextEnhanced ??
                          message.metadata?.contextEnhanced,
                        cachedResponse:
                          event.cachedResponse ??
                          message.metadata?.cachedResponse,
                      },
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
            setRetryPayload({ text });
            setMessages((current) =>
              current.map((message) =>
                message.id === botId
                  ? {
                      ...message,
                      metadata: {
                        ...message.metadata,
                        modelUsed:
                          event.modelUsed ?? message.metadata?.modelUsed,
                        fallbackUsed:
                          event.fallbackUsed ?? message.metadata?.fallbackUsed,
                      },
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
        {
          signal: abortController.signal,
          timeoutMs: REQUEST_TIMEOUT_MS,
          retries: 1,
        },
      );

      if (!completed) {
        setMessages((current) =>
          current.map((message) =>
            message.id === botId && message.status === "streaming"
              ? {
                  ...message,
                  status: "final",
                  text:
                    message.text.trim().length > 0
                      ? message.text
                      : "No content returned.",
                }
              : message,
          ),
        );
      }
    } catch (err: unknown) {
      if ((err as { name?: string })?.name !== "AbortError") {
        setRetryPayload({ text });
        const friendly = toFriendlyErrorMessage(err);
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
      requestAbortRef.current = null;
    }
  }

  function cancelRequest() {
    if (requestAbortRef.current) {
      requestAbortRef.current.abort();
      requestAbortRef.current = null;
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
    <div className="pixel-assistant fixed bottom-4 right-4 z-50 sm:bottom-6 sm:right-6">
      {!open ? (
        <button
          onClick={() => setOpen(true)}
          className="assistant-launcher group relative flex h-14 w-14 items-center justify-center border bg-light-surface/95 p-1 text-light-text transition-all duration-200 dark:bg-dark-surface/90 dark:text-dark-text sm:h-auto sm:w-auto sm:justify-start sm:gap-3 sm:px-md sm:py-2"
          aria-label="Open assistant"
        >
          <span className="assistant-ping" />
          <span className="relative z-10 flex items-center gap-2 text-sm font-semibold">
            <motion.span
              initial={{ scale: 1 }}
              animate={{ scale: [1, 1.08, 1] }}
              transition={{
                repeat: Infinity,
                duration: 2.6,
                ease: "easeInOut",
              }}
              className="assistant-avatar flex h-10 w-10 items-center justify-center overflow-hidden"
            >
              <Image src="/kai-bot-pixel.gif" alt="" width={45} height={40} unoptimized />
            </motion.span>
            <span className="hidden sm:inline">TALK TO KAI BOT</span>
          </span>
        </button>
      ) : (
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="assistant-panel flex max-h-[calc(100dvh-2rem)] w-[calc(100vw-2rem)] max-w-[420px] flex-col overflow-hidden bg-light-surface/95 dark:bg-dark-surface/95 sm:max-h-[min(720px,calc(100dvh-3rem))]"
        >
          <div className="relative flex flex-shrink-0 items-center justify-between px-sm py-sm sm:px-md">
            <div className="flex items-center gap-2 font-semibold">
              <span className="assistant-avatar flex h-10 w-10 items-center justify-center overflow-hidden">
                <Image src="/kai-bot-pixel.gif" alt="" width={45} height={40} unoptimized />
              </span>
              <div className="min-w-0">
                <div className="truncate text-sm font-black leading-none sm:text-base">KAI BOT · NPC GUIDE</div>
                <div className="text-[11px] font-normal text-light-text-secondary dark:text-dark-text-secondary">
                  {lastModelUsed
                    ? `Model: ${lastModelUsed}${lastTtftMs ? ` · TTFT ${Math.round(lastTtftMs)}ms` : ""}${lastLatencyMs ? ` · ${Math.round(lastLatencyMs)}ms` : ""}${lastFallbackUsed ? " · fallback" : ""}`
                    : "Online · Quick replies"}
                </div>
                <div className="mt-1 flex flex-wrap items-center gap-1">
                  {lastContextEnhanced ? (
                    <MetadataPill
                      icon={<Brain className="h-3 w-3" />}
                      label="Context-enhanced"
                    />
                  ) : null}
                  {lastCachedResponse ? (
                    <MetadataPill
                      icon={<Database className="h-3 w-3" />}
                      label="Cached response"
                    />
                  ) : null}
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
            className="assistant-log min-h-0 flex-1 space-y-sm overflow-y-auto px-sm py-sm sm:max-h-[55vh] sm:px-md"
          >
            {messages.length === 0 && !loading && (
              <div className="assistant-empty rounded-lg border border-dashed border-light-border/80 bg-light-background/70 px-md py-lg text-center text-small text-light-text-secondary dark:border-dark-border/70 dark:bg-dark-background/60 dark:text-dark-text-secondary">
                <strong className="mb-2 block">QUEST LOG EMPTY</strong>
                Ask about skills, projects, experience, or begin a collaboration quest.
              </div>
            )}

            {messages.map((message) => (
              <MessageBubble key={message.id} message={message} />
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

            {!loading && retryPayload && (
              <div className="flex items-center justify-between rounded-lg border border-light-border/80 bg-light-background/70 px-sm py-sm text-small dark:border-dark-border/80 dark:bg-dark-background/60">
                <span className="text-light-text-secondary dark:text-dark-text-secondary">
                  Request failed. Retry?
                </span>
                <button
                  onClick={() => send(retryPayload)}
                  className="rounded-md bg-light-primary px-sm py-1 text-xs font-semibold text-white hover:opacity-90 dark:bg-dark-primary"
                >
                  Retry
                </button>
              </div>
            )}
          </div>

          <div className="flex-shrink-0 border-t border-light-border/70 bg-light-background/70 px-sm py-sm backdrop-blur dark:border-dark-border/70 dark:bg-dark-background/60 sm:px-md">
            <div className="flex items-center gap-3 rounded-xl border border-light-border/80 bg-white/70 px-sm py-1 shadow-inner focus-within:border-light-primary focus-within:ring-2 focus-within:ring-light-accent/50 dark:border-dark-border/80 dark:bg-dark-surface/60 dark:focus-within:border-dark-primary dark:focus-within:ring-dark-accent/40">
              <input
                ref={inputRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKey}
                placeholder="Type your quest..."
                aria-label="Ask Kai's assistant"
                className="flex-1 bg-transparent px-sm py-sm text-small placeholder:text-light-text-secondary focus:outline-none dark:placeholder:text-dark-text-secondary"
              />
              <button
                onClick={() => {
                  if (loading) {
                    cancelRequest();
                  } else {
                    void send();
                  }
                }}
                aria-label="Send message"
                disabled={!loading && debouncedInput.trim().length === 0}
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
