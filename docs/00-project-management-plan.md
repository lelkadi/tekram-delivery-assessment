# Tekram Technical Lead Assessment — Project Management Plan

> **Deadline:** 2026-07-10 (48 hours from kickoff). **Passing score:** 75/100.
> **Hard gates:** Architecture ≥ 15/20, Coding ≥ 18/25, Leadership ≥ 10/15 — miss any one and the total doesn't matter.
> **Auto-fail concerns:** weak architecture rationale, poor documentation, no testing, ignoring scalability/security, weak CI/CD knowledge, poor business thinking, missing the deadline.

## 1. Priority Model

Priorities are driven by (a) hard minimum thresholds, (b) point weight, (c) the "automatic concern" list.

| Priority | Part | Points | Threshold | Why this priority |
|---|---|---|---|---|
| **P0** | Part 2 — Coding Challenge | 25 | 18/25 | Highest weight + hard gate + only part needing real build/test cycles. Unit tests are effectively mandatory ("lack of testing" = auto concern). |
| **P0** | Part 1 — Architecture | 20 | 15/20 | Hard gate; "weak rationale" = auto concern. Diagrams required. |
| **P0** | Part 6 — Leadership | 15 | 10/15 | Hard gate. Pure writing — cheap to do well, fatal to skip. |
| **P1** | Part 3 — Database Design | 10 | — | Feeds directly into P0 coding schema; do the design once, use it twice. |
| **P1** | Part 5 — DevOps | 10 | — | "Weak CI/CD knowledge" = auto concern; also overlaps Part 1 content. |
| **P1** | Part 4 — Debugging | 10 | — | Self-contained written scenario. |
| **P2** | Part 8 — Documentation | 5 | — | Low points but "poor documentation" = auto concern → README/API docs/deploy guide must be genuinely good, produced alongside P0 work, polished at the end. |
| **P2** | Part 7 — AI Strategy | 5 | — | Short proposal document. |
| **P3** | Part 9 — Business Thinking | Bonus | — | "Poor business thinking" is an auto concern, so do a credible version — but only after P0–P2 are locked. |
| **P4 (lowest)** | Frontend demo | Not in rubric | — | Not requested by the brief — Part 2 asks for a **backend** only. Zero rubric points, so it never displaces gated work. Kept as a nice-to-have differentiator: thin UI (login/register, browse restaurants, place an order) hitting the real API. Starts only after every P0–P3 deliverable exists; first thing dropped if time is short. |

**Rule of thumb:** never let a P1–P4 task block a P0 task. The coding challenge gets the only true build/QA loop; everything else is a written deliverable with a review gate.

## 2. Agent Team & Assignment

Roster lives in [.ai-roster/team.yaml](../.ai-roster/team.yaml); GitHub issues + `status:*` labels are the queue (state machine `0-intake → 11-done` via `.ai-roster/skills/github_flow.sh`).

| Role | Runtime / model | Assessment responsibility |
|---|---|---|
| PM — Orchestrator | claude-code / opus | Owns priorities and the H24/H40-45/H45 gates; cuts scope deliberately, never silently. Doesn't dispatch or code. |
| PM — Doc Intake | claude-code / opus | Seeds epics + typed work issues from this plan and the design docs, keeps priorities honest. |
| Researcher | claude-code / sonnet | Reference gathering for Architecture, DevOps, Debugging, AI parts; first drafts of written parts. |
| Architect — Spec | claude-code / opus | Part 1 architecture + diagrams, Part 3 schema, coding-challenge spec (endpoints, layers, DTOs). |
| Engineering Lead | opencode / deepseek-v4-pro | Orchestrates every code issue: fetches, briefs the engineer with a self-contained task, verifies the local commit independently, publishes (push+PR+label), hands off to QA/Architect-review. Never implements — see [.ai-roster/eng_lead_instructions.md](../.ai-roster/eng_lead_instructions.md). |
| Backend Engineer | opencode / deepseek-v4-flash | Implements the engineering lead's brief only: JWT auth, restaurants list/search/pagination, order creation (stock, delivery fee, coupons), unit tests. Commits locally; never touches GitHub. |
| QA | claude-code / sonnet | Part 2 only: run the API for real, verify every endpoint + edge cases (invalid coupon, out-of-stock, bad JWT), check tests pass. Deliberately a different model family than the engineer, to avoid correlated blind spots. |
| PM — Verification | human-triggered (Loukan) | Your gate: score each part against [.ai-roster/rubric-checklist.md](../.ai-roster/rubric-checklist.md) before it's marked done. |
| Architect — Review | claude-code / opus | Code review of Part 2; consistency review of Parts 1/3/5 (architecture ↔ schema ↔ DevOps must tell one story). Only role that merges or closes a code issue. |
| Web Engineer (conditional, P4) | opencode / deepseek-v4-flash | Only spun up after P0–P3 are done: thin frontend demo against the Part 2 API, briefed by the engineering lead exactly like the backend engineer. `write_scope` is this repo's `web/**` (careeree's `apps/web/**` glob doesn't apply here); backend engineer's scope is `src/**` + `tests/**` so the two can't collide if both are active. |

**Delegation model:** engineers never touch GitHub — the engineering lead owns every `github_flow.sh`
call on a code issue except the engineer's own local `git commit`. Cross-runtime handoffs
(opencode eng-lead ↔ claude-code QA/architect-review) are CLI shell-outs; there's no native
call between the two products. Full contract: [.ai-roster/rules/delegation.md](../.ai-roster/rules/delegation.md).

**Docs-heavy adaptation:** careeree's worker-engineer was **removed from the roster** (no background-job scope in this assessment). For document-only issues the full 12-stage pipeline is collapsed to: `intake → draft (researcher/architect) → review (architect or PM) → done`. QA stage applies only to the coding challenge (and, if reached, the frontend demo). Full workflow documentation: [.ai-roster/README.md](../.ai-roster/README.md).

## 3. Timeline (48h)

### Day 1 — build the gated parts

| Window | Work | Owner |
|---|---|---|
| H0–H2 | Bootstrap: push repo, run `bootstrap-labels.sh`, sync agents, PM seeds all 9 epics + sub-issues with priority labels. | PM-Intake |
| H2–H6 | **Part 3 schema + Part 2 spec** (one data model reused for both), Part 1 architecture outline. | Architect |
| H6–H18 | **Part 2 build** in vertical slices: (1) scaffold + JWT auth, (2) restaurants/search/pagination, (3) orders + stock + fees + coupons, (4) unit tests + Clean Architecture polish. Commit per slice. | Backend Engineer |
| H6–H14 (parallel) | **Part 1 architecture doc + diagrams** (Mermaid/C4: context, containers, data flow), covering all listed concerns incl. future verticals. | Architect + Researcher |
| H14–H18 (parallel) | **Part 6 Leadership doc** draft (team of 9: sprints, reviews, mentoring, KPIs, hiring, conflict, silos). | Researcher → PM review |
| H18–H24 | **QA on Part 2** (live API verification + test run), fix loop; Architect reviews Part 1 draft. | QA, Backend, Architect |

**End-of-Day-1 gate (your PM-verify):** Part 2 API works end-to-end with tests; Part 1 and Part 6 drafts exist. If Part 2 is behind, cut Clean Architecture bonus polish — never cut tests.

### Day 2 — written parts, review wave, polish

| Window | Work | Owner |
|---|---|---|
| H24–H30 | **Part 4 Debugging** runbook (investigation → root causes → immediate/permanent fixes → monitoring) and **Part 5 DevOps** (CI/CD, Azure, K8s, Redis, secrets, rollback) — must reuse/match Part 1 decisions. | Researcher drafts, Architect reviews |
| H30–H34 | **Part 7 AI Strategy** + **Part 9 Business Thinking** (24h actions + 30-day recovery plan). | Researcher → PM review |
| H34–H40 | **Part 8 Documentation pass:** README, setup guide, API docs (OpenAPI/Swagger), deployment guide, env vars, folder structure, technical-decisions doc, future improvements. | Backend + Researcher |
| H40–H45 | **Full review wave:** Architect review (code + cross-doc consistency) and PM audit scoring every part against the rubric. Fix highest-scoring-impact gaps first. | Architect, PM-Verify (you) |
| H40–H45 (only if ahead of schedule) | **Frontend demo (P4, bonus)** — thin UI over the Part 2 API. Cut without hesitation if the review wave needs the time instead. | Web Engineer |
| H45–H48 | Buffer. Final repo hygiene, links check, submission. **Ship at H48 regardless** — missing the deadline is an auto-fail. | You |

## 4. Deliverables Checklist

- [ ] GitHub repository (public/shared at submission), clean history
- [ ] `docs/architecture.md` + diagrams (Part 1)
- [ ] Working backend: auth, restaurants, orders + unit tests (Part 2)
- [ ] `docs/database-schema.md` + ERD (Part 3)
- [ ] `docs/incident-runbook.md` (Part 4)
- [ ] `docs/devops.md` (Part 5)
- [ ] `docs/leadership.md` (Part 6)
- [ ] `docs/ai-strategy.md` (Part 7)
- [ ] `README.md`, setup guide, API docs, deployment guide, `docs/technical-decisions.md` (Part 8)
- [ ] `docs/business-recovery-plan.md` (Part 9, bonus)
- [ ] Frontend demo (P4, bonus — not in rubric, only if time remains)

## 5. Issue Queue Conventions

- One **epic issue per part** (`epic` + `part-1` … `part-9` labels), sub-issues for Part 2 slices linked from a task-list checklist in the epic body (each line `- [ ] #<n>` — GitHub renders progress automatically). Epics are tracking-only: agents never claim an issue labeled `epic`; they claim its sub-issues. Single-issue parts (most doc parts) get one issue with the `part-N` label and no `epic` label.
- Labels: existing `status:*` set + `priority:P0|P1|P2|P3|P4` + `type:doc|type:code` (drives pipeline choice and lane usage — see §6).
- Every issue body states: deliverable file path, rubric points, acceptance criteria, and the collapsed or full pipeline it follows.
- Agents claim work via `github_flow.sh` exactly as in the roster, now with `MAX_LANES=3` parallel lanes (§6).

## 6. Parallel Execution Model

The whole team runs concurrently; isolation happens at three layers, each already in the roster:

| Layer | Mechanism | Where |
|---|---|---|
| Issue ownership | `agent:claimed:<id>` labels with read-back tiebreak | `github_flow.sh claim` |
| Files | Per-agent persistent git worktrees (`~/.agent-worktrees/tekram-delivery-assessment/<agent-id>`); QA checks out engineers' pushed branches as read-only `qa-issue-<n>` aliases in its own worktree | `github_flow.sh start` / `qa-checkout` |
| Runtime | **Lanes** (`MAX_LANES=3`): lane N gets its own API port (`30N1`), database (`tekram_laneN`), and Redis db (`N`) | `github_flow.sh` lane helpers |

**Pipeline parallelism:** stages pipeline across issues — researcher drafts issue B while the architect specs issue A, the backend engineer builds slice 3 while QA tests slice 2 (engineer's `submit` releases its lane immediately; QA acquires its own). Doc and code issues run fully in parallel: `type:doc` issues never acquire a lane (they don't run the stack), so three doc agents can't starve QA.

**Shared infrastructure (decided):** one `docker compose` stack on the host runs Postgres + Redis for everyone; lanes isolate via per-lane database names and Redis db numbers, **not** per-agent containers. Docker-per-agent is explicitly rejected for v1 (image builds + credential plumbing with no isolation gain over worktrees). Escalation path if lanes ever interfere: compose-project-per-lane (`docker compose -p laneN`), not needed day one.

**Merge policy for parallel code work:** spec lands scaffold + shared types first; subsequent slices own disjoint directories (`src/auth/`, `src/restaurants/`, `src/orders/`); branches are short-lived, rebased on `main` before `submit`, merged promptly after review. Backend (`src/**`, `tests/**`) and web (`web/**`) write scopes are disjoint by construction.

**Repo/service shape (working decision, architect ratifies in Part 1):** modular monolith in this single repo — one deployable API, module boundaries on the directory level as above. No microservices split and no per-module deploy pipelines for the challenge itself; the Part 1 architecture doc describes the extraction path (which modules become services first, and at what scale trigger). Rationale: at 15k orders/day a modular monolith is the defensible engineering answer, and it shows stronger judgment than premature microservices.

## 7. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Part 2 overruns (biggest time sink) | Vertical slices committed independently; bonus items (DI polish, repo pattern extras) cut before core features or tests. |
| Docs written by different agents contradict each other | Architect consistency review at H40 across Parts 1/3/5/8. |
| Roster friction burns setup time | Timebox bootstrap to 2h; if `sync-agents.js`/labels misbehave, fall back to manual issue creation and plain subagents. |
| Deadline | H45 hard content freeze; buffer is polish only. |
