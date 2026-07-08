# .ai-roster — Tekram Assessment Agent Team

Single place to understand **who does what, in which order, under which rules**. Sources of
truth: [team.yaml](team.yaml) (roster + workflow constants), `*_instructions.md` (per-role
briefs), [rules/](rules/) (team-wide rules injected into **every** agent at sync time),
[skills/github_flow.sh](skills/github_flow.sh) (the state-machine mechanics). Project plan and
priorities: [docs/00-project-management-plan.md](../docs/00-project-management-plan.md).

## The team

| Agent | Runtime / model | Responsibility |
|---|---|---|
| `pm-doc-intake` | claude-code / opus | Seeds the queue **from `docs/`** (plan + design docs): epics + typed work issues, labels, acceptance criteria. Runs at bootstrap and whenever a design doc adds scope. |
| `pm-intake` | claude-code / opus | Turns **founder feedback** into atomic INVEST stories. Used in the feedback/testing round after the build exists. |
| `researcher` | claude-code / sonnet | Reference gathering + first drafts of written deliverables. Posts one greppable "Research Notes" comment per issue. |
| `architect-spec` | claude-code / opus | Part 1 architecture + diagrams, Part 3 schema, Part 2 spec (endpoints, layers, DTOs). Moves code issues `2-needs-spec → 3-ready-for-dev`. |
| `backend-engineer` | opencode / deepseek | Part 2 build in `src/**` + `tests/**` (hard write scope). One issue = one branch = one PR. |
| `web-engineer` | opencode / deepseek | **P4 bonus only** — thin frontend demo in `web/**`, spun up only after all P0–P3 deliverables exist. |
| `qa` | claude-code / sonnet | Code issues only: checks out the PR branch read-only in its own worktree, runs the API + tests for real, verdict `6-qa-failed` / `7-qa-passed`. |
| `pm-verify` | claude-code / opus, **human-triggered** | The founder's gate: scores deliverables against the rubric → `8-pm-rejected` / `9-pm-verified`. |
| `architect-review` | claude-code / opus | Final review. Code: correctness + spec conformance. Docs: cross-document consistency (architecture ↔ schema ↔ DevOps must tell one story). Only stage allowed to close issues → `11-done`. |

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

## A work issue's life, step by step (code issue)

1. `pm-doc-intake` creates the issue (labels: `part-N`, `priority:*`, `type:code`, status) with
   deliverable path, rubric points, and checkbox acceptance criteria; links it in its epic's
   task list (`- [ ] #n` — auto-checks on close).
2. `architect-spec` posts the spec as one comment → `3-ready-for-dev`.
3. Engineer: `github_flow.sh fetch` → `claim <n>` (race-safe) → `start <n>` (branch `issue-<n>`
   in its own persistent worktree + lane env) → build → `submit <n> "<msg>" <files...>`
   (explicit paths, opens PR, → `5-in-review`, releases the lane).
4. `qa`: `qa-checkout <n>` (read-only alias branch, own worktree, own lane) → runs the stack
   from `.lane-env` → verdict label + one `qa-comment` on the PR.
5. `pm-verify` (human-triggered) scores against the rubric.
6. `architect-review` reviews and either rejects (back to the engineer) or merges + closes →
   `11-done`, then `cleanup <n>`.

Doc issues skip 2, 4, 5: drafter claims at step 3 (no lane), reviewer closes at step 6.

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

Everything in [rules/](rules/) binds every agent on every task (injected at sync time):

- [rules/git.md](rules/git.md) — **atomic commits always**, explicit staging, one issue = one
  branch, rebase before submit, no secrets in commits.

Add a rule: drop a `.md` in `rules/`, re-run `npm run sync-agents`.

## Conventions cheat-sheet

- Epics: `epic` + `part-N` labels, tracking-only, never claimable, task-list body.
- Comments: one per handoff, posted on the **work issue** (never the epic); researcher/spec/QA
  comments use their exact greppable headings.
- Only `architect-review` closes work issues; `pm-doc-intake` closes finished epics.
- Env every agent needs: `GH_AGENT_ID=<role-id>` (worktree + claim identity), `GH_TOKEN`
  (repo-scoped PAT), `MAX_LANES` (default 3).

## Bootstrap order (H0–H2)

1. `npm install` (tooling deps: js-yaml) → `npm run sync-agents`
2. `REPO=lelkadi/tekram-delivery-assessment .ai-roster/scripts/bootstrap-labels.sh`
3. `docker compose up -d` (Postgres + Redis, once the compose file lands with the scaffold)
4. Run `pm-doc-intake` to seed epics + issues from `docs/`
5. Start stage agents on their queues
