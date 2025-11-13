import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { vi } from "vitest";
import Assistant from "./Assistant";

describe("Assistant component", () => {
  beforeEach(() => {
    // clear localStorage and mocks
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("sends a message and displays bot response", async () => {
    // mock fetch to return text
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({ ok: true, text: async () => "Bot reply" })
    );

    render(<Assistant />);

    // open assistant
    const header = screen.getByRole("button", { name: /kai's assistant/i });
    fireEvent.click(header);

    const input = screen.getByPlaceholderText(/ask me something/i);
    fireEvent.change(input, { target: { value: "Hello" } });
    const send = screen.getByRole("button", { name: /send/i });
    fireEvent.click(send);

    await waitFor(() =>
      expect(screen.getByText(/Bot reply/)).toBeInTheDocument()
    );
  });

  it("loads persisted history from localStorage", () => {
    const history = [{ sender: "bot", text: "Persisted message" }];
    localStorage.setItem("kai_assistant_history_v1", JSON.stringify(history));

    render(<Assistant />);
    const header = screen.getByRole("button", { name: /kai's assistant/i });
    fireEvent.click(header);

    expect(screen.getByText(/Persisted message/)).toBeInTheDocument();
  });
});
