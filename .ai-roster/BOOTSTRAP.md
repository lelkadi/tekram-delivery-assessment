# Bootstrap Runbook — GitHub-Issues-Driven Agent Workflow

> One-time setup. Run by the founder (these steps touch credentials/auth/Homebrew and are
> intentionally NOT automated by an agent). After this, the P1–P4 artifacts already committed in
> `.ai-roster/` and `.github/` are live and usable.

## 1. Homebrew + `gh` CLI
The core tap on this machine is stale (last updated 2022) — installing `gh` without updating first
fetches an ancient version.
```bash
brew update
brew install gh jq
```

## 2. `gh` authentication
Recommended (decision #3): a fine-grained PAT exported as `GH_TOKEN`, scopes `repo`, `issues`,
`pull_requests`, used by all agents non-interactively. Founder's own interactive session can instead
use `gh auth login`.
```bash
# Option A — PAT (recommended for agents)
export GH_TOKEN="<your-fine-grained-PAT>"
# DO NOT COMMIT THIS FILE WITH A REAL TOKEN. Set the env var in your shell profile directly.
# echo 'export GH_TOKEN="<your-fine-grained-PAT>"' >> ~/.zshrc   # uncomment to persist
gh auth status   # should show token-based auth

# Option B — interactive (founder's own use)
gh auth login --git-protocol https --hostname github.com
```
Optional: configure a bot-like git identity for agent commits so `git log` distinguishes them from
founder commits, e.g. per-worktree `git config user.name "careeree-agent"`.

## 3. `git` version check
A stale Homebrew `git` (2.23.0) may shadow Apple's newer `git` (2.39.2) on PATH.
```bash
which -a git
git --version   # want >= 2.39
# if stale: brew upgrade git   (or remove the Homebrew one and rely on Apple's)
```

## 4. `js-yaml` dependency
`sync-agents.js` requires `js-yaml`. Install the workspace devDependency:
```bash
cd /Users/loukan/projects/loukan/tekram-delivery-assessment
npm install
node -e "require('js-yaml')"   # must not throw
```

## 5. Worktree scaffolding
```bash
mkdir -p ~/.agent-worktrees/tekram-delivery-assessment ~/.agent-worktrees/.lanes
```

## 6. GitHub labels
```bash
chmod +x .ai-roster/scripts/bootstrap-labels.sh
REPO=lelkadi/tekram-delivery-assessment .ai-roster/scripts/bootstrap-labels.sh
gh label list --repo lelkadi/tekram-delivery-assessment   # verify full set present
```

## 7. Issue template
Already committed at `.github/ISSUE_TEMPLATE/feedback-story.yml`. Verify it appears as "Feedback-derived
User Story" when running `gh issue create --repo lelkadi/tekram-delivery-assessment --web` or in the GitHub UI's
"New issue" picker.

## 8. Sync agents to both runtimes
```bash
node .ai-roster/scripts/sync-agents.js
```
Confirm:
- `.claude/agents/*.md` created (7 files), each with valid frontmatter (`name`, `description`, `tools`, `model`).
- `.agents/agents/*/agent.json` created for Antigravity roles (if that runtime's model ids are verified — the roster has deprecated unverified Antigravity model references).

## 9. Branch protection on `main` (recommended before agents start opening PRs)
```bash
gh api repos/lelkadi/tekram-delivery-assessment/branches/main/protection -X PUT \
  -f required_status_checks=null \
  -F enforce_admins=true \
  -F required_pull_request_reviews='{"required_approving_review_count":1}' \
  -F restrictions=null
```
(Tune to taste — at minimum, require a PR rather than direct pushes to `main`.)

## 10. Optional: CI state-machine enforcement (P6, not required for P0–P5)
A `.github/workflows/state-machine.yml` that validates label transitions and runs `dotnet test`
against a CI-only Postgres/Redis service container (isolated from the local dev lane stack). Build
this only if the manual flow proves out first.

---

## Quick verification checklist
```bash
gh auth status                                   # ✅ authenticated
git --version                                    # ✅ >= 2.39
node -e "require('js-yaml')"                     # ✅ no throw
gh label list --repo lelkadi/tekram-delivery-assessment | wc -l    # ✅ ~26 labels
ls .claude/agents/ | wc -l                       # ✅ 7
ls .opencode/agents/ | wc -l                     # ✅ 4
```

Once all of the above pass, the workflow is live. Start with **P5** from the roadmap: drive one real
piece of founder feedback through the full 7-stage loop end-to-end (including one deliberate reject)
before relying on it for real work.
