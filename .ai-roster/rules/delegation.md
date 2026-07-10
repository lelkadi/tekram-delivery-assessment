# Delegation Rules (all agents)

1. **Who dispatches whom:** `eng-lead` dispatches `backend-engineer`/`web-engineer` and hands
   off to `architect-review`. QA picks up independently from the issue comment eng-lead posts
   (eng-lead never shells out to QA). No other role dispatches another agent. Engineers never
   call each other; QA and architect-review never dispatch anything, only report a verdict.
2. **Briefs must be self-contained.** Anyone dispatching another agent (currently: `eng-lead`
   only) must give it everything needed — goal, files, spec excerpt, ACs, working directory. The
   receiving agent should never need to open the GitHub issue itself to understand its task.
3. **Verification precedes publication.** Whoever dispatches a task also verifies its output
   (build, tests, a live spot-check) before that output goes anywhere public (push, PR, label
   change, comment). Never relay a worker's self-report as verified fact.
4. **Merge/close authority is exclusive.** Only `architect-review` and
   `architect-review-opencode` merge PRs and close `type:code` issues (dual-gate — first to
   accept wins; either can reject). Only `architect-review` or the drafter's reviewer (per the
   collapsed pipeline) closes `type:doc` issues. `eng-lead` publishes (push + PR + label) but
   never merges.
5. **No silent escalation.** If a brief can't be completed as written (missing spec detail,
   conflicting instruction), the receiving agent stops and reports back — it does not guess, and
   it does not widen its own scope to compensate.
