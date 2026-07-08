# Tekram Technical Lead Assessment

72-hour technical lead assessment: scalable multi-vertical delivery platform (food delivery, taxi, supermarkets, housekeeping) — architecture, backend coding challenge, database design, debugging, DevOps, leadership, AI strategy, and business thinking.

## Repository layout

- `docs/00-project-management-plan.md` — delivery plan, priorities, 48h timeline
- `docs/` — all written deliverables (architecture, schema, runbooks, leadership, AI strategy)
- `.ai-roster/` — multi-agent workflow definition (roles, instructions, GitHub-issue state machine)
- Backend coding challenge (Part 2) lives at the repo root under `src/` once scaffolded

## Workflow

Work is queued as GitHub issues with `status:*` + `priority:*` labels and driven by a multi-agent
team (PM, researcher, architect, backend engineer, QA, reviewers) defined in `.ai-roster/team.yaml`.
See the plan doc for the pipeline each deliverable follows.
