export type ContactRequest = {
  name: string;
  email: string;
  message: string;
};

export type ContactResponse = {
  message?: string;
  Message?: string; // Backend may return capitalized
};

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

export type AssistantRequest = {
  message: string;
  history: ConversationMessage[];
  context?: AssistantContext;
};

export type AssistantResponseMeta = {
  text: string;
  modelUsed?: string;
  latencyMs?: number;
  fallbackUsed?: boolean;
};

export type AssistantProblem = {
  title?: string;
  detail?: string;
  status?: number;
  errorCode?: string;
  retryable?: boolean;
};

export type AssistantStreamEvent =
  | {
      type: "delta";
      messageId: string;
      text?: string;
      modelUsed?: string;
      fallbackUsed?: boolean;
      estimatedCostUsd?: number;
    }
  | {
      type: "completed";
      messageId: string;
      modelUsed?: string;
      fallbackUsed?: boolean;
      ttftMs?: number;
      latencyMs?: number;
      throughputTokensPerSecond?: number;
      estimatedCostUsd?: number;
    }
  | {
      type: "error";
      messageId: string;
      errorCode?: string;
      retryable?: boolean;
      errorMessage?: string;
      modelUsed?: string;
      fallbackUsed?: boolean;
    };
