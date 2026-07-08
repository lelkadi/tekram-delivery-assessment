# Architect Unified Plan: GitHub-Issues-Driven Multi-Agent Feedback Workflow

**Role:** Lead Architect (synthesis)
**Date:** 2026-06-29
**Status:** PLANNING ONLY — founder reviews this to greenlight implementation. No code/roster files rewritten yet.
**Inputs reconciled:** `CLAUDE.md`, `01-researcher-workflow-design.md`, `02-devops-tooling.md`, all 8 draft `.ai-roster/` files.
**Live re-verifications performed by this synthesis** (CLAUDE.md §1.4 "trust but verify"): js-yaml resolution, `gh` presence, `.github/` presence, worktree list, `.claude`/`.agents` dirs.

---

## 1. Executive Summary + Corrected Loop

The founder wants the existing `docs/feedback-round-2`-style wave-gate process (Engineer → QA → Architect → PM, markdown reports in `docs/`) re-hosted as a **GitHub-issue-driven, multi-agent pipeline** where the issue thread *is* the report directory, atomic user-stories flow independently, engineers work in **parallel git worktrees**, and the whole thing is callable from **both Claude Code and Antigravity**.

The draft `.ai-roster/` was written against a fictitious **Vue.js/Pinia/Gemini** stack and is missing 3 of the 7 founder-requested stages (Researcher, PM-verification, Architect-code-review) and both reject loops. This plan fixes the stack contract, defines the full 7-stage + reject-loop state machine, a parallel-safe claim + worktree + shared-stack lane scheme, the correct dual-environment sync targets, and a phased rollout.

**The canonical founder loop (corrected, with reject paths):**

```
 founder feedback
        │
        ▼
 [PM-INTAKE]            split into atomic INVEST user-stories, one issue each,
   (Claude Code)        with verbatim founder quote + acceptance criteria
        │
        ▼
 [RESEARCHER]           investigate APIs/schema/"does this already exist",
   (Claude Code)        attach Research Notes comment
        │
        ▼
 [ARCHITECT-SPEC]       technical blueprint per issue: exact files, migrations,
   (Claude Code)        API contract, CLAUDE.md rules that apply
        │
        ▼
 [ENGINEERS]  ◄──────────────────────────────────┐  per-issue branch + git worktree,
   (Antigravity, parallel team)                   │  claim-locked, update issue as they go
        │                                         │
        ▼                                         │
 [QA]  ── fail ──► (loop back) ───────────────────┤  real stack + Playwright, impl ↔ spec
   (Antigravity)                                  │
        │ pass                                    │
        ▼                                         │
 [PM-VERIFICATION] ── fail ──► (loop back) ───────┤  interactive test vs founder intent /
   (Claude Code, founder-adjacent)                │  acceptance criteria, on the RUNNING app
        │ pass                                    │
        ▼                                         │
 [ARCHITECT-CODE-REVIEW] ── reject ──► (loop back)┘  accept/reject the diff vs CLAUDE.md rules
   (Claude Code)
        │ accept
        ▼
   merge PR · close issue · remove worktree · release lane
```

All three reject gates (QA / PM / Architect) bounce the **same** issue back to the **same** engineer/worktree. A **3-strike circuit breaker** escalates to the founder. Everything — every handoff, every verdict — is an append-only comment on the issue.

---

## 2. Final Issue State Machine

### 2.1 Canonical status label set (mutually exclusive, exactly one per issue)

Adopt the researcher's 12-state machine. **Resolution of the devops vs researcher label naming clash:** the devops report used `status: 2b-claimed` with a **space** after the colon (matching the original draft `status: 2-ready-for-dev`). Spaces in label names are legal but fragile in shell `grep`/`--json` pipelines. **Decision: no space** — `status:<n>-<slug>` (colon, no space). This sorts correctly in GitHub's UI, is greppable with anchored patterns, and the numeric prefix makes "skipped a stage" trivially detectable. All existing draft references with a space are corrected on sync.

```
status:0-intake          # PM drafting/splitting; not yet a finished atomic issue
status:1-needs-research  # filed; Researcher should investigate
status:2-needs-spec      # research attached (or N/A); Architect should design
status:3-ready-for-dev   # spec attached; open for Engineer claim (UNCLAIMED)
status:4-in-progress     # an Engineer claimed it; worktree exists; actively building
status:5-in-review       # PR opened; awaiting QA
status:6-qa-failed       # QA found defects vs spec; back to same Engineer
status:7-qa-passed       # matches spec; awaiting PM verification
status:8-pm-rejected     # fails founder-intent / acceptance criteria; back to Engineer
status:9-pm-verified     # acceptance criteria met from running app; awaiting Arch review
status:10-arch-rejected  # code review rejected; back to Engineer
status:11-done           # Architect accepted; PR merged; issue closed
```

### 2.2 Orthogonal (multi-select) labels

```
# Area (which part of the real monorepo)
area:frontend   area:backend   area:worker   area:db   area:shared   area:llm   area:infra

# Priority
priority:P0-blocker   priority:P1-high   priority:P2-normal   priority:P3-low

# Claim / ownership (parallel-engineer lock — see §5.4)
agent:claimed:<engineer-id>     # e.g. agent:claimed:eng-1
lane:<n>                         # which shared-stack lane this issue holds (lane:1..lane:3)

# Signals
blocked                 # depends on another open issue ("Blocked by #N")
founder-priority        # founder explicitly named it; also the circuit-breaker escalation tag
needs:research          # Researcher found unresolved unknowns (supplements stage)
strike:1  strike:2  strike:3   # reject-loop counter (circuit breaker — see §2.5)
```

### 2.3 Transition table

| # | From | Actor | Action | To | Notes |
|---|---|---|---|---|---|
| 1 | (none) | PM | Split feedback → atomic story, create issue via Issue Form | `0-intake` | One issue per INVEST story (§4 rubric) |
| 2 | `0-intake` | PM | Confirm well-formed, apply label | `1-needs-research` | If trivially small, PM may skip to `2-needs-spec` and comment why |
| 3 | `1-needs-research` | Researcher | Post Research Notes comment, swap label | `2-needs-spec` | If too complex, re-label `0-intake` + "needs PM re-scope" comment |
| 4 | `2-needs-spec` | Architect | Post Architect Spec comment, swap label | `3-ready-for-dev` | Spec names exact files/migrations |
| 5 | `3-ready-for-dev` | Engineer | **Claim** (add `agent:claimed:<id>` + `lane:<n>` + self-assign + comment), create worktree+branch | `4-in-progress` | Atomic-ish check-then-act guard (§5.4) |
| 6 | `4-in-progress` | Engineer | Implement, atomic commits, open PR (`Refs #N` during loop), Engineer Notes comment | `5-in-review` | |
| 7 | `5-in-review` | QA | Pull branch, run real stack in its lane, Playwright, post QA Results | `7-qa-passed` / `6-qa-failed` | QA checks impl ↔ spec only |
| 8 | `6-qa-failed` | Engineer | Fix in same worktree, force-push, re-request | `5-in-review` | Same engineer keeps claim; increments `strike:n` if same gate |
| 9 | `7-qa-passed` | PM | Interactively exercise running app vs acceptance criteria, post PM Verification | `9-pm-verified` / `8-pm-rejected` | PM = founder intent, not spec |
| 10 | `8-pm-rejected` | Engineer | Fix (quote failed AC), force-push | `5-in-review` | Increment `strike:n` |
| 11 | `9-pm-verified` | Architect | Review diff vs CLAUDE.md rules, post Architect Verdict | `11-done` / `10-arch-rejected` | Final gate |
| 12 | `10-arch-rejected` | Engineer | Fix (file:line findings), force-push | `5-in-review` | Increment `strike:n` |
| 13 | `11-done` | Architect/orchestrator | Merge PR (`Closes #N`), close issue, `cleanup <n>` (remove worktree, release lane) | (closed) | Only place `gh issue close` happens |
| CB | any `*-failed`/`*-rejected` reached **3×** total | (auto) | Add `founder-priority` + `strike:3`, comment requesting founder review, **freeze** (no further auto-loop) | (frozen) | Circuit breaker (§2.5) |

**Design choice (from researcher, adopted):** rejects do **not** reset to `3-ready-for-dev` — that would let a different engineer pick it up mid-flight and lose worktree context. They return to `5-in-review`'s predecessor implicitly by letting the same engineer resume the same worktree. Only an explicit **abandonment sweep** (engineer crashed/timed out, no commit activity > N hours) resets to `3-ready-for-dev` and clears `agent:claimed:*` + releases the lane.

### 2.4 ASCII state diagram

```
 feedback ─► [0-intake]──►[1-needs-research]──►[2-needs-spec]──►[3-ready-for-dev]
   (PM)         (PM)         (Researcher)        (Architect)       │  (UNCLAIMED)
                                                                   │ Engineer claims
                                                                   ▼
                                            ┌───────────────►[4-in-progress]
                                            │                      │ PR opened
                          re-claim          │                      ▼
                       (same worktree)      │                 [5-in-review]◄──────────┐
                                            │                      │  (QA)            │
                                            │            ┌── fail ─┴─ pass ──┐        │
                                            │            ▼                   ▼        │
                                            │      [6-qa-failed]        [7-qa-passed] │
                                            │            │                   │ (PM)   │
                                            └────────────┘            ┌─ fail─┴─pass─┐│
                                            ▲                         ▼              ▼│
                                            │                   [8-pm-rejected] [9-pm-verified]
                                            │                         │              │ (Architect)
                                            ├─────────────────────────┘      ┌─reject─┴─accept─┐
                                            │                                ▼                 ▼
                                            │                        [10-arch-rejected]   [11-done]
                                            └────────────────────────────────┘         merge+close+
                                                                                        cleanup worktree
   CIRCUIT BREAKER: 3rd entry into ANY *-failed/*-rejected on one issue
   ─► add founder-priority + strike:3, comment "human review needed", FREEZE (stop auto-loop)
```

### 2.5 Circuit breaker (3-strike escalation)

- A `strike:n` label tracks total rejections across **all** gates for one issue (QA fail + PM reject + Arch reject all count toward the same counter — the founder cares "is this issue thrashing," not "which gate").
- On the **3rd** strike: the rejecting agent adds `founder-priority`, sets `strike:3`, posts a comment summarizing the three failure reasons, and **does not re-loop**. The issue freezes until the founder manually intervenes (re-scope, re-spec, or accept-with-debt).
- **Founder decision required:** strike count = 3 is the recommendation; founder may set 2 (stricter) or 5 (more patient). See §10.

---

## 3. Issue Schema / Templates

### 3.1 Layering decision (from researcher, adopted)

- **GitHub Issue Form** (`.github/ISSUE_TEMPLATE/feedback-story.yml`) for **PM-intake only** — forces `user_story`, `founder_quote`, `acceptance_criteria`, `area`, `priority` to be filled before an issue exists.
- **Every downstream role posts an append-only COMMENT** (never edits the body). The body stays PM-authored and stable; the comment thread is the version-controlled audit log. This avoids body-edit races between concurrent agents and maps 1:1 onto the old `docs/.../reports/` directory.

### 3.2 PM-intake Issue Form (`.github/ISSUE_TEMPLATE/feedback-story.yml`)

```yaml
name: Feedback-derived User Story
description: An atomic story split from founder feedback by the PM agent.
title: "[Story] <short imperative summary>"
labels: ["status:0-intake"]
body:
  - type: textarea
    id: user_story
    attributes: { label: User Story, description: "As a <role>, I want <capability>, so that <benefit>." }
    validations: { required: true }
  - type: textarea
    id: founder_quote
    attributes: { label: Founder Feedback (verbatim), description: "Exact founder words. Never paraphrase. Mark 'derived, not founder-stated' if inferred." }
    validations: { required: true }
  - type: textarea
    id: acceptance_criteria
    attributes: { label: Acceptance Criteria, description: "Concrete, observable from the running app (curl/Playwright/psql).", value: "- [ ] \n- [ ] " }
    validations: { required: true }
  - type: dropdown
    id: area
    attributes:
      label: Area
      multiple: true
      options: ["frontend (apps/web)", "backend (apps/api)", "worker (apps/worker)", "db (packages/db)", "shared (packages/shared, packages/ui)", "llm (OpenAI agent graphs)", "infra (.ai-roster, CI)"]
    validations: { required: true }
  - type: dropdown
    id: priority
    attributes: { label: Priority, options: [P0-blocker, P1-high, P2-normal, P3-low] }
    validations: { required: true }
  - type: checkboxes
    id: invest_check
    attributes:
      label: PM Atomicity Self-Check (INVEST)
      options:
        - { label: "Independent — no hard dependency on another open story to be testable" }
        - { label: "Negotiable — solution approach not over-specified (that's Architect's job)" }
        - { label: "Valuable — traces to a specific founder complaint" }
        - { label: "Estimable — an engineer could size it after reading the spec" }
        - { label: "Small — one engineer, one branch, one PR, ≤2 area labels" }
        - { label: "Testable — acceptance criteria objectively checkable from running app" }
```

### 3.3 Append-only role comment templates

Each role posts ONE comment with the exact heading so the thread is greppable via `gh issue view <n> --comments`:

```markdown
## 🔎 Research Notes — <date>, agent: researcher
**Findings:** …
**Relevant existing code:** `apps/api/src/…`, `packages/db/…`
**External constraints (rate limits/auth):** …
**Recommendation:** proceed as-scoped / simplify to: …
```
```markdown
## 🏗️ Architect Spec — <date>, agent: architect
**Files to create/modify:** `apps/api/src/routes/…` — … ; `packages/db/migrations/00NN_….sql` — …
**API contract:** `POST /v1/…` → `{ … }`
**DB changes:** column types, Drizzle diff, next-free migration number (verify at build time, CLAUDE.md §7)
**Data flow:** …
**CLAUDE.md rules that apply:** [ ] dark-mode semantic tokens [ ] safeParseJson [ ] migration→both DBs [ ] no mocking
**Out of scope:** …
```
```markdown
## ⚙️ Engineer Notes — <date>, agent: <engineer-id>, branch: issue-<n>, worktree: <path>, lane: <n>
**Implemented:** …
**Commits (atomic, specific filenames per CLAUDE.md §5):** <sha> "<msg>" …
**Deviations from spec & why:** …
**Live verification (paste real output, never assumed):** `curl …` / `psql …`
**PR:** #<pr-number>
```
```markdown
## 🧪 QA Results — <date>, agent: qa
**Verdict:** PASS / FAIL
**Tests run:** Vitest suite(s), Playwright scenarios (in lane:<n>)
**Screenshots:** light + dark mode paths
**Spec compliance:** AC-by-AC checklist
**If FAIL:** exact repro + expected vs actual
```
```markdown
## ✅ PM Verification — <date>, agent: pm
**Verdict:** PASS / FAIL
**AC walkthrough (from the RUNNING app, not the QA report):** quote each AC, met/unmet
**Founder-intent check:** does this resolve the founder_quote in spirit?
**If FAIL:** which AC failed + observed behaviour
```
```markdown
## 🛡️ Architect Verdict — <date>, agent: architect
**Verdict:** ACCEPT / REJECT
**Code review:** architecture fit, CLAUDE.md rule compliance, security, reuse/simplification
**If REJECT:** file:line findings the engineer must address
```

**Handoff discipline:** every label transition MUST be accompanied by its role comment. A bare label flip with no comment is an incomplete handoff — the next agent halts and flags "predecessor comment missing."

---

## 4. Roster Reconciliation

### 4.1 File-by-file: what's wrong → what it must become

| File | What's wrong | What it must become |
|---|---|---|
| `team.yaml` | `frontend_engineer.display_name: "Vue.js Engineer"`; all 3 Antigravity agents pinned to `gemini-3.5-flash` (unverified model id); references `frontend_instructions.md` **which does not exist on disk**; Claude models are Antigravity-style strings (`claude-opus-4-8-thinking`) with no Claude Code `tools:`/alias fields; no claim/lane/area config | Add `web_engineer`/`backend_engineer`/`worker_engineer` (real apps). Add per-agent `tools:` list + `claude_model_alias` (opus/sonnet/haiku) for Claude-Code agents. Add separate `architect_spec` and `architect_review` entries (same body, two invocations) + `pm_intake` and `pm_verify`. Verify Antigravity's real model menu before locking any `gemini-*` id. Create the missing `web_instructions.md`. |
| `pm_instructions.md` | "Vue.js PWA"; writes label `status: 1-needs-spec` (skips Research stage, wrong slug+space); no INVEST rubric; no founder-quote traceability | Rewrite to real stack + INVEST rubric (§4.2 of researcher). Two roles: **pm-intake** (creates `status:0-intake`→`1-needs-research`) and **pm-verify** (the verification gate). Stack-contract header. |
| `architect_instructions.md` | "Vue.js PWA"; "Vuex/Pinia state changes"; only knows the spec stage; lists `status: 1-needs-spec`/`2-ready-for-dev` (wrong slugs+space) | Rewrite to Drizzle/Fastify/BullMQ. **Two roles:** architect-spec (`2-needs-spec`→`3-ready-for-dev`) and architect-review (`9-pm-verified`→`11-done`/`10-arch-rejected`). Spec template §3.3. |
| `researcher_instructions.md` | Generic; no stack contract; no issue-comment workflow (says "output markdown summary") | Rewrite: reads `status:1-needs-research`, posts Research Notes comment, swaps to `2-needs-spec`. Real monorepo read access + WebSearch/WebFetch. |
| `engineer_instructions.md` (frontend) | "Senior Vue.js Engineer"; `<script setup>`; calls `start <issue_id>` with no claim/lane/worktree; `submit` does `git add .` | Rename to **web_instructions.md**: Next.js 15 App Router / React 19 / Tailwind / `packages/ui`; semantic dark-mode tokens (CLAUDE.md §6); claim+lane+worktree protocol; explicit-file commits. |
| `backend_instructions.md` | "supports the Vue.js PWA"; "Never modify frontend Vue components"; same start/submit gaps | Rewrite: Fastify :3001, Drizzle, migrations to BOTH `careeree`+`careeree_test`, `safeParseJson`, no-mocking. "Never modify `apps/web`." Real claim/lane/worktree. |
| (missing) `worker_instructions.md` | Does not exist; worker (`apps/worker`, BullMQ, entry `src/bootstrap.ts`) has no agent | **Create.** BullMQ jobs, entry is `bootstrap.ts` NOT `index.ts` (CLAUDE.md §10). |
| `qa_instructions.md` | Generic browser flow; `fetch` expects `3-in-review` but script hardcodes `2-ready-for-dev`; "instruct the review agent to re-label" (no agent does that) | Rewrite: claims its own lane, real stack, Playwright light+dark, restore dev user `tier=premium` after tests (CLAUDE.md §10), posts QA Results, self-labels `6-qa-failed`/`7-qa-passed`. |
| `scripts/sync-agents.js` | `require('js-yaml')` crashes (not installed/hoisted); emits `claude.json customCommands` (not a real Claude Code surface); maps Claude agents to slash commands (wrong — breaks "fresh context") | Rewrite per §6: emit `.claude/agents/<id>.md` (frontmatter + body) and `.agents/agents/<id>/agent.json`. |
| `skills/github_flow.sh` | `git add .` (CLAUDE.md violation); no claim; no worktree; no lane; `fetch` hardcodes one label; no `gh auth` check | Rewrite per §7: `fetch`/`claim`/`start`/`submit`(explicit files)/`qa-comment`/`cleanup`/`lanes`. |

### 4.2 Final agent list, environment, and model

**Resolution of PM/Architect "single vs split" ambiguity:** the founder loop has the Architect appearing **twice** (spec, then code-review) and the PM appearing **twice** (intake, then verification). Implement these as **four distinct agent definitions** (so each has a scoped prompt + correct label transitions + fresh context), even though architect-spec and architect-review share most of their body. This matches CLAUDE.md §4.1 "independent subagent, fresh context, no combining roles."

| Agent id | Stage | Environment | Why | Model (recommend) |
|---|---|---|---|---|
| `pm-intake` | feedback → atomic issues | **Claude Code** subagent | Long-context judgment, founder-traceability continuity | `opus` (intake judgment is genuinely complex) |
| `researcher` | research, attach findings | **Claude Code** subagent | WebSearch/WebFetch + whole-monorepo reads | `sonnet` (well-scoped reads) |
| `architect-spec` | technical blueprint | **Claude Code** subagent | Cross-cutting schema/route/job design | `opus` (CLAUDE.md §11 "complex/cross-cutting") |
| `web-engineer` | `apps/web` impl | **Antigravity** | Native worktree convention; cheap parallel tier | confirm Antigravity menu; CLAUDE.md §11 "moderate" tier (not the absolute cheapest) |
| `backend-engineer` | `apps/api` impl | **Antigravity** | same | same |
| `worker-engineer` | `apps/worker` impl | **Antigravity** | same | same |
| `qa` | test vs spec, Playwright | **Antigravity** | live browser automation skill | confirm Antigravity menu; "moderate-medium" tier |
| `pm-verify` | interactive vs founder intent | **Claude Code** (founder-adjacent) | Must exercise running app; founder-intent context lives here | `opus` |
| `architect-review` | accept/reject diff | **Claude Code** subagent | Final code-quality/CLAUDE.md gate | `opus` |

**Model caveats (both reports agree):** (1) `gemini-3.5-flash` is **unverified** against Antigravity's real model menu — confirm before locking into `team.yaml`, or generated `agent.json` silently fails at runtime. (2) The Claude `model:` frontmatter wants short aliases (`opus`/`sonnet`/`haiku`), **not** `claude-opus-4-8-thinking` — `team.yaml` needs a separate `claude_model_alias` field per Claude-Code agent. (3) Per CLAUDE.md §11, engineers/QA are "moderate, well-defined plan" → mid-tier, not necessarily the cheapest model available.

---

## 5. Worktree + Parallelism + Shared-Stack Plan

### 5.1 Worktree layout (two roots, by runtime — do NOT invent a third)

```
/Users/loukan/projects/loukan/careeree/                 # main checkout, ALWAYS on main, never built/run by agents
/Users/loukan/.agent-worktrees/tekram-delivery-assessment/issue-<n>/      # Claude-Code-runtime engineer worktrees
/Users/loukan/.gemini/antigravity/worktrees/careeree/issue-<n>/  # Antigravity-runtime worktrees (existing convention — adopt as-is)
/Users/loukan/.agent-worktrees/.lanes/lane-<n>.lock     # lane lockfiles
```

Both roots are outside the main working tree (`.gitignore` already excludes `.claude/`, `.agents/`, `.gemini/`). Since engineers run in **Antigravity**, their primary root is the existing `~/.gemini/antigravity/worktrees/careeree/`; the `.agent-worktrees` root is the Claude-Code equivalent for any Claude-side worktree work.

### 5.2 Branch naming

Pin explicitly — do not trust `gh issue develop`'s default slug:
```bash
git fetch origin
gh issue develop <n> --name "issue-<n>" --base main      # creates remote branch, does NOT check out here
git worktree add <worktree-root>/issue-<n> issue-<n>
corepack pnpm install --frozen-lockfile                  # symlink-only against shared store; tens of seconds
```
Branch = `issue-<n>` exactly. pnpm store is global (`~/Library/pnpm/store/v11`), so per-worktree install is cheap (symlinks, not 854MB).

### 5.3 Shared-stack contention — lanes (the real hard problem)

The non-mocked stack (Postgres :5432, Redis :6379, API :3001, Web :3000) is a **single shared resource**. N engineers running `pnpm dev` simultaneously port-collide and corrupt each other's DB/Redis state. Solution: a small fixed pool of **lanes**, each with its own ports + DB + Redis index, inside the single Postgres/Redis processes:

| Lane | Web | API | Postgres DB | Redis DB |
|---|---|---|---|---|
| 0 (main/CI) | 3000 | 3001 | `careeree` / `careeree_test` | 0 |
| 1 | 3010 | 3011 | `careeree_lane1` | 1 |
| 2 | 3020 | 3021 | `careeree_lane2` | 2 |
| 3 | 3030 | 3031 | `careeree_lane3` | 3 |

- **Postgres:** one process (today's reality); each lane = its own DB via `CREATE DATABASE careeree_laneN TEMPLATE careeree_test;`. Worktree `.env` `DATABASE_URL` points at its lane DB.
- **Redis:** one process; lane = logical DB index in `REDIS_URL` (`redis://localhost:6379/N`).
- **Ports:** `PORT`/`next -p` per lane.
- **Lane lock:** `start` picks first free lane (1..MAX_LANES), writes `lane-<n>.lock` (PID + issue#), applies `lane:<n>` label; **fails fast** ("all lanes busy") rather than silently sharing. Released on `cleanup`.

### 5.4 Claim protocol (combine researcher's `agent:claimed:<id>` + devops's lane lock)

```bash
# 1. List unclaimed ready work
gh issue list --label status:3-ready-for-dev --search '-label:"agent:claimed"' --json number,title --limit 5
# 2. Pick one; attempt claim (check-then-act guard)
gh issue view <n> --json labels -q '.labels[].name' | grep -q '^agent:claimed:' && exit 1
gh issue edit <n> --add-label "agent:claimed:eng-1" --add-label "lane:1" \
                  --add-label "status:4-in-progress" --remove-label "status:3-ready-for-dev"
gh issue comment <n> --body "Claimed by eng-1 at $(date -u +%FT%TZ). Worktree: …/issue-<n>, lane: 1"
# 3. Read-back tiebreak (GitHub has no label CAS): re-fetch; if a SECOND agent:claimed:* exists,
#    the alphabetically-later id backs off (removes its label + lane, picks next issue).
```

### 5.5 SERIALIZED vs MULTI-LANE — **recommendation: start serialized (one global lock), Phase 2 multi-lane**

The existing `docs/feedback-round-2` flow is mostly sequential and the founder is one person reviewing. A single global stack lock (`lane:0` only, one engineer holds the stack at a time) is **one lockfile vs. a DB-templating + port-offset system** — far less to build and debug. The state machine, claim labels, worktrees, and per-issue branches are all built the same way regardless; only the lane-allocation layer differs. **Start serialized; build the 3-lane scheme only if serialization proves to be the bottleneck.** (Both reports independently reached this conclusion.) Note: even serialized, multiple engineers can still hold *worktrees* and write *code* in parallel — only the **running stack** (QA/PM live tests) is serialized. That already captures most of the parallelism benefit.

### 5.6 Cleanup

`submit` leaves the worktree in place (QA/Architect still need to inspect it). A separate `cleanup <n>` (run by Architect on accept, or orchestrator post-merge) does `git worktree remove` + lane-lock release + `git worktree prune`. Stale-sweep policy: remove worktrees for issues closed > 7 days (founder decision on exact window — §10).

---

## 6. Sync Architecture

### 6.1 Source of truth → emit targets

```
SOURCE OF TRUTH:
  .ai-roster/team.yaml                 # id, environment, model, claude_model_alias, tools, skills
  .ai-roster/<role>_instructions.md    # system-prompt body

  node .ai-roster/scripts/sync-agents.js   ──►

CLAUDE CODE TARGET   (environment: claude-code):
  .claude/agents/<id>.md               # YAML frontmatter (name, description, tools, model) + body
                                       # NOT claude.json customCommands; NOT slash commands
                                       # → true subagents (Agent tool, subagent_type: <id>), fresh context

ANTIGRAVITY TARGET   (environment: antigravity):
  .agents/agents/<id>/agent.json       # { name, model, thinking, system_instruction, skills }
                                       # (verify exact key names against current Antigravity docs)
```

**Resolution of the sync tension:** the researcher under-stated the bug ("js-yaml is present"); the devops report was right. **Live-verified for this plan:** `js-yaml@4.2.0` exists in the pnpm store (`node_modules/.pnpm/js-yaml@4.2.0`) but is **NOT hoisted** to `node_modules/js-yaml` and **NOT** declared in `package.json` — so `require('js-yaml')` throws `Cannot find module`. **Fix:** `corepack pnpm add -D js-yaml -w` (declares it → hoists it → resolvable).

The original `sync-agents.js` is wrong on three counts and must be rewritten: (1) crashes on the missing require; (2) writes `claude.json customCommands`, which Claude Code never reads; (3) maps Claude agents to **slash commands** (run in the *current* conversation context) instead of **subagents** (fresh context — required by CLAUDE.md §4.1). The Antigravity branch is roughly correct and kept, pending schema verification.

### 6.2 Generated `.claude/agents/<id>.md` skeleton

```markdown
---
name: backend-engineer
description: Senior Backend Engineer (apps/api, Fastify, Drizzle, migrations). Use for issues labeled status:3-ready-for-dev, area:backend.
tools: Read, Edit, Write, Bash, Grep, Glob
model: sonnet
---

<body = contents of backend_instructions.md, which now opens with the stack-contract header
 and the claim/lane/worktree protocol>
```

### 6.3 Generated `.agents/agents/<id>/agent.json` skeleton

```json
{
  "name": "Backend API Engineer",
  "model": "<verified-antigravity-model-id>",
  "thinking": "low",
  "system_instruction": "<contents of backend_instructions.md>",
  "skills": ["github_flow"]
}
```

### 6.4 What `sync-agents.js` must be rewritten to do

1. `require('js-yaml')` resolves (after the devDep fix).
2. For `environment: claude-code`: emit `.claude/agents/<id>.md` with frontmatter built from `team.yaml` (`name`, `description` from `display_name`+area, `tools` list, `model` from `claude_model_alias`) + body = instructions file. `mkdir -p .claude/agents`.
3. For `environment: antigravity`: keep the existing `.agents/agents/<id>/agent.json` emit, using the **verified** model id.
4. Remove all `claude.json`/`customCommands` code.
5. Fail loudly if any `instructions_file` is missing (today it would silently throw on the missing `frontend_instructions.md`).

---

## 7. Corrected `github_flow.sh` Command Surface

Fixes the `git add .` violation, makes it worktree/lane/claim-aware, adds a `gh auth` guard.

```
github_flow.sh fetch [--label <status-label>]         # default status:3-ready-for-dev & -label:agent:claimed; accepts ANY label (QA passes status:5-in-review)
github_flow.sh claim  <issue_id>                       # NEW: check-then-act label flip + agent:claimed:<id> + assignee + comment fingerprint + read-back tiebreak (§5.4)
github_flow.sh start  <issue_id> [lane_n]              # gh issue develop --name issue-<n> (NO --checkout); git worktree add; acquire lane lock + write lane-scoped .env; pnpm install --frozen-lockfile
github_flow.sh submit <issue_id> "<message>" <file1> [file2 ...]   # explicit file list — git add <files>; commit; push; gh pr create (Refs #N during loop); label → status:5-in-review.  NEVER git add . / -A
github_flow.sh qa-comment <pr_number> "<message>"      # unchanged
github_flow.sh cleanup <issue_id>                      # NEW: git worktree remove + lane release + git worktree prune
github_flow.sh lanes                                   # NEW: print free/held lanes + holder
```

Guards to add at top of script:
```bash
command -v gh >/dev/null || { echo "Error: gh not installed"; exit 1; }
gh auth status &>/dev/null || { echo "Error: gh not authenticated — run gh auth login"; exit 1; }
```
`submit` core (no glob): agent stages explicit files; script does `git diff --cached --quiet && { echo "nothing staged"; exit 1; }; git commit -m "$MESSAGE"`.
Use `Closes #N` only on final merge (`status:11-done`); use `Refs #N` during the loop to avoid premature auto-close.

---

## 8. Bootstrap Checklist (one-time founder setup — do NOT run during planning)

```bash
# 1. Homebrew + tools (core tap is 4+ yrs stale — update FIRST or gh installs ancient version)
brew update
brew install gh jq
gh auth login --git-protocol https --hostname github.com   # or export GH_TOKEN=<fine-grained PAT: repo,issues,pull_requests>
gh auth status

# 2. git hygiene (stale Homebrew git 2.23.0 shadows Apple git 2.39.2 on PATH)
which -a git && brew upgrade git && git --version          # confirm >= 2.39

# 3. js-yaml as a declared devDep (currently only in pnpm store, not resolvable)
cd /Users/loukan/projects/loukan/careeree && corepack pnpm add -D js-yaml -w
node -e "require('js-yaml')"                                # must not throw

# 4. worktree + lane scaffolding (dirs only)
mkdir -p /Users/loukan/.agent-worktrees/tekram-delivery-assessment /Users/loukan/.agent-worktrees/.lanes

# 5. GitHub labels (full canonical set, colon-no-space)
for s in 0-intake 1-needs-research 2-needs-spec 3-ready-for-dev 4-in-progress 5-in-review \
         6-qa-failed 7-qa-passed 8-pm-rejected 9-pm-verified 10-arch-rejected 11-done; do
  gh label create "status:$s" --repo lelkadi/tekram-delivery-assessment --force
done
for a in frontend backend worker db shared llm infra; do gh label create "area:$a" --repo lelkadi/tekram-delivery-assessment --force; done
for p in P0-blocker P1-high P2-normal P3-low; do gh label create "priority:$p" --repo lelkadi/tekram-delivery-assessment --force; done
gh label create "founder-priority" --repo lelkadi/tekram-delivery-assessment --force
gh label create "blocked" --repo lelkadi/tekram-delivery-assessment --force

# 6. Issue template
mkdir -p .github/ISSUE_TEMPLATE   # then add feedback-story.yml (§3.2)

# 7. Branch protection on main (requires admin PAT; finalize once CI workflow exists)
gh api repos/lelkadi/tekram-delivery-assessment/branches/main/protection -X PUT ...   # require PR + status checks

# 8. OPTIONAL: .github/workflows/state-machine.yml — validate label transitions + run pnpm -r test
#    on a CI Postgres/Redis service container (isolated from the local lane stack)
```

---

## 9. Phased Implementation Roadmap

Each phase is independently verifiable before the next begins (mirrors the wave-gate discipline).

| Phase | Deliverable | Independently verifiable by |
|---|---|---|
| **P0 — Bootstrap** | §8 steps 1-3 (gh, js-yaml, git). No workflow logic yet. | `gh auth status` OK; `node -e "require('js-yaml')"` no throw; `git --version >= 2.39` |
| **P1 — Roster content fix** | Rewrite all `*_instructions.md` to the real stack with stack-contract headers; create `web_instructions.md` + `worker_instructions.md`; split PM/Architect into 4 roles; fix `team.yaml` (real names, `claude_model_alias`, `tools`, verified Antigravity model) | Read each file; confirm zero "Vue"/"Pinia"/"gemini-3.5-flash"(unverified) strings; every `instructions_file` exists on disk |
| **P2 — Sync rewrite** | Rewrite `sync-agents.js` → `.claude/agents/<id>.md` + `.agents/agents/<id>/agent.json`; remove `claude.json` code | `node sync-agents.js` runs clean; inspect a generated `.claude/agents/backend-engineer.md` has valid frontmatter; Antigravity `agent.json` valid JSON |
| **P3 — GitHub scaffolding** | §8 steps 5-6: all labels created, issue Form live | `gh label list` shows full set; open a test issue via the Form |
| **P4 — Skill rewrite** | Rewrite `github_flow.sh`: claim/start/submit(explicit-files)/cleanup/lanes + auth guard + worktree; **serialized single-lane** first | Run `fetch`/`claim`/`start` against a throwaway test issue end-to-end on the real stack; confirm a worktree appears + branch `issue-<n>` + no `git add .` |
| **P5 — End-to-end dry run** | Drive ONE real founder-feedback item through all 7 stages + one deliberate reject loop, on the live stack | Issue closes at `11-done`; every stage left its template comment; reject incremented `strike:n`; worktree cleaned up |
| **P6 — (optional) Parallelism + CI** | 3-lane DB/port/Redis scheme; `.github/workflows/state-machine.yml`; branch protection | Two engineers hold lanes 1+2 simultaneously without DB/port collision; CI blocks a bad label transition |

**Recommended stop-and-review point:** founder reviews after **P5** (one full issue lifecycle proven) before investing in P6 parallelism.

---

## 10. Risks & Open Founder Decisions (consolidated)

**Open decisions (need founder sign-off before/during build):**

1. **PM-verification autonomy — flagged loudest.** The brief says PM "performs an interactive testing/verification round," which reads as **founder-involved/human-triggered**, unlike the other 6 autonomous stages. **My recommendation: make `pm-verify` a human-triggered, synchronous gate the founder (or a founder-supervised Claude Code session) runs** — it is the cheapest place to keep a human in the loop on "did we actually solve what I meant," and it preserves the founder-intent continuity. The other 6 stages run autonomously. **Founder must confirm** this posture rather than letting `pm-verify` default to "just another autonomous agent."
2. **Serialized vs multi-lane start.** Recommend **serialized (single global stack lock)** for v1 (P0-P5), multi-lane only in P6 if it's a bottleneck. Founder confirms appetite for the extra DB/port machinery.
3. **Circuit-breaker strike count.** Recommend **3**. Founder may pick 2 (stricter) or 5.
4. **Antigravity model id.** `gemini-3.5-flash` is unverified — founder/DevOps must confirm against Antigravity's live model menu before P1 locks it into `team.yaml`. Wrong id → silent runtime failure.
5. **Worktree stale-sweep window.** Recommend remove worktrees for issues closed > 7 days. Founder sets the number.
6. **gh auth model.** Recommend a single fine-grained PAT as `GH_TOKEN` (scopes: repo/issues/pull_requests) with a bot-like git identity per worktree so `git log` distinguishes agent commits. Founder confirms PAT vs interactive `gh auth login`.

**Risks (mitigated, no decision needed):**

7. **Claim race is best-effort, not atomic** — GitHub has no label compare-and-swap. Mitigated by check-then-act + read-back tiebreak (§5.4); acceptable at "a few" parallel engineers. Revisit with a Redis-backed lock (Redis is already in-stack) if the founder wants 10+ parallel engineers.
8. **Context-bloat on long reject loops** — on re-claim, the resuming engineer reads only the **latest** comment of each role-type, not the full thread; templates stay terse and link/result-based.
9. **Rate limits** — 5,000 req/hr/token; mitigated by backoff polling (not tight loops), `--limit` caps on all `list` calls, and preferring MCP where it can batch.
10. **Antigravity ↔ Claude Code interop** — the labels + state machine are the ONLY shared contract; neither tool assumes the other's session format. `sync-agents.js` emits both runtimes' native formats from one source of truth.
11. **Git/Homebrew staleness** (git 2.23.0 shadowing 2.39.2, core tap 4yrs old) — handled in P0 bootstrap; not a design risk once fixed.

---

## 11. Founder Decisions — LOCKED (2026-06-29)

| # | Decision | Locked answer |
|---|---|---|
| 1 | PM-verification autonomy | **Human-triggered gate** (`pm-verify` run by founder / founder-supervised Claude Code session). Other 6 stages autonomous. |
| 2 | Serialized vs multi-lane | **Serialized first** (single global stack lock for live testing; code still written in parallel across worktrees). Multi-lane deferred to P6, build only if bottlenecked. |
| 3 | GitHub auth | **Fine-grained PAT as `GH_TOKEN`** (scopes: repo/issues/pull_requests) + bot-like git identity per worktree so agent commits are distinguishable in `git log`. |
| 4 | Circuit-breaker strike count | **3 strikes** (total across QA+PM+Architect gates). |
| 5 | Worktree stale-sweep window | **7 days** (default; revisit if noisy). |
| 6 | Antigravity model id | **OPEN** — `gemini-3.5-flash` unverified. `team.yaml` carries a `TODO-VERIFY` placeholder; founder confirms against Antigravity's live model menu before P2 sync emits Antigravity `agent.json`s. |

*End of unified plan. Decisions locked → implementing P1–P4 (file authoring); P0 machine steps handed to founder as a runbook.*
