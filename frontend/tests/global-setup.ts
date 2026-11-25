const { spawn } = require("child_process");
const { resolve, join } = require("path");
const { writeFileSync, unlinkSync, existsSync } = require("fs");

let mockServerProcess = null;
const ENV_FILE = join(__dirname, "..", ".env.local");

module.exports = async function globalSetup() {
  // Create .env.local for Next.js dev server
  writeFileSync(ENV_FILE, "BACKEND_API_URL=http://localhost:3001\n");

  // Start the mock backend server
  const mockServerPath = resolve(__dirname, "mock-backend.js");

  // Run the JavaScript file directly with node
  mockServerProcess = spawn("node", [mockServerPath], {
    stdio: "inherit",
  });

  // Wait for the server to start
  await new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error("Mock server startup timeout")), 10000);

    const checkServer = async () => {
      try {
        const response = await fetch("http://localhost:3001/api/health/live");
        if (response.ok) {
          clearTimeout(timeout);
          resolve();
        } else {
          setTimeout(checkServer, 100);
        }
      } catch {
        setTimeout(checkServer, 100);
      }
    };

    checkServer();
  });

  // Store the process for teardown
  global.__MOCK_SERVER_PROCESS__ = mockServerProcess;
};