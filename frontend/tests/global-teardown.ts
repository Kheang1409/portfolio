const { join } = require("path");
const { unlinkSync, existsSync } = require("fs");

const ENV_FILE = join(__dirname, "..", ".env.local");

module.exports = async function globalTeardown() {
  const mockServerProcess = global.__MOCK_SERVER_PROCESS__;
  if (mockServerProcess) {
    mockServerProcess.kill("SIGTERM");
    await new Promise((resolve) => {
      mockServerProcess.on("exit", () => resolve());
      setTimeout(resolve, 1000);
    });
  }

  // Clean up .env.local
  if (existsSync(ENV_FILE)) {
    unlinkSync(ENV_FILE);
  }
};