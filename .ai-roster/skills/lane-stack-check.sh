#!/usr/bin/env bash
# lane-stack-check.sh — preflight before testing: is THIS worktree's lane actually usable?
# Run from a worktree containing .lane-env (written by github_flow.sh start / qa-checkout).
# Catches the classic parallel-lane failures early: compose stack down, lane database missing,
# stale .lane-env pointing at a lane someone else now owns.
set -euo pipefail
[ -f .lane-env ] || { echo "FAIL: no .lane-env here — run github_flow.sh start/qa-checkout first."; exit 1; }
set -a; . ./.lane-env; set +a
echo "lane env: PORT=$PORT DATABASE_URL=${DATABASE_URL##*@} REDIS_URL=$REDIS_URL"
fail=0

warn_adhoc() {  # $1 = container name, $2 = service — compose names are <project>-<service>-<n>
  case "$1" in
    *-"$2"-*) ;;
    *) echo "WARN: $2 container '$1' is not compose-managed (ad-hoc docker run? see rules/infra.md)";;
  esac
}

pg_ctr="$(docker ps --format '{{.Names}}' 2>/dev/null | grep -m1 postgres || true)"
if [ -n "$pg_ctr" ]; then
  warn_adhoc "$pg_ctr" postgres
  db="${DATABASE_URL##*/}"
  docker exec "$pg_ctr" pg_isready -U postgres >/dev/null 2>&1 \
    && echo "postgres: up ($pg_ctr)" || { echo "FAIL: postgres container not ready"; fail=1; }
  docker exec "$pg_ctr" psql -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$db'" 2>/dev/null | grep -q 1 \
    && echo "database '$db': exists" || { echo "FAIL: database '$db' missing (re-run compose init or create it)"; fail=1; }
else
  echo "FAIL: no postgres container running — docker compose up -d (from the main repo root)"; fail=1
fi

redis_ctr="$(docker ps --format '{{.Names}}' 2>/dev/null | grep -m1 redis || true)"
if [ -n "$redis_ctr" ]; then
  warn_adhoc "$redis_ctr" redis
  docker exec "$redis_ctr" redis-cli ping 2>/dev/null | grep -q PONG \
    && echo "redis: up ($redis_ctr)" || { echo "FAIL: redis not answering PING"; fail=1; }
else
  echo "FAIL: no redis container running — docker compose up -d"; fail=1
fi

# Stale-lane detection: does the lane lock for OUR issue still exist and match?
LANE_DIR="${LANE_DIR:-$HOME/.agent-worktrees/.lanes}"
lane_from_port="${PORT#30}"; lane_from_port="${lane_from_port%1}"   # 30N1 -> N (3001 -> 0 = serialized)
if [ -n "$lane_from_port" ] && [ "$lane_from_port" != "0" ] && [ -f "$LANE_DIR/lane-$lane_from_port.lock" ]; then
  echo "lane $lane_from_port lock: $(cat "$LANE_DIR/lane-$lane_from_port.lock")"
  echo "  ^ if that issue number isn't yours, your .lane-env is STALE — re-run start/qa-checkout."
fi

if nc -z localhost "$PORT" 2>/dev/null; then
  echo "port $PORT: something is listening (API already running on this lane)"
else
  echo "port $PORT: free — start the API before curling it"
fi
[ "$fail" = 0 ] && echo "PASS: lane is usable" || echo "FAILED checks above — fix before testing"
exit "$fail"
