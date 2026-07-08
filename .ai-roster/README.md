# .ai-roster — Tekram Assessment Agent Team

Single place to understand **who does what, in which order, under which rules**. Sources of
truth: [team.yaml](team.yaml) (roster + workflow constants), `*_instructions.md` (per-role
briefs), [rules/](rules/) (team-wide rules injected into **every** agent at sync time),
[skills/github_flow.sh](skills/github_flow.sh) (the state-machine mechanics). Project plan and
priorities: [docs/00-project-management-plan.md](../docs/00-project-management-plan.md).

## The team

| Agent | Runtime / model | Responsibility |
|---|---|---|
| `pm-orchestrator` | claude-code / opus | Owns priorities and gates (H24/H40-45/H45), cuts scope deliberately. No `Agent`/`Task` tool — directs, never dispatches or codes. |
| `pm-doc-intake` | claude-code / opus | Seeds the queue **from `docs/`** (plan + design docs): epics + typed work issues, labels, acceptance criteria. Runs at bootstrap and whenever a design doc adds scope. |
| `pm-intake` | claude-code / opus | Turns **founder feedback** into atomic INVEST stories. Used in the feedback/testing round after the build exists. |
| `researcher` | claude-code / sonnet | Reference gathering + first drafts of written deliverables. Posts one greppable "Research Notes" comment per issue. |
| `architect-spec` | claude-code / opus | Part 1 architecture + diagrams, Part 3 schema, Part 2 spec (endpoints, layers, DTOs). Moves code issues `2-needs-spec → 3-ready-for-dev`. |
| `eng-lead` | opencode / deepseek-v4-pro | Orchestrates `type:code` issues: fetches, briefs engineers with self-contained tasks, verifies their local commits, publishes (push+PR+label), hands off to QA/architect-review. Never implements. |
| `backend-engineer` | opencode / deepseek-v4-flash | Implements a eng-lead brief in `src/**` + `tests/**` (hard write scope). Commits locally; never pushes, never touches GitHub. |
| `web-engineer` | opencode / deepseek-v4-flash | **P4 bonus only** — implements a eng-lead brief in `web/**`, spun up only after all P0–P3 deliverables exist. |
| `qa` | claude-code / sonnet | Code issues only: checks out the PR branch read-only in its own worktree, runs the API + tests for real, verdict `6-qa-failed` / `7-qa-passed`. Deliberately a different model family than the engineers — reviewing your own implementer's blind spots with the same model is weaker than a second opinion. |
| `pm-verify` | claude-code / opus, **human-triggered** | The founder's gate: scores deliverables against [rubric-checklist.md](rubric-checklist.md) → `8-pm-rejected` / `9-pm-verified`. |
| `architect-review` | claude-code / opus | Final review. Code: correctness + spec conformance. Docs: cross-document consistency (architecture ↔ schema ↔ DevOps must tell one story). Only stage allowed to close issues → `11-done`, and the only one allowed to merge. |

Sync roster → runtimes: `npm run sync-agents` (emits `.claude/agents/*.md` and
`.opencode/agents/*.md`, appending every file in `rules/` to every agent).

## The two pipelines

Issues are labeled `type:doc` or `type:code` at creation (by `pm-doc-intake`); the label decides
the pipeline and whether a runtime lane is needed.

**`type:doc`** (Parts 1, 3–9 — written deliverables; no lane, no QA):

```
3-ready-for-dev → 4-in-progress → 5-in-review → 11-done
   (pm seeds)      (drafter claims)  (submit/PR)   (architect-review closes)
```

Research and spec happen *inside* the draft step. Rework = review comment + move back to
`4-in-progress`; the rejection/QA labels are never used for docs.

**`type:code`** (Part 2 slices, P4 frontend — full state machine):

```
0-intake → 1-needs-research → 2-needs-spec → 3-ready-for-dev → 4-in-progress
  → 5-in-review → {6-qa-failed | 7-qa-passed} → {8-pm-rejected | 9-pm-verified}
  → {10-arch-rejected | 11-done}
```

(`pm-doc-intake` seeds code issues at `2-needs-spec` since the design docs cover research.)

## A work issue's life, step by step (code issue — eng-lead orchestrated)

Engineers never touch GitHub. `eng-lead` owns every `github_flow.sh` call on a code issue
except the engineer's local commit itself; see [eng_lead_instructions.md](eng_lead_instructions.md)
and [rules/delegation.md](rules/delegation.md) for the full contract.

1. `pm-doc-intake` creates the issue (labels: `part-N`, `priority:*`, `type:code`, status) with
   deliverable path, rubric points, and checkbox acceptance criteria; links it in its epic's
   task list (`- [ ] #n` — auto-checks on close).
2. `architect-spec` posts the spec as one comment → `3-ready-for-dev`.
3. `eng-lead`: `fetch` → `claim <n>` → prepares the engineer's worktree
   (`GH_AGENT_ID=backend-engineer start <n>`) → compiles a self-contained brief (goal, files,
   spec excerpt, ACs, working directory) → dispatches (`opencode run --agent backend-engineer
   --cwd <worktree> "<brief>"`, or a `claude` CLI shell-out for a claude-code role).
4. Engineer implements, tests, verifies live, `git commit`s locally, returns a summary. No push,
   no PR, no labels — that's steps 5-6.
5. `eng-lead` independently re-verifies the commit (diff matches brief? tests pass? live
   spot-check?). Fails → new brief to the same engineer, back to step 4. Passes → `publish <n>
   "<summary>"` (push, open PR, → `5-in-review`, release the lane), posting the engineer's
   summary as the handoff comment.
6. `qa`: `qa-checkout <n>` (read-only alias branch, own worktree, own lane) → runs the stack
   from `.lane-env` → verdict label + one `qa-comment` on the PR. Fail → back to `eng-lead`
   step 3 with QA's repro.
7. `pm-verify` (human-triggered) scores against [rubric-checklist.md](rubric-checklist.md).
8. `architect-review` reviews and either rejects (back to `eng-lead`) or merges + closes →
   `11-done`, then `cleanup <n>`. Only role that merges or closes a code issue.

Doc issues skip the eng-lead entirely and steps 6-7: drafter claims directly at step 3 (no
lane), reviewer closes at step 8.

## Parallelism model (how everyone works at once without conflicts)

| Layer | Mechanism |
|---|---|
| Issue ownership | `agent:claimed:<id>` label + read-back tiebreak (`claim`) |
| Files | Per-agent persistent worktrees under `~/.agent-worktrees/tekram-delivery-assessment/<agent-id>`; disjoint `write_scope` globs (`src/**` vs `web/**`) |
| Runtime | Lanes, `MAX_LANES=3`: lane N = API port `30N1`, database `tekram_laneN`, Redis db `N`. `type:doc` issues never take a lane. |
| Infra | One shared `docker compose` stack (Postgres + Redis) on the host — per-lane DB names, **not** per-agent containers (see [docs/technical-decisions.md](../docs/technical-decisions.md) TD-002) |

Stages pipeline across issues: researcher drafts issue B while architect specs issue A; the
engineer builds slice 3 while QA tests slice 2.

## Rules

Everything in [rules/](rules/) binds every agent on every task (injected at sync time — every
agent pays this context cost on every invocation, so keep this directory lean; contrast with
skills below, which cost nothing until actually invoked):

- [rules/git.md](rules/git.md) — **atomic commits always**, explicit staging, one issue = one
  branch, rebase before submit, no secrets in commits.
- [rules/delegation.md](rules/delegation.md) — who may brief/dispatch whom, briefs must be
  self-contained, verification precedes publication, merge/close authority is exclusive to
  `architect-review`.

Add a rule: drop a `.md` in `rules/`, re-run `npm run sync-agents`.

## Skills (loaded on demand, not injected into every prompt)

Unlike `rules/`, these cost nothing unless actually invoked — right for anything that's only
sometimes relevant, so it doesn't tax every other task:

- [skills/github_flow.sh](skills/github_flow.sh) — the state machine (fetch/claim/start/submit/
  publish/qa-checkout/cleanup/lanes).
- [skills/gh-env.sh](skills/gh-env.sh) — source this for a direct `gh` call outside
  `github_flow.sh`; derives `GH_TOKEN` from the untracked repo-local credential file.
- [skills/lane-stack-check.sh](skills/lane-stack-check.sh) — QA/engineer preflight: compose stack
  up? lane database exists? `.lane-env` stale? Run from the worktree before any testing.
  (Browser/screenshot tooling is deliberately absent until P4 frontend work actually starts —
  Part 2 has no UI.)
- [rubric-checklist.md](rubric-checklist.md) — every part's points/threshold/auto-fail concerns
  in one table; `pm-verify` scores against this instead of re-deriving the rubric from memory
  each time.

## Conventions cheat-sheet

- Epics: `epic` + `part-N` labels, tracking-only, never claimable, task-list body.
- Comments: one per handoff, posted on the **work issue** (never the epic); researcher/spec/QA
  comments use their exact greppable headings.
- Only `architect-review` closes work issues; `pm-doc-intake` closes finished epics.
- Env every agent needs: `GH_AGENT_ID=<role-id>` (worktree + claim identity — `GH_TOKEN` is
  self-sourced by `github_flow.sh`/`gh-env.sh` from the untracked repo-local credential file, no
  env setup needed), `MAX_LANES` (default 3).
- Cross-runtime handoffs (opencode ↔ claude-code) are always a CLI shell-out — there's no native
  call between the two products. `eng-lead` (opencode) reaches `qa`/`architect-review`
  (claude-code) via the `claude` CLI, and reaches engineers (opencode) via `opencode run`.

## Bootstrap order (H0–H2)

1. `npm install` (tooling deps: js-yaml) → `npm run sync-agents`
2. `REPO=lelkadi/tekram-delivery-assessment .ai-roster/scripts/bootstrap-labels.sh`
3. `docker compose up -d` (Postgres + Redis, once the compose file lands with the scaffold)
4. Run `pm-doc-intake` to seed epics + issues from `docs/`
5. Start stage agents on their queues; `eng-lead` drives `type:code` issues end-to-end
