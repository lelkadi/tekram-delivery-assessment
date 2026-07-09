# Infra Rules (all agents, all stages)

1. **One shared stack — never ad-hoc containers.** Postgres + Redis run as ONE `docker compose`
   stack started from the repo root (`docker compose up -d`). Never `docker run` a postgres/redis
   container directly: an ad-hoc container squats the shared port (5432/6379) and breaks
   `docker compose up -d` for every other agent. (If `docker compose` itself errors with
   "unknown shorthand flag", the CLI plugin isn't wired — see BOOTSTRAP.md, founder-run.)
2. **Port conflict = stop and surface.** If `docker compose up -d` fails with "port is already
   allocated", something outside compose owns the port. Report the container (name, image,
   whether it holds data) and stop — do NOT delete it, and do NOT work around it by starting
   your own container on another port.
3. **Lane isolation is logical, not physical.** Parallel work is isolated by lane database
   (`tekram_laneN`) and Redis db number (`/N`) from your `.lane-env` — never by extra service
   containers. Preflight with `skills/lane-stack-check.sh` before testing.
