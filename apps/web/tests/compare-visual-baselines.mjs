// Exact PNG comparison for the reviewed local Chrome fixture baselines.
// This command never updates or accepts a changed baseline.
import { createHash } from "node:crypto";
import { readFile, readdir } from "node:fs/promises";
import { join, resolve } from "node:path";

const baselineDir = resolve("tests/visual-baselines");
const storefrontDir = resolve(process.env.STOREFRONT_CAPTURE_DIR || "../../artifacts/p11-run/storefront-baselines");
const baselines = (await readdir(baselineDir)).filter((name) => name.startsWith("storefront-") && name.endsWith(".png"));
if (!baselines.length) throw new Error("No visual baselines found.");

const changed = [];
for (const name of baselines) {
  const suffix = name.slice("storefront-".length);
  const captured = join(storefrontDir, suffix.startsWith("fixture-") ? suffix : `fixture-${suffix}`);
  try {
    const [expected, actual] = await Promise.all([readFile(join(baselineDir, name)), readFile(captured)]);
    const hash = (bytes) => createHash("sha256").update(bytes).digest("hex");
    if (hash(expected) !== hash(actual)) changed.push(name);
  } catch {
    changed.push(name);
  }
}
if (changed.length) {
  console.error(`${changed.length}/${baselines.length} visual baselines missing or changed:\n${changed.join("\n")}`);
  process.exitCode = 1;
} else console.log(`${baselines.length}/${baselines.length} visual baselines match exactly.`);
