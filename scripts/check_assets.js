const http = require("http");
const https = require("https");
const uri = "http://127.0.0.1:3001";
function get(url) {
  return new Promise((resolve) => {
    const lib = url.startsWith("https") ? https : http;
    lib
      .get(url, (res) => {
        let len = 0;
        res.on("data", (c) => (len += c.length));
        res.on("end", () =>
          resolve({ status: res.statusCode, length: len, url })
        );
      })
      .on("error", (e) => resolve({ status: 0, error: e.message, url }));
  });
}

(async function () {
  try {
    const homepage = await get(uri);
    if (!homepage || homepage.status !== 200) {
      console.error("Failed to fetch homepage", homepage);
      process.exit(1);
    }
    console.log("Fetched homepage, size:", homepage.length);
    // fetch full HTML (we need the body). Use http.request to collect body.
    await new Promise((resolve, reject) => {
      const lib = http;
      lib
        .get(uri, (res) => {
          let s = "";
          res.on("data", (c) => (s += c.toString()));
          res.on("end", async () => {
            const regex =
              /(?:href|src)="([^"]*(?:\.css|_next\/static\/[^"]*))"/g;
            let m;
            const urls = [];
            while ((m = regex.exec(s))) urls.push(m[1]);
            const unique = [...new Set(urls)];
            console.log("Found assets:", unique.length);
            for (const u of unique) {
              const full = u.startsWith("http")
                ? u
                : u.startsWith("/")
                ? uri + u
                : uri + "/" + u;
              const res = await get(full);
              if (res.status === 200)
                console.log(full, "->", res.status, res.length);
              else console.log(full, "->", res.status, res.error || "failed");
            }
            resolve();
          });
        })
        .on("error", (e) => {
          console.error("ERR", e.message);
          reject(e);
        });
    });
    process.exit(0);
  } catch (e) {
    console.error(e);
    process.exit(2);
  }
})();
