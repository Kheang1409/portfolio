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

export type AssistantRequest = {
  message: string;
  history: ConversationMessage[];
};
