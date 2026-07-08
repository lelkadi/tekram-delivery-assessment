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
| [TD-004](#td-004--net-8-aspnet-core-as-the-backend-stack) | .NET 8 / ASP.NET Core backend, Scalar for API docs | Accepted | — |
| [TD-005](#td-005--postgres-schema-per-module-jsonb-snapshot-for-order-customizations) | Postgres schema-per-module; JSONB snapshot for order-time customizations | Accepted | Customization catalog outgrows a snapshot (e.g. loyalty-points-per-option) |
| [TD-006](#td-006--qa-gate-on-gemini-via-antigravity-third-family-independence) | QA gate on Gemini (antigravity) for third-family independence | Accepted, **pending runtime spike** | Antigravity spike fails, or a verified equal-independence runtime appears |

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

---

## TD-004 — .NET 8 / ASP.NET Core as the backend stack

**Status:** accepted
**Date:** 2026-07-08

**Context.** The assessment brief (Part 2) leaves the implementation language unspecified, but
[ORIGINAL_REQUEST.md](../ORIGINAL_REQUEST.md) §R3 — the founder's kickoff instruction that
seeded this whole document set — explicitly directs a "highly scalable, production-ready .NET
Core platform, using Scalar for API documentation." That direction was never contradicted or
walked back in any later doc, so it stands as the real stack decision; it just hadn't been
codified here yet. This entry closes that gap so the architect/engineer split (§ARCHITECT_SPEC,
`.ai-roster/backend_instructions.md`) has one unambiguous source of truth instead of the generic
Node/Fastify boilerplate left over in `.ai-roster/architect_spec_instructions.md` from the
roster's other project (Careeree) — that file's STACK CONTRACT does **not** apply to this repo.

**Decision.**
- **Runtime/language:** .NET 8 (LTS), C#.
- **Web framework:** ASP.NET Core Minimal API, endpoints grouped per module
  (`MapGroup("/api/auth")`, etc.) — avoids a controller-per-module ceremony layer while still
  routing through the same Application/Domain/Infrastructure layering inside each module.
- **ORM:** Entity Framework Core 8 + `Npgsql.EntityFrameworkCore.PostgreSQL`, code-first
  migrations (`dotnet ef migrations add`), applied to both the primary and lane-N databases
  (mirrors the "both DBs" migration discipline the roster boilerplate states for its own stack).
- **API documentation:** native `Microsoft.AspNetCore.OpenApi` document generation + **Scalar**
  (`Scalar.AspNetCore`) as the interactive docs UI at `/scalar` — per the founder's explicit
  instruction, in place of Swagger UI.
- **Auth:** `Microsoft.AspNetCore.Authentication.JwtBearer`; password hashing via
  `BCrypt.Net-Next`.
- **Cache/rate-limit:** `StackExchange.Redis`.
- **Validation:** FluentValidation.
- **Logging:** Serilog, structured JSON sink.
- **Tests:** xUnit + FluentAssertions + `WebApplicationFactory<Program>` integration tests
  against the real lane Postgres/Redis (no mocking the stack itself, only `EMAIL_MOCK`/`SMS_MOCK`
  at the notification-gateway boundary — same principle as the roster's no-mocking rule, applied
  to this stack).

**Rejected alternatives.**
- *Node.js/TypeScript (Fastify/Express + Drizzle or Prisma):* the roster's generic
  `architect_spec_instructions.md`/`backend_instructions.md` boilerplate assumes this, but it was
  copied in from an unrelated project template and was never the founder's instruction for
  Tekram — using it would silently override an explicit, still-standing directive.
- *Clean Architecture as separate class-library projects (`Tekram.Domain.csproj`,
  `Tekram.Application.csproj`, …):* rejected for this assessment's timeframe and for consistency
  with [TD-001](#td-001--modular-monolith-not-microservices-for-now) and the PRD's locked
  deliverable paths (`src/auth/`, `src/restaurants/`, `src/orders/`) — layering is expressed as
  folders *within* each module, not as project references, so the parallel-engineer merge model
  in the project plan §6 still holds (disjoint directories, no `.csproj` reference churn).

**Full detail:** see [docs/architecture.md](./architecture.md) §3 (component/layering view) and
§9 (deliverable-path ↔ module map).

---

## TD-005 — Postgres schema-per-module; JSONB snapshot for order-time customizations

**Status:** accepted
**Date:** 2026-07-08

**Context.** [docs/database-schema.md](./database-schema.md) (Part 3) needs a concrete rule for
(a) how module ownership of tables (TD-001) maps onto Postgres, and (b) how an order line item
records the customizations (size, add-ons) a customer picked, given that the menu itself can
change after the order is placed.

**Decision.**
- **Schema-per-module:** each module owns a Postgres schema matching its directory
  (`auth.*`, `restaurants.*`, `orders.*`, and, once built, `billing.*`, `ride_hailing.*`, …) —
  the same module-ownership boundary from TD-001, made visible in the database itself rather
  than only in application code.
- **JSONB snapshot for customizations:** `orders.order_items.customizations` stores the
  resolved `{group, option, price_modifier_usd}` selections as JSONB **at order time**, rather
  than a normalized `order_item_customizations` join table pointing back at the live
  `restaurants.menu_item_customization_options` rows.

**Rejected alternative.** A fully normalized `order_item_customizations` table referencing
`menu_item_customization_options` by foreign key. Rejected because an order must remain an
immutable historical record — if a restaurant renames a customization option or changes its
price next week, a normalized FK would silently reinterpret last month's order through this
week's menu. The JSONB snapshot freezes exactly what the customer paid for, at the price they
paid, permanently — at the cost of losing a live FK constraint on customization choices (accepted
here since price/name integrity for historical orders matters more than referential purity on a
field that is never queried across orders).

**Revisit trigger.** If a future feature needs to query *across* orders by customization choice
(e.g. "how many Large pizzas sold this month" for merchant analytics/Loyalty), add a
denormalized reporting column or materialized view rather than reversing the snapshot — reversing
it would break the immutability guarantee this decision exists for.

---

## TD-006 — QA gate on Gemini via antigravity (third-family independence)

**Status:** accepted, **pending runtime spike** (see prerequisite)
**Date:** 2026-07-08

**Context.** The QA gate protects the highest-weighted, hard-gated deliverable (Part 2 coding
challenge, 25 pts, min 18). Its value is *independent* verification — catching what the
implementer missed. Engineers run on deepseek; every other reviewer (eng-lead's own verify pass,
architect-review) shares a family with either the engineers (deepseek) or the doc authors
(claude).

**Decision.** Run QA on **Gemini `gemini-3.5-flash` via the antigravity runtime** — a third
model family, decorrelated from both deepseek and claude, for the most independent gate possible.
`thinking: medium`. Model id is the current stable Gemini Flash per Google's canonical docs
(ai.google.dev/gemini-api/docs/models, 2026-07-08), chosen for its "agentic and coding tasks"
profile.

**Accepted risk + hard prerequisite.** Antigravity is the one runtime in this roster whose model
ids and tool/permission schema were never verified; a wrong id or broken tool bridge **fails
silently**, and at a gate a silent no-op QA looks identical to a pass. Two things are unverified:
(1) that antigravity's `agent.json` `model` field accepts the Gemini *API* id `gemini-3.5-flash`
verbatim (vs an Antigravity-specific menu alias), and (2) that an antigravity agent can actually
run bash (docker/curl/dotnet test) and load the `github_flow` skill. **Before trusting QA
verdicts at the gate, run the loud-failure spike:** give it a deliberately wrong id on a throwaway
issue and confirm it *errors visibly*; then confirm on a real issue that QA produced genuine
output (curl/test results), not an empty verdict. Until the spike passes, treat QA verdicts as
advisory and keep architect-review (claude) as the real gate. The eng-lead's handoff step already
requires confirming QA actually ran before trusting the label it set.

**Rejected alternatives.** Claude-sonnet QA (the prior default) — safe and already a different
family from the deepseek engineers, but only *second*-family independence; kept as the documented
fallback if the antigravity spike fails. DeepSeek QA — rejected outright: same family as the
engineers, so correlated blind spots defeat the gate's purpose.

**Revisit trigger.** The spike fails and can't be fixed quickly (→ fall back to claude-sonnet), or
a runtime with equal independence and verified reliability becomes available.
