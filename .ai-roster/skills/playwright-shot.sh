#!/usr/bin/env bash
# playwright-shot.sh — screenshot a URL in light + dark mode. P4-frontend-demo QA only.
# Usage: playwright-shot.sh <url> <out-dir>
# Requires: npx playwright (installed as a dev dep once the P4 frontend exists — not before).
set -euo pipefail
URL="${1:?Usage: playwright-shot.sh <url> <out-dir>}"
OUT="${2:?Usage: playwright-shot.sh <url> <out-dir>}"
mkdir -p "$OUT"
node -e '
const { chromium } = require("playwright");
(async () => {
  const url = process.argv[1], out = process.argv[2];
  const browser = await chromium.launch();
  for (const scheme of ["light", "dark"]) {
    const ctx = await browser.newContext({ colorScheme: scheme });
    const page = await ctx.newPage();
    await page.goto(url, { waitUntil: "networkidle" });
    await page.screenshot({ path: `${out}/${scheme}.png`, fullPage: true });
    await ctx.close();
  }
  await browser.close();
})();
' "$URL" "$OUT"
echo "Screenshots written to $OUT/light.png and $OUT/dark.png"
