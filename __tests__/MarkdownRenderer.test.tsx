import { render, screen } from "@testing-library/react";
import React from "react";
import MarkdownRenderer from "../src/components/MarkdownRenderer";

test("renders markdown including lists and tables and gfm", () => {
  const md = `# Title\n\n- one\n- two\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n- [ ] task1\n- [x] task2\n\n~~strike~~`;
  render(React.createElement(MarkdownRenderer, { content: md }));
  expect(screen.getByText("Title")).toBeInTheDocument();
  expect(screen.getByText("one")).toBeInTheDocument();
  expect(screen.getByText("1")).toBeInTheDocument();
  expect(screen.getByText("task1")).toBeInTheDocument();
  expect(screen.getByText("strike")).toBeInTheDocument();
});
