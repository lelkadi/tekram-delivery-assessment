# DevOps Report: GitHub-Issues-Driven Multi-Agent Workflow — Tooling, Worktrees, Agent-Sync, CI

**Author:** DevOps Expert (subagent)
**Date:** 2026-06-29
**Status:** Investigation only — no implementation. All facts below were re-verified live on this machine.
**Scope reviewed:** `.ai-roster/team.yaml`, `.ai-roster/scripts/sync-agents.js`, `.ai-roster/skills/github_flow.sh`, `.ai-roster/*_instructions.md` (early draft, currently Vue.js/Pinia-flavored — mismatched with the real stack per `CLAUDE.md`, flagged below).

---

## 0. Executive context (verified live, this machine)

| Fact | Verified value |
|---|---|
| `which gh` | not found (exit 1) |
| `gh` via Homebrew | formula resolvable but **stale**: `brew info gh` reports `gh: stable 2.7.0` — real upstream `gh` is in the 2.6x line as of 2026. This is because `homebrew-core` tap on this machine was last updated **2022-04-07** (4+ years stale). `brew update` MUST run before `brew install gh` or it will fetch a years-old version. |
| `which git` (PATH order) | resolves to `/usr/local/bin/git` → **git 2.23.0** (released 2019, Homebrew-installed, abandoned). The newer `/usr/bin/git` (Apple Git, Xcode CLT) is **2.39.2** but is shadowed by PATH order. |
| `git worktree` | supported by both binaries (it's old enough), but git 2.23.0 lacks worktree fixes from 2.3x+ (e.g. better `prune`/`move` reliability, `--orphan` support). Recommend pinning to the newer git either way. |
| `node -v` | v24.11.1 — matches `CLAUDE.md` requirement (`>=24.11.1`). |
| `corepack pnpm -v` | 11.7.0 — matches `packageManager: "pnpm@11.7.0"` in root `package.json`. |
| `npm -v` | 11.6.2 (used only to invoke `corepack`/devDeps; not the package manager of record). |
| `js-yaml` | NOT installed anywhere in the repo (no `node_modules/js-yaml` reachable); `node -e "require('js-yaml')"` throws `Cannot find module 'js-yaml'`. `sync-agents.js` will crash on first run. |
| `jq` | not found (exit 1) — not currently used by `github_flow.sh`, but recommended below for safe JSON handling instead of ad hoc string parsing. |
| `brew --version` | Homebrew 3.4.5 (itself old; current Homebrew is 4.x). |
| Existing worktree already in use | `git worktree list` on this repo shows a **live, pre-existing** worktree: `/Users/loukan/.gemini/antigravity/worktrees/careeree/implement-wave-six-features` at commit `dd83332` on branch `implement-wave-six-features`. This is evidence Antigravity already creates worktrees under `~/.gemini/antigravity/worktrees/<repo>/<branch>/` today — any new scheme must either adopt or deliberately diverge from this convention. |
| `.claude/worktrees/` | Empty directory already exists in the repo at `/Users/loukan/projects/loukan/careeree/.claude/worktrees/` — suggests Claude Code (or a prior session) anticipated worktree usage here too, but nothing has been placed in it yet. |
| `.gitignore` | Already excludes `.claude/`, `.agents/`, `.gemini/`, `node_modules`, `dist/`, `.next/` — good, this means worktree scratch dirs under any of these three roots are already invisible to git status/diff in the main tree. |
| `node_modules` size | 854MB at repo root (pnpm hoists most deps to root `node_modules` via symlinks per workspace); per-app `node_modules` dirs are tiny (footprint is mostly symlinks back to the root + pnpm's global content-addressable store). |
| pnpm store location | `/Users/loukan/Library/pnpm/store/v11` — **outside the repo**, globally shared. This is the key fact that makes multi-worktree pnpm cheap (see §2.4). |
| `.github/` directory | **Does not exist.** No GitHub Actions workflows, no issue templates, no CODEOWNERS. Branch protection / label automation / CI must be built from scratch. |
| Repo remote | `https://github.com/lelkadi/tekram-delivery-assessment.git` (confirmed via `git remote -v`). |
| `apps/api/.env` keys (names only) | `NODE_ENV, PORT, DATABASE_URL, REDIS_URL, JWT_PRIVATE_KEY, JWT_PUBLIC_KEY, JWT_ISSUER, JWT_AUDIENCE, APP_URL, WEB_ORIGIN, OPENAI_KEY, BILLING_MOCK` — confirms `PORT`, `DATABASE_URL`, `REDIS_URL` are the three knobs that must be varied per-worktree if running the stack concurrently (see §2.5). |

---

## 1. Tooling gap analysis

### 1.1 What's missing and exact install commands (macOS, do NOT run yet — founder bootstrap step)

```bash
# 1. Homebrew itself is stale (core tap last synced 2022-04-07) — update first,
#    otherwise `brew install gh` silently installs gh 2.7.0 instead of current.
brew update

# 2. GitHub CLI — required by github_flow.sh and by every agent role that
#    touches issues/PRs/labels.
brew install gh

# 3. Authenticate gh as a real GitHub account/token. Two options:
#    (a) Interactive, human-owned PAT (simplest for a single founder-run repo):
gh auth login --git-protocol https --hostname github.com
#    (b) Headless / agent-friendly, scoped token via env var (preferred for
#        agents that must run non-interactively — see §5.4):
export GH_TOKEN="<fine-grained PAT with repo + issues + pull_requests scopes>"
gh auth status   # verify

# 4. js-yaml — required by .ai-roster/scripts/sync-agents.js, currently absent.
#    Project is a pnpm monorepo, so install as a devDependency at the repo
#    root (NOT global npm install) so it resolves via node_modules:
corepack pnpm add -D js-yaml -w

# 5. jq — recommended (not currently required, but should be adopted — see
#    §4) for safe JSON parsing instead of shell string interpolation:
brew install jq

# 6. git — the PATH-shadowed Homebrew git (2.23.0, from /usr/local/bin) should
#    either be upgraded or removed in favor of the newer Apple Git already on
#    this machine (2.39.2 at /usr/bin/git):
brew upgrade git
#    OR, if not actively needed, `brew uninstall git` and let PATH fall
#    through to /usr/bin/git. Either way, confirm with:
which -a git && git --version
```

### 1.2 Dependency table

| Tool | Status (verified) | Action | Why |
|---|---|---|---|
| `gh` CLI | Not installed | `brew update && brew install gh` then `gh auth login` | `github_flow.sh` hard-depends on it (`command -v gh` guard); all agent roles use it for issues/PRs/labels |
| `js-yaml` | Not installed | `corepack pnpm add -D js-yaml -w` | `sync-agents.js` line 4 `require('js-yaml')` throws immediately otherwise |
| `git` | Installed but PATH resolves to stale 2.23.0 | `brew upgrade git` (or uninstall and rely on Apple Git 2.39.2) | Worktree reliability, modern flags used in §2 |
| `jq` | Not installed | `brew install jq` | Recommended for `github_flow.sh` JSON handling robustness (§4) |
| Homebrew core tap | 4+ years stale | `brew update` | Root cause of misleadingly old `gh`/other formula versions |
| Node | v24.11.1 — OK | none | Matches `CLAUDE.md` / `package.json engines` |
| corepack pnpm | 11.7.0 — OK | none | Matches `packageManager` field |

---

## 2. Git worktree strategy for parallel agents

### 2.1 Directory layout

Do **not** nest worktrees inside the main checkout (`/Users/loukan/projects/loukan/careeree/`) under a tracked path — `.gitignore` already excludes `.claude/`, `.agents/`, `.gemini/`, so any of those three are safe parents, but nesting worktrees inside the *same* repo's working tree is fragile (pnpm recursive scripts, IDE file-watchers, and `find`/`grep` tooling can recurse into them unless every tool respects `.gitignore`).

**Recommendation:** mirror the convention Antigravity has *already* established on this machine — sibling directories outside the main repo, one per active branch:

```
/Users/loukan/
  projects/loukan/careeree/                          # main checkout, always on `main`, never built/run directly by agents
  .agent-worktrees/
    careeree/
      issue-142/      # engineer_backend, branch issue-142
      issue-143/      # engineer_frontend, branch issue-143
      issue-144/      # qa, branch issue-144 (checked out read-only for verification)
```

This keeps worktrees:
- Outside the main repo's working tree (no interference with main-tree `find`/watchers).
- Consistent with the existing `/Users/loukan/.gemini/antigravity/worktrees/careeree/<branch>/` pattern already in active use (confirmed live: `dd83332 [implement-wave-six-features]`) — either adopt that exact path for Antigravity-environment agents, or alias `.agent-worktrees/tekram-delivery-assessment/` for Claude-Code-environment agents and leave Antigravity's own convention alone. **Do not invent a third location** — two worktree roots (one per agent runtime) is already enough to track.
- Easy to `rm -rf` wholesale for cleanup without touching the main repo.

Note `CLAUDE.md`'s `/Users/loukan/pg-careeree-data` is the **Postgres data directory**, unrelated to worktrees — it is not a candidate location and should not be confused with worktree storage. It is relevant only as the thing that makes Postgres a single shared resource (§2.5).

### 2.2 Branch naming

- `issue-<n>` exactly, e.g. `issue-142` — this matches what `gh issue develop <n> --checkout` generates automatically (confirmed behavior; `gh issue develop` derives the branch name from the issue number/title and defaults to a slug like `<n>-<title-slug>` unless `--name` is passed). **Pin it explicitly** rather than trusting the default slug, so downstream tooling (PR title parsing, worktree path) can rely on a fixed format:
  ```bash
  gh issue develop <n> --name "issue-<n>" --checkout
  ```

### 2.3 `gh issue develop` + worktrees

`gh issue develop <n> --checkout` checks the branch out **in the current working directory** — it does not create a worktree by itself. For parallel agents this must be combined with `git worktree add` so each agent gets an isolated filesystem, not a `git checkout` that fights other agents in the same tree:

```bash
# Run from the main repo clone:
git fetch origin
gh issue develop <n> --name "issue-<n>" --base main   # creates the remote-tracked branch, does NOT check it out here
git worktree add /Users/loukan/.agent-worktrees/tekram-delivery-assessment/issue-<n> issue-<n>
```

If `gh issue develop` is run with `--checkout` from inside an already-checked-out worktree, it will check the branch out *there*, which is fine too — but the create-branch step and the worktree-add step should be treated as two explicit, scripted steps rather than relying on `--checkout`'s side effect, since `--checkout` assumes you're standing in the directory you want the branch active in.

### 2.4 Avoiding double-claims on the same issue (the actual hard problem)

`github_flow.sh fetch` currently just lists issues with `status: 2-ready-for-dev` — there is **no claim/lock step**, so two agents racing `fetch` → `start` can both pick issue #142. Fix: make `start` atomic against the label itself, using the label transition as the lock:

```bash
# inside github_flow.sh `start`, BEFORE creating the branch:
gh issue edit "$TARGET_ID" --remove-label "status: 2-ready-for-dev" --add-label "status: 2b-claimed"
# gh issue edit is not compare-and-swap, so add a verification read-back:
CURRENT=$(gh issue view "$TARGET_ID" --json labels -q '.labels[].name')
if ! echo "$CURRENT" | grep -q "status: 2b-claimed"; then
  echo "Error: failed to claim issue #$TARGET_ID (label mismatch — race lost to another agent)"; exit 1
fi
```

This is not true compare-and-swap (GitHub's REST API has no atomic label CAS), but in practice the race window is small and the read-back catches the rare case where two `gh issue edit` calls land within the same second. A stronger approach for a small team: have each agent self-assign via `gh issue edit <n> --add-assignee <bot-login>` (assignee writes are also not atomic, but combining label-flip + assignee + a unique `claimed-by:<bot-login>` comment posted immediately gives a 3-way fingerprint an Architect/PM can audit if a double-claim ever slips through).

### 2.5 The shared local stack is the real contention point

This is the most important point in this section. Per `CLAUDE.md` §2/§3, the **real** (non-mocked) stack is:

- Postgres on **:5432**, data dir `/Users/loukan/pg-careeree-data` — single instance, single data directory.
- Redis on **:6379** — single instance.
- API Fastify on **:3001**, Web Next.js on **:3000**, Worker (no port, but binds Postgres+Redis).

If N engineer-agent worktrees each try to run `pnpm dev`/`pnpm dev:api`/`pnpm dev:worker` simultaneously, they will:
1. **Port-collide** on 3000/3001 (second process fails to bind).
2. **Share one Postgres instance and one database**, so two agents' migrations/test-data writes corrupt each other's runs (per `CLAUDE.md` §1 "no mocking" rule, this can't be avoided by mocking the DB away).
3. **Share one Redis**, so tier/entitlement keys (`tier:<id>`) and BullMQ queues collide across agents' test runs.

**Proposed allocation scheme** — give each concurrently-running worktree a deterministic port/DB offset derived from a small integer "lane" rather than the issue number (issue numbers are unbounded; lanes are a fixed small pool matching how many agents you actually run at once, e.g. 1-4):

| Lane | Web port | API port | Postgres DB name | Redis DB index |
|---|---|---|---|---|
| 0 (main/CI) | 3000 | 3001 | `careeree` / `careeree_test` | 0 |
| 1 | 3010 | 3011 | `careeree_lane1` | 1 |
| 2 | 3020 | 3021 | `careeree_lane2` | 2 |
| 3 | 3030 | 3031 | `careeree_lane3` | 3 |

Mechanics:
- **Postgres**: single running `postgres` process (already true today), but each lane gets its own **database** (`CREATE DATABASE careeree_lane1 TEMPLATE careeree_test;`) inside that one instance — cheap, isolated, and migrations can be re-applied per lane without affecting others. `DATABASE_URL` in each worktree's `.env` points at its lane DB.
- **Redis**: Redis supports 16 logical DBs by default (`SELECT n` / `redis://localhost:6379/<n>`) — assign each lane a DB index in `REDIS_URL`. This is a single `redis-server` process already (matches `CLAUDE.md`'s "never put Redis under /tmp" + "single instance" reality) — no extra process needed, just URL-suffix routing.
- **Ports**: `PORT` env var for API per lane (3011/3021/3031), and `next dev -p 301x` / `next start -p 301x` for web. The worker has no listening port but must point at the same lane's `DATABASE_URL`/`REDIS_URL`.
- **Lane assignment**: a lockfile-based scheme, e.g. `/Users/loukan/.agent-worktrees/.lanes/lane-<n>.lock` containing the PID + issue number holding it; `start` script picks the first free lane (1..3), writes the lock, and **fails fast with an explicit error** ("all lanes busy, wait or increase MAX_LANES") rather than silently sharing a lane. This caps real concurrency to however many lanes the founder provisions (recommend starting with 2-3, matching realistic local CPU/RAM headroom for N Next.js dev servers + N Fastify + 1 shared Postgres/Redis).
- **Serialization fallback**: if the founder doesn't want to manage multiple DBs/ports at all, the simplest viable v1 is **strict serialization** — only one engineer agent may hold the stack (lane 0, the default ports) at a time, enforced by a single global lockfile (`/Users/loukan/.agent-worktrees/.lanes/global.lock`). This is simpler to build and matches the wave-gated, mostly-sequential nature of the existing `docs/feedback-round-2` workflow, at the cost of zero engineer parallelism. **Recommend starting here and only building the multi-lane scheme if serialization proves to be the bottleneck** — it's a one-file lockfile script vs. a DB-templating + port-offset system.

### 2.6 pnpm / node_modules cost per worktree

Because the pnpm store (`/Users/loukan/Library/pnpm/store/v11`, confirmed live) is a single global content-addressable cache **outside any worktree**, each new worktree's `pnpm install` mostly creates symlinks into that shared store rather than re-downloading/re-extracting packages — so per-worktree disk cost is small (tens of MB of symlinks + lockfile-driven `node_modules` trees), not another 854MB. Still budget for:
- `pnpm install` time per worktree (symlink-only, but still walks the full dependency graph — expect tens of seconds, not the multi-minute cold-cache case).
- `.next/` build caches are NOT shared across worktrees (each is its own — already gitignored, confirmed).
- Recommend each worktree-start script run `corepack pnpm install --frozen-lockfile` immediately after `git worktree add`, before any agent touches code.

### 2.7 Cleanup of stale worktrees

```bash
# Periodic (e.g. PM/Architect wave-boundary, or a cron-style check):
git worktree list --porcelain | ...   # parse, cross-reference against open `status: 2-ready-for-dev`/`3-in-review` issues
git worktree remove /Users/loukan/.agent-worktrees/tekram-delivery-assessment/issue-<n>   # once issue is closed/merged
git worktree prune                     # clears administrative files for worktrees whose directories were deleted out-of-band
```
A worktree should be removed when its issue's PR is merged or closed, and its lane lock released at the same time. Recommend the `submit` step in `github_flow.sh` (after PR creation) leave the worktree in place (so QA/Architect can still inspect it) and have a separate `cleanup <issue_id>` command that the PM/orchestrator runs once the issue is fully closed — never auto-delete on submit, since the engineer's job isn't done until review passes.

---

## 3. Agent-sync mechanism — critique of `sync-agents.js`

### 3.1 Verified: where Claude Code actually reads agents/commands from

Checked live on this machine (`/Users/loukan/.claude/` has no `agents/` or `commands/` subdirectory yet — global scope unused so far; project scope at `/Users/loukan/projects/loukan/careeree/.claude/` currently has only `settings.local.json` and `worktrees/`).

The canonical, documented Claude Code locations are:

| Artifact | Project scope | User/global scope |
|---|---|---|
| Custom subagents | `.claude/agents/<name>.md` | `~/.claude/agents/<name>.md` |
| Slash commands | `.claude/commands/<name>.md` | `~/.claude/commands/<name>.md` |

**Subagent file format** (`.claude/agents/<name>.md`) — YAML frontmatter + body system prompt:
```markdown
---
name: backend-engineer
description: Senior Backend Engineer for API endpoints, migrations, business logic. Use proactively for backend issues labeled status:2-ready-for-dev.
tools: Read, Edit, Write, Bash, Grep, Glob
model: sonnet
---

You are a Senior Backend Engineer...
<body = the system prompt, currently living in backend_instructions.md>
```

**Slash command file format** (`.claude/commands/<name>.md`) — optional frontmatter + prompt body, invoked as `/<name>`:
```markdown
---
description: Invoke the Lead Architect agent
---

<prompt body — currently living in architect_instructions.md>
```

### 3.2 What `sync-agents.js` gets wrong

```js
const CLAUDE_CONFIG_PATH = path.join(__dirname, '../claude.json');
...
claudeJson.customCommands = { ...claudeCustomCommands };
fs.writeFileSync(CLAUDE_CONFIG_PATH, JSON.stringify(claudeJson, null, 2));
```

- **`claude.json` with a `customCommands` key is not a real Claude Code config surface.** Confirmed: no such file exists yet in this repo, and Claude Code's actual configuration/settings file is `.claude/settings.json` (project) / `~/.claude/settings.json` (global) — neither supports a `customCommands` map; agents and commands are **file-based** (one Markdown file per agent/command, per §3.1), not entries in a JSON config blob. As written, `sync-agents.js` produces a JSON file Claude Code will never read.
- **It maps every `environment: "claude-code"` entry (PM, Architect, Researcher) to a slash *command*, not a subagent.** Given `CLAUDE.md`'s wave-gated model — "Engineer → QA → Architect → PM" as **independent subagents with fresh context** — these three should sync to `.claude/agents/<id>.md` (true subagents, invocable via the `Agent` tool with `subagent_type: <id>`, fresh context guaranteed) rather than slash commands (which run in the *current* conversation's context, violating the "fresh context" requirement in `CLAUDE.md` §4.3/§4.1).
- **`require('js-yaml')` will throw immediately** since the package isn't installed (verified above) — the script cannot currently run at all, before even reaching the path-mapping bug.
- No `tools:` or `model:` frontmatter is emitted for the Claude Code side — required fields for a working `.claude/agents/*.md` file. `team.yaml` already carries `model:` per agent (e.g. `claude-opus-4-8-thinking`, `claude-sonnet-4-6`) — note these model strings need to map to whatever Claude Code's `model:` frontmatter accepts (short aliases like `opus`/`sonnet`/`haiku`, or this installation's accepted full IDs — verify against current Claude Code docs before relying on the exact string, since `claude-opus-4-8-thinking` is not a standard short alias).
- The Antigravity side (`.agents/agents/<id>/agent.json`) is more plausible (matches the existing empty `.agents/agents/` dir already gitignored in this repo) but should be double checked against current Antigravity docs for the exact schema key names (`system_instruction` vs `systemInstruction` vs `instructions`, etc.) — not verifiable from this machine since Antigravity's own config schema isn't introspectable via CLI here.

### 3.3 Recommended sync target

Treat `team.yaml` + the per-role `*_instructions.md` files as the single source of truth (as designed), but fix the emission targets:

```
.ai-roster/team.yaml                          # source of truth: id, model, environment, skills
.ai-roster/<role>_instructions.md             # source of truth: system prompt body

  sync-agents.js  -->

.claude/agents/<id>.md          # for environment: claude-code  → frontmatter (name, description, tools, model) + body = instructions_file content
.agents/agents/<id>/agent.json  # for environment: antigravity  → existing schema in sync-agents.js, keep as-is pending Antigravity schema verification
```

Concretely, rewrite the Claude Code branch of `syncAgents()` from:
```js
claudeCustomCommands[agentId] = { description: ..., prompt: promptText };
```
to:
```js
const frontmatter = [
  '---',
  `name: ${agentId}`,
  `description: ${config.display_name}${config.description_suffix ? ' — ' + config.description_suffix : ''}`,
  `tools: ${(config.tools || ['Read','Edit','Write','Bash','Grep','Glob']).join(', ')}`,
  `model: ${config.claude_model_alias || 'sonnet'}`,
  '---',
  '',
].join('\n');
fs.writeFileSync(path.join(CLAUDE_AGENTS_DIR, `${agentId}.md`), frontmatter + promptText);
```
This requires adding a `tools:` list and a short `claude_model_alias` (e.g. `opus`/`sonnet`/`haiku`) per Claude-Code-environment entry in `team.yaml`, since the model strings currently there (`claude-opus-4-8-thinking`) look like Antigravity-style model IDs, not Claude Code's frontmatter aliases.

---

## 4. Critique of `github_flow.sh`

| Issue | Detail | Fix |
|---|---|---|
| `git add .` | Directly violates `CLAUDE.md` §1.3/§5: "Commit atomically with specific filenames. Never `git add -A` or `git add .`." This is a hard constraint, not a style preference. | Replace with an explicit, agent-supplied file list: `submit <issue_id> "<message>" "<file1> <file2> ..."`, or have the agent run `git add <files>` itself before calling `submit`, and have `submit` only do `git diff --cached --quiet || git commit -m "$MESSAGE"` (fails loudly if nothing staged, never globs). |
| Worktree-unaware | `start` does `gh issue develop "$TARGET_ID" --checkout` in-place — assumes a single shared working directory, conflicting with the parallel-worktree design in §2. | `start` should `git worktree add <lane-path> issue-<n>` (after `gh issue develop --name issue-<n>` without `--checkout`), then `cd` the agent's working context into the new worktree path. |
| No claim mechanism | `fetch` lists candidates; nothing prevents two agents calling `start` on the same issue number — confirmed by reading the script: no label-flip happens until `submit`, and even then it only transitions `2-ready-for-dev → 3-in-review`, with no intermediate "claimed" state. | Add the `status: 2b-claimed` (or assignee-based) lock described in §2.4, applied at `start`, not at `submit`. |
| No port/DB lane handling | Script has zero awareness of the shared-stack contention problem (§2.5). | `start` should also acquire a lane lock and write a lane-scoped `.env.local` (or export `PORT`/`DATABASE_URL`/`REDIS_URL` overrides) into the new worktree. |
| Hard `gh` dependency with only an existence check | `command -v gh` checks installed, but never checks `gh auth status` — a not-logged-in `gh` will fail confusingly deep inside `fetch`/`start`/`submit`. | Add `gh auth status &>/dev/null || { echo "Error: gh not authenticated, run gh auth login"; exit 1; }` alongside the existing install check. |
| `qa-comment` takes a PR id positionally but other commands take an issue id | Easy for an agent (or instructions.md) to pass the wrong number — `qa_instructions.md` correctly calls it with `<pr_id>`, but nothing in the script enforces/labels which ID type is expected at the call site beyond the usage string. | Low severity; rename the param in usage text to make the type unambiguous (`qa-comment <pr_number> "<message>"`), already mostly done — just tighten consistency with `start`'s `<issue_id>`. |
| String message via `$3` only | `submit`'s `$MESSAGE` is a single positional arg — multi-word messages need careful quoting by the calling agent (the instructions.md files do quote correctly, but it's a footgun for any agent that doesn't). | Acceptable for now given instructions already quote properly; flag as a known fragility, not a blocker. |

### 4.1 Proposed corrected command surface

```
github_flow.sh fetch [--label <status-label>]                     # unchanged, but should accept any status label, not hardcode "2-ready-for-dev" (qa_instructions.md already calls fetch expecting "3-in-review" results — current script doesn't support that)
github_flow.sh claim <issue_id>                                    # NEW: atomic-ish label flip + assignee + comment fingerprint (§2.4)
github_flow.sh start <issue_id> <lane_n>                           # creates branch via `gh issue develop --name issue-<n>` (no --checkout), then `git worktree add`, then writes lane-scoped env, then `pnpm install`
github_flow.sh submit <issue_id> "<message>" <file1> [file2 ...]   # explicit file list, never `git add .`/`-A`
github_flow.sh qa-comment <pr_number> "<message>"                  # unchanged
github_flow.sh cleanup <issue_id>                                  # NEW: git worktree remove + lane lock release, run after merge/close
github_flow.sh lanes                                                # NEW: introspection — print which lanes are free/held and by whom
```

Note `qa_instructions.md` already calls `fetch` expecting it to surface `status: 3-in-review` items, but the current script hardcodes `status: 2-ready-for-dev` in the `fetch` case — this is an existing bug in the draft, independent of the worktree work, worth fixing in the same pass.

---

## 5. CI / safety

### 5.1 Branch protection on `main`

No `.github/` directory exists yet (confirmed) — there is currently zero CI and zero branch protection. Minimum recommended ruleset on `main` (configured via `gh api` or the GitHub UI, not yet doable headlessly until `gh` is installed/authed):
- Require PR before merging (no direct pushes — this also backstops the "never push directly to main" instruction already present in `engineer_instructions.md`/`backend_instructions.md`).
- Require at least 1 review/approval before merge — even if the "reviewer" is itself an agent (the QA/Architect role), GitHub's branch protection can require a passing required status check rather than a human approval, which fits an agent-driven flow better than human-approval gating.
- Require status checks to pass (the CI workflow described in §5.2) before merge.
- Optionally require linear history / disallow force-push.

### 5.2 PR-to-issue linking

`github_flow.sh submit` already does this correctly via PR body text (`gh pr create --body "...Automated implementation for issue #$TARGET_ID..."`) — GitHub auto-links `#<n>` mentions in PR bodies to the issue and (if the magic close-keyword form `Closes #<n>` / `Fixes #<n>` is used) will auto-close the issue on merge. **Recommend switching the body text to use `Closes #$TARGET_ID`** explicitly so issue-closing is automatic on merge rather than left to a separate `gh issue edit --add-label` step that could drift out of sync with actual merge state.

### 5.3 Label automation / state-machine enforcement via GitHub Actions

Since there's no CI yet, propose a minimal `.github/workflows/state-machine.yml` that:
- Triggers on `pull_request` (opened/synchronize) and `issues` (labeled) events.
- Validates that a PR's linked issue has an allowed label transition (e.g. block a PR from being labeled `3-in-review` if the issue isn't currently `2-ready-for-dev` or `2b-claimed`) — this is the GitHub-side backstop for the claim race in §2.4, catching it after the fact even if the local lockfile scheme has a gap.
- Runs the actual test suite (`corepack pnpm --recursive test`) as a required status check — this is also the natural place to run a real Postgres+Redis **in CI** (GitHub Actions service containers), separate from and not contending with the local dev-stack contention problem in §2.5 (CI has its own isolated Postgres/Redis per run).
- Optionally auto-applies `status: 4-qa-failed` if the CI run fails, mirroring what `qa_instructions.md` step 5 says a QA agent should do manually.

### 5.4 Secrets / auth for agents running `gh`

- For a single-founder repo, the simplest model is a **single fine-grained PAT** stored as `GH_TOKEN` in each agent's environment (not committed, not in `.env` files that get gitignored-but-still-risk-leaking via agent tool output). Scope it minimally: `repo` (issues, PRs, contents) — avoid `admin:org`/broader scopes.
- `gh` picks up `GH_TOKEN` (or `GITHUB_TOKEN`) from the environment automatically, ahead of the locally-stored `gh auth login` keychain credential — meaning interactive `gh auth login` (human-owned) and `GH_TOKEN`-env (agent-owned) can coexist without fighting, as long as agent processes are launched with `GH_TOKEN` exported and the founder's interactive shell is not.
- Recommend a **separate machine user or fine-grained token per "swimlane"** is overkill for this scale; a single bot-scoped PAT with a clearly bot-like git identity (`user.name`/`user.email` set to e.g. `careeree-agent <agent@noreply>` in each worktree's local git config, not global) makes it easy to distinguish agent-authored commits from the founder's in `git log`.
- Never let `github_flow.sh` or any instructions.md print the token; confirmed none of the current scripts do.

---

## 6. Concrete dependency checklist (founder runs once)

```bash
# --- 1. Homebrew + core tools ---
brew update                       # core tap here is 4+ years stale; must run first
brew install gh jq
gh auth login --git-protocol https --hostname github.com
gh auth status                    # confirm

# --- 2. git hygiene (stale Homebrew git shadows newer Apple Git on PATH) ---
which -a git                      # confirm /usr/local/bin/git (old) is found before /usr/bin/git
brew upgrade git                  # or: brew uninstall git
git --version                     # confirm >= 2.39 after fix

# --- 3. repo-local JS dependency for the sync script ---
cd /Users/loukan/projects/loukan/careeree
corepack pnpm add -D js-yaml -w
node .ai-roster/scripts/sync-agents.js   # should now run without throwing (still emits to wrong paths until §3.3 fix lands)

# --- 4. worktree root + lane-lock scaffolding (directories only, no code) ---
mkdir -p /Users/loukan/.agent-worktrees/tekram-delivery-assessment
mkdir -p /Users/loukan/.agent-worktrees/.lanes

# --- 5. GitHub repo setup (once gh is authed) ---
gh label create "status: 1-needs-spec"        --color "ededed" --repo lelkadi/tekram-delivery-assessment
gh label create "status: 2-ready-for-dev"     --color "0e8a16" --repo lelkadi/tekram-delivery-assessment
gh label create "status: 2b-claimed"          --color "fbca04" --repo lelkadi/tekram-delivery-assessment
gh label create "status: 3-in-review"         --color "1d76db" --repo lelkadi/tekram-delivery-assessment
gh label create "status: 4-qa-failed"         --color "d73a4a" --repo lelkadi/tekram-delivery-assessment
# branch protection on main (requires repo admin on the PAT):
gh api repos/lelkadi/tekram-delivery-assessment/branches/main/protection -X PUT -f required_status_checks.strict=true ... # finalize exact payload once CI workflow (§5.3) exists
```

---

## Open items for the team (not blockers, but flag to founder/Architect)

1. **Instruction files are Vue.js/Pinia-flavored** (`engineer_instructions.md`, `architect_instructions.md`, `backend_instructions.md` all say "Vue.js PWA"/"Vuex/Pinia") — but `CLAUDE.md` says the real stack is **Next.js 15 + React 19**. This is a content mismatch outside DevOps scope, but it will produce confidently wrong code from any engineer agent until corrected — flagging since it directly affects whether the workflow being built is exercised against the real stack per the "no mocking, verify live" hard constraint.
2. `team.yaml` models (`gemini-3.5-flash` for all three Antigravity roles, `claude-opus-4-8-thinking` for PM/Researcher) should be sanity-checked against `CLAUDE.md` §11's model-selection-by-complexity guidance — PM/Researcher at max-effort Opus for what are largely label/comment operations looks like overkill by that rubric, though that's a cost call for the team, not a tooling gap.
3. No `.env.example` / lane-template found at `apps/api/` — when building the lane-allocation scheme (§2.5), the per-lane `.env` generation should derive from whatever the canonical `.env` template is; confirm one exists or gets created before wiring lanes.
