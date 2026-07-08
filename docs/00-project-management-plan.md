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

**Rule of thumb:** never let a P1–P3 task block a P0 task. The coding challenge gets the only true build/QA loop; everything else is a written deliverable with a review gate.

## 2. Agent Team & Assignment

Roster lives in [.ai-roster/team.yaml](../.ai-roster/team.yaml); GitHub issues + `status:*` labels are the queue (state machine `0-intake → 11-done` via `.ai-roster/skills/github_flow.sh`).

| Role | Runtime / model | Assessment responsibility |
|---|---|---|
| PM — Intake | claude-code / opus | Seed all issues from this plan, keep priorities honest, cut scope when behind. |
| Researcher | claude-code / sonnet | Reference gathering for Architecture, DevOps, Debugging, AI parts; first drafts of written parts. |
| Architect — Spec | claude-code / opus | Part 1 architecture + diagrams, Part 3 schema, coding-challenge spec (endpoints, layers, DTOs). |
| Backend Engineer | opencode / deepseek | Part 2 build: JWT auth, restaurants list/search/pagination, order creation (stock, delivery fee, coupons), unit tests. |
| QA | claude-code / sonnet | Part 2 only: run the API for real, verify every endpoint + edge cases (invalid coupon, out-of-stock, bad JWT), check tests pass. |
| PM — Verification | human-triggered (Loukan) | Your gate: score each part against the rubric before it's marked done. |
| Architect — Review | claude-code / opus | Code review of Part 2; consistency review of Parts 1/3/5 (architecture ↔ schema ↔ DevOps must tell one story). |

**Docs-heavy adaptation:** web-engineer and worker-engineer from the roster are **not used** (no frontend/worker in scope). For document-only issues the full 12-stage pipeline is collapsed to: `intake → draft (researcher/architect) → review (architect or PM) → done`. QA stage applies only to the coding challenge.

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

## 5. Issue Queue Conventions

- One **epic issue per part** (`part-1` … `part-9` labels), sub-issues for Part 2 slices.
- Labels: existing `status:*` set + `priority:P0|P1|P2|P3`.
- Every issue body states: deliverable file path, rubric points, acceptance criteria, and the collapsed or full pipeline it follows.
- Agents claim work via `github_flow.sh` exactly as in the roster; serialized lane (v1) is fine — only Part 2 needs the live stack.

## 6. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Part 2 overruns (biggest time sink) | Vertical slices committed independently; bonus items (DI polish, repo pattern extras) cut before core features or tests. |
| Docs written by different agents contradict each other | Architect consistency review at H40 across Parts 1/3/5/8. |
| Roster friction burns setup time | Timebox bootstrap to 2h; if `sync-agents.js`/labels misbehave, fall back to manual issue creation and plain subagents. |
| Deadline | H45 hard content freeze; buffer is polish only. |
