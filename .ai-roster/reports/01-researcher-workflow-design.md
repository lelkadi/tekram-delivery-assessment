# Research Report: GitHub-Issues-Driven Multi-Agent Workflow for Careeree

**Role:** Technical Researcher
**Date:** 2026-06-29
**Status:** RESEARCH ONLY — no implementation. Input for Architect synthesis.
**Reviewed inputs:** `/Users/loukan/projects/loukan/careeree/CLAUDE.md` (full), all 8 files
under `.ai-roster/` (`team.yaml`, `*_instructions.md` ×6, `scripts/sync-agents.js`,
`skills/github_flow.sh`), repo remote (`github.com/lelkadi/tekram-delivery-assessment`), local tooling probe.

---

## 0. Reconciliation: draft vs. reality (read this first)

The `.ai-roster/` draft was written against a fictitious stack and has structural gaps. Both
must be fixed before the Architect synthesizes the final plan.

### 0.1 Stack errors in the draft

| Draft says | Reality | Files affected |
|---|---|---|
| "Vue.js PWA", Vuex/Pinia | Next.js 15 + React 19, no Vuex/Pinia exists | `architect_instructions.md`, `frontend_instructions.md` (referenced in `team.yaml` but the file doesn't exist on disk — see 0.3), `pm_instructions.md` |
| `gemini-3.5-flash` for engineers/QA | No constraint against Gemini in Antigravity, but model choice should follow CLAUDE.md §11 cost/complexity matrix, and Antigravity's actual available models should be confirmed, not assumed | `team.yaml` |
| "Never modify frontend Vue components" (backend engineer rule) | Should read "Never modify `apps/web` Next.js code" | `backend_instructions.md` |
| `<script setup>` Composition API | Should be Next.js 15 App Router / React 19 Server & Client Components, Tailwind, the existing `packages/ui` design system | `frontend_instructions.md` (missing file) |
| No mention of Fastify, BullMQ, Drizzle, Postgres/pgvector, Redis at all | These are the actual systems engineers touch | all `*_instructions.md` |
| No mention of CLAUDE.md hard constraints (no-mocking, dark-mode tokens, atomic commits, jsonb/`safeParseJson`) | These are non-negotiable project rules that every engineer/QA prompt must inherit | all `*_instructions.md` |

**Recommendation:** Every role's instructions file must open with a short "stack contract"
block that restates: pnpm monorepo, Fastify API :3001, Next.js/React web :3000, BullMQ worker,
Postgres 12+pgvector :5432, Redis :6379, Drizzle ORM, plus a pointer to read `CLAUDE.md` in
full before doing anything. Treat `CLAUDE.md` as the constitution; the per-role instructions
are bylaws that must not contradict it.

### 0.2 Tooling gaps (noted for DevOps, not fixed here)

- `gh` CLI: **not installed** (`which gh` → not found). `github_flow.sh` and any MCP-less flow
  hard-depend on it. Either install `gh` or fully commit to GitHub MCP tools (`create_issue`,
  `add_comment`, `update_issue`, `list_issues`) as the only interface — **don't require both**.
  Recommend: MCP for Claude Code roles (PM, Architect, Researcher, Architect-review), `gh` CLI
  for Antigravity engineer/QA roles that operate from a shell skill. This is exactly the
  split the draft already implies — just needs `gh` actually installed for it to work.
- `js-yaml`: contrary to the brief, it **is present** in the workspace (hoisted via pnpm,
  `js-yaml@4.2.0` under `node_modules/.pnpm`). `scripts/sync-agents.js`'s `require('js-yaml')`
  should resolve. Worth a real `node .ai-roster/scripts/sync-agents.js` smoke test before
  trusting it, but it is not a blocking dependency gap the way the brief assumed.
- `team.yaml` references `frontend_instructions.md`, but only `backend_instructions.md` exists
  on disk — the frontend engineer's instructions file is **missing entirely**. This will make
  `sync-agents.js` throw on `fs.readFileSync`. Flag for Engineer/DevOps wave.
- Antigravity already has a real worktree convention on this machine:
  `~/.gemini/antigravity/worktrees/careeree/<branch-name>/` (confirmed: one exists for
  `implement-wave-six-features`). The new design should **adopt this exact convention** rather
  than invent a new worktree path scheme, so Antigravity-native and Claude-Code-native agents
  converge on the same physical layout.

### 0.3 Process gaps in the draft

The draft's state machine (`1-needs-spec → 2-ready-for-dev → 3-in-review → 4-qa-failed`) is
missing 3 of the 7 stages the founder asked for:

1. No **Researcher** stage/label at all (jumps straight to spec).
2. No **PM verification** stage (interactive testing of acceptance criteria) — QA in the draft
   does browser verification, but that's QA-against-spec, not PM-against-founder-intent. These
   are different concerns and must be different stages.
3. No **Architect code-review / accept-or-reject** stage, and therefore **no reject→re-engineer
   loop**. The draft's QA can bounce to `4-qa-failed`, but nothing ever routes back from QA-pass
   to a human-equivalent design authority for final accept. This is the most important missing
   piece — it's explicitly called out in the brief as stage 7.
4. `github_flow.sh submit` runs `git add .` — **directly violates CLAUDE.md §1.3 / §5**
   ("Never `git add -A` or `git add .`... atomic commits, specific filenames only"). This script
   must be rewritten to stage explicit paths (e.g., via `git diff --name-only` allowlist or the
   engineer explicitly listing files it touched).
5. No worktree usage anywhere — `github_flow.sh start` just does `gh issue develop --checkout`,
   which checks out a branch **in the current working directory**, not an isolated worktree.
   That makes true engineer-team parallelism unsafe (two engineers checking out two branches in
   the same directory stomp each other). This must be fixed at the design level (see §6).
6. No `agent:claimed` / ownership-lock mechanism — nothing stops two parallel engineer agents
   from both picking issue #42 off the same `status:2-ready-for-dev` list.

These six gaps are addressed concretely below.

---

## 1. Issue state machine

### 1.1 Label set (status: track)

Extending the draft's 4 labels to the full 7-stage loop plus the reject cycle:

```
status:0-intake          # PM is drafting/splitting; not yet a finished atomic issue
status:1-needs-research  # PM has filed it; Researcher should investigate
status:2-needs-spec      # Research attached (or skipped as N/A); Architect should design
status:3-ready-for-dev   # Spec attached; open for Engineer claim
status:4-in-progress     # An Engineer has claimed it and is actively working (worktree exists)
status:5-in-review       # PR opened; awaiting QA
status:6-qa-failed       # QA found defects vs. spec; bounced back to Engineer
status:7-qa-passed       # QA confirms it matches spec; awaiting PM verification
status:8-pm-rejected      # PM verification failed against founder intent/acceptance criteria; bounced back to Engineer
status:9-pm-verified     # PM confirms acceptance criteria met from the running app
status:10-arch-rejected  # Architect code review rejected the implementation; bounced back to Engineer
status:11-done           # Architect accepted; ready to merge / merged
```

This is long, but each status is a distinct accountable handoff and the founder explicitly
asked for traceability through every one of the 7 stages plus both loop-back paths. Collapsing
stages to "save labels" is exactly what caused stages to go missing in the v1 draft.

**Naming note:** keep the `status:<n>-<slug>` numeric-prefix convention from the draft — it
sorts correctly in GitHub's label UI and in `gh issue list --label` output, and numeric prefixes
make "is this issue stuck/skipped a stage" trivially greppable.

### 1.2 ASCII state diagram

```
                         ┌─────────────────────────────────────────────────────┐
                         │                                                     │
   founder feedback      │                                                     │
        │                │                                                     │
        v                │                                                     │
 ┌──────────────┐         │                                                     │
 │ 0-intake     │ (PM)     │                                                     │
 └──────┬───────┘         │                                                     │
        │ PM splits into atomic stories, opens issue                            │
        v                                                                       │
 ┌──────────────────┐                                                          │
 │ 1-needs-research  │ (Researcher)                                             │
 └──────┬────────────┘                                                          │
        │ Researcher attaches findings comment                                  │
        v                                                                       │
 ┌──────────────┐                                                              │
 │ 2-needs-spec │ (Architect)                                                   │
 └──────┬───────┘                                                              │
        │ Architect attaches technical blueprint                                │
        v                                                                       │
 ┌──────────────────┐                                                          │
 │ 3-ready-for-dev   │ <───────────────────────────────────────────┐            │
 └──────┬────────────┘                                              │            │
        │ Engineer claims (agent:claimed:<id> label + assignee)     │            │
        v                                                            │            │
 ┌──────────────────┐                                               │            │
 │ 4-in-progress     │ (Engineer, in isolated git worktree)          │            │
 └──────┬────────────┘                                               │            │
        │ PR opened, issue linked                                   │            │
        v                                                            │            │
 ┌──────────────┐                                                   │            │
 │ 5-in-review  │ (QA)                                               │            │
 └──────┬───────┘                                                   │            │
        │                                                            │            │
        ├── fail ──> ┌──────────────┐ ─── re-claim ──────────────────┘            │
        │            │ 6-qa-failed  │                                             │
        │            └──────────────┘                                            │
        │                                                                        │
        │ pass                                                                   │
        v                                                                        │
 ┌──────────────┐                                                                │
 │ 7-qa-passed  │ (PM verification)                                              │
 └──────┬───────┘                                                                │
        │                                                                        │
        ├── fail ──> ┌──────────────────┐ ─── re-claim ─────────────────────────┘
        │            │ 8-pm-rejected     │
        │            └──────────────────┘
        │
        │ pass
        v
 ┌──────────────────┐
 │ 9-pm-verified     │ (Architect code review)
 └──────┬────────────┘
        │
        ├── reject ──> ┌──────────────────┐ ─── re-claim ──────────────────────────┐
        │              │ 10-arch-rejected  │                                       │
        │              └──────────────────┘                                       │
        │                                                                          │
        │ accept                                                                   │
        v                                                                          │
 ┌──────────────┐                                                                  │
 │ 11-done      │ → PR merged, worktree removed, branch deleted                    │
 └──────────────┘                                                                  │
                                                                                    │
        (any of 6-qa-failed / 8-pm-rejected / 10-arch-rejected route back to ──────┘
         3-ready-for-dev or directly to 4-in-progress if same engineer resumes
         the same worktree/branch — see transition table for the distinction)
```

### 1.3 Transition table

| # | From state | Actor | Action | To state | Notes |
|---|---|---|---|---|---|
| 1 | (none) | PM | Splits founder feedback into atomic story, `create_issue` | `0-intake` | One issue per atomic story (see §5 rubric) |
| 2 | `0-intake` | PM | Confirms story is well-formed, applies label | `1-needs-research` | If trivially small/no unknowns, PM may skip straight to `2-needs-spec` and note why in a comment |
| 3 | `1-needs-research` | Researcher | Investigates, posts findings comment (template §2), swaps label | `2-needs-spec` | If research concludes the feature is too complex for this iteration, Researcher instead comments and re-labels to `0-intake` with a "needs PM re-scope" note (not shown in diagram — rare path) |
| 4 | `2-needs-spec` | Architect | Posts technical blueprint comment, swaps label | `3-ready-for-dev` | Blueprint must name exact files/migrations per §2 |
| 5 | `3-ready-for-dev` | Engineer | Atomically claims: adds `agent:claimed:<engineer-id>` label + self-assigns + comments "claiming", creates worktree+branch | `4-in-progress` | Claim must be a single atomic GitHub API call sequence with a check-then-act guard (see §3.4) to avoid two engineers racing |
| 6 | `4-in-progress` | Engineer | Implements, commits atomically, opens PR linked to issue (`Closes #N` or `Fixes #N` only used at final merge — during loop use `Refs #N` to avoid premature auto-close) | `5-in-review` | Engineer updates issue with "Engineer Notes" section (§2) before flipping label |
| 7 | `5-in-review` | QA | Pulls branch, runs real stack (Postgres/Redis/API/Web), Playwright screenshots, posts QA report | `7-qa-passed` (pass) or `6-qa-failed` (fail) | QA never touches PM-verification or Architect-review concerns — QA checks impl ↔ spec, not impl ↔ founder-intent |
| 8 | `6-qa-failed` | Engineer | Removes own claim is NOT needed (same engineer/branch resumes) — fixes code in same worktree, force-pushes branch, re-requests QA | `5-in-review` | If original engineer is unavailable/stuck, issue can be re-claimed by a different engineer from `6-qa-failed` directly (skips re-spec) |
| 9 | `7-qa-passed` | PM | Pulls branch (or uses QA's already-running env), interactively exercises the feature against the issue's Acceptance Criteria from the founder's original wording | `9-pm-verified` (pass) or `8-pm-rejected` (fail) | This is the stage missing from the draft — distinct from QA: QA = "matches spec," PM = "matches founder intent" |
| 10 | `8-pm-rejected` | Engineer | Same loop-back as #8 | `5-in-review` (after fix) | PM rejection comment must quote the specific acceptance criterion that failed |
| 11 | `9-pm-verified` | Architect | Reviews the actual diff (`git diff`/PR files) for code quality, architecture fit, CLAUDE.md rule compliance (tokens, jsonb, migrations) | `11-done` (accept) or `10-arch-rejected` (reject) | This is the stage missing from the draft — Architect-as-final-gate, mirroring the existing wave-gate's "Architect produces GO/NO-GO" |
| 12 | `10-arch-rejected` | Engineer | Same loop-back as #8/#10 | `5-in-review` (after fix) | Loop can in theory repeat; recommend a 3-strike escalation to PM/founder if same issue rejects 3× (see §7 risks) |
| 13 | `11-done` | Engineer or Architect | Merge PR, delete branch, remove worktree | (issue closed) | `gh issue close` only happens here — never earlier, so the issue stays the single source of truth through the whole loop |

**Key design choice:** rejections from QA/PM/Architect do **not** reset to `3-ready-for-dev`
(which would let a different engineer pick it up mid-flight and lose context) — they return to
`5-in-review`'s predecessor implicitly by letting the **same** engineer resume the **same**
worktree/branch. Only if an issue is explicitly abandoned (engineer agent times out / crashes)
should an orchestrator manually reset the label to `3-ready-for-dev` and clear the
`agent:claimed:*` label so a fresh engineer can re-claim.

---

## 2. Issue template / schema

### 2.1 Issue forms vs. body template — recommendation

Use **both**, in layers:
- **GitHub Issue Forms** (`.github/ISSUE_TEMPLATE/*.yml`) for the **PM-intake** form only. This
  gives the PM (or founder, if they ever file directly) a structured front door — forces
  `user_story`, `acceptance_criteria`, `area` to be filled before an issue can be created. Issue
  Forms render to a normal markdown body underneath, so every downstream role still just reads
  issue body + comments.
- **A fixed body template with named markdown sections** for everything that gets *appended*
  after creation (Research Notes, Architect Spec, Engineer Notes, QA Results, PM Verification,
  Architect Verdict). GitHub Issue Forms can't post-hoc edit structured fields per-section the
  way agents need to — agents should **append a clearly delimited markdown section** via
  `add_comment` (preferred) or by editing the issue body to inject a section (riskier — body
  edits can race). **Recommendation: every role after PM posts a COMMENT, never edits the
  body.** The issue body stays PM-authored and stable; the comment thread is the append-only
  audit log GitHub already version-controls. This avoids merge-conflict-style races between
  concurrent agents and matches "documented and traceable in the issues themselves."

### 2.2 Issue Form (PM intake) — concrete YAML

`.github/ISSUE_TEMPLATE/feedback-story.yml`:

```yaml
name: Feedback-derived User Story
description: An atomic story split from founder feedback by the PM agent.
title: "[Story] <short imperative summary>"
labels: ["status:0-intake"]
body:
  - type: textarea
    id: user_story
    attributes:
      label: User Story
      description: "As a <role>, I want <capability>, so that <benefit>."
      placeholder: "As a premium user, I want my goal hierarchy to persist across sessions, so that I don't lose progress."
    validations:
      required: true
  - type: textarea
    id: founder_quote
    attributes:
      label: Founder Feedback (verbatim)
      description: Paste the exact founder words this story traces back to. Never paraphrase.
    validations:
      required: true
  - type: textarea
    id: acceptance_criteria
    attributes:
      label: Acceptance Criteria
      description: Checklist of concrete, testable, observable outcomes.
      value: |
        - [ ]
        - [ ]
    validations:
      required: true
  - type: dropdown
    id: area
    attributes:
      label: Area
      multiple: true
      options:
        - frontend (apps/web)
        - backend (apps/api)
        - worker (apps/worker)
        - db (packages/db / migrations)
        - shared (packages/shared, packages/ui)
    validations:
      required: true
  - type: dropdown
    id: priority
    attributes:
      label: Priority
      options: [P0-blocker, P1-high, P2-normal, P3-low]
    validations:
      required: true
  - type: checkboxes
    id: invest_check
    attributes:
      label: PM Atomicity Self-Check (INVEST)
      options:
        - label: Independent — no hard dependency on another open story to be testable
        - label: Negotiable — solution approach isn't over-specified here (that's the Architect's job)
        - label: Valuable — traces to a specific founder complaint, not an inferred nice-to-have
        - label: Estimable — an engineer could give a rough size after reading the spec
        - label: Small — completable as one engineer, one branch, one PR
        - label: Testable — acceptance criteria above are objectively checkable
```

### 2.3 Comment-section template (used by every downstream role)

Each role posts ONE comment using this exact heading so the issue is greppable via
`gh issue view <n> --comments` or the GitHub search API (`in:comments`):

```markdown
## 🔎 Research Notes — <date>, agent: researcher
**Findings:**
- ...
**Relevant existing code:** `apps/api/src/...`, `packages/db/...`
**Rate limits / external constraints:** ...
**Recommendation:** proceed as-scoped / simplify to: ...
```

```markdown
## 🏗️ Architect Spec — <date>, agent: architect
**Files to create/modify:**
- `apps/api/src/routes/...` — ...
- `packages/db/migrations/00NN_....sql` — ...
**API contract:** `POST /v1/...` → `{ ... }`
**DB changes:** column types, Drizzle schema diff, migration number (verify next-free at build time per CLAUDE.md §7)
**Data flow:** ...
**CLAUDE.md rules that apply:** [ ] dark-mode tokens [ ] safeParseJson [ ] migration applied to both DBs [ ] no mocking
**Out of scope:** ...
```

```markdown
## ⚙️ Engineer Notes — <date>, agent: <engineer-id>, branch: issue-<n>-<slug>, worktree: <path>
**Implemented:** ...
**Commits:** <sha> "<message>", <sha> "<message>"  (atomic, specific filenames per CLAUDE.md §5)
**Deviations from spec (if any) and why:** ...
**Live verification performed:** `curl ...` output / `psql` query result (paste real output, not assumed)
**PR:** #<pr-number>
```

```markdown
## 🧪 QA Results — <date>, agent: qa
**Verdict:** PASS / FAIL
**Tests run:** Vitest suite(s), Playwright scenarios
**Screenshots:** docs/.../screenshots/issue-<n>-<state>.png (light + dark mode)
**Spec compliance:** acceptance-criteria-by-acceptance-criteria checklist
**If FAIL:** exact repro steps + expected vs actual
```

```markdown
## ✅ PM Verification — <date>, agent: pm
**Verdict:** PASS / FAIL
**Acceptance criteria walkthrough:** (quote each AC from the issue body, mark met/unmet from the *running app*, not the report)
**Founder-intent check:** does this actually resolve the founder-quote above, in spirit?
**If FAIL:** which AC failed and what was observed instead
```

```markdown
## 🛡️ Architect Verdict — <date>, agent: architect
**Verdict:** ACCEPT / REJECT
**Code review findings:** architecture fit, CLAUDE.md rule compliance, security, reuse/simplification
**If REJECT:** specific file:line findings the engineer must address
```

---

## 3. Labels taxonomy

### 3.1 Status labels (mutually exclusive, exactly one per issue)
`status:0-intake` … `status:11-done` (full list in §1.1). Color-code by stage group:
gray (intake/research) → blue (spec) → yellow (dev/in-progress) → purple (review) → red
(any `*-failed`/`*-rejected`) → green (`*-verified`/`done`).

### 3.2 Role/area labels (multi-select, orthogonal to status)
```
area:frontend     # apps/web
area:backend      # apps/api
area:worker       # apps/worker (BullMQ)
area:db           # packages/db, migrations
area:shared       # packages/shared, packages/ui, packages/config
area:llm          # OpenAI agent-graph changes (chat/embeddings model map)
area:infra        # CI, tooling, .ai-roster itself
```
An issue can carry multiple `area:*` labels (e.g., a feature touching both `area:backend` and
`area:db`), but each PR/worktree should ideally touch only the areas listed — if an engineer
finds they need an unlisted area, that's a signal the Architect under-scoped the spec (kick
back a comment, don't silently expand scope).

### 3.3 Priority labels
```
priority:P0-blocker
priority:P1-high
priority:P2-normal
priority:P3-low
```

### 3.4 Parallel ownership / claim labels — concrete mechanism

The draft has **no claim mechanism**, which is the single biggest gap for "a TEAM of engineer
agents... IN PARALLEL." Recommended design:

```
agent:claimed:<engineer-id>     # e.g. agent:claimed:eng-1, agent:claimed:eng-2
```

**Claim protocol (must be followed exactly to avoid a race):**

1. Engineer agent lists issues with `status:3-ready-for-dev` AND no `agent:claimed:*` label.
2. Engineer picks ONE candidate issue.
3. Engineer immediately attempts the claim as a single check-then-act sequence:
   ```bash
   gh issue view <n> --json labels -q '.labels[].name' | grep -q '^agent:claimed:' && exit 1
   gh issue edit <n> --add-label "agent:claimed:eng-1" --add-label "status:4-in-progress" --remove-label "status:3-ready-for-dev"
   gh issue comment <n> --body "Claimed by eng-1 at $(date -u +%FT%TZ). Worktree: ../careeree-issue-<n>"
   ```
4. **This is not perfectly race-proof** (GitHub has no atomic compare-and-swap on labels) — two
   engineers could both pass the check in the same instant. Mitigation: immediately *after*
   adding the label, re-fetch the issue and confirm `agent:claimed:eng-1` is the ONLY
   `agent:claimed:*` label present. If a second engineer's label is also present (both raced),
   the engineer with the **alphabetically/numerically later** ID backs off, removes its own
   label, and picks the next issue. This is a "last writer loses" tiebreak — cheap, deterministic,
   no external lock service needed. Given GitHub's API latency (~100-300ms), the collision
   window is small but nonzero with >2 parallel engineers; for this team's scale (a "team" of a
   few engineer agents, not dozens), this is acceptable. If the founder later runs 10+ parallel
   engineers, revisit with a real lock (e.g., a GitHub Projects "in progress" column with a
   single-writer bot, or a tiny Redis-backed lock service — Redis is already in the stack).
5. On abandonment (engineer crashes / times out without submitting), an orchestrator/PM sweep
   job should detect issues stuck in `status:4-in-progress` with no commit activity for >N hours
   and reset them (remove `agent:claimed:*`, revert to `status:3-ready-for-dev`).

### 3.5 Other labels
```
needs:research        # explicit signal Researcher found unresolved unknowns (rare, supplements stage)
blocked               # depends on another open issue — link via "Blocked by #N"
founder-priority      # founder explicitly called this out by name (traceability marker)
```

---

## 4. Per-role responsibilities & handoffs (mapped to real stack)

| Role | Environment | Why this environment | Reads | Writes | Real-stack touchpoints |
|---|---|---|---|---|---|
| **PM (intake)** | Claude Code | Needs deep familiarity with `CLAUDE.md`, prior `docs/feedback-round-2/00-feedback-log.md`-style founder traceability, and judgment about atomicity — best served by Claude's longer-context reasoning, run by the founder directly in their primary CLI | Founder's pasted feedback, existing open issues (avoid dupes) | New issues via Issue Form, `status:0-intake`→`1-needs-research` | None directly — pure planning |
| **Researcher** | Claude Code | Needs `WebSearch`/`WebFetch` for external API docs and the ability to read across the whole monorepo for "does this already exist" — Claude Code's tool surface fits | Issue body, repo source (e.g., `apps/api/src/agents/` for the 7 OpenAI agent graphs), external docs | Research Notes comment, label → `2-needs-spec` | Read-only `psql`/schema inspection if researching DB feasibility |
| **Architect (spec)** | Claude Code | Cross-cutting reasoning about Drizzle schema, Fastify route structure, BullMQ job design — needs the same model tier as the existing wave-gate Architect (CLAUDE.md §11: "complex/cross-cutting" → opus) | Issue + Research Notes, `packages/db` schema, `apps/api` route conventions | Architect Spec comment, label → `3-ready-for-dev` | Read-only schema/migration inspection |
| **Engineers (frontend/backend/worker)** | Antigravity (per draft) — **recommend confirming this is deliberate, not accidental** | Antigravity already has a native worktree convention on this machine (`~/.gemini/antigravity/worktrees/careeree/<branch>/`) and is positioned as the "many cheap parallel workers" tier in the draft (`gemini-3.5-flash`, low thinking). Running engineers in Antigravity, planning/review in Claude Code, mirrors a cheap-execution/expensive-judgment split. **Caveat:** model choice should still follow CLAUDE.md §11 — a "moderate, well-defined plan" tier model, not necessarily the absolute cheapest available. Verify Antigravity's actual model menu before locking `gemini-3.5-flash` into `team.yaml` — that exact model name should be confirmed against Antigravity's current model list, not assumed correct. | Architect Spec section, relevant area of repo | Code, atomic commits, Engineer Notes comment, PR, label → `5-in-review` | **Full real stack**: starts local Postgres/Redis/API/worker/web per CLAUDE.md §3, runs real migrations against `careeree` (dev) — never against prod data |
| **QA** | Antigravity (`browser_agent` skill) | Needs live browser automation (Playwright-equivalent) against the running app — Antigravity's `browser_agent` skill is built for this; matches CLAUDE.md §8's "always render and screenshot" discipline | PR diff, Architect Spec (to check impl ↔ spec), issue acceptance criteria (for awareness, not final verdict) | QA Results comment, screenshots, label → `7-qa-passed`/`6-qa-failed` | Full real stack; must restore dev user `tier=premium` after tests per CLAUDE.md §10 |
| **PM (verification)** | Claude Code (founder-run) or Antigravity | This is explicitly an *interactive* human-in-the-loop-flavored check against founder intent — recommend this runs wherever the **founder** is sitting, since the brief frames it as the founder's proxy ("performs an interactive testing/verification round"). Default: Claude Code, since PM-intake already lives there and continuity of "what did the founder actually mean" context matters | Issue body (founder-quote field), acceptance criteria, running app | PM Verification comment, label → `9-pm-verified`/`8-pm-rejected` | Exercises the **running app** directly — never trusts the QA report alone, per CLAUDE.md §8 ("PM reviews MUST confirm against the running app, not reports") |
| **Architect (code review)** | Claude Code | Final code-quality/architecture gate — needs to read full diffs, check CLAUDE.md rule compliance (token usage, migration discipline, atomic commits), same model tier as spec-Architect | PR diff (`git diff`, `gh pr diff`), Architect Spec (compare as-built vs as-specced) | Architect Verdict comment, label → `11-done` or `10-arch-rejected` | Read-only; may run targeted `psql`/`curl` to spot-check engineer's "Live verification performed" claims (CLAUDE.md §1.4 "Trust but verify" applies to agents reviewing agents too) |

**Handoff discipline:** every label transition MUST be accompanied by a comment using the
template in §2.3 — a bare label flip with no comment is treated as an incomplete handoff and
should be flagged by whichever agent picks up the issue next ("predecessor comment missing,
halting").

---

## 5. Atomic-story rubric (INVEST-style, Careeree-specific)

The PM uses these concrete tests before creating an issue. If any fails, the PM must split,
merge, or re-scope before filing.

1. **Independent:** Can this be built and merged without another *currently-open* issue landing
   first? Example pass: "Add `goal.archived_at` column + archive endpoint" is independent of
   "Show archived goals in UI" only if the UI story explicitly depends on the column existing —
   in which case it's NOT independent and should either (a) be sequenced with `Blocked by #N`,
   or (b) be merged into one issue if genuinely inseparable (e.g., a migration with no
   meaningful partial-done state).
2. **Negotiable:** The PM's issue body describes *what* and *why* (per the existing
   `pm_instructions.md` rule, which is correct and should be kept), not *how*. Fail example: PM
   issue says "add a Drizzle `jsonb` column and use `safeParseJson`" — that's Architect's job.
   Pass example: PM issue says "users should be able to archive a goal without losing its
   history."
3. **Valuable:** Every issue's "Founder Feedback (verbatim)" field must be traceable to an
   actual founder quote — if the PM is inferring a need the founder didn't state, flag it
   explicitly as `derived, not founder-stated` rather than presenting it as a direct ask. This
   preserves the founder-traceability discipline that the existing wave-gate PM role already
   does well (CLAUDE.md §4.2, "re-derive founder traceability independently") — just moved from
   a markdown table into the issue's own field.
4. **Estimable:** After the Architect spec lands, an engineer should be able to estimate size
   without further clarifying questions. If a PM-filed issue is so vague the Architect can't
   spec it without going back to the founder, it failed atomicity at intake, not at spec time —
   route back to `0-intake`, don't let it limp through.
5. **Small — concrete Careeree sizing heuristic:** one issue should map to roughly **one PR
   touching ≤ 2 `area:*` labels** and completable by one engineer in one focused session.
   Examples of right-sized splits from how this project actually works:
   - "Premium gating is inconsistent" (a real founder-shaped complaint) splits into:
     - Issue A (`area:backend`): "Add a single `hasEntitlement()` helper checking both
       `users.tier` and Redis `tier:<id>` consistently" (per CLAUDE.md §9, "Two entitlement
       sources").
     - Issue B (`area:frontend`): "Replace ad-hoc premium checks in `apps/web` with the new
       helper's API response shape."
     - These are NOT independent (B depends on A's contract), so B gets `Blocked by #A`.
   - "Dark mode is broken in the goal tree" splits by *component*, not by *token* — one issue
     per visibly-broken component (per CLAUDE.md §6 discipline), not one giant "fix all dark
     mode" mega-issue that never closes.
6. **Testable:** Acceptance criteria must be observable from the running app (a `curl` response
   shape, a Playwright-visible state, a `psql` row) — never "code looks correct," which is
   exactly the failure mode CLAUDE.md §1.1 was written to prevent ("never assumed from source
   code").

**Anti-pattern to explicitly reject:** an issue titled after a *founder feedback session* (e.g.
"Feedback Round 3 — performance") rather than after a *single story*. The wave-docs system did
this (one big markdown doc per round); the whole point of moving to issues is to break that
monolith into independently-flowing atomic units. The PM's first job is literally the opposite
of what the old `02-product-requirements.md` style produced.

---

## 6. Best-practice research: multi-agent GitHub-issue orchestration, worktrees, issue-as-source-of-truth

*(Synthesized from known patterns as of this model's training; treat specifics as directional,
not verified against current GitHub API docs — recommend the Architect/DevOps teammate
spot-check rate limits and current `gh`/MCP capabilities before locking the design.)*

- **Issue-as-source-of-truth pattern:** treating the GitHub issue thread as the only persistent
  state (no parallel markdown docs, no agent memory of "what happened last time") is the same
  principle behind ChatOps and "GitOps for product work" — the system of record must be
  queryable by any fresh agent with zero prior context, which is also exactly this project's
  existing "independent subagent with fresh context" rule (CLAUDE.md §4.1). The issue comment
  thread *is* the wave-gate report directory, just hosted on GitHub instead of in `docs/`. This
  is a clean, low-risk translation of an already-proven internal pattern — the main risk is
  losing the discipline of "write a structured report," not the platform change itself.
- **Git worktrees for parallel agents:** the standard pattern (also already used by this
  project's Antigravity setup) is one worktree per active branch under a shared parent
  directory, e.g. `<repo>-worktrees/issue-<n>-<slug>/`, each with its own `node_modules`
  (or a shared pnpm store via `pnpm install` per worktree — pnpm's content-addressable store
  makes this cheap) and its **own copy of `.env`** files (critical: each worktree needs API/DB
  env vars; recommend symlinking `.env` rather than duplicating secrets). Each worktree is
  fully isolated for git operations (different branch checked out simultaneously) but **shares
  the same Postgres/Redis instances** unless the design explicitly wants per-worktree DB
  isolation (it shouldn't, for this project — `careeree_test` is already the shared test DB
  per CLAUDE.md §9, and running N Postgres instances for N parallel engineers is unnecessary
  overhead at this team's scale). The real isolation engineers need is *port* isolation if two
  engineers both want to run `next dev` — recommend each worktree's engineer picks a free port
  in a reserved range (e.g., 3010-3019 for web, 3101-3109 for API) recorded in its Engineer Notes
  comment, OR (simpler) engineers don't run the full stack themselves at all and rely on QA's
  single shared running instance for end-to-end checks, doing only unit-level verification
  locally. **Recommend the latter** — it matches CLAUDE.md's existing single-shared-stack
  convention and avoids port-collision bugs entirely.
- **Rate-limit concerns:** GitHub's REST/GraphQL API has a 5,000 req/hr authenticated limit per
  token. With N engineer agents polling `gh issue list` on a loop, plus QA/PM/Architect each
  doing their own polling, this is reachable but not trivially exceeded at this team's scale (a
  handful of agents, not dozens). Recommend: (a) agents poll on a backoff schedule, not tight
  loops, (b) the existing `github_flow.sh fetch`'s `--limit 5` cap is good practice and should be
  kept/extended to all `list` calls, (c) prefer GitHub MCP tools where available since MCP
  servers can cache/batch more intelligently than raw `gh` shell calls.
- **Context-bloat concerns:** issue threads will accumulate Research/Spec/Engineer/QA/PM/
  Architect comments across possibly multiple reject-loops. By the time an issue reaches its
  3rd `arch-rejected` cycle, the thread could be long enough to matter for a fresh agent's
  context window. Recommend: (a) each comment template stays terse and link-based (paste a
  `psql`/`curl` *result*, not a transcript), (b) on re-claim after rejection, the resuming
  engineer agent should be instructed to read only the **latest** comment of each role-type
  (most recent Architect Spec, most recent rejection reason) rather than the full thread
  history — the orchestrating prompt should say this explicitly, since "read the whole issue"
  is the naive default that will bloat fast.
- **Antigravity ↔ Claude Code interop:** since both must be able to drive this workflow, the
  state machine and labels are the *only* shared contract — neither tool should assume the
  other's internal session/agent format. This is already implicit in the draft's
  `sync-agents.js` (exports the same `team.yaml` to both Claude custom-commands and Antigravity
  `agent.json`), which is the right idea; the gap is only that the underlying instructions need
  the stack fixes from §0 and the new labels from §1/§3.

---

## 7. Open questions / risks for the founder

1. **Model selection for Antigravity engineers/QA is unverified.** `gemini-3.5-flash` is named in
   the draft `team.yaml` but was not confirmed against Antigravity's actual current model menu
   in this research pass (no Antigravity environment access from this session). Verify before
   the DevOps/Architect wave locks it in — if wrong, `sync-agents.js`'s generated `agent.json`
   files will silently fail at runtime.
2. **`gh` CLI is not installed** in this Claude Code environment. If any Claude-Code-side role
   is expected to also shell out to `gh` (vs. pure MCP), that role will fail until it's
   installed. Recommend deciding definitively: Claude-Code roles use **MCP only**,
   Antigravity roles use **`gh` CLI only** (matches the draft's actual skill assignment) — and
   install `gh` specifically wherever Antigravity's skill execution environment runs, not
   necessarily in this Claude Code sandbox.
3. **Claim-race mitigation (§3.4) is best-effort, not atomic.** GitHub has no compare-and-swap
   primitive for labels. Acceptable at "a few parallel engineers" scale; revisit if the founder
   wants double-digit parallel engineers.
4. **Reject-loop escalation has no circuit breaker.** Nothing in the current design stops an
   issue from cycling `5-in-review → 6/8/10-*-rejected → 5-in-review` indefinitely if an
   engineer agent keeps making the same mistake. Recommend a hard rule: after 3 rejections from
   any single gate (QA, PM, or Architect) on the same issue, auto-escalate to
   `founder-priority` + a comment requesting human (founder) review, rather than looping forever.
   **This needs founder sign-off on the strike count.**
5. **Worktree cleanup ownership is undefined.** Who deletes the worktree and branch after
   `11-done`? Recommend the Architect (final accepter) or a lightweight post-merge hook does
   this — but a stale-worktree sweep policy (e.g., remove worktrees for issues closed >7 days)
   should be decided explicitly, or `~/.gemini/antigravity/worktrees/careeree/` will accumulate
   indefinitely (already has at least one long-lived entry: `implement-wave-six-features`).
6. **PM-verification stage's environment is ambiguous by design (§4)** — recommend the founder
   confirm whether they personally want to be the human triggering this stage, or whether a PM
   *agent* should do it autonomously by treating acceptance criteria as a checklist against a
   live Playwright run. The brief's wording ("performs an interactive testing/verification
   round") reads as founder-involved; if so, this stage may need to stay **synchronous /
   manual-trigger** rather than fully autonomous like the other 6 stages — a meaningfully
   different automation posture that the Architect's final plan should call out explicitly, not
   bury as "just another agent."
7. **`frontend_instructions.md` is missing from disk** despite being referenced in `team.yaml`
   — this will break `sync-agents.js` today, independent of any redesign. Quick fix, but
   flagging since it's a pre-existing break, not a new-design risk.
8. **`github_flow.sh`'s `git add .`** is a CLAUDE.md violation baked into the current skill
   script and must be rewritten as part of implementation, not treated as cosmetic.

---

## Summary of concrete recommendations (for Architect synthesis)

1. Adopt the 12-state machine in §1 (replacing the draft's 4-state one), with explicit
   `agent:claimed:<id>` labels for parallel-safe engineer claiming (§3.4).
2. Use GitHub Issue Forms only for PM-intake; everything else is an appended, template-shaped
   comment (§2) — never edit the issue body after creation.
3. Rewrite all `*_instructions.md` files to reference the real stack (Fastify/Next.js/Drizzle/
   BullMQ/Postgres+pgvector/Redis) and to open with a CLAUDE.md-compliance contract.
4. Fix `github_flow.sh`'s `git add .` to stage explicit paths; add real `git worktree add`
   support using the existing Antigravity convention
   (`~/.gemini/antigravity/worktrees/careeree/<branch>/`) instead of in-place `--checkout`.
5. Add the missing `frontend_instructions.md` and verify Antigravity's real model menu before
   trusting `gemini-3.5-flash` in `team.yaml`.
6. Decide and document the PM-verification stage's human-involvement posture explicitly — don't
   let it default to "just another autonomous agent" without founder sign-off.
7. Add a 3-strike reject-loop circuit breaker escalating to founder review.
