#!/usr/bin/env bash
# gh-env.sh — export the repo-scoped GH_TOKEN for direct `gh` calls (issue create, label edit…).
# Source it:  source .ai-roster/skills/gh-env.sh
# The token itself lives only in .git/credentials-tekram (untracked, chmod 600); this script
# derives it at runtime and never writes it anywhere. github_flow.sh does the same on its own,
# so sourcing this is only needed for gh commands made OUTSIDE github_flow.sh.
if [ -z "${GH_TOKEN:-}" ]; then
  # host git is 2.23 — no --path-format=absolute; resolve relative --git-common-dir by hand
  _common_git_dir="$(git rev-parse --git-common-dir 2>/dev/null || true)"
  case "$_common_git_dir" in
    "") ;;
    /*) ;;
    *) _common_git_dir="$(git rev-parse --show-toplevel 2>/dev/null)/$_common_git_dir" ;;
  esac
  if [ -n "$_common_git_dir" ] && [ -f "$_common_git_dir/credentials-tekram" ]; then
    GH_TOKEN="$(sed -E 's#https://[^:]+:([^@]+)@.*#\1#' "$_common_git_dir/credentials-tekram")"
    export GH_TOKEN
  else
    echo "gh-env: no credential file at \$GIT_DIR/credentials-tekram — export GH_TOKEN manually" >&2
  fi
fi
