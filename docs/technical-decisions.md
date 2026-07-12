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
| [TD-006](#td-006--qa-gate-on-gemini-via-antigravity-third-family-independence) | QA gate on Gemini (antigravity) for third-family independence | **Superseded by TD-007** (2026-07-09) | — |
| [TD-007](#td-007--qa-gate-on-claude-code--sonnet-high-effort-supersedes-td-006) | QA gate on claude-code / sonnet, high effort | Accepted | Verified third-family runtime appears with slack to migrate, or correlated QA/architect misses observed |
| [TD-008](#td-008--qa-persists-a-black-box-e2e-suite-per-issue-one-fact-per-ac) | QA persists a black-box e2e suite per issue (one fact per AC) | Accepted | Suite runtime slows the gate materially, or P4 UI needs true browser e2e |
| [TD-009](#td-009--qa-dual-gate-on-opencode--deepseek-v4-pro) | QA dual-gate on opencode / deepseek-v4-pro | Accepted | — |
| [TD-010](#td-010--architect-review-dual-gate-on-opencode--gpt-55) | Architect-review dual-gate on opencode / GPT-5.5 | Accepted | Correlated architect misses observed, or GPT-5.5 regresses significantly vs Claude Opus |
| [TD-011](#td-011--architect-review-third-gate-on-antigravity--gemini-35-flash) | Architect-review third gate on antigravity / Gemini 3.5 Flash | Accepted | — |
| [TD-012](#td-012--engineering-lead-gate-on-antigravity--gemini-35-flash) | Engineering Lead gate on antigravity / Gemini 3.5 Flash | Accepted | — |

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
`.ai-roster/agents/backend_instructions.md`) has one unambiguous source of truth instead of the generic
Node/Fastify boilerplate left over in `.ai-roster/agents/architect_spec_instructions.md` from the
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

**Status:** SUPERSEDED by [TD-007](#td-007--qa-gate-on-claude-code--sonnet-high-effort-supersedes-td-006) (2026-07-09) — the revisit trigger below was exercised: the runtime spike was never run and the deadline left no room to verify a silent-failure runtime at the gate.
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

## TD-007 — QA gate on claude-code / sonnet, high effort (supersedes TD-006)

**Status:** accepted
**Date:** 2026-07-09

**Context.** TD-006 put QA on Gemini via antigravity for third-family independence, hard-gated on
a loud-failure spike that was never run. The deadline is 2026-07-10; the spike is unbudgeted work,
and an unverified silent-failure runtime at the gate is the worst available failure mode — a no-op
QA that looks identical to a pass.

**Decision.** Exercise TD-006's own revisit trigger and documented fallback: QA runs on
**claude-code / sonnet** with `effort: high` (on this runtime thinking depth follows effort; there
is no separate thinking knob). Sonnet rather than opus keeps QA off the exact model that performs
architect-review and pm-verify.

**Trade accepted.** Second-family independence (claude vs the deepseek engineers) instead of
third-family, and QA now shares a family with the downstream reviewers. Mitigated by TD-008: the
gate's independence shifts from model family to *method* — the verdict rests on persistent,
machine-checkable e2e tests derived from acceptance criteria, and a test's red/green does not
share a same-family reviewer's blind spots.

**Rejected alternatives.** Running the antigravity spike first (unbudgeted against the deadline;
fallback was pre-approved in TD-006). Opus QA (maximum capability, but zero decorrelation from the
reviewing gates). DeepSeek QA (same family as the engineers — rejected in TD-006, still rejected).

**Revisit trigger.** A verified third-family runtime with loud failures becomes available AND
there is slack to migrate; or evidence of correlated misses between QA and architect-review.

## TD-008 — QA persists a black-box e2e suite per issue (one fact per AC)

**Status:** accepted
**Date:** 2026-07-09

**Context.** QA's verification was ephemeral — curl transcripts in a PR comment: evidence that
evaporates after the verdict, re-verifies nothing on later regressions, and (after TD-007 put QA
in the reviewers' model family) carries no independent weight of its own.

**Decision.** For every code issue it reviews, QA writes module-organized black-box tests under
`tests/e2e/<Module>/<Feature>Tests.cs` (one xUnit project, module folders mirroring
`Auth`/`Restaurants`/`Orders` — files named by feature, not issue: tests outlive issues): one
fact per acceptance criterion named `AC<i>_<Behavior>`, each touched class tagged
`[Trait("issue", "<n>")]` so `dotnet test --filter issue=<n>` reproduces exactly that issue's
acceptance bar. Black-box `HttpClient` against the running lane API — no project reference to
`src/**`, no `WebApplicationFactory` (deliberately decorrelated from the engineers' in-process
integration tests) — env-gated by `E2E_BASE_URL` (facts skip when unset, so a bare `dotnet test`
in CI without a live stack stays green). The suite is committed atomically
(`test(e2e): AC coverage for #<n>`) and pushed onto the PR branch **PASS or FAIL** — red facts are
the rejection's machine-checkable repro, and the engineer's fix must turn them green.
`tests/e2e/**` is QA's only write scope; engineers must never edit or weaken it (rules/git.md #5).
*(2026-07-09, same day: layout changed from the initial per-issue `Issue<N>Tests.cs` to the
module layout above — per-issue files are meaningless after merge and don't compose into a
regression harness; issue traceability moved into the trait.)*

**Consequences.** QA is no longer strictly read-only (write scope carved to `tests/e2e/**`). The
accumulated suite doubles as a regression harness for later issues. The eng-lead's "did QA
actually run" check becomes mechanical: a `test(e2e)` commit must exist on the branch.

**Revisit trigger.** Suite runtime slows the gate materially (→ split into a nightly run), or P4
UI work needs true browser e2e (→ extend the location/convention, not this decision).

---

## TD-009 — QA dual-gate on opencode / deepseek-v4-pro

**Status:** accepted
**Date:** 2026-07-09

**Context.** After TD-007 moved QA off the unverified antigravity runtime and onto
claude-code/sonnet, QA shared a model family with the downstream reviewers (both on Claude).
TD-008 mitigated this by shifting gate independence from model family to *method* (persistent
e2e tests), but model-family diversity at the QA layer was still desirable — a second gate on
an unrelated family would catch sonnet-specific blind spots.

**Decision.** Add a second QA agent (`qa-opencode`) running on DeepSeek v4-pro via the opencode
runtime — the same model family as the engineers, but operating as an **independent dual-gate**
alongside the primary sonnet QA:

- Both agents claim the same issue from `status:5-in-review` and run independently.
- Each writes its own e2e tests under `tests/e2e/` (opencode enforces this as a hard
  permission boundary; sonnet QA uses prose guidance).
- **First PASS wins** — the first agent to pass transitions the issue to `status:7-qa-passed`.
  Both must fail for the issue to go to `status:6-qa-failed`.

The DeepSeek family overlap with engineers is acceptable because the gate's independence comes
from *method* (black-box e2e tests vs in-process integration tests), not model family — the
same principle established in TD-008.

**Rejected alternative.** Adding a third family (e.g., OpenAI GPT via opencode) — deferred
because DeepSeek v4-pro was already configured and available on the opencode runtime, needing
no new provider setup.

**Revisit trigger.** Evidence that the dual DeepSeek gates (engineer + QA) share correlated
blind spots that a third-family gate would catch.

---

## TD-010 — Architect-review dual-gate on opencode / GPT-5.5

**Status:** accepted
**Date:** 2026-07-10

**Context.** The architect-review is the final gate before `11-done` — the only role that merges
PRs and closes issues. It currently runs on Claude Opus, the same model family as pm-verify
(Opus) and the QA primary gate (sonnet). With no model-family diversity at this layer, a
correlated blind spot across all three review gates could miss issues the engineers (DeepSeek)
also missed — leaving the pipeline's last defense with no independent perspective.

**Decision.** Add a second architect-review agent (`architect-review-opencode`) running on OpenAI
GPT-5.5 via the opencode runtime. Both architect agents work as a **dual-gate** (parallel,
first-accept-wins):
- Both claim the same issue from `status:9-pm-verified` and review independently.
- **Reject:** either may transition to `status:10-arch-rejected` (first reject wins).
- **Accept:** first to finish merges the PR, transitions to `status:11-done`, and closes.
  The other agent's merge attempt fails harmlessly (GitHub rejects already-merged PRs).
- The existing `architect_review_instructions.md` is shared — same protocol, same verdict
  format, same merge/close workflow.

This completes three-family diversity across the review chain:
- **DeepSeek** → engineers + architect-spec
- **Anthropic Claude** → QA (primary) + pm-verify + architect-review (primary)
- **OpenAI GPT-5.5** → architect-review (dual-gate)

**Rejected alternatives.**
- *Replace architect-review entirely with GPT-5.5:* loses the verified Claude Opus gate and
  its track record across the assessment.
- *Sequential review (Opus then GPT-5.5):* adds latency to the critical path without
  corresponding safety benefit — if Opus already accepted, forcing a second review catches
  only Opus-specific errors, not consensus errors (the dual-gate model catches both).
- *Keep the single gate:* continued correlated risk at the final review layer — see
  TD-007's revisit trigger ("correlated QA/architect misses observed"), now pre-emptively
  addressed.

**Revisit trigger.** Evidence of correlated misses between the two architect agents, or
GPT-5.5 quality regression relative to Claude Opus on code review tasks.

## TD-011 — Architect-review third gate on antigravity / Gemini 3.5 Flash

**Status:** Accepted
**Date:** 2026-07-12

**Context.** In TD-010, the architect-review process was structured as a dual-gate (Claude Opus on claude-code and GPT-5.5 on opencode) to ensure third-family model independence at the final code review layer. However, additional runtime diversity was desired to avoid relying solely on the Anthropic/OpenAI API/tooling infrastructure.

**Decision.** Add a third review gate running on the `antigravity` environment using the `Gemini 3.5 Flash` model. The three reviewer gates (Claude Opus, GPT-5.5, Gemini 3.5 Flash) review the issue independently. The first ACCEPT merges and closes the PR.

**Rejected alternatives.**
- *Run all gates on one runtime:* leaves the workflow vulnerable to runtime CLI/API outages.
- *Sequential review:* too slow for developer loop.

**Revisit trigger.** Quality degradation of Gemini 3.5 Flash on code review tasks, or high rate of false rejects.

## TD-012 — Engineering Lead gate on antigravity / Gemini 3.5 Flash

**Status:** Accepted
**Date:** 2026-07-12

**Context.** The Engineering Lead (`eng-lead`) acts as the code-issue orchestrator, preparing the developer's worktree, compiling briefs, dispatching engineers, verifying their commits, and publishing PRs. It originally runs on `opencode` with `deepseek-v4-pro`. To ensure runtime diversity at the orchestration level, a version running on the `antigravity` environment is needed.

**Decision.** Create a new version of the Engineering Lead (`eng-lead-antigravity`) running on the `antigravity` environment with the `Gemini 3.5 Flash` model.

**Rejected alternatives.**
- *Only use opencode:* leaves the orchestration gate vulnerable to a single runtime or provider outage.

**Revisit trigger.** Gemini 3.5 Flash failing to write precise engineering briefs or verify diffs correctly.

