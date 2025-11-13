import { render, screen } from "@testing-library/react";
import React from "react";
import { vi } from "vitest";

vi.mock("../src/components/Layout", () => ({
  __esModule: true,
  default: (props: any) =>
    React.createElement("div", { "data-testid": "layout" }, props.children),
}));
vi.mock("../src/components/Card", () => ({
  __esModule: true,
  default: (props: any) =>
    React.createElement("div", { "data-testid": "card" }, props.children),
}));

import BlogIndex from "../pages/blog/index";

const posts = Array.from({ length: 12 }).map((_, i) => ({
  slug: `post-${i + 1}`,
  title: `Post ${i + 1}`,
  date: `2024-0${(i % 9) + 1}-01`,
  summary: `Summary ${i + 1}`,
  link: `/blog/post-${i + 1}`,
}));

test("renders pagination and metadata", () => {
  render((<BlogIndex posts={posts} />) as any);
  expect(screen.getByText("Post 1")).toBeInTheDocument();
  expect(screen.getByText("Post 5")).toBeInTheDocument();
  expect(screen.getByText("Post 6")).toBeInTheDocument();
});
