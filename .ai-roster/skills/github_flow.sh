#!/usr/bin/env bash
#
# github_flow.sh — state-machine skill for the Careeree issue-driven agent workflow.
# Worktree-aware, claim-aware, lane-aware. Used by Claude Code, opencode, and Antigravity agents.
#
# Worktrees are PER AGENT ROLE, not per issue: each $GH_AGENT_ID gets exactly one persistent
# worktree at $WORKTREE_ROOT/$GH_AGENT_ID, reused across every issue that agent handles — `start`
# / `qa-checkout` switch branches inside it instead of creating a fresh worktree + pnpm install
# per issue. GH_AGENT_ID MUST be set to a stable per-role id (web-engineer, backend-engineer,
# worker-engineer, qa) — if left at the default, every role collides on the same worktree.
#
# Env knobs:
#   GH_AGENT_ID     agent role identity for claims/commits/worktree naming (default: eng-1 — set
#                   this explicitly per role, see above)
#   WORKTREE_ROOT   where per-agent worktrees live (default: ~/.agent-worktrees/tekram-delivery-assessment)
#   MAX_LANES       concurrent live-stack lanes (default: 3 = parallel; set 1 to serialize).
#                   Issues labeled type:doc never acquire a lane — they don't run the stack.
#                   Postgres + Redis are one shared docker compose stack on the host; lanes get
#                   isolation via per-lane database names / Redis db numbers, not per-lane containers.
#
# Conventions: repo lelkadi/tekram-delivery-assessment, branch issue-<n> (QA: qa-issue-<n>, see qa-checkout),
# labels status:<n>-<slug> (colon, no space).

set -euo pipefail

REPO="lelkadi/tekram-delivery-assessment"
AGENT_ID="${GH_AGENT_ID:-eng-1}"
WORKTREE_ROOT="${WORKTREE_ROOT:-$HOME/.agent-worktrees/tekram-delivery-assessment}"
LANE_DIR="${LANE_DIR:-$HOME/.agent-worktrees/.lanes}"
MAX_LANES="${MAX_LANES:-3}"

# ── guards ───────────────────────────────────────────────────────────────────
command -v gh >/dev/null 2>&1 || { echo "Error: gh CLI not installed (brew update && brew install gh)"; exit 1; }
gh auth status >/dev/null 2>&1 || { echo "Error: gh not authenticated — run 'gh auth login' or export GH_TOKEN"; exit 1; }
mkdir -p "$WORKTREE_ROOT" "$LANE_DIR"

COMMAND="${1:-help}"; shift || true

# ── lane helpers (serialized v1: MAX_LANES=1 → a single global stack lock) ─────
acquire_lane() {  # $1 = issue ; echoes lane number or fails
  local issue="$1" n
  for n in $(seq 1 "$MAX_LANES"); do
    local lock="$LANE_DIR/lane-$n.lock"
    if ( set -o noclobber; echo "issue=$issue pid=$$ at=$(date -u +%FT%TZ)" > "$lock" ) 2>/dev/null; then
      echo "$n"; return 0
    fi
  done
  echo "Error: all $MAX_LANES lane(s) busy — live stack is in use. Try later or raise MAX_LANES." >&2
  return 1
}
release_lane() {  # $1 = issue : release any lane this issue holds
  local issue="$1" f
  for f in "$LANE_DIR"/lane-*.lock; do
    [ -e "$f" ] || continue
    grep -q "issue=$issue " "$f" && rm -f "$f"
  done
}
lane_env() {  # $1 = lane number → prints lane-scoped env
  local n="$1"
  if [ "$n" = "1" ] && [ "$MAX_LANES" = "1" ]; then
    echo "PORT=3001"; echo "WEB_PORT=3000"
    echo "DATABASE_URL=postgres://localhost:5432/tekram"
    echo "REDIS_URL=redis://localhost:6379/0"
  else
    echo "PORT=30${n}1"; echo "WEB_PORT=30${n}0"
    echo "DATABASE_URL=postgres://localhost:5432/tekram_lane${n}"
    echo "REDIS_URL=redis://localhost:6379/${n}"
  fi
}

is_doc_issue() {  # $1 = issue → 0 if labeled type:doc (no live stack, no lane needed)
  gh issue view "$1" --repo "$REPO" --json labels -q '.labels[].name' | grep -q '^type:doc$'
}

# ── worktree helpers (per-agent persistent worktree, branch-switched per issue) ────────────────
ensure_worktree_on_branch() {
  # ensure_worktree_on_branch <wt> <local_branch> <remote_source_branch> <base_ref> [force]
  #   First call for this agent (wt doesn't exist yet): creates the persistent worktree, tracking
  #   origin/<remote_source_branch> if it exists, else branching fresh off <base_ref>.
  #   Subsequent calls: refuses to switch branches if $wt has uncommitted changes (the agent must
  #   `submit` or explicitly stash first — never auto-stashed, to avoid silently hiding work);
  #   otherwise checks out <local_branch>, creating it (tracking origin/<remote_source_branch>, or
  #   <base_ref> if that doesn't exist either) if not already present locally.
  #   force=1: always `checkout -B` to reset <local_branch> to match origin/<remote_source_branch>.
  #   Only for QA's read-only qa-issue-<n> aliases, which must pick up an engineer's force-pushed
  #   fixes and never carry commits of their own — never use force=1 for an engineer branch.
  local wt="$1" local_branch="$2" remote_source="$3" base_ref="$4" force="${5:-}"
  if [ ! -d "$wt" ]; then
    if git show-ref --verify --quiet "refs/remotes/origin/$remote_source"; then
      git worktree add -B "$local_branch" "$wt" "origin/$remote_source"
    else
      git worktree add -B "$local_branch" "$wt" "$base_ref"
    fi
    return 0
  fi
  local dirty
  dirty="$(git -C "$wt" status --porcelain)"
  if [ -n "$dirty" ]; then
    echo "Error: $AGENT_ID's worktree ($wt) has uncommitted changes on branch $(git -C "$wt" branch --show-current) — submit or explicitly stash before switching to $local_branch:" >&2
    echo "$dirty" >&2
    return 1
  fi
  git -C "$wt" fetch origin --quiet
  if [ "$force" = "1" ] && git -C "$wt" show-ref --verify --quiet "refs/remotes/origin/$remote_source"; then
    git -C "$wt" checkout -B "$local_branch" "origin/$remote_source"
  elif git -C "$wt" show-ref --verify --quiet "refs/heads/$local_branch"; then
    git -C "$wt" checkout "$local_branch"
  elif git -C "$wt" show-ref --verify --quiet "refs/remotes/origin/$remote_source"; then
    git -C "$wt" checkout -b "$local_branch" "origin/$remote_source"
  else
    git -C "$wt" checkout -b "$local_branch" "$base_ref"
  fi
}

case "$COMMAND" in
  fetch)
    LABEL="status:3-ready-for-dev"
    [ "${1:-}" = "--label" ] && LABEL="$2"
    echo "Fetching issues labeled '$LABEL' (excluding claimed)..." >&2
    gh issue list --repo "$REPO" --label "$LABEL" \
      --search '-label:"agent:claimed"' --json number,title,labels --limit 5
    ;;

  claim)  # claim <issue> — check-then-act + read-back tiebreak (GitHub has no label CAS)
    n="${1:?Usage: claim <issue>}"
    if gh issue view "$n" --repo "$REPO" --json labels -q '.labels[].name' | grep -q '^agent:claimed:'; then
      echo "Issue #$n already claimed — pick another."; exit 1
    fi
    gh issue edit "$n" --repo "$REPO" \
      --add-label "agent:claimed:$AGENT_ID" --add-label "status:4-in-progress" \
      --remove-label "status:3-ready-for-dev"
    gh issue comment "$n" --repo "$REPO" --body "Claimed by $AGENT_ID at $(date -u +%FT%TZ)."
    # read-back tiebreak: if a second claimant snuck in, alphabetically-later id backs off
    others=$(gh issue view "$n" --repo "$REPO" --json labels -q '.labels[].name' \
             | grep '^agent:claimed:' | sed 's/^agent:claimed://' | sort)
    winner=$(echo "$others" | head -1)
    if [ -n "$others" ] && [ "$winner" != "$AGENT_ID" ]; then
      echo "Lost claim race for #$n to '$winner' — backing off."
      gh issue edit "$n" --repo "$REPO" --remove-label "agent:claimed:$AGENT_ID"
      exit 1
    fi
    echo "Claimed #$n as $AGENT_ID."
    ;;

  start)  # start <issue> [lane] — checkout branch issue-<n> in THIS AGENT's persistent worktree.
          # type:doc issues skip lanes + .lane-env + install entirely: they never run the live
          # stack, so they must not consume a lane that an engineer or QA needs.
    n="${1:?Usage: start <issue> [lane]}"
    branch="issue-$n"
    wt="$WORKTREE_ROOT/$AGENT_ID"
    if is_doc_issue "$n"; then
      git -C "$(git rev-parse --show-toplevel)" fetch origin --quiet
      gh issue develop "$n" --repo "$REPO" --name "$branch" --base main >/dev/null 2>&1 || true
      ensure_worktree_on_branch "$wt" "$branch" "$branch" "origin/main" || exit 1
      echo "Ready (doc issue — no lane): branch '$branch' checked out in $AGENT_ID's worktree '$wt'. cd \"$wt\" to begin."
      exit 0
    fi
    lane="${2:-$(acquire_lane "$n")}"
    gh issue edit "$n" --repo "$REPO" --add-label "lane:$lane" >/dev/null 2>&1 || true
    git -C "$(git rev-parse --show-toplevel)" fetch origin --quiet
    # create the linked branch (best effort) before resolving it below
    gh issue develop "$n" --repo "$REPO" --name "$branch" --base main >/dev/null 2>&1 || true
    ensure_worktree_on_branch "$wt" "$branch" "$branch" "origin/main" || exit 1
    lane_env "$lane" > "$wt/.lane-env"
    echo "Lane $lane env written to $wt/.lane-env — source it before running the stack."
    ( cd "$wt" && corepack pnpm install --frozen-lockfile >/dev/null 2>&1 ) && echo "pnpm install done."
    echo "Ready: branch '$branch' checked out in $AGENT_ID's worktree '$wt', lane $lane. cd \"$wt\" to begin."
    ;;

  qa-checkout)  # qa-checkout <issue> [lane] — checkout a READ-ONLY qa-issue-<n> alias of the
                # engineer's origin/issue-<n> in QA's persistent worktree (never issue-<n> itself:
                # that name may be checked out in the engineer's OWN worktree at the same time, and
                # git refuses to check out one branch in two worktrees at once)
    n="${1:?Usage: qa-checkout <issue> [lane]}"
    branch="issue-$n"
    qa_branch="qa-issue-$n"
    wt="$WORKTREE_ROOT/$AGENT_ID"
    lane="${2:-$(acquire_lane "$n")}"
    git -C "$(git rev-parse --show-toplevel)" fetch origin --quiet
    git show-ref --verify --quiet "refs/remotes/origin/$branch" || {
      echo "Error: origin/$branch doesn't exist yet — the engineer hasn't pushed a PR for #$n. Nothing to review."; exit 1; }
    # force=1: always reset qa-issue-<n> to match origin/issue-<n> — QA re-reviews after an
    # engineer's force-push fix must see the latest commits, and QA never commits to this branch.
    ensure_worktree_on_branch "$wt" "$qa_branch" "$branch" "origin/main" 1 || exit 1
    lane_env "$lane" > "$wt/.lane-env"
    echo "Lane $lane env written to $wt/.lane-env — source it before running the stack."
    ( cd "$wt" && corepack pnpm install --frozen-lockfile >/dev/null 2>&1 ) && echo "pnpm install done."
    echo "Ready: origin/$branch checked out as '$qa_branch' in QA's worktree '$wt', lane $lane. cd \"$wt\" to begin."
    ;;

  submit)  # submit <issue> "<msg>" <file1> [file2 ...] — EXPLICIT files only, never git add .
    n="${1:?Usage: submit <issue> \"<msg>\" <file...>}"; shift
    msg="${1:?commit message required}"; shift
    [ "$#" -ge 1 ] || { echo "Error: at least one explicit file path required (no 'git add .')"; exit 1; }
    git add -- "$@"
    git diff --cached --quiet && { echo "Error: nothing staged from the given files."; exit 1; }
    git commit -m "$msg

Refs #$n
Co-Authored-By: Claude <noreply@anthropic.com>"
    git push -u origin HEAD
    gh pr create --repo "$REPO" --base main --head "issue-$n" \
      --title "Issue #$n: $msg" --body "Implements #$n (Refs #$n). $msg" 2>/dev/null \
      || echo "PR may already exist for issue-$n."
    gh issue edit "$n" --repo "$REPO" --add-label "status:5-in-review" --remove-label "status:4-in-progress"
    claim_label=$(gh issue view "$n" --repo "$REPO" --json labels -q '.labels[].name' | grep '^agent:claimed:' || true)
    [ -n "$claim_label" ] && gh issue edit "$n" --repo "$REPO" --remove-label "$claim_label"
    # Release the lane here, not just at cleanup/accept: the engineer is done actively using the
    # live stack the moment it submits (live verification already happened pre-submit), and QA is
    # the next stage that needs it (qa-checkout acquires its own lane) — holding it all the way to
    # Architect-accept would deadlock qa-checkout for no reason.
    release_lane "$n"
    echo "Submitted #$n → status:5-in-review."
    ;;

  qa-comment)
    pr="${1:?Usage: qa-comment <pr> \"<message>\"}"; msg="${2:?message required}"
    gh pr comment "$pr" --repo "$REPO" --body "$msg"
    echo "Posted QA comment to PR #$pr."
    ;;

  cleanup)  # cleanup <issue> — release lane + claim (run on accept/merge). Worktrees are now
            # persistent per-agent (see `start`/`qa-checkout`) — this no longer removes anything;
            # use `wipe` for that.
    n="${1:?Usage: cleanup <issue>}"
    release_lane "$n"  # idempotent — submit already releases it; harmless if already free
    claim_label=$(gh issue view "$n" --repo "$REPO" --json labels -q '.labels[].name' | grep '^agent:claimed:' || true)
    [ -n "$claim_label" ] && gh issue edit "$n" --repo "$REPO" --remove-label "$claim_label"
    echo "Cleaned up #$n (lane + claim released; worktree untouched — see 'wipe' for teardown)."
    ;;

  wipe)  # wipe — force-remove THIS agent's persistent worktree (manual recovery/reset only; not
         # part of the normal per-issue flow, since worktrees are meant to be reused across issues)
    wt="$WORKTREE_ROOT/$AGENT_ID"
    if [ -d "$wt" ]; then
      git worktree remove "$wt" --force
      echo "Removed $AGENT_ID's worktree $wt."
    else
      echo "$AGENT_ID has no worktree at $wt — nothing to wipe."
    fi
    git worktree prune
    ;;

  lanes)
    echo "MAX_LANES=$MAX_LANES. Held lanes:"
    shopt -s nullglob
    held=0
    for f in "$LANE_DIR"/lane-*.lock; do echo "  $(basename "$f"): $(cat "$f")"; held=1; done
    [ "$held" = 0 ] && echo "  (none — all lanes free)"
    ;;

  *)
    cat >&2 <<'EOF'
Careeree GitHub Flow skill — commands:
  fetch  [--label <status>]                list ready, unclaimed issues (default status:3-ready-for-dev)
  claim  <issue>                           claim with read-back tiebreak
  start  <issue> [lane]                    checkout branch issue-<n> in THIS agent's persistent
                                            worktree ($WORKTREE_ROOT/$GH_AGENT_ID) + lane + .lane-env
                                            + install. Refuses to switch if the worktree is dirty.
  qa-checkout <issue> [lane]               QA-only: checkout origin/issue-<n> as local qa-issue-<n>
                                            in QA's persistent worktree (force-reset each time)
  submit <issue> "<msg>" <file...>         stage EXPLICIT files, commit, push, open PR,
                                            → status:5-in-review, release lane
  qa-comment <pr> "<message>"              post QA report to a PR
  cleanup <issue>                          release lane + claim (on accept). Worktree persists.
  wipe                                     force-remove THIS agent's persistent worktree (manual
                                            recovery only — not part of the normal per-issue flow)
  lanes                                    show lane occupancy

GH_AGENT_ID must be set to a stable per-role id (web-engineer, backend-engineer, worker-engineer,
qa) before calling start/qa-checkout/wipe — worktrees are now keyed by agent role, not by issue.
EOF
    exit 1
    ;;
esac
