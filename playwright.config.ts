import { defineConfig } from "@playwright/test";
const port = process.env.PLAYWRIGHT_PORT || "3000";

export default defineConfig({
  testDir: "./tests",
  workers: 1,
  timeout: 60000,
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || `http://localhost:${port}`,
    channel: "chrome",
    headless: true,
  },
  webServer: {
    command: `npm run dev -- --port ${port}`,
    url: `http://localhost:${port}`,
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
  reporter: "list",
});
