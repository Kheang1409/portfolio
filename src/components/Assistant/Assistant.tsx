"use client";
import React, { useState, useRef, useEffect } from "react";
import styles from "./Assistant.module.css";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import type { Pluggable } from "unified";
import { askAssistant } from "../../lib/assistants";

import type { AssistantMsg } from "../../lib/assistants";

type Msg = AssistantMsg;

const STORAGE_KEY = "kai_assistant_history_v1";

export default function Assistant() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<Msg[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [devDetails, setDevDetails] = useState<string | null>(null);
  const [devPanelOpen, setDevPanelOpen] = useState(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as Msg[];
        if (Array.isArray(parsed)) setMessages(parsed);
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
    if (open && inputRef.current) {
      inputRef.current.focus();
    }
  }, [open]);

  async function send() {
    const text = input.trim();
    if (!text) return;
    const userMsg: Msg = { sender: "user", text };
    setMessages((m) => [...m, userMsg]);
    setInput("");
    setLoading(true);

    try {
      const reply = await askAssistant(text);
      const botMsg: Msg = { sender: "bot", text: reply };
      setMessages((m) => [...m, botMsg]);
    } catch (err: unknown) {
      console.error(err);
      if (process.env.NODE_ENV === "development") {
        setDevDetails(String(err));
      }
      setMessages((m) => [
        ...m,
        { sender: "bot", text: "Something went wrong. Please try again." },
      ]);
    } finally {
      setLoading(false);
    }
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
    <div className={styles.chatbotContainer} data-open={open}>
      <button
        className={styles.header}
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        aria-controls="kai-assistant-body"
      >
        <span>🤖 Kai's Assistant</span>
        <span className={styles.toggleIcon}>{open ? "–" : "+"}</span>
      </button>

      {open && (
        <div id="kai-assistant-body" className={styles.body}>
          <div
            className={styles.messages}
            ref={scrollRef}
            role="status"
            aria-live="polite"
          >
            {messages.map((m, i) => (
              <div
                key={i}
                className={`${styles.message} ${
                  m.sender === "user" ? styles.user : styles.bot
                }`}
              >
                {m.sender === "bot" ? (
                  <ReactMarkdown
                    remarkPlugins={[remarkGfm]}
                    rehypePlugins={[rehypeHighlight as Pluggable]}
                  >
                    {m.text}
                  </ReactMarkdown>
                ) : (
                  m.text
                )}
              </div>
            ))}
            {loading && <div className={styles.loading}>Loading...</div>}
          </div>

          {devDetails && (
            <div className={styles.devPanel}>
              <button
                className={styles.devToggle}
                onClick={() => setDevPanelOpen((v) => !v)}
                aria-expanded={devPanelOpen}
                aria-controls="dev-error-details"
              >
                {devPanelOpen ? "Hide debug details" : "Show debug details"}
              </button>
              {devPanelOpen && (
                <pre id="dev-error-details" className={styles.devContent}>
                  {devDetails}
                </pre>
              )}
            </div>
          )}

          <div className={styles.form}>
            <input
              ref={inputRef}
              className={styles.input}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKey}
              placeholder="Ask me something..."
              aria-label="Ask Kai's assistant"
            />
            <button
              className={styles.sendBtn}
              onClick={send}
              disabled={loading}
              aria-disabled={loading}
            >
              Send
            </button>
            <button
              className={styles.sendBtn}
              onClick={clearHistory}
              style={{ marginLeft: 8 }}
            >
              Clear
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
