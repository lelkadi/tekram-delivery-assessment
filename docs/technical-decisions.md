# Technical Decisions

Living document — one entry per significant decision, appended as the project evolves. Each
entry records context, the decision, alternatives rejected, and (where relevant) the milestone
that would trigger revisiting it. This doc is also the Part 8 "technical decisions document"
deliverable.

## Index

| ID | Decision | Status | Revisit trigger |
|---|---|---|---|
| [TD-001](#td-001--modular-monolith-not-microservices-for-now) | Modular monolith, not microservices | Accepted (architect ratifies in Part 1) | ~10x order volume, >2 teams, or per-vertical isolation needs |
| [TD-002](#td-002--shared-infrastructure-via-one-docker-compose-stack-isolation-via-lanes) | One shared docker compose stack; lane-based isolation | Accepted | Lanes interfering → compose-project-per-lane |
| [TD-003](#td-003--collapsed-pipeline-for-document-deliverables) | Collapsed pipeline for doc deliverables | Accepted | — |

---

## TD-001 — Modular monolith, not microservices (for now)

**Status:** accepted (pending architect ratification in the Part 1 architecture doc)
**Date:** 2026-07-08

**Context.** The platform must serve Food Delivery, Taxi, Supermarkets, and Housekeeping at
1M users / 50k DAU / 15k daily orders, with future verticals (Pharmacy, Parcels, Loyalty,
Merchant Dashboard). The coding challenge must be built, tested, and documented within the
assessment window by parallel agents whose safety depends on stable module boundaries.

**Decision.** One repository, one deployable API — a **modular monolith**. Module boundaries
live at the directory level (`src/auth/`, `src/restaurants/`, `src/orders/`, …), each module
exposing an explicit interface; cross-module calls go through those interfaces, never through
another module's internals. The database is shared but tables are module-owned.

**Rejected alternatives.**
- *Microservices now:* at 15k orders/day (~0.2 orders/sec sustained) the operational cost
  (network failure modes, distributed tracing, per-service CI/CD, data consistency) buys nothing
  measurable. Premature decomposition is the classic "weak architecture rationale."
- *Monorepo modules auto-deployed as separate services in CI/CD:* worst of both — distributed
  runtime complexity while the code is still entangled enough to need lockstep deploys.

**Microservices milestone — revisit when any of these triggers fire:**
1. **Scale:** sustained order volume approaches ~10x (≥150k/day) or a single module (search,
   dispatch/matching, notifications) shows an independent scaling profile the monolith's
   horizontal scaling can't serve economically.
2. **Team:** engineering grows past roughly two pizza-box teams (~10–12 engineers) and deploy
   contention (release trains, merge queues) measurably slows delivery.
3. **Isolation:** a vertical (e.g. Taxi dispatch) needs a different availability tier, tech
   stack, or compliance boundary than the rest.

Extraction order when triggered (highest independent-scaling pressure first): notifications →
search/discovery → dispatch/matching → payments. The module interfaces above are the future
service contracts; extraction is a lift, not a rewrite.

---

## TD-002 — Shared infrastructure via one docker compose stack; isolation via lanes

**Status:** accepted
**Date:** 2026-07-08

**Context.** Multiple agents (engineers + QA) run code and tests concurrently on one machine
and must not collide on ports, databases, or Redis state.

**Decision.** One `docker compose` stack hosts Postgres + Redis for everyone. Concurrency
isolation comes from **lanes** (`.ai-roster/skills/github_flow.sh`, `MAX_LANES=3`): lane N gets
its own API port (`30N1`), database (`tekram_laneN`), and Redis db number. Code isolation comes
from per-agent git worktrees. Issues labeled `type:doc` bypass lanes entirely.

**Rejected alternative.** Docker image per agent: adds image builds and credential plumbing
without adding isolation beyond what worktrees + lanes already provide. Escalation path if
lanes ever interfere: one compose project per lane (`docker compose -p laneN`) — a config
change, not a redesign.

---

## TD-003 — Collapsed pipeline for document deliverables

**Status:** accepted
**Date:** 2026-07-08

**Context.** 7 of 9 assessment parts are prose documents; the roster's 12-stage issue state
machine is designed for code (QA can execute tests; a reviewer can reject an implementation).

**Decision.** `type:doc` issues follow `3-ready-for-dev → 4-in-progress → 5-in-review →
11-done`; research and spec happen inside the draft step, QA/PM-verify labels are unused, and
rework is a review comment plus a move back to `4-in-progress`. The full state machine applies
only to `type:code` issues (Part 2 slices, P4 frontend demo).
