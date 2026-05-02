export type ConversationMessage = {
  role: "user" | "assistant";
  content: string;
};

export type AssistantUserProfile = {
  userId?: string;
  displayName?: string;
  locale?: string;
};

export type AssistantContext = {
  userProfile?: AssistantUserProfile;
  systemPersona?: string;
  metadata?: Record<string, string>;
};

export type AiMetadata = {
  modelUsed?: string;
  latencyMs?: number;
  fallbackUsed?: boolean;
  contextEnhanced?: boolean;
  cachedResponse?: boolean;
  ttftMs?: number;
};

export type AssistantRequest = {
  message: string;
  history?: ConversationMessage[];
  context?: AssistantContext;
};

export type StreamDeltaChunk = {
  type: "delta";
  messageId: string;
  text?: string;
  modelUsed?: string;
  fallbackUsed?: boolean;
  estimatedCostUsd?: number;
  contextEnhanced?: boolean;
  cachedResponse?: boolean;
};

export type StreamCompletedChunk = {
  type: "completed";
  messageId: string;
  modelUsed?: string;
  fallbackUsed?: boolean;
  ttftMs?: number;
  latencyMs?: number;
  throughputTokensPerSecond?: number;
  estimatedCostUsd?: number;
  contextEnhanced?: boolean;
  cachedResponse?: boolean;
};

export type StreamErrorChunk = {
  type: "error";
  messageId: string;
  errorCode?: string;
  retryable?: boolean;
  errorMessage?: string;
  modelUsed?: string;
  fallbackUsed?: boolean;
};

export type StreamChunk =
  | StreamDeltaChunk
  | StreamCompletedChunk
  | StreamErrorChunk;

export type ContactRequest = {
  name: string;
  email: string;
  message: string;
};

export type ContactResponse = {
  message?: string;
  Message?: string;
};

export type ApiProblem = {
  title?: string;
  detail?: string;
  status?: number;
  errorCode?: string;
  retryable?: boolean;
  message?: string;
};
