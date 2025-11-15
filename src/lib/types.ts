export type ContactRequest = {
  name: string;
  email: string;
  message: string;
};

export type ContactResponse = {
  message: string;
};

export type AssistantRequest = {
  message: string;
};
