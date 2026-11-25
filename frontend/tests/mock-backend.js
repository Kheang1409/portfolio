const { createServer } = require("http");
const { parse } = require("url");

const MOCK_RESUME = {
  personals: { preferredName: "Test User", email: "test@example.com" },
  summary: "Test summary",
  skills: ["TypeScript", "React"],
  experiences: [
    { company: "Test Co", role: "Developer", startDates: "2020-01", endDate: "2023-01", bulletPoints: ["Did stuff"] },
  ],
  projects: [
    { name: "Test Project", description: "A test project", skills: ["TypeScript", "Node.js"] },
  ],
  educations: [
    { institution: "Test University", degree: "BS Computer Science", startDates: "2016-09", endDate: "2020-06" },
  ],
  certifications: [
    { title: "Test Cert", issuer: "Test Org", date: "2023-01-15" },
  ],
};

const MOCK_HEALTH = { status: "healthy" };

function handleRequest(req, res) {
  const parsedUrl = parse(req.url || "", true);
  const path = parsedUrl.pathname;

  console.log(`[Mock Backend] ${req.method} ${path}`);

  res.setHeader("Content-Type", "application/json");
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");

  if (req.method === "OPTIONS") {
    res.writeHead(204);
    res.end();
    return;
  }

  if (path === "/api/health/live" && req.method === "GET") {
    res.writeHead(200);
    res.end(JSON.stringify(MOCK_HEALTH));
    return;
  }

  if (path === "/api/resumes/latest" && req.method === "GET") {
    res.writeHead(200);
    res.end(JSON.stringify(MOCK_RESUME));
    return;
  }

  if (path === "/api/donations/config" && req.method === "GET") {
    res.writeHead(200);
    res.end(JSON.stringify({ clientId: "sandbox-public-client-id", currency: "USD", minimumDonation: 1, maximumDonation: 5000 }));
    return;
  }

  if (path === "/api/donations/leaderboard" && req.method === "GET") {
    res.writeHead(200);
    res.end(JSON.stringify({ entries: [], totalDonations: 0, totalAmount: 0 }));
    return;
  }

  if (path === "/api/donations/orders" && req.method === "POST") {
    let body = "";
    req.on("data", (chunk) => { body += chunk; });
    req.on("end", () => {
      res.writeHead(200);
      res.end(JSON.stringify({ orderId: "ORDER-TEST" }));
    });
    return;
  }

  if (path === "/api/donations/capture" && req.method === "POST") {
    let body = "";
    req.on("data", (chunk) => { body += chunk; });
    req.on("end", () => {
      res.writeHead(200);
      res.end(JSON.stringify({ status: "COMPLETED" }));
    });
    return;
  }

  res.writeHead(404);
  res.end(JSON.stringify({ error: "Not found" }));
}

const server = createServer(handleRequest);
const PORT = 3001;

server.listen(PORT, () => {
  console.log(`Mock backend server running on http://localhost:${PORT}`);
});

process.on("SIGINT", () => {
  server.close(() => process.exit(0));
});
process.on("SIGTERM", () => {
  server.close(() => process.exit(0));
});

module.exports = { server, PORT };