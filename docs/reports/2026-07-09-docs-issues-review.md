# Docs & Issue-Queue Review — 2026-07-09 (~H22 of 48)

> **Scope:** every doc under `docs/`, the `.ai-roster` workflow definition, and the full GitHub
> issue/PR queue (`lelkadi/tekram-delivery-assessment`), cross-referenced against the assessment
> brief and the project plan's timeline/checklist.
> **Purpose:** identify gaps and propose new issues for `pm-doc-intake` to create and process.
> **Deadline context:** submission 2026-07-10; the plan's Day-1→Day-2 boundary (H24) is hours away.

## TL;DR

The Part 2 (coding) pipeline is healthy and far along — 15 sub-slice PRs are open at
`status:5-in-review`. **Everything else is missing from the queue.** The plan (§5) calls for one
epic per part, seeded at bootstrap; only the Part 2 epic exists. Five gradable written
deliverables (Parts 4, 5, 6, 7, 9) and the Part 8 documentation pass have **no issue and no
draft**, including **Part 6 Leadership — a hard gate (min 10/15)** whose timeline slot (H14–H18)
has already passed. Meanwhile all 15 PRs are queued behind a QA runtime (antigravity/Gemini,
TD-006) whose mandatory "loud-failure spike" has no tracking issue and hasn't run.

**Recommended immediate actions:** seed the five doc-part issues (Part 6 first), create and run
the TD-006 QA spike issue, and decide the merge order for the stacked PR train.

---

## 1. State snapshot

### Docs inventory vs deliverables checklist

| Part | Deliverable | Status |
|---|---|---|
| 1 — Architecture (20, gate 15) | `docs/architecture.md` | ✅ exists, 7 Mermaid diagrams — **unreviewed** (never went through a doc-pipeline issue) |
| 2 — Coding (25, gate 18) | `src/**` + `tests/**` | 🟡 15 PRs open, all `5-in-review`, blocked on QA |
| 3 — Database (10) | `docs/database-schema.md` | ✅ exists, ERD + DDL — **unreviewed** |
| 4 — Debugging (10) | `docs/incident-runbook.md` | ❌ missing, no issue |
| 5 — DevOps (10) | `docs/devops.md` | ❌ missing, no issue (only `architecture.md` §12 summary) |
| 6 — Leadership (15, **gate 10**) | `docs/leadership.md` | ❌ **missing, no issue — hard gate** |
| 7 — AI Strategy (5) | `docs/ai-strategy.md` | ❌ missing, no issue |
| 8 — Documentation (5) | README, setup guide, API docs, deploy guide | 🟡 `technical-decisions.md` exists; README is a 20-line stub; no setup/deploy guide; no issue |
| 9 — Business (bonus) | `docs/business-recovery-plan.md` | ❌ missing, no issue (`01-business-strategy.md` is *pre-work*, it does not answer the Part 9 outage-recovery prompt) |

Supporting docs (not graded directly): `00-project-management-plan.md`,
`01-business-strategy.md`, `02-prd.md`, `technical-design-blueprint.md` — all present and
substantial.

### Issue queue inventory

- **#1** — Epic: Part 2 (tracking `- [ ] #2–#5`, all boxes unchecked).
- **#2–#5** — the four original slices, still labeled `status:3-ready-for-dev` although their
  work was decomposed into and completed via sub-issues.
- **#7–#21** — 15 sub-slice issues, all `status:5-in-review` with matching open PRs #22–#36.
- **No issues exist for Parts 1, 3, 4, 5, 6, 7, 8, 9** — despite plan §5 ("one epic issue per
  part") and the H0–H2 bootstrap step ("PM seeds all 9 epics + sub-issues").

### PR/branch topology (matters for merge planning)

All 15 PRs target `main`, but the branches form a **single linear stack**: `issue-9 ⊇ issue-8`,
`issue-10 ⊇ issue-9`, … `issue-21 ⊇ issue-20` (only `issue-8` doesn't literally contain
`issue-7`'s commit). `issue-21` carries all 19 commits — it *is* the integrated build. Each PR's
GitHub diff against `main` therefore shows cumulative work, not just its own slice. This
contradicts the plan §6 merge policy (short-lived branches independently rebased on `main`) and
means naive per-PR review/QA duplicates effort ~15×.

---

## 2. Findings

**F1 — The doc pipeline was never seeded (highest-impact gap).** 5 of 9 gradable parts plus the
Part 8 pass have no issue, no owner, no draft. Part 6 is a hard gate: scoring 9/15 there fails
the assessment even with a perfect total. Per the timeline, Parts 4/5 are due in the
H24–H30 window that starts now, Parts 7/9 at H30–H34, Part 8 at H34–H40. The End-of-Day-1 gate
("Part 1 and Part 6 drafts exist") is currently **failed** on the Part 6 half.

**F2 — QA gate is unverified while 15 PRs wait on it.** TD-006 explicitly requires a
loud-failure spike ("give it a deliberately wrong id… confirm it errors visibly") *before
trusting QA verdicts*, and until then QA verdicts are advisory with architect-review as the real
gate. There is no issue tracking the spike, and nothing has moved past `5-in-review`. This is
the critical path for the highest-weighted part.

**F3 — Stacked PR train has no integration plan.** With the linear stack described above, the
efficient path is: QA the stack head (`issue-21` / PR #36 = the full build) against the full
acceptance criteria + the blueprint §10 verification checklist, then merge bottom-up (PR #22 →
#36) or merge the head and close the rest as integrated. Nobody owns this decision yet, and 15
independent QA runs × compose/migrate/test on 3 lanes doesn't fit the remaining budget.

**F4 — Queue state is internally inconsistent.** Parent slices #2–#5 still sit at
`status:3-ready-for-dev`, which makes them **claimable** — an agent doing `fetch` could claim #2
and redo work that's already in PR. Epic #1's task list tracks only #2–#5, so the epic shows 0%
progress while the real work is 15 PRs deep. GitHub auto-checks epic boxes only when the listed
sub-issues close, and #2–#5 will never close through normal flow because their sub-issues (#7–#21)
are what the pipeline is actually driving.

**F5 — Parts 1 and 3 are checked off but never reviewed.** The plan's checklist marks
`architecture.md` and `database-schema.md` done, yet they never passed through any pipeline
stage; per roster rules, only `architect-review` closes deliverables, and its cross-doc
consistency review (H40–H45) has no issue. Concrete inconsistencies already visible for that
review to catch:

- **Broken provenance link:** [technical-decisions.md:99](../technical-decisions.md) (TD-004)
  links to `../ORIGINAL_REQUEST.md`, which is **gitignored and absent from the working tree** —
  it 404s for any assessor browsing the repo, in the very entry that justifies the stack choice.
  ("Poor documentation" is an auto-fail concern.)
- **Module-path drift:** PRD §3, TD-001/TD-004, and `architecture.md` §3/§13 all state module
  paths as `src/auth/`, `src/restaurants/`, `src/orders/`; the actual build is
  `src/Tekram.Api/src/<module>/` (solution at root, one API project). Consistent with the
  `src/**` write-scope glob, but the documented deliverable paths are literally wrong — either
  ratify the real layout (a TD-007 entry) or fix the doc references.
- **Duration framing:** `README.md` says "72-hour assessment" (matching the brief) while the
  plan self-imposes 48h and the deadline of 2026-07-10. Harmless internally, but pick one story
  for the assessor.

**F6 — Nothing protects the endgame.** No issue exists for the H45–H48 submission pass (repo
shareability, link check, epic closure, label cleanup, final rubric self-score) even though
"missing the deadline" is an auto-fail and the deliverables checklist's first line — "GitHub
repository (public/shared at submission), clean history" — is unowned.

**F7 — "Weak CI/CD knowledge" is defended only by prose.** Part 5 will *describe* CI/CD, but the
repo has no workflow file. A minimal GitHub Actions `dotnet build + test` pipeline is cheap,
makes the DevOps doc demonstrably real, and strengthens Parts 2/5/8 simultaneously. Optional but
high leverage-per-hour.

---

## 3. Proposed new issues

Ordered by priority; bodies follow the plan §5 conventions (deliverable path, rubric points,
acceptance criteria, pipeline). Suggested creation: `pm-doc-intake`, today.

### NI-1 · Spike: verify antigravity QA runtime fails loudly (TD-006 prerequisite) — `priority:P0-blocker` · `part-2` · `type:code`

- **Deliverable:** spike result comment + go/no-go decision recorded in `docs/technical-decisions.md` (TD-006 status update).
- **Why now:** all 15 Part 2 PRs are blocked at `5-in-review` pending QA verdicts that TD-006 says must not be trusted until this spike passes.
- **ACs:**
  - [ ] Antigravity agent given a deliberately wrong model id on a throwaway task **errors visibly** (no silent no-op).
  - [ ] On a real issue, QA produces genuine execution evidence (curl output / `dotnet test` results), not an empty verdict.
  - [ ] TD-006 updated: spike passed → QA verdicts binding; spike failed → QA flipped to the documented claude-sonnet fallback and `team.yaml` updated.
- **Pipeline:** process spike — close directly after the decision comment; no lane beyond the spike run itself.

### NI-2 · Part 6 — Leadership document — `priority:P0-blocker` · `part-6` · `type:doc`

- **Deliverable:** `docs/leadership.md`. **Rubric:** 15 pts, **hard gate 10/15**.
- **Why now:** its H14–H18 timeline slot has passed; the End-of-Day-1 gate requires a draft. Pure writing — cheap to do well, fatal to skip.
- **ACs:**
  - [ ] Covers every brief-named topic for the team of 2 seniors / 3 mids / 2 juniors / QA / DevOps: sprint planning, code reviews, documentation culture, mentoring, KPIs, hiring, conflict resolution, preventing knowledge silos.
  - [ ] Concrete mechanisms (cadences, review SLAs, KPI definitions), not platitudes.
  - [ ] Consistent with the engineering practices this repo itself demonstrates (issue queue, review gates, atomic commits) — that's a differentiator worth one explicit section.
- **Pipeline:** collapsed doc pipeline (`3-ready-for-dev → 4-in-progress → 5-in-review → 11-done`).

### NI-3 · Part 2 — Integration & merge-order plan for the stacked PR train — `priority:P0-blocker` · `part-2` · `type:code`

- **Deliverable:** decision comment on epic #1 + executed merges.
- **ACs:**
  - [ ] QA strategy decided: full-suite QA on stack head PR #36 (`issue-21`, contains all 19 commits) + blueprint §10 end-to-end checklist, instead of 15 duplicate runs.
  - [ ] Merge order decided and executed by `architect-review` (bottom-up #22→#36, or merge head + close the rest as integrated) with epic/sub-issue closure flowing correctly.
  - [ ] `main` ends up with the complete, green build; superseded PRs closed with a pointer to the merge.
- **Pipeline:** ops decision on existing issues; no new code.

### NI-4 · Part 4 — Incident runbook (debugging scenario) — `priority:P1-high` · `part-4` · `type:doc`

- **Deliverable:** `docs/incident-runbook.md`. **Rubric:** 10 pts. **Window:** H24–H30 (now).
- **ACs:**
  - [ ] Walks the brief's outage (100% CPU, slow DB, duplicate orders, delayed notifications, riders can't accept jobs) through: investigation → root causes → immediate fixes → permanent fixes → monitoring.
  - [ ] Root causes plausibly interconnected (one incident, not five), referencing this architecture's actual components (Postgres, Redis, queues from `architecture.md` §6/§10).
  - [ ] Duplicate-orders section ties back to the idempotency/uniqueness decisions in `database-schema.md`.

### NI-5 · Part 5 — DevOps document — `priority:P1-high` · `part-5` · `type:doc`

- **Deliverable:** `docs/devops.md`. **Rubric:** 10 pts; "weak CI/CD knowledge" is an auto-fail concern. **Window:** H24–H30.
- **ACs:**
  - [ ] Covers CI/CD, Azure architecture, Kubernetes, Redis, load balancing, secrets management, logging, backups, rollback strategy, monitoring, deployment approach.
  - [ ] **Reuses and expands** `architecture.md` §12 — zero contradictions (same environments, same pipeline stages); architecture doc's summary stays the summary.
  - [ ] Rollback + backup strategies reference the actual stack (EF Core migrations, Postgres, the modular monolith's single deployable).

### NI-6 · Queue hygiene — reconcile slices #2–#5 with sub-issues and epic #1 — `priority:P1-high` · `part-2` · `type:code`

- **ACs:**
  - [ ] #2–#5 no longer claimable as work: relabel to tracking (mirroring `epic` semantics) or advance their `status:*` to reflect their sub-issues' true state.
  - [ ] Epic #1 body updated so progress is visible (either keep `- [ ] #2–#5` with parents closing when their sub-issues close, or track #7–#21 directly).
  - [ ] Rule recorded (one line in `.ai-roster/README.md`): when a slice is decomposed, the parent becomes tracking-only immediately.

### NI-7 · Cross-doc consistency review (Parts 1/3/5/8 + PRD/TDs) — `priority:P1-high` · `part-1` · `type:doc`

- **Deliverable:** review comments + fix commits across `docs/`. **Window:** H40–H45 wave, but the known items below can land earlier. Owner: `architect-review`.
- **ACs:**
  - [ ] `architecture.md` ↔ `database-schema.md` ↔ `devops.md` ↔ README tell one story (stack, module map, environments, ports).
  - [ ] **Fix TD-004's broken link** to gitignored/absent `ORIGINAL_REQUEST.md` — inline the relevant §R3 quotation (or commit a redacted excerpt) so the provenance survives on GitHub.
  - [ ] **Resolve module-path drift:** documented `src/auth/` vs actual `src/Tekram.Api/src/auth/` — ratify the real layout in a TD-007 entry or correct PRD §3/§5, TD-001/TD-004, `architecture.md` §3/§13.
  - [ ] Unify the 72h (README/brief) vs 48h (plan) framing.
  - [ ] Parts 1 and 3, currently checked off without review, get an explicit review verdict here.

### NI-8 · Part 7 — AI strategy proposal — `priority:P2-normal` · `part-7` · `type:doc`

- **Deliverable:** `docs/ai-strategy.md`. **Rubric:** 5 pts. **Window:** H30–H34.
- **ACs:**
  - [ ] Proposes AI features for all six brief-named areas: customer support, recommendations, fraud detection, demand forecasting, delivery optimization, developer productivity.
  - [ ] Each feature: value hypothesis, data it needs (tied to entities in `database-schema.md`), build-vs-buy stance, rough sequencing.
  - [ ] Consistent with `architecture.md` §11's future-verticals story (AI Features is one of the brief's named future capabilities).

### NI-9 · Part 8 — Documentation pass (README, setup, deploy, API docs) — `priority:P2-normal` · `part-8` · `type:doc`

- **Deliverable:** overhauled `README.md` + `docs/deployment-guide.md` (or equivalent sections). **Rubric:** 5 pts; "poor documentation" is an auto-fail concern. **Window:** H34–H40, after Part 2 merges.
- **ACs:**
  - [ ] README: project overview, folder structure (the *real* `src/Tekram.Api/src/<module>` layout), prerequisites, setup guide (compose up → migrate → run → verify, sourced from blueprint §10), test instructions, links to every part's deliverable.
  - [ ] Environment variables documented and a tracked `.env.example` committed (`.gitignore` already whitelists it; none exists).
  - [ ] API documentation: Scalar UI link + how to reach it (`/scalar`), per TD-004.
  - [ ] Deployment guide + dependencies + future improvements sections present (brief names each explicitly).
  - [ ] Future improvements references TD-001's extraction triggers rather than inventing a new list.

### NI-10 · CI workflow — build + test on PR — `priority:P2-normal` · `part-5` · `type:code`

- **Deliverable:** `.github/workflows/ci.yml`.
- **Why:** turns the Part 5 CI/CD narrative into a working artifact for ~an hour of work; green checks on the repo the assessor opens.
- **ACs:**
  - [ ] On PR + push to `main`: `dotnet build` + `dotnet test` with Postgres/Redis service containers (mirroring `docker-compose.yml`).
  - [ ] Pipeline stages match what `docs/devops.md` (NI-5) describes.
- **Note:** cut without hesitation if the P0/P1 queue is behind — it defends an auto-concern but carries no direct points.

### NI-11 · Part 9 — Business recovery plan — `priority:P3-low` · `part-9` · `type:doc`

- **Deliverable:** `docs/business-recovery-plan.md`. **Rubric:** bonus, but "poor business thinking" is an auto-fail concern. **Window:** H30–H34.
- **ACs:**
  - [ ] Answers the brief's exact scenario: orders 1,000/day → 100/day after a server outage — investigations, dashboards, metrics, Growth/Marketing collaboration, first-24h actions, 30-day recovery plan.
  - [ ] Reuses `01-business-strategy.md` context (competitors who absorb churned demand, cash-liquidity dynamics) instead of generic recovery advice — but stays an operational plan, not a strategy rehash.
  - [ ] Ties trust-recovery metrics to Part 4's monitoring outputs (one incident story across both docs).

### NI-12 · Submission & repo-hygiene checklist (H45–H48) — `priority:P1-high` · `part-8` · `type:doc`

- **Deliverable:** executed checklist, closed as the final act.
- **Why P1 despite being last:** "missing the deadline" is an auto-fail; this is the only issue that owns shipping.
- **ACs:**
  - [ ] Repo public/shared per submission instructions; clean history confirmed.
  - [ ] All epics closed, no dangling `status:*`/`agent:claimed:*` labels on closed issues, link check across `docs/` passes (no `ORIGINAL_REQUEST.md`-style 404s).
  - [ ] Plan §4 deliverables checklist fully ticked and truthful; final self-score against `.ai-roster/rubric-checklist.md` recorded.
  - [ ] Ship at H48 regardless of remaining polish.

### NI-13 (optional) · P4 frontend demo epic — pre-seed, explicitly gated — `priority:P4-bonus` · `type:code`

- Pre-create the epic in `0-intake` with a **blocked** marker so the web-engineer spin-up decision at H40–H45 is a label flip, not a scoping exercise. Zero rubric points; first thing dropped. Skip creating it at all if the queue above isn't green by H40.

---

## 4. Minor observations (no issue warranted)

- Issue #15's body states `BaseDeliveryFeeUsd = 1.50` and, two lines later, "flat .50 for graded
  core" — a body typo (blueprint SS6.1.4 is authoritative); QA's fee assertions should cite the
  blueprint, not the issue body.
- Closed test issue #6 ("TEST — will delete") is fine to leave; NI-12's label sweep covers it.
- `docs/technical-design-blueprint.md` is doing quiet double duty as the Part 2 spec (sub-issues
  cite "Blueprint SS6.x"); no action needed, but the Part 8 README should *not* link it as
  assessor-facing documentation — it's an internal build artifact.
- The `.agents/` directory (teamwork previews) is gitignored — good; nothing leaks.

## 5. Suggested processing order

1. **Today, before H24:** NI-1 (QA spike) and NI-2 (Leadership) in parallel — they're on
   different runtimes and different owners; then NI-3 (merge plan) as soon as NI-1 resolves.
2. **H24–H34:** NI-4, NI-5, NI-8, NI-11 through the doc pipeline; NI-6 whenever `pm-doc-intake`
   touches the queue anyway.
3. **H34–H45:** NI-9 (docs pass) once Part 2 is merged; NI-7 (consistency review) as the review
   wave; NI-10 only if green everywhere else.
4. **H45–H48:** NI-12. NI-13 only if everything above is done.
