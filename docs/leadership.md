# Part 6 — Leadership & Team Management

> **Scope:** how I would run Tekram's engineering team — 2 Senior Developers, 3 Mid-level
> Developers, 2 Junior Developers, 1 QA Engineer, 1 DevOps Engineer (9 people) — covering sprint
> planning, code reviews, documentation, mentoring, KPIs, hiring, conflict resolution, and
> preventing knowledge silos.
>
> **Reading guide:** every practice here is stated as a concrete mechanism (a cadence, an SLA, a
> definition, a ritual with an owner), not a value statement. §10 shows the same practices
> operating in this very repository.

## 1. Operating model: pods around module ownership

The team is too small for a platform/feature-team split and too big to run as one undifferentiated
pool. I organize it as **two delivery pods anchored on the modular monolith's module boundaries**
(auth/identity, restaurants/catalog, orders/fulfillment — see `docs/architecture.md` §3):

| Pod | Members | Primary modules |
|---|---|---|
| **Pod A — Marketplace** | Senior 1 (lead), Mid 1, Mid 2, Junior 1 | restaurants, search, promotions |
| **Pod B — Transactions** | Senior 2 (lead), Mid 3, Junior 2 | orders, payments, notifications |
| **Cross-cutting** | QA, DevOps | test strategy + pipeline; embedded in both pods' ceremonies |

Rules that make pods work at this size:

- **Ownership means stewardship, not exclusivity.** Every module has a named owner *and a named
  backup owner* from the other pod (see §9 on silos). Anyone may open a PR anywhere; the owner
  reviews it.
- **QA and DevOps are members of the team, not a downstream service.** QA sits in refinement and
  writes acceptance criteria *with* the developers (tests are designed before code, not after);
  DevOps owns the pipeline as a product with the team as customers.
- **I stay 80% management / 20% technical** at this size: I still review high-risk designs and do
  occasional PRs (never on the critical path), because a lead who can't read the code can't
  arbitrate technical conflict (§8) or calibrate performance (§6).

## 2. Sprint planning

**Cadence: 2-week sprints**, aligned to a release train (deploy at least once per sprint; the
CI/CD pipeline in `docs/devops.md` makes deploys cheap enough to do far more often).

| Ceremony | When | Length | Who |
|---|---|---|---|
| Backlog refinement | Weekly, mid-sprint | 60 min | Whole team; QA mandatory |
| Sprint planning | Day 1 | 90 min | Whole team |
| Daily standup | Daily | 15 min, hard stop | Whole team |
| Review/demo | Last day | 45 min | Team + stakeholders (Product, Ops, Support) |
| Retrospective | Last day | 45 min | Team only |

Mechanics that matter more than the calendar:

- **Definition of Ready** — a story enters the sprint only if it has: a user-story statement, a
  verbatim link to the originating request/feedback, checkbox acceptance criteria observable from
  the running system, and a size ≤ 3 days for one engineer. Bigger items are split first
  (INVEST); an item that can't be split becomes a timeboxed spike with a written output.
- **Capacity, not velocity theatre.** Plan against realistic capacity: juniors count at ~60% and
  their mentor-senior at ~80% (mentoring time is *planned*, not squeezed in — §5). Velocity is
  tracked over 6 sprints for forecasting only; it is never a target and never compared across
  pods (Goodhart's law).
- **20% structural allowance** every sprint for tech debt, tooling, and incident follow-ups
  (Part 4's "permanent fixes" land through this lane, so they don't lose the priority fight
  against features forever).
- **One sprint goal per pod**, written as an outcome ("restaurant search returns in <300 ms on
  50k-restaurant dataset"), not a ticket list. Mid-sprint scope changes go through me; the trade
  is always explicit — something of equal size leaves.
- **Definition of Done:** code merged, tests green in CI, docs updated (§4), deployed to staging,
  ACs demonstrated. "Done except deploy" is not done.

## 3. Code reviews

Reviews are the highest-leverage quality *and* teaching mechanism the team has, so they get
explicit service levels:

- **Review SLA:** first review response within **4 working hours**; author responds to review
  comments within the same. A PR unreviewed at 24h escalates automatically to the pod lead —
  by rule, reviewing others' PRs comes **before** writing new code after standup.
- **PR size cap: ~400 changed lines** (soft). Beyond that, reviewers rubber-stamp; authors must
  split (stacked PRs are fine). One PR = one logical change, mirroring the atomic-commit rule.
- **Who reviews:** every PR needs one approval; PRs touching **payments, auth, or migrations**
  need two, one of which is the module owner or a senior. Review assignment deliberately crosses
  levels — juniors review seniors' PRs too (they must be able to say "I don't understand this,"
  which is a legitimate review finding; if a junior can't follow the code, neither will the
  next maintainer).
- **What review is for**, in priority order: correctness and edge cases → security and data
  integrity → readability and naming → design fit with module boundaries. **Not for:** style
  nits — formatters and analyzers (`dotnet format`, Roslyn analyzers) are CI's job; humans don't
  argue about whitespace.
- **Tone contract:** comments critique code, never people; blocking comments state the concrete
  risk ("this double-charges on retry because…"), and reviewers distinguish `blocking:` from
  `nit:` explicitly. Author merges when green — no drive-by re-litigating after approval.
- **No self-merge, no exceptions** — including me and the seniors. The moment leads bypass the
  gate, the gate is dead.

## 4. Documentation culture

Documentation fails when it's a separate activity someone "should" do later. It works when it is
**part of the definition of done and has a reader**:

- **Decision log (ADRs).** Every architecturally significant decision gets a numbered entry —
  context, options, decision, consequences, revisit-triggers — in a `technical-decisions.md`-style
  log. Decisions made in Slack or a hallway don't exist until logged. This is the single highest
  ROI document a team keeps: it kills re-litigation (§8) and onboards newcomers into *why*, not
  just *what*.
- **Docs-with-the-diff.** A PR that changes behavior updates the affected doc *in the same PR*
  (README, runbook, API annotations). Reviewers block on stale docs like they block on missing
  tests. API documentation is generated from code (OpenAPI/Scalar), so it cannot drift.
- **Runbooks over tribal knowledge.** Every alert that can page a human has a runbook entry
  (symptom → diagnosis → mitigation), written by whoever resolved it last (Part 4's incident
  runbook is the template).
- **Living docs get owners; dead docs get deleted.** Each doc has a named owner and a
  last-reviewed date; anything untouched for two quarters is either re-validated or archived.
  A wrong doc is worse than no doc.
- **Writing is a first-class engineering skill** — it's in the competency matrix (§6) and I give
  feedback on design docs with the same seriousness as on code.

## 5. Mentoring

With 2 juniors and 3 mids, mentoring is a scheduled system, not goodwill:

- **Explicit pairing:** each junior is paired with the senior in their pod (S1→J1, S2→J2) for a
  quarter, then rotates. Mids get growth mentoring from me plus technical stretch via the
  seniors. The mentor relationship is *planned capacity* (§2), with a stated goal per quarter
  ("J1 ships a feature end-to-end including the migration and the runbook entry, unassisted").
- **Weekly 1:1s, 30 minutes, never skipped** — mine with everyone (9 × 30 min is 4.5 h/week; it
  is the highest-leverage time I spend). 1:1s are the employee's agenda: growth, friction,
  concerns — status goes in standup, not here. Mentors additionally hold a weekly technical hour
  with their junior (pairing on real tickets, not toy exercises).
- **Deliberate task assignment:** juniors get tickets *one notch above* their comfort zone with a
  named safety net, not the leftovers. Mids get rotations through unfamiliar modules (§9) and
  each leads one project per half — scoping, breakdown, and demo — as the concrete path to
  senior.
- **Teaching rituals:** biweekly 45-min engineering session (brown-bag, incident walkthrough, or
  "how module X actually works"), rotating presenter — presenting is itself a growth mechanism;
  juniors present by their second quarter.
- **Code review as mentoring:** seniors are coached to review junior PRs with questions
  ("what happens if the coupon expires between validation and commit?") rather than corrections,
  and to leave at least one "here's why" comment per review.

## 6. KPIs & performance

Two dashboards, never mixed: **team health** (system metrics) and **individual growth**
(never system metrics).

**Team KPIs — DORA plus quality, reviewed monthly with the team:**

| Metric | Definition | Starting target |
|---|---|---|
| Deployment frequency | Production deploys per week | ≥ 2/week, trending to daily |
| Lead time for change | Merge → running in production | < 1 day |
| Change failure rate | % of deploys causing rollback/hotfix | < 15% |
| MTTR | Incident start → mitigated | < 1 hour |
| Escaped defects | Bugs found in prod vs pre-prod, per sprint | Trending down; each escape gets a 15-min "which gate missed it?" review |
| Review latency | PR opened → first review | p50 < 4 working hours (the §3 SLA, measured not assumed) |
| Sprint predictability | Committed vs delivered sprint goals | ≥ 80% over rolling 6 sprints |

These are *team* metrics for finding system problems (review latency up → check WIP and pairing
load), not sticks for individuals.

**Individual performance — competency matrix, not output counting.** Never lines of code, commit
counts, or ticket counts (all instantly gameable, all anti-collaboration — they punish the person
who spends the afternoon unblocking a teammate). Instead a public competency matrix per level
across five axes: technical execution, design/architecture, quality mindset, communication &
documentation, and team impact (mentoring, reviews, unblocking others). Each person keeps a
quarterly growth plan (2–3 concrete goals set in 1:1s); formal reviews twice a year with peer
input, but **no feedback appears there for the first time** — anything worth saying in a review
was worth saying in a 1:1 months earlier. QA and DevOps get matrices for their own crafts, not
developer criteria with the serial numbers filed off.

## 7. Hiring

The team's next hires (a mid-level backend dev and, at scale, an SRE) go through a structured,
bias-resistant pipeline — total candidate time ≤ 5 hours:

1. **Screen (30 min, me):** motivation, communication, one deep-dive into a past project — can
   they explain a technical decision and its trade-offs?
2. **Practical exercise (2 h max, timeboxed):** a small, realistic task on a scaled-down codebase
   shaped like ours — e.g. "add coupon support to this order endpoint; it has a concurrency bug,
   find it." Reviewing a deliberately flawed PR is an equally good variant, and cheaper for the
   candidate. No 8-hour take-homes — they select for free time, not skill.
3. **Technical interview (60–90 min, both seniors):** walk through *their* exercise submission
   (people discuss their own code far more revealingly than whiteboard puzzles), then extend it:
   "how does this behave with two concurrent orders for the last stock unit?"
4. **Team fit (30 min, one mid + one junior, no leads in the room):** candidates behave
   differently without authority present, and the team gets a real vote.

**Scorecards are written independently before any debrief** (no anchoring), against criteria
fixed before the search opened. Hiring bar: a clear yes on trajectory and collaboration beats a
marginal yes on current skills — juniors are hired on slope, seniors on judgment. Every offer is
followed by a **30/60/90 onboarding plan**: first PR merged in week 1 (a groomed
`good-first-issue`), on-call shadowing by day 60, owning a module as backup owner by day 90 —
onboarding speed is a direct measure of our documentation (§4).

## 8. Conflict resolution

Conflict at this size is healthy signal; the job is to keep it about the work.

- **Technical disagreements** get a decision *mechanism*, so they end: (1) the disagreeing
  parties write the options down — one page, trade-offs, in ADR format; writing alone resolves
  half of them; (2) timebox the debate (a week of argument costs more than most wrong choices);
  (3) if genuinely balanced, the **module owner decides**; if cross-module, the seniors and I
  decide together; (4) log it as an ADR and everyone practices **disagree-and-commit** — the
  decision is revisited only when its logged revisit-trigger fires, not re-litigated per sprint.
  Where cheap, settle by experiment instead: a one-day spike beats a three-day debate.
- **Interpersonal friction** is caught early in 1:1s (their real purpose) and handled directly
  and privately: facts first, both sides separately, then together. Praise is public;
  correction is always private. The standard I hold: criticize ideas hard, people never — and I
  model taking public pushback on my own decisions gracefully, because the team calibrates to
  what I *do*.
- **Blameless postmortems and retros:** incidents and failed sprints are analyzed as system
  failures ("what allowed this?"), never people failures ("who did this?"). The first time a
  retro finding turns into a punishment is the last honest retro the team holds. Retro actions
  are tracked like tickets — max 2–3 per retro, each with an owner and a due sprint; a retro
  whose actions evaporate teaches the team retros are theatre.
- **Escalation path is explicit:** peer-to-peer → pod lead → me, and skipping levels is fine
  when the concern *is* the level in between. Nobody should ever be stuck with no route.

## 9. Preventing knowledge silos

With 9 people, one resignation can erase a domain. Silos are attacked structurally:

- **Backup ownership (bus factor ≥ 2, enforced):** every module, pipeline, and operational duty
  has a named owner *and* backup from the other pod. Twice a year we run a **bus-factor audit**:
  list every system, name who can operate it alone; anything with one name becomes next
  quarter's rotation target.
- **Rotation with a receipt:** every mid and junior rotates to an unfamiliar module at least
  once per half, and the rotation isn't done until they've shipped a change there *and* improved
  its docs (the newcomer sees exactly what's missing; the expert no longer can).
- **Cross-pod review by default** for module-boundary changes (§3) keeps read-fluency spread
  even where write-ownership is concentrated.
- **DevOps and QA are the most dangerous silos** — deliberately: developers rotate through
  pipeline duty with the DevOps engineer (one dev per sprint owns "the build"), and QA pairs
  with developers on test design so test-craft lives in the team, not in one person. On-call
  (once the product warrants it) follows the same rule: primary + secondary, never a fixed hero.
- **Documentation as the passive layer:** ADRs, runbooks, and docs-with-the-diff (§4) mean the
  written system lags reality by days, not quarters — rotation covers what writing can't.

## 10. Evidence: these practices, operating in this repository

This assessment was itself delivered by a team — of AI agents — managed with exactly the
mechanisms above; the repo is the audit trail:

| Practice (§) | Where it's visible here |
|---|---|
| Definition of Ready, INVEST stories (§2) | Every work issue carries a deliverable path, rubric points, and checkbox acceptance criteria observable from the running system (issue template + `.ai-roster/pm_doc_intake_instructions.md`); oversized slices were split into sub-issues |
| Explicit review gates, no self-merge (§3) | The issue state machine (`status:0-intake → … → 11-done`, `.ai-roster/team.yaml`) enforces independent QA, PM verification, and architect review; only the reviewing role merges/closes — the implementer never does |
| Review as machine-checked contract (§3, §6) | QA writes a black-box e2e test per acceptance criterion, committed onto the PR branch (TD-008 in `docs/technical-decisions.md`) — the verdict is a green suite, not an opinion |
| Atomic changes (§3) | `.ai-roster/rules/git.md`: one commit = one logical change, explicit staging only, one issue = one branch; `git log` shows the rule holding |
| Decision log with revisit-triggers (§4, §8) | `docs/technical-decisions.md` TD-001…TD-008 — including TD-007 *superseding* TD-006 when its revisit-trigger fired: a logged decision changed for logged reasons, instead of being re-argued |
| Docs have owners and gates (§4) | Doc deliverables flow through the same issue pipeline with review before close; cross-doc consistency has its own review issue |
| Attribution and handoffs (§8) | Every stage transition posts an attributed comment (`github_flow.sh transition`) — the timeline always answers "who moved this, when, why" |
| Bus factor on process itself (§9) | The whole operating model is codified in `.ai-roster/` (roster, rules, state machine), not in anyone's head — a new agent (or human) onboards from the repo alone |

The point of the parallel: none of these mechanisms care whether the "engineer" is a person or a
model. They are the same controls I'd install for the human team of 9 — small units of work with
explicit acceptance, independent review with teeth, decisions written down with their reasons,
and no single point of knowledge failure.

---

*Sibling docs: process & priorities in `docs/00-project-management-plan.md`; architecture and
module boundaries in `docs/architecture.md`; decision log in `docs/technical-decisions.md`;
operational incident practice in `docs/incident-runbook.md` (Part 4).*
