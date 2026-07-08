# Rubric Checklist (pm-verify reference — score every part against this, not from memory)

Source: [docs/assessment-brief.pdf](../docs/assessment-brief.pdf). Passing score: 75/100.

| Part | Points | Min threshold | Deliverable |
|---|---|---|---|
| 1 — Architecture | 20 | **15** (hard gate) | `docs/architecture.md` + diagrams |
| 2 — Coding Challenge | 25 | **18** (hard gate) | working API + tests, repo/README/API docs |
| 3 — Database Design | 10 | — | `docs/database-schema.md` + ERD |
| 4 — Debugging | 10 | — | `docs/incident-runbook.md` |
| 5 — DevOps | 10 | — | `docs/devops.md` |
| 6 — Leadership | 15 | **10** (hard gate) | `docs/leadership.md` |
| 7 — AI Strategy | 5 | — | `docs/ai-strategy.md` |
| 8 — Documentation | 5 | — | README, setup guide, API docs, deploy guide, `docs/technical-decisions.md` |
| 9 — Business Thinking | Bonus | — | `docs/business-recovery-plan.md` |

**Auto-fail concerns — check every one, every part, regardless of point score:**
- [ ] Weak architecture rationale
- [ ] Poor documentation
- [ ] Lack of testing
- [ ] Ignoring scalability/security
- [ ] Weak CI/CD knowledge
- [ ] Poor business thinking
- [ ] Missing the deadline

**Per-part verification (fill in when scoring):**
- Which ACs from the issue were met / unmet, from the running app or the actual doc content —
  never from a prior stage's self-report.
- Does the hard-gate part clear its minimum on its own, independent of total score?
- Any auto-fail concern present? If yes, this blocks a pass regardless of point total — escalate
  to pm-orchestrator before marking `9-pm-verified`.

Keep this file in sync if the brief changes — it's the single source pm-verify scores against,
so drift here is drift in every score.
