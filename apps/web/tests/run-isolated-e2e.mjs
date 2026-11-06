// Start and stop the production Next server around Playwright tests.
// Requires an already-running API backed by a disposable database.
import { spawn } from "node:child_process";
import { once } from "node:events";
import { fileURLToPath } from "node:url";
import { setTimeout as delay } from "node:timers/promises";

const cwd = fileURLToPath(new URL("../", import.meta.url));
const next = fileURLToPath(new URL("../node_modules/next/dist/bin/next", import.meta.url));
const playwright = fileURLToPath(new URL("../node_modules/@playwright/test/cli.js", import.meta.url));
const web = process.env.E2E_WEB_BASE_URL;
const api = process.env.API_BASE_URL;
if (process.env.E2E_ISOLATED_DATABASE !== "true" || !web || !api ||
    new URL(web).hostname !== "127.0.0.1" || new URL(api).hostname !== "127.0.0.1")
  throw new Error("Isolated E2E requires loopback web/API URLs and a disposable DB marker.");

const webUrl = new URL(web);
const child = spawn(process.execPath, [next, "start", "--hostname", "127.0.0.1", "--port", webUrl.port], {
  cwd, env: process.env, windowsHide: true, stdio: ["ignore", "pipe", "pipe"],
});
let output = "";
for (const stream of [child.stdout, child.stderr]) stream.on("data", (chunk) => { output = (output + chunk).slice(-6000); });
try {
  let ready = false;
  for (let attempt = 0; attempt < 80; attempt++) {
    if (child.exitCode !== null) throw new Error(`Isolated Next stopped: ${output}`);
    try { if ((await fetch(web)).ok) { ready = true; break; } } catch { /* still starting */ }
    await delay(250);
  }
  if (!ready) throw new Error(`Isolated Next did not become ready: ${output}`);
  const tests = spawn(process.execPath, [playwright, "test", ...process.argv.slice(2)], {
    cwd, env: process.env, windowsHide: true, stdio: "inherit",
  });
  const [code] = await once(tests, "exit");
  if (code !== 0) process.exitCode = code || 1;
} finally {
  if (child.exitCode === null) {
    const stopped = once(child, "exit");
    child.kill();
    await stopped;
  }
}
