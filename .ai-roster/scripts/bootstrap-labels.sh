#!/usr/bin/env bash
# bootstrap-labels.sh — create the canonical GitHub label set for the workflow.
# Run once (idempotent via --force). Requires: gh installed + authenticated.
set -euo pipefail
REPO="${REPO:-lelkadi/tekram-delivery-assessment}"
command -v gh >/dev/null || { echo "gh not installed"; exit 1; }
gh auth status >/dev/null || { echo "gh not authenticated"; exit 1; }

echo "Creating status labels (colon, no space)..."
for s in 0-intake 1-needs-research 2-needs-spec 3-ready-for-dev 4-in-progress 5-in-review \
         6-qa-failed 7-qa-passed 8-pm-rejected 9-pm-verified 10-arch-rejected 11-done; do
  gh label create "status:$s" --repo "$REPO" --color "ededed" --force
done

echo "Creating area labels..."
for a in frontend backend worker db shared llm infra; do
  gh label create "area:$a" --repo "$REPO" --color "0e8a16" --force
done

echo "Creating priority labels..."
gh label create "priority:P0-blocker" --repo "$REPO" --color "b60205" --force
gh label create "priority:P1-high"    --repo "$REPO" --color "d93f0b" --force
gh label create "priority:P2-normal"  --repo "$REPO" --color "fbca04" --force
gh label create "priority:P3-low"     --repo "$REPO" --color "c2e0c6" --force
gh label create "priority:P4-bonus"   --repo "$REPO" --color "e6f4ea" --force

echo "Creating type labels (type:doc issues skip lanes + QA per the collapsed pipeline)..."
gh label create "type:doc"  --repo "$REPO" --color "1d76db" --force
gh label create "type:code" --repo "$REPO" --color "0052cc" --force

echo "Creating epic + part labels (one epic issue per assessment part)..."
gh label create "epic" --repo "$REPO" --color "3e4b9e" --force
for p in 1 2 3 4 5 6 7 8 9; do
  gh label create "part-$p" --repo "$REPO" --color "bfd4f2" --force
done

echo "Creating signal labels..."
for x in founder-priority blocked "strike:1" "strike:2" "strike:3"; do
  gh label create "$x" --repo "$REPO" --color "5319e7" --force
done

echo "Done. NOTE: agent:claimed:<id> and lane:<n> labels are created on demand by github_flow.sh."
