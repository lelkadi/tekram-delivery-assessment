-- Per-lane databases for parallel agent execution (TD-002 / github_flow.sh lane_env).
-- Runs once on first container init; the main "tekram" db is created by POSTGRES_DB.
CREATE DATABASE tekram_lane1;
CREATE DATABASE tekram_lane2;
CREATE DATABASE tekram_lane3;
