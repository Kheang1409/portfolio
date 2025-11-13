import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./vitest.setup.ts",
    include: [
      "**/__tests__/**/*.test.{ts,tsx,js}",
      "src/**/*.test.{ts,tsx,js}",
    ],
    coverage: {
      reporter: ["text", "lcov"],
    },
  },
});
