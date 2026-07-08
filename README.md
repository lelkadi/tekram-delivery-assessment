# Tekram Technical Lead Assessment

72-hour technical lead assessment: scalable multi-vertical delivery platform (food delivery, taxi, supermarkets, housekeeping) — architecture, backend coding challenge, database design, debugging, DevOps, leadership, AI strategy, and business thinking.

## Repository layout

- `docs/00-project-management-plan.md` — delivery plan, priorities, 48h timeline
- `docs/architecture.md` — Part 1: system/container/component diagrams, data flows, scalability, security
- `docs/database-schema.md` — Part 3: ERD, DDL, indexes, scaling notes
- `docs/technical-decisions.md` — Part 8: stack + design decisions log (incl. .NET 8/ASP.NET Core, Scalar)
- `docs/` — all other written deliverables (runbooks, DevOps, leadership, AI strategy)
- `.ai-roster/` — multi-agent workflow definition (roles, instructions, GitHub-issue state machine)
- Backend coding challenge (Part 2) lives at the repo root under `src/` once scaffolded

## Workflow

Work is queued as GitHub issues with `status:*` + `priority:*` labels and driven by a multi-agent
team (PM, researcher, architect, backend engineer, QA, reviewers) defined in `.ai-roster/team.yaml`.
See the plan doc for the pipeline each deliverable follows.
